using System;
using System.Buffers;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using RainyReignGames.RevealMask;
using RealisticBleeding.Components;
using ThunderRoad;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDecalSystem : AEntitySetSystem<float>
	{
		private const float ProjectionDepth = 0.06f;

		private static readonly Vector3Int BoundsDimensions = new Vector3Int(4, 4, 4);

		private static readonly int BloodDropsID = Shader.PropertyToID("_BloodDrops");
		private static readonly int CellsID = Shader.PropertyToID("_Cells");
		private static readonly int BoundsMatrixID = Shader.PropertyToID("_BoundsMatrix");
		private static readonly int BoundsDimensionsID = Shader.PropertyToID("_BoundsDimensions");
		private static readonly int MultiplierID = Shader.PropertyToID("_Multiplier");

		private readonly Dictionary<RevealMaterialController, BloodDropGrid> _bloodDrops =
			new Dictionary<RevealMaterialController, BloodDropGrid>();

		private readonly ComputeBuffer _bloodDropsBuffer;
		private readonly ComputeBuffer _cellsBuffer;
		private readonly CommandBuffer _commandBuffer;
		
		private readonly ProfilerMarker _updateProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, $"RealisticBleeding.{nameof(SurfaceBloodDecalSystem)}.Update");

		private readonly ProfilerMarker _renderingProfilerMarker =
			new ProfilerMarker(ProfilerCategory.Scripts, $"RealisticBleeding.{nameof(SurfaceBloodDecalSystem)}.OnBeginFrameRendering");

		private Material _decalMaterial;

		public SurfaceBloodDecalSystem(EntitySet set) : base(set)
		{
			RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;

			_bloodDropsBuffer = new ComputeBuffer(512, BloodDropGPU.SizeOf);

			var totalCellsCount = BoundsDimensions.x * BoundsDimensions.y * BoundsDimensions.z;
			_cellsBuffer = new ComputeBuffer(totalCellsCount, CellGPU.SizeOf);

			_commandBuffer = new CommandBuffer { name = "Realistic Blood - Decal Drawing" };

			Catalog.LoadAssetAsync("RealisticBloodDecal", (Shader shader) => { _decalMaterial = new Material(shader); }, null);
		}

		public override void Dispose()
		{
			_bloodDropsBuffer.Dispose();
			_cellsBuffer.Dispose();
			_commandBuffer.Dispose();

			RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			using (_updateProfilerMarker.Auto())
			{
				// Prepare draw call data for later
				foreach (var entity in entities)
				{
					ref var surfaceCollider = ref entity.Get<SurfaceCollider>();
					ref var bloodDrop = ref entity.Get<BloodDrop>();

					var col = surfaceCollider.Collider;

					var rb = col.attachedRigidbody;
					if (!rb) continue;

					if (!rb.TryGetComponent(out RagdollPart ragdollPart)) continue;

					var worldPos = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);
					var normal = surfaceCollider.LastNormal;

					var offset = normal * ProjectionDepth;

					var startPos = worldPos + offset;
					var endPos = worldPos - offset;

					var radius = Mathf.Clamp(bloodDrop.Size * 0.5f, 0.003f, 0.02f);

					var maxRadius = radius * 2;
					var maxSqrRadius = maxRadius * maxRadius;

					var bloodDropGPU = new BloodDropGPU(startPos, endPos, radius);

					foreach (var rendererData in ragdollPart.renderers)
					{
						if (!rendererData.revealDecal) continue;

						var revealMaterialController = rendererData.revealDecal.revealMaterialController;

						if (revealMaterialController)
						{
							var renderer = rendererData.renderer;

							if (!renderer.isVisible) continue;

							var bounds = renderer.bounds;
							var sqrDistance = bounds.SqrDistance(worldPos);

							if (sqrDistance > maxSqrRadius) continue;

							if (!_bloodDrops.TryGetValue(revealMaterialController, out var bloodDrops))
							{
								bloodDrops = BloodDropGrid.Get(bounds);
								_bloodDrops[revealMaterialController] = bloodDrops;
							}

							bloodDrops.Add(bloodDropGPU);
						}
					}
				}
			}
		}

		private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
		{
			using (_renderingProfilerMarker.Auto())
			{
				try
				{
					var projectionMatrix = Matrix4x4.Ortho(0, 1, 0, 1, -1, 100);
					var multiplier = 1f;

					foreach (var keyValuePair in _bloodDrops)
					{
						_commandBuffer.Clear();

						var revealMaterialController = keyValuePair.Key;
						var bloodDropsGrid = keyValuePair.Value;

						bloodDropsGrid.StoreIntoBuffers(_commandBuffer, _bloodDropsBuffer, _cellsBuffer);

						var shouldClear = revealMaterialController.ActivateRevealMaterials();

						_commandBuffer.SetProjectionMatrix(projectionMatrix);
						_commandBuffer.SetRenderTarget(revealMaterialController.MaskTexture);
						_commandBuffer.SetGlobalBuffer(BloodDropsID, _bloodDropsBuffer);
						_commandBuffer.SetGlobalBuffer(CellsID, _cellsBuffer);
						_commandBuffer.SetGlobalMatrix(BoundsMatrixID, bloodDropsGrid.Matrix);
						_commandBuffer.SetGlobalVector(BoundsDimensionsID, (Vector3)BoundsDimensions);
						_commandBuffer.SetGlobalFloat(MultiplierID, multiplier);

						if (shouldClear)
						{
							_commandBuffer.ClearRenderTarget(false, true, Color.clear);
						}

						var renderer = revealMaterialController.GetRenderer();
						var submeshCount = revealMaterialController.GetSubmeshCount();

						for (var submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
						{
							_commandBuffer.DrawRenderer(renderer, _decalMaterial, submeshIndex);
						}

						context.ExecuteCommandBuffer(_commandBuffer);
					}

					// Clean up
					foreach (var bloodDropGrid in _bloodDrops.Values)
					{
						bloodDropGrid.Dispose();
					}

					_bloodDrops.Clear();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private struct BloodDropGPU
		{
			public const int SizeOf = sizeof(float) * 7;

			public Vector3 StartPos;
			public Vector3 EndPos;
			public float InverseRadius;

			public BloodDropGPU(Vector3 startPos, Vector3 endPos, float radius)
			{
				StartPos = startPos;
				EndPos = endPos;
				InverseRadius = 1 / radius;
			}
		}

		private struct CellGPU
		{
			public const int SizeOf = sizeof(int) * 2;

			public int StartIndex;
			public int Count;

			public CellGPU(int startIndex, int count)
			{
				StartIndex = startIndex;
				Count = count;
			}
		}

		private class BloodDropGrid : IDisposable
		{
			private static readonly ObjectPool<BloodDropGrid> Pool = new ObjectPool<BloodDropGrid>(grid => grid.Initialize(BoundsDimensions), null);

			private Vector3Int _dim;
			private List<BloodDropGPU>[,,] _grid;
			private Bounds[,,] _cellBounds;

			public Matrix4x4 Matrix { get; private set; }

			private static readonly CellGPU[] CellArray = new CellGPU[BoundsDimensions.x * BoundsDimensions.y * BoundsDimensions.z];

			public static BloodDropGrid Get(Bounds worldBounds)
			{
				var grid = Pool.Get();
				grid.SetWorldBounds(worldBounds);

				return grid;
			}

			private void Initialize(Vector3Int dimensions)
			{
				_dim = dimensions;
				_grid = new List<BloodDropGPU>[_dim.x, _dim.y, _dim.z];
				_cellBounds = new Bounds[_dim.x, _dim.y, _dim.z];
			}

			private void SetWorldBounds(Bounds bounds)
			{
				var size = bounds.size;
				var min = bounds.min;

				Matrix = Matrix4x4.TRS(min, Quaternion.identity, size).inverse;

				var cellSize = new Vector3(size.x / _dim.x, size.y / _dim.y, size.z / _dim.z);

				var cellBounds = new Bounds();
				cellBounds.SetMinMax(min, min + cellSize);

				for (var z = 0; z < _dim.z; z++)
				{
					for (var y = 0; y < _dim.y; y++)
					{
						for (var x = 0; x < _dim.x; x++)
						{
							_cellBounds[x, y, z] = new Bounds(cellBounds.center + Vector3.Scale(cellSize, new Vector3(x, y, z)), cellSize);
						}
					}
				}
			}

			public void Add(in BloodDropGPU bloodDrop)
			{
				var sqrRadius = 1f / bloodDrop.InverseRadius;
				sqrRadius *= sqrRadius;

				for (var z = 0; z < _dim.z; z++)
				{
					for (var y = 0; y < _dim.y; y++)
					{
						for (var x = 0; x < _dim.x; x++)
						{
							ref var bounds = ref _cellBounds[x, y, z];

							var minSqrDistance = Mathf.Min(bounds.SqrDistance(bloodDrop.StartPos), bounds.SqrDistance(bloodDrop.EndPos));

							if (minSqrDistance < sqrRadius)
							{
								AddToCell(in bloodDrop, new Vector3Int(x, y, z));
							}
						}
					}
				}
			}

			private void AddToCell(in BloodDropGPU bloodDropGPU, Vector3Int cell)
			{
				var list = _grid[cell.x, cell.y, cell.z];

				if (list == null)
				{
					list = ListPool<BloodDropGPU>.Get();
					_grid[cell.x, cell.y, cell.z] = list;
				}

				list.Add(bloodDropGPU);
			}

			private void Clear()
			{
				for (var x = 0; x < _dim.x; x++)
				{
					for (var y = 0; y < _dim.y; y++)
					{
						for (var z = 0; z < _dim.z; z++)
						{
							_grid[x, y, z]?.Clear();
						}
					}
				}
			}

			public void StoreIntoBuffers(CommandBuffer commandBuffer, ComputeBuffer bloodDropsBuffer, ComputeBuffer cellsBuffer)
			{
				var dropsIndex = 0;
				
				for (var z = 0; z < _dim.z; z++)
				{
					for (var y = 0; y < _dim.y; y++)
					{
						for (var x = 0; x < _dim.x; x++)
						{
							var flatIndex = GetFlattenedIndex(x, y, z);
							var drops = _grid[x, y, z];

							var cellGPU = new CellGPU(dropsIndex, 0);

							if (drops != null && drops.Count > 0)
							{
								commandBuffer.SetComputeBufferData(bloodDropsBuffer, drops, 0, dropsIndex, drops.Count);

								dropsIndex += drops.Count;
								cellGPU.Count = drops.Count;
							}

							CellArray[flatIndex] = cellGPU;
						}
					}
				}
				
				commandBuffer.SetComputeBufferData(cellsBuffer, CellArray);
			}

			private int GetFlattenedIndex(int x, int y, int z)
			{
				return z * _dim.x * _dim.y + y * _dim.x + x;
			}

			public void Dispose()
			{
				Clear();

				Pool.Release(this);
			}
		}
	}
}
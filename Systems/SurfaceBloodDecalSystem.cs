using System;
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
		private const float ProjectionDepth = 0.05f;

		private const float CellSize = 0.12f;
		private const int MaxBoundsDimension = 20;
		private const int MaxCellCount = MaxBoundsDimension * MaxBoundsDimension * MaxBoundsDimension;

		private static readonly int BloodDropsID = Shader.PropertyToID("_BloodDrops");
		private static readonly int CellsID = Shader.PropertyToID("_Cells");
		private static readonly int BoundsMatrixID = Shader.PropertyToID("_BoundsMatrix");
		private static readonly int BoundsDimensionsID = Shader.PropertyToID("_BoundsDimensions");
		private static readonly int BoundsVolumeID = Shader.PropertyToID("_BoundsVolume");

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
			_cellsBuffer = new ComputeBuffer(MaxCellCount, CellGPU.SizeOf);

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
							if (!bounds.Contains(worldPos)) continue;

							if (!_bloodDrops.TryGetValue(revealMaterialController, out var bloodDrops))
							{
								bloodDrops = BloodDropGrid.Get();
								_bloodDrops[revealMaterialController] = bloodDrops;
							}
							
							bloodDrops.SetWorldBounds(bounds);

							bloodDrops.Add(in bloodDropGPU, worldPos);
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

						Vector4 boundsDimensions = (Vector3) bloodDropsGrid.Dimensions;
						_commandBuffer.SetGlobalVector(BoundsDimensionsID, boundsDimensions);
						_commandBuffer.SetGlobalInt(BoundsVolumeID, bloodDropsGrid.Dimensions.GetVolume());
						
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
			public float InverseSqrRadius;

			public BloodDropGPU(Vector3 startPos, Vector3 endPos, float radius)
			{
				StartPos = startPos;
				EndPos = endPos;
				InverseSqrRadius = 1 / (radius * radius);
			}
		}

		private struct CellGPU
		{
			public const int SizeOf = sizeof(int) * 2;

			public uint StartIndex;
			public uint Count;

			public CellGPU(int startIndex, int count)
			{
				StartIndex = (uint) startIndex;
				Count = (uint) count;
			}
		}

		private class BloodDropGrid : IDisposable
		{
			private static readonly ObjectPool<BloodDropGrid> Pool = new ObjectPool<BloodDropGrid>(null, null);

			private const string ProfilerMarkerPrefix = "RealisticBleeding." + nameof(SurfaceBloodDecalSystem) + "." + nameof(BloodDropGrid) + ".";

			private static readonly ProfilerMarker AddProfilerMarker =
				new ProfilerMarker(ProfilerCategory.Scripts, ProfilerMarkerPrefix + nameof(Add));

			private static readonly ProfilerMarker AddToCellProfilerMarker =
				new ProfilerMarker(ProfilerCategory.Scripts, ProfilerMarkerPrefix + nameof(AddToCell));
			
			private static readonly ProfilerMarker SetWorldBoundsProfilerMarker =
				new ProfilerMarker(ProfilerCategory.Scripts, ProfilerMarkerPrefix + nameof(SetWorldBounds));

			private static readonly CellGPU[] CellArray = new CellGPU[MaxCellCount];
				
			private readonly List<BloodDropGPU>[,,] _grid;

			public Matrix4x4 Matrix { get; private set; }
			public Vector3Int Dimensions { get; private set; }

			public BloodDropGrid()
			{
				_grid = new List<BloodDropGPU>[MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension];
			}

			static BloodDropGrid()
			{
				// Prewarm
				const int prewarmCount = 24;
				var list = new List<BloodDropGrid>(prewarmCount);
				
				for (var i = 0; i < prewarmCount; i++)
				{
					list.Add(Pool.Get());
				}

				foreach (var bloodDropGrid in list)
				{
					Pool.Release(bloodDropGrid);
				}
				
				list.Clear();
			}
			
			public static BloodDropGrid Get()
			{
				var grid = Pool.Get();

				return grid;
			}

			public void SetWorldBounds(Bounds bounds)
			{
				using (SetWorldBoundsProfilerMarker.Auto())
				{
					var size = bounds.size;
					var min = bounds.min;

					var dim = Vector3Int.CeilToInt(new Vector3(size.x / CellSize, size.y / CellSize, size.z / CellSize));

					dim = Vector3Int.Min(dim, new Vector3Int(MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension));

					Dimensions = dim;
					Matrix = Matrix4x4.TRS(min, Quaternion.identity, size).inverse;
				}
			}

			public void Add(in BloodDropGPU bloodDrop, Vector3 worldPos)
			{
				using (AddProfilerMarker.Auto())
				{
					var boundsStartPos = Matrix.MultiplyPoint3x4(worldPos).ScaledBy(Dimensions);

					var startPosCoords = Vector3Int.FloorToInt(boundsStartPos);

					AddToCell(in bloodDrop, startPosCoords);
				}
			}

			private void AddToCell(in BloodDropGPU bloodDropGPU, Vector3Int cell)
			{
				using (AddToCellProfilerMarker.Auto())
				{
					var list = _grid[cell.x, cell.y, cell.z];

					if (list == null)
					{
						list = ListPool<BloodDropGPU>.Get();
						_grid[cell.x, cell.y, cell.z] = list;
					}

					list.Add(bloodDropGPU);
				}
			}

			private void Clear()
			{
				for (var x = 0; x < Dimensions.x; x++)
				{
					for (var y = 0; y < Dimensions.y; y++)
					{
						for (var z = 0; z < Dimensions.z; z++)
						{
							_grid[x, y, z]?.Clear();
						}
					}
				}
			}

			public void StoreIntoBuffers(CommandBuffer commandBuffer, ComputeBuffer bloodDropsBuffer, ComputeBuffer cellsBuffer)
			{
				var dropsIndex = 0;

				for (var z = 0; z < Dimensions.z; z++)
				{
					for (var y = 0; y < Dimensions.y; y++)
					{
						for (var x = 0; x < Dimensions.x; x++)
						{
							var flatIndex = GetFlattenedIndex(x, y, z);
							var drops = _grid[x, y, z];

							var cellGPU = new CellGPU(dropsIndex, 0);

							if (drops != null && drops.Count > 0)
							{
								commandBuffer.SetComputeBufferData(bloodDropsBuffer, drops, 0, dropsIndex, drops.Count);

								dropsIndex += drops.Count;
								cellGPU.Count = (uint) drops.Count;
							}

							CellArray[flatIndex] = cellGPU;
						}
					}
				}
				
				var cellCount = Dimensions.GetVolume();
				commandBuffer.SetComputeBufferData(cellsBuffer, CellArray, 0, 0, cellCount);
			}

			private int GetFlattenedIndex(int x, int y, int z)
			{
				return z * Dimensions.x * Dimensions.y + y * Dimensions.x + x;
			}

			public void Dispose()
			{
				Clear();

				Pool.Release(this);
			}
		}
	}
}
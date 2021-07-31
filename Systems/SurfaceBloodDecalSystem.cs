using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using RainyReignGames.RevealMask;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDecalSystem : AEntitySetSystem<float>
	{
		private const float ProjectionDepth = 0.06f;
		
		private static readonly int BloodDropsID = Shader.PropertyToID("_BloodDrops");
		private static readonly int BloodDropCountID = Shader.PropertyToID("_BloodDropCount");
		private static readonly int MultiplierID = Shader.PropertyToID("_Multiplier");

		private readonly Dictionary<RevealMaterialController, List<BloodDropGPU>> _bloodDrops =
			new Dictionary<RevealMaterialController, List<BloodDropGPU>>();

		private readonly CommandBuffer _commandBuffer;
		private readonly ComputeBuffer _bloodDropsBuffer;

		private Material _decalMaterial;

		public SurfaceBloodDecalSystem(EntitySet set) : base(set)
		{
			RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
			
			_bloodDropsBuffer = new ComputeBuffer(64, BloodDropGPU.SizeOf);
			_commandBuffer = new CommandBuffer {name = "Realistic Blood - Decal Drawing"};
			
			Catalog.LoadAssetAsync("RealisticBloodDecal", (Shader shader) =>
			{
				_decalMaterial = new Material(shader);
			}, null);
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
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

						var sqrDistance = (worldPos - renderer.bounds.ClosestPoint(worldPos)).sqrMagnitude;

						if (sqrDistance > maxSqrRadius) continue;

						if (!_bloodDrops.TryGetValue(revealMaterialController, out var bloodDrops))
						{
							bloodDrops = ListPool<BloodDropGPU>.Get();
							_bloodDrops[revealMaterialController] = bloodDrops;
						}
						
						bloodDrops.Add(bloodDropGPU);
					}
				}
			}
		}

		private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
		{
			try
			{
				var projectionMatrix = Matrix4x4.Ortho(0, 1, 0, 1, -1, 100);
				var multiplier = 1f;

				foreach (var keyValuePair in _bloodDrops)
				{
					_commandBuffer.Clear();
					
					var revealMaterialController = keyValuePair.Key;
					var bloodDrops = keyValuePair.Value;

					var bloodDropCount = Mathf.Min(bloodDrops.Count, _bloodDropsBuffer.count);
					
					_bloodDropsBuffer.SetData(bloodDrops, 0, 0, bloodDropCount);

					var shouldClear = revealMaterialController.ActivateRevealMaterials();
					
					_commandBuffer.SetProjectionMatrix(projectionMatrix);
					_commandBuffer.SetRenderTarget(revealMaterialController.MaskTexture);
					_commandBuffer.SetGlobalBuffer(BloodDropsID, _bloodDropsBuffer);
					_commandBuffer.SetGlobalInt(BloodDropCountID, bloodDropCount);
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
				foreach (var bloodDropList in _bloodDrops.Values)
				{
					ListPool<BloodDropGPU>.Release(bloodDropList);
				}
				
				_bloodDrops.Clear();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
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
	}
}
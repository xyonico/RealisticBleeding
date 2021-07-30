using System.Collections.Generic;
using System.Globalization;
using DefaultEcs;
using RainyReignGames.RevealMask;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDecalSystem : BaseSystem
	{
		private static Texture2D _particleTexture;

		private static Texture2D ParticleTexture
		{
			get
			{
				if (_particleTexture != null)
				{
					return _particleTexture;
				}

				foreach (var texture2D in Resources.FindObjectsOfTypeAll<Texture2D>())
				{
					if (texture2D.name == "Default-Particle")
					{
						_particleTexture = texture2D;
						break;
					}
				}

				return _particleTexture;
			}
		}

		private static readonly RevealData[] RevealData =
		{
			new RevealData
			{
				blendOp = BlendOp.Add
			}
		};

		private static readonly List<RevealMaterialController> RevealMaterialControllers = new List<RevealMaterialController>(16);

		public SurfaceBloodDecalSystem(EntitySet set) : base(set)
		{
		}

		protected override void Update(float state, in Entity entity)
		{
			ref var surfaceCollider = ref entity.Get<SurfaceCollider>();
			ref var bloodDrop = ref entity.Get<BloodDrop>();

			RevealMaterialControllers.Clear();
			var col = surfaceCollider.Collider;

			var rb = col.attachedRigidbody;
			if (!rb) return;

			if (!rb.TryGetComponent(out RagdollPart ragdollPart)) return;

			foreach (var rendererData in ragdollPart.renderers)
			{
				if (!rendererData.revealDecal) continue;

				var revealMaterialController = rendererData.revealDecal.revealMaterialController;

				if (revealMaterialController)
				{
					var renderer = rendererData.renderer;

					if (!renderer.isVisible) continue;
					var name = renderer.name;

					var culture = CultureInfo.InvariantCulture;
					if (culture.CompareInfo.IndexOf(name, "vfx", CompareOptions.IgnoreCase) >= 0) continue;
					if (culture.CompareInfo.IndexOf(name, "hair", CompareOptions.IgnoreCase) >= 0) continue;

					RevealMaterialControllers.Add(revealMaterialController);
				}
			}

			if (RevealMaterialControllers.Count == 0) return;

			var normal = surfaceCollider.LastNormal;
			var posOffset = normal * 0.07f;

			var worldPos = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);
			
			GameManager.local.StartCoroutine(RevealMaskProjection.ProjectAsync(worldPos + posOffset, -normal, Vector3.up, 0.12f,
				bloodDrop.Size, ParticleTexture, new Vector4(0.7f, 0, 0, 0), RevealMaterialControllers, RevealData, null));
		}
	}
}
using System.Collections.Generic;
using System.Globalization;
using RainyReignGames.RevealMask;
using ThunderRoad;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding
{
	public class BloodDropDecalDrawer : MonoBehaviour
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

		private static readonly RevealData[] _revealData =
		{
			new RevealData
			{
				blendOp = BlendOp.Add
			}
		};

		private static readonly List<RevealMaterialController> _revealMaterialControllers = new List<RevealMaterialController>(16);

		private BloodDrop _bloodDrop;

		public float SizeMultiplier { get; set; } = 1;

		private void Awake()
		{
			_bloodDrop = GetComponent<BloodDrop>();
		}

		private void Update()
		{
			if (!_bloodDrop.HasUpdated) return;
			
			if (_bloodDrop.SurfaceCollider != null)
			{
				var size = 0.01f * SizeMultiplier * EntryPoint.Configuration.BloodStreakWidthMultiplier;
				
				_revealMaterialControllers.Clear();
				var col = _bloodDrop.SurfaceCollider;

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
						
						_revealMaterialControllers.Add(revealMaterialController);
					}
				}
				
				var normal = _bloodDrop.LastSurfaceNormal;
				var posOffset = normal * 0.07f;

				StartCoroutine(RevealMaskProjection.ProjectAsync(transform.position + posOffset, -normal, Vector3.up, 0.12f,
					size, ParticleTexture, new Vector4(0.7f, 0, 0, 0),
					_revealMaterialControllers, _revealData, null));

				_bloodDrop.HasUpdated = false;
			}
		}
	}
}
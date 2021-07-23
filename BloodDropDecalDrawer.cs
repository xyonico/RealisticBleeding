using System.Collections.Generic;
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

		private static readonly List<Renderer> _renderers = new List<Renderer>(8);

		private BloodDrop _bloodDrop;

		public float SizeMultiplier { get; set; } = 1;

		private void Awake()
		{
			_bloodDrop = GetComponent<BloodDrop>();
		}

		private void Update()
		{
			if (_bloodDrop.SurfaceCollider != null)
			{
				var rigid = _bloodDrop.SurfaceCollider.attachedRigidbody;
				if (rigid != null)
				{
					var part = rigid.GetComponent<RagdollPart>();
					if (part != null)
					{
						_renderers.Clear();

						foreach (var rendererData in part.renderers)
						{
							var renderer = rendererData.renderer;

							if (renderer == null) continue;
							if (!renderer.isVisible) continue;

							var nameLower = renderer.name.ToLower();

							if (nameLower.Contains("_vfx")) continue;
							if (nameLower.Contains("hair")) continue;

							_renderers.Add(renderer);
						}

						var normal = _bloodDrop.LastSurfaceNormal;
						var posOffset = normal * 0.07f;

						StartCoroutine(RevealMaskProjection.ProjectAsync(transform.position + posOffset, -normal, Vector3.up, 0.12f,
							0.01f * SizeMultiplier * EntryPoint.Configuration.BloodStreakWidthMultiplier, ParticleTexture, new Vector4(0.7f, 0, 0, 0),
							_renderers.ToArray(), _revealData, null));
					}
				}
			}
		}
	}
}
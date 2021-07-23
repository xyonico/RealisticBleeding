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

		private static readonly RevealData _revealData = new RevealData
		{
			blendOp = BlendOp.Add
		};
		
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
						var renderers = new Renderer[part.renderers.Count];
						var revealData = new RevealData[part.renderers.Count];

						for (var i = 0; i < part.renderers.Count; i++)
						{
							renderers[i] = part.renderers[i].renderer;
							revealData[i] = _revealData;
						}

						var normal = _bloodDrop.LastSurfaceNormal;
						var posOffset = normal * 0.07f;

						StartCoroutine(RevealMaskProjection.ProjectAsync(transform.position + posOffset, -normal, Vector3.up, 0.12f,
							0.005f * SizeMultiplier, ParticleTexture, new Vector4(0.4f, 0, 0, 0), renderers, revealData, null));
					}
				}
			}
		}
	}
}
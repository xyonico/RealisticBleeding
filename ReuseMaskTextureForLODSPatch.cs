using System.Collections.Generic;
using HarmonyLib;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;

namespace RealisticBleeding
{
	/// <summary>
	/// The game uses a separate reveal mask texture for each LOD and has to draw every decal on each one.
	/// This is unnecessary and slow, especially for this mod's decal drawing,
	/// so these patches detects LOD renderers and makes them share the same mask texture.
	/// </summary>
	public static class ReuseMaskTextureForLODSPatch
	{
		private static readonly int RevealMaskNameID = Shader.PropertyToID("_RevealMask");

		[HarmonyPatch(typeof(RevealDecal), "Awake")]
		public static class RemoveRevealDecalOnLODSPatch
		{
			private static bool Prefix(RevealDecal __instance)
			{
				var gameObject = __instance.gameObject;

				if (!Level.master || !Catalog.gameData.platformParameters.enableEffectReveal)
				{
					__instance.enabled = false;

					return false;
				}

				if (!IsPartOfLOD(gameObject, out var lod, out var nameWithoutLodNumber)) return true;

				if (lod == 0)
				{
					// This is part of the first LOD
					// Find the rest of the LOD renderers and add them to the RevealDecalLODS component

					var transform = __instance.transform;

					var parent = transform.parent;

					if (parent == null || parent.childCount == 1) return true;

					var lodRenderers = new List<(Renderer, MaterialInstance)>();
					
					// From the original Awake:
					__instance.revealMaterialController = __instance.gameObject.AddComponent<RevealMaterialController>();
					__instance.revealMaterialController.width = (int) __instance.maskWidth;
					__instance.revealMaterialController.height = (int) __instance.maskHeight;
					__instance.revealMaterialController.maskPropertyName = "_RevealMask";
					__instance.revealMaterialController.restoreMaterialsOnReset = false;
					__instance.revealMaterialController.renderTextureFormat = RenderTextureFormat.ARGB64;

					for (var i = 0; i < parent.childCount; i++)
					{
						var sibling = parent.GetChild(i);

						if (sibling == transform) continue;

						if (sibling.TryGetComponent(out Renderer siblingRenderer) &&
						    sibling.name.StartsWith(nameWithoutLodNumber))
						{
							if (!siblingRenderer.TryGetComponent(out MaterialInstance materialInstance))
							{
								materialInstance = siblingRenderer.gameObject.AddComponent<MaterialInstance>();
							}

							lodRenderers.Add((siblingRenderer, materialInstance));

							if (sibling.TryGetComponent(out RevealDecal siblingDecal))
							{
								siblingDecal.revealMaterialController = __instance.revealMaterialController;
							}
						}
					}

					RevealDecalLODS.AddTo(__instance.gameObject, lodRenderers);
				}

				return false;
			}

			private static bool IsPartOfLOD(GameObject gameObject, out int lod, out string nameWithoutLodNumber)
			{
				lod = -1;

				var name = gameObject.name;

				nameWithoutLodNumber = name;

				// We assume the LOD number is never more than 1 digit

				if (name.Length < 5) return false;

				if (name.Substring(name.Length - 5, 4) == "_LOD")
				{
					nameWithoutLodNumber = name.Substring(0, name.Length - 1);

					return int.TryParse(name.Substring(name.Length - 1), out lod);
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(RevealMaterialController), "ActivateRevealMaterials")]
		public static class AssignMaskTextureToLODSPatch
		{
			private static void Postfix(RevealMaterialController __instance, bool __result)
			{
				if (!__result) return;

				if (!__instance.TryGetComponent(out RevealDecalLODS revealDecalLODS)) return;

				foreach (var (renderer, materialInstance) in revealDecalLODS.LODRenderers)
				{
					if (renderer == null) continue;

					if (materialInstance == null || materialInstance.materials == null) continue;

					foreach (var material in materialInstance.materials)
					{
						RevealMaskProjection.materialsToEnableReveal.Push(material);
						material.SetTexture(RevealMaskNameID, __instance.MaskTexture);
					}
				}
			}
		}
	}

	public class RevealDecalLODS : MonoBehaviour
	{
		public List<(Renderer, MaterialInstance)> LODRenderers { get; private set; }

		public static void AddTo(GameObject gameObject, List<(Renderer, MaterialInstance)> lodRenderers)
		{
			var instance = gameObject.AddComponent<RevealDecalLODS>();

			instance.LODRenderers = lodRenderers;
		}
	}
}
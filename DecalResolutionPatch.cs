using HarmonyLib;
using ThunderRoad.Reveal;
using UnityEngine;

namespace RealisticBleeding
{
	public static class DecalResolutionPatch
	{
		[HarmonyPatch(typeof(RevealMaterialController), "Start")]
		public static class StartPatch
		{
			private static void Postfix(RevealMaterialController __instance,
				ref RenderTextureDescriptor ____maskDescriptor)
			{
				var pixelsPerMeter = EntryPoint.Configuration.DecalPixelsPerMeter;

				if (pixelsPerMeter <= 0) return;

				if (__instance.GetRenderer(out var renderer))
				{
					var bounds = renderer.bounds;
					var size = bounds.extents;

					var maxSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

					var pixels = Mathf.FloorToInt(maxSize * pixelsPerMeter);

					pixels = Mathf.Clamp(pixels, 256, 2048);

					__instance.width = pixels;
					__instance.height = pixels;

					____maskDescriptor.width = pixels;
					____maskDescriptor.height = pixels;
				}
			}
		}
	}
}
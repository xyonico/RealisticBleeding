using System;
using System.Reflection;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;

namespace RealisticBleeding
{
    public static class DecalResolutionPatch
    {
        //[HarmonyPatch(typeof(RevealMaterialController), "Start")]
        public static class StartPatch
        {
            private static ModOptionInt[] GetDecalResolutions()
            {
                Span<int> values = stackalloc int[] { 256, 512, 1024, 2048, 4096, 8192 };

                var array = new ModOptionInt[values.Length + 1];

                array[0] = new ModOptionInt("Default", 0);

                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    array[i + 1] = new ModOptionInt($"{value}px", value);
                }

                return array;
            }

            private static int _decalPixelsPerMeter = 2048;

            //[ModOptionCategory("Performance", 1)]
            /*[ModOption("Decal Pixels per Meter",
                "This overrides the resolution of all character/weapon decal textures.\n" +
                "Setting this to Default will leave the resolutions to whatever the developers chose for each model.\n",
                order = 11, valueSourceName = nameof(GetDecalResolutions), defaultValueIndex = 4)]*/
            private static void SetDecalPixelsPerMeter(int value)
            {
                _decalPixelsPerMeter = value;

                // Recreate all reveal mask textures with new resolution setting.
                foreach (var revealMaterialController in Resources.FindObjectsOfTypeAll<RevealMaterialController>())
                {
                    var descriptor = revealMaterialController.MaskDescriptor;

                    // This means this controller hasn't initialized yet, so we can ignore it.
                    if (descriptor.width == 0) continue;

                    if (!TryCalculatePixels(revealMaterialController, out var pixels)) continue;

                    revealMaterialController.width = pixels;
                    revealMaterialController.height = pixels;

                    InitMethod.Invoke(revealMaterialController, Array.Empty<object>());

                    var texture = revealMaterialController.MaskTexture;

                    if (texture == null) continue;

                    if (texture.IsCreated())
                    {
                        texture.Release();
                    }

                    texture.width = pixels;
                    texture.height = pixels;

                    texture.Create();
                }
            }

            private static readonly MethodInfo InitMethod =
                typeof(RevealMaterialController).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);

            private static void Postfix(RevealMaterialController __instance,
                ref RenderTextureDescriptor ____maskDescriptor)
            {
                if (!TryCalculatePixels(__instance, out var pixels)) return;

                __instance.width = pixels;
                __instance.height = pixels;

                ____maskDescriptor.width = pixels;
                ____maskDescriptor.height = pixels;
            }

            private static bool TryCalculatePixels(RevealMaterialController revealMaterialController, out int pixels)
            {
                var pixelsPerMeter = _decalPixelsPerMeter;

                if (pixelsPerMeter <= 0)
                {
                    pixels = 0;

                    return false;
                }

                if (revealMaterialController.GetRenderer(out var renderer))
                {
                    var bounds = renderer.bounds;
                    var size = bounds.extents;

                    var maxSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

                    pixels = Mathf.FloorToInt(maxSize * pixelsPerMeter);
                    pixels = Mathf.Clamp(pixels, 32, pixelsPerMeter);

                    return true;
                }

                pixels = 0;

                return false;
            }
        }
    }
}
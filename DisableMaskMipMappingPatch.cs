using HarmonyLib;
using ThunderRoad.Reveal;

namespace RealisticBleeding
{
	public static class DisableMaskMipMappingPatch
	{
		[HarmonyPatch(typeof(RevealMaterialController), "Start")]
		public static class StartPatch
		{
			private static void Prefix(ref bool ___generateMipMaps)
			{
				___generateMipMaps = false;
			}
		}
	}
}
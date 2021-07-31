using System;
using HarmonyLib;
using ThunderRoad;

namespace RealisticBleeding
{
	[HarmonyPatch(typeof(Creature), "Despawn", new Type[0])]
	public static class CreatureDespawnHook
	{
		public static event Action<Creature> DespawnEvent;
		
		private static void Postfix(Creature __instance)
		{
			DespawnEvent?.Invoke(__instance);
		}
	}
}
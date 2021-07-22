using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public static class DamagerPatches
	{
		[HarmonyPatch(typeof(Damager), "TryHit")]
		public static class TryHitPatch
		{
			public static void Postfix(CollisionInstance collisionInstance, bool __result)
			{
				if (!__result) return;

				OnHit(collisionInstance);
			}

			public static void OnHit(CollisionInstance collisionInstance)
			{
				var damageType = collisionInstance.damageStruct.damageType;
				if (damageType != DamageType.Pierce && damageType != DamageType.Slash) return;

				var contactPoint = collisionInstance.contactPoint;

				var bleederObject = new GameObject("Bleeder");
				var bleeder = bleederObject.AddComponent<Bleeder>();
				bleeder.transform.parent = collisionInstance.targetCollider.transform;
				bleeder.transform.position = contactPoint;
			}
		}
		
		[HarmonyPatch(typeof(Damager), "TryHitByPressure")]
		public static class TryHitByPressurePatch
		{
			public static void Postfix(CollisionInstance collisionInstance, bool __result)
			{
				if (!__result) return;

				TryHitPatch.OnHit(collisionInstance);
			}
		}
	}
}
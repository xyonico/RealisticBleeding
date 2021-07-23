using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public static class EffectInstancePatches
	{
		[HarmonyPatch(typeof(EffectInstance), "AddEffect")]
		public static class AddEffectPatch
		{
			public static void Postfix(EffectData effectData, Vector3 position, Quaternion rotation, Transform parent,
				CollisionInstance collisionInstance)
			{
				if (!Options.allowGore) return;

				if (collisionInstance == null) return;
				var ragdollPart = collisionInstance.damageStruct.hitRagdollPart;

				if (ragdollPart == null) return;

				var pressureIntensity = Catalog.GetCollisionStayRatio(collisionInstance.pressureRelativeVelocity.magnitude);
				
				var damageType = collisionInstance.damageStruct.damageType;
				if (damageType == DamageType.Unknown || damageType == DamageType.Energy) return;

				const float minBluntIntensity = 0.45f;
				const float minSlashIntensity = 0.1f;
				const float minPierceIntensity = 0.001f;

				var intensity = Mathf.Max(collisionInstance.intensity, pressureIntensity);
				
				var minIntensity = damageType == DamageType.Blunt ? minBluntIntensity :
					damageType == DamageType.Pierce ? minPierceIntensity : minSlashIntensity;
				if (intensity < minIntensity) return;

				if (damageType == DamageType.Blunt)
				{
					intensity *= 0.2f;
				}

				var multiplier = Mathf.Lerp(0.3f, 1.7f, Mathf.InverseLerp(minIntensity, 1, intensity));

				var bleederObject = new GameObject("Bleeder");
				var bleeder = bleederObject.AddComponent<Bleeder>();
				bleeder.transform.parent = collisionInstance.targetCollider.transform;
				bleeder.transform.position = position;
				bleeder.transform.rotation = rotation;
				
				bleeder.DurationMultiplier = multiplier;
				bleeder.FrequencyMultiplier = multiplier;
				bleeder.SizeMultiplier = multiplier;

				if (damageType == DamageType.Slash)
				{
					var dimensions = bleeder.Dimensions;
					dimensions.x = 0;
					dimensions.y = Mathf.Lerp(0.06f, 0.12f, intensity);
					bleeder.Dimensions = dimensions;
				}
			}
		}
	}
}
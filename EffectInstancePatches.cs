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
					intensity *= 0.4f;
				}
				else if (damageType == DamageType.Pierce)
				{
					intensity *= 2.5f;
				}

				var multiplier = Mathf.Lerp(0.6f, 1.5f, Mathf.InverseLerp(minIntensity, 1, intensity));

				var durationMultiplier = multiplier;
				var frequencyMultiplier = multiplier;
				var sizeMultiplier = multiplier;

				switch (ragdollPart.type)
				{
					case RagdollPart.Type.Neck:
						durationMultiplier *= 5;
						frequencyMultiplier *= 5;
						sizeMultiplier *= 2;
						break;
					case RagdollPart.Type.Head:
						if (damageType != DamageType.Blunt)
						{
							durationMultiplier *= 2f;
							frequencyMultiplier *= 3;
							sizeMultiplier *= 0.9f;
						}

						break;
				}

				Vector2? dimensions = null;

				if (damageType == DamageType.Slash)
				{
					dimensions = new Vector2(0, Mathf.Lerp(0.06f, 0.12f, intensity));
				}
				else if (EntryPoint.Configuration.NoseBleedsEnabled && damageType == DamageType.Blunt)
				{
					var creature = ragdollPart.ragdoll.creature;
					if (ragdollPart.type == RagdollPart.Type.Head && NoseBleed.TryGetNosePosition(creature, out var nosePosition))
					{
						if (Vector3.Distance(nosePosition, position) < 0.1f)
						{
							NoseBleed.SpawnOn(creature, 1, 1);
						}
					}
				}

				if (!EntryPoint.Configuration.BleedingFromWoundsEnabled) return;

				SpawnBleeder(position, rotation, collisionInstance.targetCollider.transform,
					durationMultiplier, frequencyMultiplier, sizeMultiplier, dimensions);
			}

			private static Bleeder SpawnBleeder(Vector3 position, Quaternion rotation, Transform parent,
				float durationMultiplier, float frequencyMultiplier, float sizeMultiplier, Vector2? dimensions = null)
			{
				var bleeder = Bleeder.Spawn(position, rotation, parent);

				bleeder.DurationMultiplier = durationMultiplier;
				bleeder.FrequencyMultiplier = frequencyMultiplier;
				bleeder.SizeMultiplier = sizeMultiplier;

				if (dimensions.HasValue)
				{
					bleeder.Dimensions = dimensions.Value;
				}

				return bleeder;
			}
		}
	}
}
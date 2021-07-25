using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public static class NoseBleed
	{
		private static readonly Vector3 UnderNoseOffset = new Vector3(0, -0.055f, 0.046f);
		private const float NostrilOffset = 0.008f;
		
		private static readonly HashSet<Creature> _bleedingCreatures = new HashSet<Creature>();
		
		public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;
			var centerEyes = creature.centerEyes;

			if (centerEyes == null) return;

			if (!_bleedingCreatures.Add(creature)) return;

			SpawnNoseBleeder(NostrilOffset); // right nostril
			SpawnNoseBleeder(-NostrilOffset); // left nostril

			void SpawnNoseBleeder(float nostrilOffset)
			{
				var noseOffset = UnderNoseOffset;
				noseOffset.x = nostrilOffset;
				var bleeder = Bleeder.Spawn(centerEyes.TransformPoint(noseOffset), centerEyes.rotation, centerEyes);
				
				bleeder.Dimensions = Vector2.zero;
				bleeder.DurationMultiplier = 2 * durationMultiplier;
				bleeder.FrequencyMultiplier = 2 * frequencyMultiplier;
				bleeder.SizeMultiplier = sizeMultiplier;
			}

			creature.StartCoroutine(DelayedRemoveCreature(creature, 4));
		}

		public static void SpawnOnDelayed(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier,
			float sizeMultiplier = 1)
		{
			if (creature == null) return;
			var centerEyes = creature.centerEyes;

			if (centerEyes == null) return;

			creature.StartCoroutine(SpawnOnDelayedRoutine(creature, delay, durationMultiplier, frequencyMultiplier, sizeMultiplier));
		}

		public static bool TryGetNosePosition(Creature creature, out Vector3 nosePosition)
		{
			nosePosition = Vector3.zero;
			
			if (creature == null) return false;
			var centerEyes = creature.centerEyes;

			if (centerEyes == null) return false;

			nosePosition = centerEyes.TransformPoint(UnderNoseOffset);
			return true;
		}

		private static IEnumerator SpawnOnDelayedRoutine(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier,
			float sizeMultiplier = 1)
		{
			yield return new WaitForSeconds(delay);

			SpawnOn(creature, durationMultiplier, frequencyMultiplier, sizeMultiplier);
		}

		private static IEnumerator DelayedRemoveCreature(Creature creature, float delay)
		{
			yield return new WaitForSeconds(delay);

			_bleedingCreatures.Remove(creature);
		}
	}
}
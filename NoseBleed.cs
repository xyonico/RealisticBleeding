using System.Collections;
using System.Collections.Generic;
using DefaultEcs;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public static class NoseBleed
	{
		private static readonly Vector3 UnderNoseOffset = new Vector3(0, -0.055f, 0.046f);
		private const float NostrilOffset = 0.008f;

		private static readonly Dictionary<Creature, (Entity left, Entity right, Coroutine coroutine)> _bleedingCreatures =
			new Dictionary<Creature, (Entity, Entity, Coroutine)>();
		
		public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;
			var centerEyes = creature.centerEyes;

			if (centerEyes == null) return;

			if (_bleedingCreatures.TryGetValue(creature, out var bleeds))
			{
				var bleeder = bleeds.left.Get<Bleeder>();
				if (frequencyMultiplier * 2 > bleeder.FrequencyMultiplier)
				{
					bleeds.left.Dispose();
					bleeds.right.Dispose();
					
					creature.StopCoroutine(bleeds.coroutine);
				}
				else
				{
					return;
				}
			}

			var left = SpawnNoseBleeder(-NostrilOffset); // left nostril
			var right = SpawnNoseBleeder(NostrilOffset); // right nostril

			Entity SpawnNoseBleeder(float nostrilOffset)
			{
				var noseOffset = UnderNoseOffset;
				noseOffset.x = nostrilOffset;
				var bleeder = Bleeder.Spawn(centerEyes, centerEyes.TransformPoint(noseOffset), centerEyes.rotation, Vector2.zero,
					2 * frequencyMultiplier, sizeMultiplier, 2 * durationMultiplier);

				return bleeder;
			}

			var coroutine = creature.StartCoroutine(DelayedRemoveCreature(creature, 4));
			
			_bleedingCreatures.Add(creature, (left, right, coroutine));
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
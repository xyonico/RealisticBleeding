using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RealisticBleeding.Components;

namespace RealisticBleeding
{
	public static class MouthBleed
	{
		private static readonly Vector3 LowerLipOffset = new Vector3(-0.12f, 0, 0.045f);
		private static readonly Quaternion RotationOffset = Quaternion.Euler(0, -55f, -90);

		private static readonly HashSet<Creature> _bleedingCreatures = new HashSet<Creature>();

		public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;

			if (!_bleedingCreatures.Add(creature)) return;

			var jawBone = creature.jaw;

			var bleeder = Bleeder.Spawn(jawBone, jawBone.TransformPoint(LowerLipOffset), jawBone.rotation * RotationOffset, new Vector2(0.05f, 0),
				frequencyMultiplier * 4, sizeMultiplier * 0.75f, durationMultiplier * 0.3f);
			
			bleeder.Set(new DisposeWithCreature(creature));

			creature.StartCoroutine(DelayedRemoveCreature(creature, 4));
		}

		public static void SpawnOnDelayed(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;

			creature.StartCoroutine(SpawnOnDelayedRoutine(creature, delay, durationMultiplier, frequencyMultiplier, sizeMultiplier));
		}

		private static IEnumerator SpawnOnDelayedRoutine(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier)
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
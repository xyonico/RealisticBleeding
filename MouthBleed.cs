using ThunderRoad;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RealisticBleeding
{
	public static class MouthBleed
	{
		private static readonly Vector3 LowerLipOffset = new Vector3(-0.12f, 0, 0.045f);
		private static readonly Quaternion RotationOffset = Quaternion.Euler(0, -55f, -90);

		private static readonly HashSet<CreatureSpeak> _animatingCreatures = new HashSet<CreatureSpeak>();

		public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;
			if (creature.speak == null) return;

			if (!_animatingCreatures.Add(creature.speak)) return;

			var jawBone = creature.speak.jaw;

			var bleeder = Bleeder.Spawn(jawBone.TransformPoint(LowerLipOffset), jawBone.rotation * RotationOffset, jawBone);

			bleeder.Dimensions = new Vector2(0.05f, 0);
			bleeder.DurationMultiplier = durationMultiplier * 0.3f;
			bleeder.FrequencyMultiplier = frequencyMultiplier * 4f;
			bleeder.SizeMultiplier = sizeMultiplier;

			creature.speak.StartCoroutine(OpenMouthRoutine(creature.speak));
		}

		public static void SpawnOnDelayed(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier = 1)
		{
			if (creature == null) return;
			if (creature.speak == null) return;

			creature.speak.StartCoroutine(SpawnOnDelayedRoutine(creature, delay, durationMultiplier, frequencyMultiplier, sizeMultiplier));
		}

		private static IEnumerator SpawnOnDelayedRoutine(Creature creature, float delay, float durationMultiplier, float frequencyMultiplier, float sizeMultiplier)
		{
			yield return new WaitForSeconds(delay);

			SpawnOn(creature, durationMultiplier, frequencyMultiplier, sizeMultiplier);
		}

		private static IEnumerator OpenMouthRoutine(CreatureSpeak speak)
		{
			speak.jawTargetWeight = 0.15f;
			speak.jawCurrentWeight = 0.00001f;

			var prevSpeed = speak.lipSyncSpeed;

			speak.lipSyncSpeed = 0.2f;

			yield return new WaitForSeconds(3);

			speak.jawTargetWeight = 0;

			yield return new WaitForSeconds(1.5f);

			speak.lipSyncSpeed = prevSpeed;

			_animatingCreatures.Remove(speak);
		}
	}
}
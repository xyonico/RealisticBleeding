using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
	public static class NoseBleed
	{
		private static readonly Vector3 UnderNoseOffset = new Vector3(0, -0.055f, 0.046f);
		private const float NostrilOffset = 0.008f;
		
		public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier)
		{
			if (creature == null) return;
			var centerEyes = creature.centerEyes;

			if (centerEyes == null) return;

			SpawnNoseBleeder(NostrilOffset); // right nostril
			SpawnNoseBleeder(-NostrilOffset); // left nostril

			void SpawnNoseBleeder(float nostrilOffset)
			{
				var noseOffset = UnderNoseOffset;
				noseOffset.x = nostrilOffset;
				var bleeder = Bleeder.Spawn(centerEyes.TransformPoint(noseOffset), Quaternion.identity, centerEyes);
				
				bleeder.Dimensions = Vector2.zero;
				bleeder.DurationMultiplier = 2 * durationMultiplier;
				bleeder.FrequencyMultiplier = 2 * frequencyMultiplier;
				bleeder.SizeMultiplier = 1f;
			}
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
	}
}
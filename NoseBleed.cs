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
				bleeder.DurationMultiplier = durationMultiplier;
				bleeder.FrequencyMultiplier = frequencyMultiplier;
				bleeder.SizeMultiplier = 0.5f;
			}
		}
	}
}
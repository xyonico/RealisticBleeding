using System;

namespace RealisticBleeding
{
	[Serializable]
	public class Configuration
	{
		public bool BleedingFromWoundsEnabled = true;
		public bool NoseBleedsEnabled = true;
		public bool MouthBleedsEnabled = true;
		public int MaxActiveBloodDrips = 24;
		public float BloodAmountMultiplier = 1;
		public float BleedDurationMultiplier = 1;
		public float BloodStreakWidthMultiplier = 1;
		public float BloodSurfaceFrictionMultiplier = 1;
	}
}
using System;
using ThunderRoad;

namespace RealisticBleeding
{
	public static class ModOptionPercentage
	{
		public const int DefaultIndex = 5;

		public static ModOptionFloat[] GetDefaults()
		{
			Span<int> values = stackalloc int[] { 0, 10, 25, 50, 75, 100, 125, 150, 175, 200, 250, 300, 400, 500, 600, 700, 800, 900, 1000 };

			var options = new ModOptionFloat[values.Length];

			for (var i = 0; i < options.Length; i++)
			{
				var value = values[i];
				options[i] = new ModOptionFloat($"{value}%", value / 100f);
			}

			return options;
		}
	}
}
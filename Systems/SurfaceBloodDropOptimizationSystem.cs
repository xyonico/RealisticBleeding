using System;
using DefaultEcs;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDropOptimizationSystem : BaseSystem
	{
		private static ModOptionInt[] GetMaxActiveBloodDropValues()
		{
			Span<int> values = stackalloc int[] { 5, 10, 20, 30, 40, 50, 75, 100, 1000 };

			var array = new ModOptionInt[values.Length];

			for (var i = 0; i < array.Length; i++)
			{
				var value = values[i];
				array[i] = new ModOptionInt(value.ToString(), value);
			}

			return array;
		}

		//[ModOptionCategory("Performance", 1)]
		//[ModOption("Max Active Blood Drops",
		//	"The max number of blood drops that can be updated each frame.\n" +
		//	"If the number of blood drops exceeds this, the blood simulation will slow down to maintain performance.",
		//	order = 10, valueSourceName = nameof(GetMaxActiveBloodDropValues), defaultValueIndex = 2)]
		private static int MaxActiveBloodDrops = 20;

		private static int _currentIndex;

		public SurfaceBloodDropOptimizationSystem(EntitySet entitySet) : base(entitySet)
		{
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			if (entities.Length == 0) return;

			var updateCount = 0;

			_currentIndex %= entities.Length;

			var startIndex = _currentIndex;

			do
			{
				var currentDrop = entities[_currentIndex++];

				currentDrop.Set<ShouldUpdate>();
				updateCount++;

				if (_currentIndex >= entities.Length)
				{
					_currentIndex = 0;
				}
			} while (updateCount < MaxActiveBloodDrops && _currentIndex != startIndex);
		}
	}
}
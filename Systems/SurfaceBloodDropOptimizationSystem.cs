using System;
using DefaultEcs;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDropOptimizationSystem : BaseSystem
	{
		private const float BloodIndexUpdateCycleDuration = 12;
		private const int OuterRangeCount = 12;

		private readonly int _maxBloodDrops;
		
		private static int _innerRangeIndex;
		private static float _bloodIndexUpdateCycleProgress;
		
		public SurfaceBloodDropOptimizationSystem(EntitySet entitySet, int maxBloodDrops) : base(entitySet)
		{
			_maxBloodDrops = maxBloodDrops;
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			// This isn't very readable currently. I'm trying to limit the amount of droplets that are updated per frame.
			// Instead of spreading it out evenly, which causes all of them to slow down, I update on a cycle.
			// That way, all the droplets get some time where they can update nearly every frame before slowing down and completely stopping.
			_bloodIndexUpdateCycleProgress += deltaTime / BloodIndexUpdateCycleDuration;
			_bloodIndexUpdateCycleProgress %= 1;

			if (entities.Length == 0) return;

			var updateCount = 0;

			var outerRangeCount = Mathf.Min(OuterRangeCount, entities.Length);

			var outerRangeStart = Mathf.FloorToInt(entities.Length * _bloodIndexUpdateCycleProgress);

			_innerRangeIndex %= outerRangeCount;

			var startIndex = _innerRangeIndex;

			do
			{
				var index = outerRangeStart + _innerRangeIndex;
				index %= outerRangeCount;

				var currentDrop = entities[index];

				currentDrop.Set<ShouldUpdate>();
				updateCount++;

				_innerRangeIndex++;

				if (_innerRangeIndex >= outerRangeCount)
				{
					_innerRangeIndex = 0;
				}
			} while (updateCount < _maxBloodDrops && _innerRangeIndex != startIndex);
		}
	}
}
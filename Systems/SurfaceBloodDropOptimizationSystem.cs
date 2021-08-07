using System;
using DefaultEcs;
using RealisticBleeding.Components;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDropOptimizationSystem : BaseSystem
	{
		private readonly int _maxBloodDrops;

		private static int _currentIndex;
		
		public SurfaceBloodDropOptimizationSystem(EntitySet entitySet, int maxBloodDrops) : base(entitySet)
		{
			_maxBloodDrops = maxBloodDrops;
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
			} while (updateCount < _maxBloodDrops && _currentIndex != startIndex);
		}
	}
}
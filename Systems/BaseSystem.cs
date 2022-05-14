using System;
using DefaultEcs;
using DefaultEcs.System;
using Unity.Profiling;

namespace RealisticBleeding.Systems
{
	public abstract class BaseSystem : ISystem<float>
	{
		private readonly EntitySet _entitySet;

		private readonly ProfilerMarker _profilerMarker; 
		
		public bool IsEnabled { get; set; } = true;
		public World World => _entitySet.World;

		public BaseSystem(EntitySet entitySet)
		{
			_entitySet = entitySet;

			_profilerMarker = new ProfilerMarker(ProfilerCategory.Scripts, GetType().Name);
		}

		void ISystem<float>.Update(float state)
		{
			using (_profilerMarker.Auto())
			{
				Update(state, _entitySet.GetEntities());
			}
		}

		protected virtual void Update(float deltaTime, in Entity entity)
		{
		}

		protected virtual void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			foreach (var entity in entities)
			{
				Update(deltaTime, entity);
			}
		}

		void IDisposable.Dispose()
		{
		}
	}
}
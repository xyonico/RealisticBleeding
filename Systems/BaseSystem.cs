using System;
using DefaultEcs;
using DefaultEcs.System;

namespace RealisticBleeding.Systems
{
	public abstract class BaseSystem : ISystem<float>
	{
		private readonly EntitySet _entitySet;
		
		public bool IsEnabled { get; set; } = true;
		public World World => _entitySet.World;

		public BaseSystem(EntitySet entitySet)
		{
			_entitySet = entitySet;
		}

		void ISystem<float>.Update(float state)
		{
			Update(state, _entitySet.GetEntities());
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
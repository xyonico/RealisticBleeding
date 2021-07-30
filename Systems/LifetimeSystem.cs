using DefaultEcs;
using RealisticBleeding.Components;

namespace RealisticBleeding.Systems
{
	public class LifetimeSystem : BaseSystem
	{
		public LifetimeSystem(EntitySet entitySet) : base(entitySet)
		{
		}

		protected override void Update(float deltaTime, in Entity entity)
		{
			ref var lifetime = ref entity.Get<Lifetime>();
			lifetime.Remaining -= deltaTime;

			if (lifetime.Remaining < 0)
			{
				entity.Dispose();
			}
		}
	}
}
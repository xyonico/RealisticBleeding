using DefaultEcs;
using DefaultEcs.Command;
using RealisticBleeding.Components;
using ThunderRoad;

namespace RealisticBleeding.Systems
{
	public class DisposeWithCreatureSystem
	{
		private readonly EntitySet _disposeWithCreatureSet;

		public DisposeWithCreatureSystem(World world)
		{
			_disposeWithCreatureSet = world.GetEntities().With<DisposeWithCreature>().AsSet();
			
			CreatureDespawnHook.DespawnEvent += OnCreatureDespawnEvent;
		}

		private void OnCreatureDespawnEvent(Creature creature)
		{
			foreach (var entity in _disposeWithCreatureSet.GetEntities())
			{
				ref var disposeWithCreature = ref entity.Get<DisposeWithCreature>();

				if (disposeWithCreature.Creature == creature)
				{
					entity.Dispose();
				}
			}
		}
	}
}
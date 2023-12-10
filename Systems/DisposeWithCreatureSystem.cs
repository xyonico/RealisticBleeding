using System.Collections.Generic;
using DefaultEcs;
using RealisticBleeding.Components;
using ThunderRoad;

namespace RealisticBleeding.Systems
{
    public class DisposeWithCreatureSystem
    {
        private readonly EntitySet _disposeWithCreatureSet;

        private static readonly HashSet<Creature> TrackedCreatures = new HashSet<Creature>();

        public DisposeWithCreatureSystem(World world)
        {
            _disposeWithCreatureSet = world.GetEntities().With<DisposeWithCreature>().AsSet();

            EventManager.onCreatureSpawn += OnCreatureSpawn;
        }

        private void OnCreatureSpawn(Creature creature)
        {
            if (!TrackedCreatures.Add(creature)) return;

            creature.OnDespawnEvent += time =>
            {
                if (time == EventTime.OnStart) return;

                OnCreatureDespawnEvent(creature);
            };
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
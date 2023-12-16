using System;
using System.Collections.Generic;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class CreatureTracker : BaseSystem
    {
        private readonly HashSet<Creature> _trackedCreatures = new HashSet<Creature>();
        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;
        private readonly FastList<Bleeder> _bleeders;

        public CreatureTracker(FastList<SurfaceBloodDrop> surfaceBloodDrops, FastList<Bleeder> bleeders)
        {
            _surfaceBloodDrops = surfaceBloodDrops;
            _bleeders = bleeders;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            var allCreatures = Creature.all;

            for (var i = 0; i < allCreatures.Count; i++)
            {
                var creature = allCreatures[i];

                if (_trackedCreatures.Add(creature))
                {
                    OnCreatureSpawn(creature);
                }
            }
        }

        private void OnCreatureSpawn(Creature creature)
        {
            try
            {
                creature.OnDespawnEvent += time =>
                {
                    if (time == EventTime.OnStart) return;

                    OnCreatureDespawnEvent(creature);
                };

                CreatureHitTracker.OnCreatureSpawn(creature);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnCreatureDespawnEvent(Creature creature)
        {
            try
            {
                for (var index = 0; index < _surfaceBloodDrops.Count; index++)
                {
                    if (_surfaceBloodDrops[index].DisposeWithCreature == creature)
                    {
                        _surfaceBloodDrops.RemoveAtSwapBack(index--);
                    }
                }

                for (var index = 0; index < _bleeders.Count; index++)
                {
                    if (_bleeders[index].DisposeWithCreature == creature)
                    {
                        _bleeders.RemoveAtSwapBack(index--);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }
}
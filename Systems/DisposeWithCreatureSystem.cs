using System;
using System.Collections.Generic;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class DisposeWithCreatureSystem
    {
        private static readonly HashSet<Creature> TrackedCreatures = new HashSet<Creature>();

        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;
        private readonly FastList<Bleeder> _bleeders;

        public DisposeWithCreatureSystem(FastList<SurfaceBloodDrop> surfaceBloodDrops, FastList<Bleeder> bleeders)
        {
            _surfaceBloodDrops = surfaceBloodDrops;
            _bleeders = bleeders;
            EventManager.onCreatureSpawn += OnCreatureSpawn;
        }

        private void OnCreatureSpawn(Creature creature)
        {
            try
            {
                if (!TrackedCreatures.Add(creature)) return;

                creature.OnDespawnEvent += time =>
                {
                    if (time == EventTime.OnStart) return;

                    OnCreatureDespawnEvent(creature);
                };
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
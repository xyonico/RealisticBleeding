using System.Collections;
using System.Collections.Generic;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
    public static class NoseBleed
    {
        private const float NostrilOffset = 0.008f;

        private static readonly Vector3 UnderNoseOffset = new Vector3(0, -0.055f, 0.046f);
        private static readonly HashSet<Creature> BleedingCreatures = new HashSet<Creature>();

        public static void SpawnOn(Creature creature, float durationMultiplier, float frequencyMultiplier,
            float sizeMultiplier = 1)
        {
            if (creature == null) return;
            var centerEyes = creature.centerEyes;

            if (centerEyes == null) return;

            if (!BleedingCreatures.Add(creature)) return;

            Collider closestCollider = null;
            var closestDistance = float.PositiveInfinity;

            var colliders = creature.ragdoll.headPart.colliderGroup.colliders;

            var position = centerEyes.TransformPoint(UnderNoseOffset);

            for (var i = 0; i < colliders.Count; i++)
            {
                var collider = colliders[i];

                var distance = Vector3.Distance(collider.ClosestPoint(position), position);

                if (distance < closestDistance)
                {
                    closestCollider = collider;
                    closestDistance = distance;
                }
            }

            if (closestCollider == null) return;

            SpawnNoseBleeder(-NostrilOffset); // left nostril
            SpawnNoseBleeder(NostrilOffset); // right nostril

            creature.StartCoroutine(DelayedRemoveCreature(creature, 4));

            return;

            void SpawnNoseBleeder(float nostrilOffset)
            {
                var noseOffset = UnderNoseOffset;
                noseOffset.x = nostrilOffset;
                var bleeder = new Bleeder(centerEyes, closestCollider, centerEyes.TransformPoint(noseOffset),
                    centerEyes.rotation, Vector2.zero,
                    2 * frequencyMultiplier, sizeMultiplier, 2 * durationMultiplier, creature);

                EntryPoint.Bleeders.TryAddNoResize(bleeder);
            }
        }

        public static void SpawnOnDelayed(Creature creature, float delay, float durationMultiplier,
            float frequencyMultiplier,
            float sizeMultiplier = 1)
        {
            if (creature == null) return;
            var centerEyes = creature.centerEyes;

            if (centerEyes == null) return;

            creature.StartCoroutine(SpawnOnDelayedRoutine(creature, delay, durationMultiplier, frequencyMultiplier,
                sizeMultiplier));
        }

        public static bool TryGetNosePosition(Creature creature, out Vector3 nosePosition)
        {
            nosePosition = Vector3.zero;

            if (creature == null) return false;
            var centerEyes = creature.centerEyes;

            if (centerEyes == null) return false;

            nosePosition = centerEyes.TransformPoint(UnderNoseOffset);
            return true;
        }

        private static IEnumerator SpawnOnDelayedRoutine(Creature creature, float delay, float durationMultiplier,
            float frequencyMultiplier,
            float sizeMultiplier = 1)
        {
            yield return new WaitForSeconds(delay);

            SpawnOn(creature, durationMultiplier, frequencyMultiplier, sizeMultiplier);
        }

        private static IEnumerator DelayedRemoveCreature(Creature creature, float delay)
        {
            yield return new WaitForSeconds(delay);

            BleedingCreatures.Remove(creature);
        }
    }
}
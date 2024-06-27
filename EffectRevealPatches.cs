using System;
using HarmonyLib;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RealisticBleeding
{
    public static class EffectRevealPatches
    {
        private static bool _bleedFromWounds = true;

        [ModOptionCategory("Features", 0)]
        [ModOptionButton]
        [ModOption("Bleeding From Wounds",
            "Whether wounds should bleed.\nNose and mouth bleed can still be enabled separately from this.",
            defaultValueIndex = 1, order = 1)]
        private static void SetBleedFromWounds(bool value)
        {
            _bleedFromWounds = value;

            if (!_bleedFromWounds)
            {
                EntryPoint.Bleeders.Clear();
            }
        }

        [ModOptionCategory("Features", 0)]
        [ModOptionButton]
        [ModOption("Nose Bleeds",
            "Whether noses should bleed when enough blunt force is applied to the head.",
            defaultValueIndex = 1, order = 2)]
        private static bool NoseBleedsEnabled { get; set; }

        [ModOptionCategory("Features", 0)]
        [ModOptionButton]
        [ModOption("Mouth Bleeds",
            "Whether blood should come from the mouth when torso is pierced.",
            defaultValueIndex = 1, order = 3)]
        private static bool MouthBleedsEnabled { get; set; }

        [ModOptionCategory("Features", 0)]
        [ModOptionButton]
        [ModOption("Player Bleeds",
            "Whether the player character should also bleed.\n" +
            "Disabling this can improve performance.",
            defaultValueIndex = 1, order = 4)]
        private static bool PlayerBleeding { get; set; }

        [HarmonyPatch(typeof(EffectInstance), "AddEffect")]
        public static class AddEffectPatch
        {
            public static CollisionInstance LastCollisionInstance { get; private set; }

            public static void Prefix(CollisionInstance collisionInstance)
            {
                if (collisionInstance != null)
                {
                    LastCollisionInstance = collisionInstance;
                }
            }
        }

        [HarmonyPatch(typeof(EffectReveal), "Play")]
        public static class PlayPatch
        {
            public static void Prefix(EffectReveal __instance)
            {
                var controllers = __instance.revealMaterialControllers;

                for (var i = controllers.Count - 1; i >= 0; i--)
                {
                    var controller = controllers[i];
                    if (controller == null || controller.GetRenderer() == null)
                    {
                        controllers.RemoveAt(i);
                    }
                }
            }

            public static void Postfix(EffectReveal __instance)
            {
                try
                {
                    if (!GameManager.options.enableCharacterReveal) return;
                    if (EntryPoint.PauseSimulation) return;

                    var collisionInstance = AddEffectPatch.LastCollisionInstance;

                    if (collisionInstance == null) return;

                    var ragdollPart = collisionInstance.damageStruct.hitRagdollPart;
                    if (ragdollPart == null) return;

                    var creature = ragdollPart.ragdoll.creature;

                    if (!PlayerBleeding && creature.isPlayer) return;

                    var pressureIntensity =
                        Catalog.GetCollisionStayRatio(collisionInstance.pressureRelativeVelocity.magnitude);

                    var damageType = collisionInstance.damageStruct.damageType;
                    if (damageType == DamageType.Unknown || damageType == DamageType.Energy) return;

                    const float minBluntIntensity = 0.2f;
                    const float minSlashIntensity = 0.01f;
                    const float minPierceIntensity = 0.001f;

                    var intensity = Mathf.Max(collisionInstance.intensity, pressureIntensity);

                    var minIntensity = damageType == DamageType.Blunt ? minBluntIntensity :
                        damageType == DamageType.Pierce ? minPierceIntensity : minSlashIntensity;
                    if (intensity < minIntensity) return;

                    if (damageType == DamageType.Blunt)
                    {
                        intensity *= 0.5f;
                    }
                    else if (damageType == DamageType.Pierce)
                    {
                        intensity *= 2.5f;
                    }

                    var multiplier = Mathf.Lerp(0.6f, 1.5f, Mathf.InverseLerp(minIntensity, 1, intensity));

                    var durationMultiplier = multiplier;
                    var frequencyMultiplier = multiplier;
                    var sizeMultiplier = multiplier;

                    switch (ragdollPart.type)
                    {
                        case RagdollPart.Type.Neck:
                            durationMultiplier *= 5;
                            frequencyMultiplier *= 4f;
                            sizeMultiplier *= 1.4f;
                            break;
                        case RagdollPart.Type.Head:
                            durationMultiplier *= 2f;
                            frequencyMultiplier *= 1.7f;
                            sizeMultiplier *= 0.9f;

                            break;
                        case RagdollPart.Type.Torso:
                            if (damageType != DamageType.Blunt)
                            {
                                durationMultiplier *= 2f;
                                frequencyMultiplier *= 2f;
                            }

                            break;
                    }

                    var dimensions = new Vector2(0.01f, 0.01f);

                    if (damageType == DamageType.Slash)
                    {
                        dimensions = new Vector2(0, Mathf.Lerp(0.06f, 0.12f, intensity));
                    }

                    var position = collisionInstance.contactPoint;
                    var rotation = Quaternion.identity;

                    if (damageType == DamageType.Blunt && ragdollPart.type == RagdollPart.Type.Head)
                    {
                        if (NoseBleedsEnabled)
                        {
                            if (NoseBleed.TryGetNosePosition(creature, out var nosePosition))
                            {
                                if (Vector3.Distance(nosePosition, position) < 0.1f)
                                {
                                    NoseBleed.SpawnOnDelayed(creature, Random.Range(0.75f, 1.75f), 1, 1, 0.9f);

                                    return;
                                }
                            }

                            if (collisionInstance.intensity > 0.45f)
                            {
                                NoseBleed.SpawnOnDelayed(creature, Random.Range(1f, 2), intensity, intensity,
                                    Mathf.Clamp(intensity, 0.8f, 1.1f));
                            }
                        }
                    }

                    if (MouthBleedsEnabled && damageType == DamageType.Pierce &&
                        ragdollPart.type == RagdollPart.Type.Torso)
                    {
                        if (intensity > 0.2f)
                        {
                            NoseBleed.SpawnOnDelayed(creature, Random.Range(2f, 4f), 0.2f, 0.1f, 0.9f);
                            MouthBleed.SpawnOnDelayed(creature, Random.Range(2, 4f), 1, 1);
                        }
                    }

                    if (!_bleedFromWounds) return;

                    for (var index = 0; index < EntryPoint.Bleeders.Count; index++)
                    {
                        ref var activeBleeder = ref EntryPoint.Bleeders[index];
                        var sqrDistance = (activeBleeder.WorldPosition - position).sqrMagnitude;

                        const float minDistance = 0.05f;

                        if (sqrDistance < minDistance * minDistance) return;
                    }

                    Collider closestCollider = null;
                    var closestDistance = float.PositiveInfinity;

                    var colliders = ragdollPart.colliderGroup.colliders;

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

                    var bleeder = new Bleeder(ragdollPart.transform, closestCollider, position, rotation,
                        dimensions, frequencyMultiplier, sizeMultiplier, durationMultiplier, creature);

                    EntryPoint.Bleeders.TryAddNoResize(bleeder);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
using RealisticBleeding.Systems;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Components
{
    public struct Bleeder
    {
        public Transform Parent;
        public Collider Collider;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector2 Dimensions;
        public float NextBleedTime;
        public float LifetimeRemaining;
        public Creature DisposeWithCreature;

        public float FrequencyMultiplier;
        public float SizeMultiplier;

        public Vector3 WorldPosition => Parent ? Parent.TransformPoint(Position) : Position;
        public Quaternion WorldRotation => Parent ? Parent.rotation * Rotation : Rotation;

        public Bleeder(Transform parent, Collider collider, Vector3 position, Quaternion rotation, Vector2 dimensions,
            float frequencyMultiplier, float sizeMultiplier, float durationMultiplier, Creature disposeWithCreature)
        {
            durationMultiplier *= BleederSystem.BleedDurationMultiplier;

            LifetimeRemaining = Random.Range(3, 8f) * durationMultiplier;
            Parent = parent;
            Collider = collider;
            Position = parent ? parent.InverseTransformPoint(position) : position;
            Rotation = parent ? Quaternion.Inverse(parent.rotation) * rotation : rotation;
            Dimensions = dimensions;
            FrequencyMultiplier = frequencyMultiplier;
            SizeMultiplier = sizeMultiplier;
            DisposeWithCreature = disposeWithCreature;

            NextBleedTime = -1;
        }
    }
}
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding
{
    public struct SurfaceBloodDrop
    {
        public Vector3 Velocity;
        public Vector3 Position;
        public float Size;
        public float LifetimeRemaining;
        public Creature DisposeWithCreature;
        public SurfaceCollider SurfaceCollider;
        public DripTime DripTime;
        public bool ShouldRenderDecal;

        public SurfaceBloodDrop(in FallingBloodDrop fallingBloodDrop, Collider collider)
        {
            Position = fallingBloodDrop.Position;
            Velocity = fallingBloodDrop.Velocity;
            Size = fallingBloodDrop.Size;
            LifetimeRemaining = fallingBloodDrop.LifetimeRemaining;
            DripTime = new DripTime(Random.Range(DripTime.RequiredMin, DripTime.RequiredMax));

            DisposeWithCreature = null;
            SurfaceCollider = default;

            ShouldRenderDecal = false;

            OnBloodDropHitSurface(ref this, collider);
        }

        private static readonly Collider[] Colliders = new Collider[32];

        private static void AttachToNearestCollider(ref SurfaceBloodDrop surfaceBloodDrop, float maxRadius)
        {
            var position = surfaceBloodDrop.Position;

            var count = Physics.OverlapSphereNonAlloc(position, maxRadius, Colliders, EntryPoint.SurfaceLayerMask,
                QueryTriggerInteraction.Ignore);

            var closestDistanceSqr = float.MaxValue;
            var closestPoint = position;
            Collider closestCollider = null;

            for (var i = 0; i < count; i++)
            {
                var col = Colliders[i];

                var point = col.ClosestPoint(position);
                var distanceSqr = (point - position).sqrMagnitude;

                if (distanceSqr < 0.0001f)
                {
                    if (Physics.ComputePenetration(EntryPoint.Collider, position, Quaternion.identity,
                            col, col.transform.position, col.transform.rotation,
                            out var direction, out var distance))
                    {
                        distanceSqr = distance * distance;
                        point = position + direction * distance;
                    }
                }

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestCollider = col;
                    closestPoint = point;
                }
            }

            if (closestCollider == null) return;

            surfaceBloodDrop.Position = closestPoint;

            OnBloodDropHitSurface(ref surfaceBloodDrop, closestCollider);
        }

        public static void OnBloodDropHitSurface(ref SurfaceBloodDrop surfaceBloodDrop, Collider collider)
        {
            surfaceBloodDrop.SurfaceCollider = new SurfaceCollider(collider, Vector3.zero);
            surfaceBloodDrop.Position = collider.transform.InverseTransformPoint(surfaceBloodDrop.Position);

            var rb = collider.attachedRigidbody;
            if (!rb) return;

            if (rb.TryGetComponent(out RagdollPart ragdollPart))
            {
                surfaceBloodDrop.DisposeWithCreature = ragdollPart.ragdoll.creature;
            }
        }
    }
}
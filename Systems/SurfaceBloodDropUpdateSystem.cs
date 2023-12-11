using System;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class SurfaceBloodDropUpdateSystem : BaseSystem
    {
        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;
        private readonly FastList<FallingBloodDrop> _fallingBloodDrops;

        // Optimization
        private int _optimizationCurrentIndex;

        // Velocity Randomization
        private const float NoiseMaxAngle = 6;
        private const float NoiseScale = 20;

        // Physics
        private const float SurfaceDrag = 45;

        private static readonly Collider[] Colliders = new Collider[32];

        private readonly SphereCollider _collider;

        [ModOptionCategory("Multipliers", 2)]
        [ModOption("Blood Surface Friction",
            "Controls the amount of surface friction applied to blood droplets.\n" +
            "Lower friction means blood droplets will move faster.",
            valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
            defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 23)]
        private static float BloodSurfaceFrictionMultiplier = 1;

        // Blood dripping
        private const float MaxVelocityToDrip = 0.1f;

        public SurfaceBloodDropUpdateSystem(FastList<SurfaceBloodDrop> surfaceBloodDrops,
            FastList<FallingBloodDrop> fallingBloodDrops, SphereCollider collider)
        {
            _surfaceBloodDrops = surfaceBloodDrops;
            _fallingBloodDrops = fallingBloodDrops;
            _collider = collider;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            if (_surfaceBloodDrops.Count == 0) return;

            for (var i = 0; i < _surfaceBloodDrops.Count; i++)
            {
                ref var bloodDrop = ref _surfaceBloodDrops[i];

                bloodDrop.LifetimeRemaining -= deltaTime;

                if (bloodDrop.LifetimeRemaining < 0)
                {
                    _surfaceBloodDrops.RemoveAtSwapBack(i--);
                }
            }
            
            var updateCount = 0;

            _optimizationCurrentIndex %= _surfaceBloodDrops.Count;

            var startIndex = _optimizationCurrentIndex;

            do
            {
                if (UpdateBloodDrop(ref _optimizationCurrentIndex, deltaTime))
                {
                    updateCount++;
                }

                _optimizationCurrentIndex++;

                if (_optimizationCurrentIndex >= _surfaceBloodDrops.Count)
                {
                    _optimizationCurrentIndex = 0;
                }
            } while (updateCount < MaxActiveBloodDrops && _optimizationCurrentIndex != startIndex && _surfaceBloodDrops.Count > 0);
        }

        private bool UpdateBloodDrop(ref int index, float deltaTime)
        {
            ref var bloodDrop = ref _surfaceBloodDrops[index];

            ref var surfaceCollider = ref bloodDrop.SurfaceCollider;

            if (surfaceCollider.Collider == null)
            {
                _surfaceBloodDrops.RemoveAtSwapBack(index--);

                return false;
            }

            bloodDrop.ShouldRenderDecal = true;

            // Velocity randomization

            var randomMultiplier =
                Mathf.Acos(
                    Mathf.Clamp01(Mathf.Abs(Vector3.Dot(surfaceCollider.LastNormal, Physics.gravity.normalized))));
            randomMultiplier *= Mathf.InverseLerp(0, 0.1f, bloodDrop.Velocity.magnitude);

            var distance = surfaceCollider.DistanceTravelled * NoiseScale;

            var randomRotation = new Vector3
            {
                x = Mathf.PerlinNoise(distance, 0) * 2f,
                y = Mathf.PerlinNoise(0, distance) * 2f,
                z = Mathf.PerlinNoise(-distance, 0) * 2f
            };

            randomRotation -= Vector3.one;

            randomRotation *= randomMultiplier;
            randomRotation *= NoiseMaxAngle;

            bloodDrop.Velocity = Quaternion.Euler(randomRotation) * bloodDrop.Velocity;

            // Physics

            var worldPos = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);

            var prevPos = worldPos;

            bloodDrop.Velocity += Physics.gravity * deltaTime;

            Depenetrate(ref worldPos, EntryPoint.SurfaceLayerMask, ref bloodDrop, ref surfaceCollider);

            bloodDrop.Velocity *= 1 - Time.deltaTime * SurfaceDrag * BloodSurfaceFrictionMultiplier;

            worldPos += bloodDrop.Velocity * deltaTime;

            if (!Depenetrate(ref worldPos, EntryPoint.SurfaceLayerMask, ref bloodDrop, ref surfaceCollider))
            {
                var closestPoint = surfaceCollider.Collider.ClosestPoint(worldPos);

                surfaceCollider.LastNormal = (worldPos - closestPoint).normalized;

                AssignNewSurfaceValues(ref bloodDrop, ref surfaceCollider, closestPoint, surfaceCollider.Collider);

                worldPos = closestPoint;
            }

            var surfaceSpeed = (prevPos - worldPos).magnitude;
            surfaceCollider.DistanceTravelled += surfaceSpeed;

            // Blood dripping
            ref var dripTime = ref bloodDrop.DripTime;

            if (surfaceSpeed < MaxVelocityToDrip * deltaTime &&
                Vector3.Dot(surfaceCollider.LastNormal, Physics.gravity.normalized) > 0)
            {
                dripTime.Remaining -= deltaTime;

                if (dripTime.Remaining <= 0)
                {
                    bloodDrop.Position = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);

                    var rb = surfaceCollider.Collider.attachedRigidbody;
                    bloodDrop.Velocity = rb ? rb.GetPointVelocity(bloodDrop.Position) : Vector3.zero;

                    _fallingBloodDrops.TryAdd(new FallingBloodDrop(bloodDrop.Position, bloodDrop.Velocity,
                        bloodDrop.Size, bloodDrop.LifetimeRemaining));

                    _surfaceBloodDrops.RemoveAtSwapBack(index--);

                    return false;
                }
            }
            else
            {
                dripTime.Remaining = dripTime.Total;
            }

            return true;
        }

        private bool Depenetrate(ref Vector3 worldPos, LayerMask surfaceLayerMask,
            ref SurfaceBloodDrop surfaceBloodDrop,
            ref SurfaceCollider surfaceCollider)
        {
            var any = false;

            var count = Physics.OverlapSphereNonAlloc(worldPos, _collider.radius, Colliders, surfaceLayerMask,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var col = Colliders[i];

                var colTransform = col.transform;

                if (Physics.ComputePenetration(_collider, worldPos, Quaternion.identity,
                        col, colTransform.position, colTransform.rotation,
                        out var direction, out var distance))
                {
                    if (Mathf.Abs(distance) < 0.001f) continue;

                    var offset = direction * distance;

                    if (!AnyNaN(offset))
                    {
                        worldPos += offset;

                        surfaceBloodDrop.Velocity = Vector3.ProjectOnPlane(surfaceBloodDrop.Velocity, direction);

                        AssignNewSurfaceValues(ref surfaceBloodDrop, ref surfaceCollider, worldPos, col);

                        any = true;
                    }
                }
            }

            if (any)
            {
                var velocityDir = surfaceBloodDrop.Velocity.normalized;
                var gravityDir = Physics.gravity.normalized;
                var tangent = Vector3.Cross(velocityDir, gravityDir).normalized;

                surfaceCollider.LastNormal = Vector3.Cross(velocityDir, tangent);
            }

            return any;
        }

        private void AssignNewSurfaceValues(ref SurfaceBloodDrop surfaceBloodDrop, ref SurfaceCollider surfaceCollider,
            Vector3 point,
            Collider collider)
        {
            surfaceCollider.Collider = collider;
            surfaceBloodDrop.Position = collider.transform.InverseTransformPoint(point);
        }

        private static bool AnyNaN(Vector3 vector3)
        {
            return float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z);
        }

        private static ModOptionInt[] GetMaxActiveBloodDropValues()
        {
            Span<int> values = stackalloc int[] { 5, 10, 20, 30, 40, 50, 75, 100, 1000 };

            var array = new ModOptionInt[values.Length];

            for (var i = 0; i < array.Length; i++)
            {
                var value = values[i];
                array[i] = new ModOptionInt(value.ToString(), value);
            }

            return array;
        }

        [ModOptionCategory("Performance", 1)]
        [ModOption("Max Active Blood Drops",
            "The max number of blood drops that can be updated each frame.\n" +
            "If the number of blood drops exceeds this, the blood simulation will slow down to maintain performance.",
            order = 10, valueSourceName = nameof(GetMaxActiveBloodDropValues), defaultValueIndex = 2)]
        private static int MaxActiveBloodDrops = 20;
    }
}
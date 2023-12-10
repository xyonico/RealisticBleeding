using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class FallingBloodDropSystem : BaseSystem
    {
        private readonly FastList<FallingBloodDrop> _fallingBloodDrops;
        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;

        public FallingBloodDropSystem(FastList<FallingBloodDrop> fallingBloodDrops,
            FastList<SurfaceBloodDrop> surfaceBloodDrops)
        {
            _fallingBloodDrops = fallingBloodDrops;
            _surfaceBloodDrops = surfaceBloodDrops;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            for (var index = 0; index < _fallingBloodDrops.Count; index++)
            {
                ref var bloodDrop = ref _fallingBloodDrops[index];
                
                bloodDrop.Velocity += Physics.gravity * deltaTime;

                if (Physics.SphereCast(bloodDrop.Position, bloodDrop.Size, bloodDrop.Velocity.normalized, out var hit,
                        bloodDrop.Velocity.magnitude * deltaTime,
                        EntryPoint.SurfaceAndEnvironmentLayerMask, QueryTriggerInteraction.Ignore))
                {
                    bloodDrop.Position = hit.point;

                    _fallingBloodDrops.RemoveAtSwapBack(index);

                    if (EntryPoint.EnvironmentLayerMask.Contains(hit.collider.gameObject.layer))
                    {
                        FallingBloodDrop.OnFallingDropHitEnvironment(in bloodDrop, hit.normal);

                        continue;
                    }

                    var surfaceBloodDrop = new SurfaceBloodDrop(in bloodDrop, hit.collider);

                    _surfaceBloodDrops.TryAdd(in surfaceBloodDrop);
                }
                else
                {
                    bloodDrop.Position += bloodDrop.Velocity * deltaTime;
                    bloodDrop.LifetimeRemaining -= deltaTime;

                    if (bloodDrop.LifetimeRemaining < 0)
                    {
                        _fallingBloodDrops.RemoveAtSwapBack(index);
                    }
                }
            }
        }
    }
}
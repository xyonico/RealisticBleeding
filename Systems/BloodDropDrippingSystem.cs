using DefaultEcs;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class BloodDropDrippingSystem : BaseSystem
	{
		private const float DripTimeRequiredMin = 0.25f;
		private const float DropTimeRequiredMax = 0.75f;
		private const float MaxVelocityToDrip = 0.08f;
		
		public BloodDropDrippingSystem(EntitySet entitySet) : base(entitySet)
		{
		}

		protected override void Update(float deltaTime, in Entity entity)
		{
			ref var surfaceCollider = ref entity.Get<SurfaceCollider>();
			
			if (surfaceCollider.LastSurfaceSpeed < MaxVelocityToDrip * deltaTime && Vector3.Dot(surfaceCollider.LastNormal, Physics.gravity.normalized) > 0)
			{
				if (!entity.Has<DripTime>())
				{
					entity.Set(new DripTime(Random.Range(DripTimeRequiredMin, DropTimeRequiredMax)));
				}

				ref var dripTime = ref entity.Get<DripTime>();
				dripTime.Remaining -= deltaTime;

				if (dripTime.Remaining <= 0)
				{
					ref var bloodDrop = ref entity.Get<BloodDrop>();

					bloodDrop.Position = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);
					
					var rb = surfaceCollider.Collider.attachedRigidbody;
					bloodDrop.Velocity = rb ? rb.GetPointVelocity(bloodDrop.Position) : Vector3.zero;

					entity.Remove<SurfaceCollider>();
					entity.Remove<DisposeWithCreature>();
					entity.Remove<DripTime>();
				}
			}
			else
			{
				if (entity.Has<DripTime>())
				{
					ref var dripTime = ref entity.Get<DripTime>();
					dripTime.Remaining = dripTime.Total;
				}
			}
		}

		private struct DripTime
		{
			public readonly float Total;
			public float Remaining;

			public DripTime(float total)
			{
				Total = total;
				Remaining = total;
			}
		}
	}
}
using System;
using DefaultEcs;
using RealisticBleeding.Components;
using RealisticBleeding.Messages;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class FallingBloodDropSystem : BaseSystem
	{
		public FallingBloodDropSystem(EntitySet entitySet) : base(entitySet)
		{
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			ref var layerMasks = ref World.Get<LayerMasks>();

			foreach (var entity in entities)
			{
				ref var bloodDrop = ref entity.Get<BloodDrop>();
				
				bloodDrop.Velocity += Physics.gravity * deltaTime;

				if (Physics.SphereCast(bloodDrop.Position, bloodDrop.Size, bloodDrop.Velocity.normalized, out var hit, bloodDrop.Velocity.magnitude * deltaTime,
					layerMasks.Combined, QueryTriggerInteraction.Ignore))
				{
					bloodDrop.Position = hit.point;
					
					if (layerMasks.Environment.Contains(hit.collider.gameObject.layer))
					{
						World.Publish(new BloodDropHitEnvironment(entity, hit.collider, hit.normal));

						return;
					}

					World.Publish(new BloodDropHitSurface(entity, hit.collider));
				}
				else
				{
					bloodDrop.Position += bloodDrop.Velocity * deltaTime;
				}
			}
		}
	}
}
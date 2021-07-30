using DefaultEcs;
using RealisticBleeding.Messages;
using UnityEngine;

namespace RealisticBleeding.Components
{
	public struct BloodDrop
	{
		public Vector3 Velocity;
		public Vector3 Position;
		public float Size;

		public static Entity Spawn(Vector3 position, Vector3 velocity, float size, float lifetimeMultiplier = 1, bool attachToNearest = true)
		{
			var entity = EntryPoint.World.CreateEntity();
			entity.Set(new BloodDrop
			{
				Position = position,
				Velocity = velocity,
				Size = size
			});
			
			entity.Set(new Lifetime(Random.Range(5f, 8f) * lifetimeMultiplier));

			if (attachToNearest)
			{
				AttachToNearestCollider(entity, 0.3f);
			}

			return entity;
		}
		
		private static readonly Collider[] Colliders = new Collider[32];
		
		public static void AttachToNearestCollider(Entity entity, float maxRadius)
		{
			ref var bloodDrop = ref entity.Get<BloodDrop>();
			
			var position = bloodDrop.Position;

			var count = Physics.OverlapSphereNonAlloc(position, maxRadius, Colliders, EntryPoint.World.Get<LayerMasks>().Surface, QueryTriggerInteraction.Ignore);

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

			bloodDrop.Position = closestPoint;

			EntryPoint.World.Publish(new BloodDropHitSurface(entity, closestCollider));
		}
	}
}
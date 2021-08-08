using System;
using DefaultEcs;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDropPhysicsSystem : BaseSystem
	{
		private const float SurfaceDrag = 45;

		private static readonly Collider[] _colliders = new Collider[32];

		private readonly SphereCollider _collider;
		private readonly float _surfaceFrictionMultiplier;

		public SurfaceBloodDropPhysicsSystem(EntitySet entitySet, SphereCollider collider, float surfaceFrictionMultiplier) : base(entitySet)
		{
			_collider = collider;
			_surfaceFrictionMultiplier = surfaceFrictionMultiplier;
		}

		protected override void Update(float deltaTime, ReadOnlySpan<Entity> entities)
		{
			ref var layerMasks = ref World.Get<LayerMasks>();
			ref var deltaTimeMultiplier = ref World.Get<DeltaTimeMultiplier>();

			deltaTime *= deltaTimeMultiplier.Value;

			foreach (var entity in entities)
			{
				ref var bloodDrop = ref entity.Get<BloodDrop>();
				ref var surfaceCollider = ref entity.Get<SurfaceCollider>();

				if (surfaceCollider.Collider == null)
				{
					entity.Dispose();

					continue;
				}

				var worldPos = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);

				var prevPos = worldPos;

				bloodDrop.Velocity += Physics.gravity * deltaTime;

				Depenetrate(ref worldPos, layerMasks.Surface, ref bloodDrop, ref surfaceCollider);

				bloodDrop.Velocity *= 1 - Time.deltaTime * SurfaceDrag * _surfaceFrictionMultiplier;

				worldPos += bloodDrop.Velocity * deltaTime;

				if (!Depenetrate(ref worldPos, layerMasks.Surface, ref bloodDrop, ref surfaceCollider))
				{
					var closestPoint = surfaceCollider.Collider.ClosestPoint(worldPos);

					surfaceCollider.LastNormal = (worldPos - closestPoint).normalized;

					AssignNewSurfaceValues(ref bloodDrop, ref surfaceCollider, closestPoint, surfaceCollider.Collider);

					worldPos = closestPoint;
				}

				var speed = (prevPos - worldPos).magnitude;
				surfaceCollider.DistanceTravelled += speed;
				surfaceCollider.LastSurfaceSpeed = speed;
			}
		}

		private bool Depenetrate(ref Vector3 worldPos, LayerMask surfaceLayerMask, ref BloodDrop bloodDrop, ref SurfaceCollider surfaceCollider)
		{
			var any = false;

			var count = Physics.OverlapSphereNonAlloc(worldPos, _collider.radius, _colliders, surfaceLayerMask, QueryTriggerInteraction.Ignore);

			for (var i = 0; i < count; i++)
			{
				var col = _colliders[i];

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

						bloodDrop.Velocity = Vector3.ProjectOnPlane(bloodDrop.Velocity, direction);

						AssignNewSurfaceValues(ref bloodDrop, ref surfaceCollider, worldPos, col);

						any = true;
					}
				}
			}

			if (any)
			{
				var velocityDir = bloodDrop.Velocity.normalized;
				var gravityDir = Physics.gravity.normalized;
				var tangent = Vector3.Cross(velocityDir, gravityDir).normalized;

				surfaceCollider.LastNormal = Vector3.Cross(velocityDir, tangent);
			}

			return any;
		}

		private void AssignNewSurfaceValues(ref BloodDrop bloodDrop, ref SurfaceCollider surfaceCollider, Vector3 point, Collider collider)
		{
			surfaceCollider.Collider = collider;
			bloodDrop.Position = collider.transform.InverseTransformPoint(point);
		}

		private static bool AnyNaN(Vector3 vector3)
		{
			return float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z);
		}
	}
}
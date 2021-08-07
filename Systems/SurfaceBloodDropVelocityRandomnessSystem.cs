using DefaultEcs;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class SurfaceBloodDropVelocityRandomnessSystem : BaseSystem
	{
		private const float NoiseMaxAngle = 6;
		private const float NoiseScale = 20;

		public SurfaceBloodDropVelocityRandomnessSystem(EntitySet set) : base(set)
		{
		}

		protected override void Update(float state, in Entity entity)
		{
			ref var bloodDrop = ref entity.Get<BloodDrop>();
			ref var surfaceCollider = ref entity.Get<SurfaceCollider>();
			
			var randomMultiplier = Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(surfaceCollider.LastNormal, Physics.gravity.normalized))));
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
		}
	}
}
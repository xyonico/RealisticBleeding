using DefaultEcs;
using UnityEngine;

namespace RealisticBleeding.Messages
{
	public readonly struct BloodDropHitEnvironment
	{
		public readonly Entity Entity;
		public readonly Collider Collider;
		public readonly Vector3 Normal;

		public BloodDropHitEnvironment(Entity entity, Collider collider, Vector3 normal)
		{
			Entity = entity;
			Collider = collider;
			Normal = normal;
		}
	}
}
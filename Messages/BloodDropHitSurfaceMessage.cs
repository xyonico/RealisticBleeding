using DefaultEcs;
using UnityEngine;

namespace RealisticBleeding.Messages
{
	public readonly struct BloodDropHitSurface
	{
		public readonly Entity Entity;
		public readonly Collider Collider;

		public BloodDropHitSurface(Entity entity, Collider collider)
		{
			Entity = entity;
			Collider = collider;
		}
	}
}
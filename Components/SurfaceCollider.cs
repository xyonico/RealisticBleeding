using UnityEngine;

namespace RealisticBleeding.Components
{
	public struct SurfaceCollider
	{
		public Collider Collider;
		public Vector3 LastNormal;
		public float DistanceTravelled;

		public SurfaceCollider(Collider collider, Vector3 lastNormal)
		{
			Collider = collider;
			LastNormal = lastNormal;

			DistanceTravelled = Random.Range(-100f, 100f);
		}
	}
}
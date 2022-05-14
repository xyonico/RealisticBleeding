using UnityEngine;

namespace RealisticBleeding
{
	public static class Vector3Extensions
	{
		public static int GetVolume(in this Vector3Int vector)
		{
			return vector.x * vector.y * vector.z;
		}

		public static Vector3 ScaledBy(in this Vector3 a, Vector3 b)
		{
			return Vector3.Scale(a, b);
		}
	}
}
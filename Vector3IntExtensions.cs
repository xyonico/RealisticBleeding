using UnityEngine;

namespace RealisticBleeding
{
	public static class Vector3IntExtensions
	{
		public static int GetVolume(in this Vector3Int vector)
		{
			return vector.x * vector.y * vector.z;
		}
	}
}
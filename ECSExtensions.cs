using DefaultEcs;

namespace RealisticBleeding
{
	public static class ECSExtensions
	{
		public static ref T GetOrDefault<T>(this Entity entity, in T defaultComponent)
		{
			if (entity.Has<T>())
			{
				return ref entity.Get<T>();
			}

			entity.Set(defaultComponent);

			return ref entity.Get<T>();
		}
	}
}
using Unity.Profiling;

namespace RealisticBleeding.Systems
{
    public abstract class BaseSystem
    {
        private readonly ProfilerMarker _profilerMarker;

        public bool IsEnabled { get; set; } = true;

        public BaseSystem()
        {
            _profilerMarker =
                new ProfilerMarker(ProfilerCategory.Scripts, $"RealisticBleeding.{GetType().Name}.Update");
        }

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            using (_profilerMarker.Auto())
            {
                UpdateInternal(deltaTime);
            }
        }

        protected abstract void UpdateInternal(float deltaTime);
    }
}
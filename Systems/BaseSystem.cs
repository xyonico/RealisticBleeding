namespace RealisticBleeding.Systems
{
    public abstract class BaseSystem
    {
        public bool IsEnabled { get; set; } = true;

        public void Update(float deltaTime)
        {
            if (!IsEnabled) return;

            UpdateInternal(deltaTime);
        }

        protected abstract void UpdateInternal(float deltaTime);
    }
}
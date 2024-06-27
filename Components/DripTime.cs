namespace RealisticBleeding.Components
{
    public struct DripTime
    {
        public const float RequiredMin = 0.75f;
        public const float RequiredMax = 1.25f;
        
        public readonly float Total;
        public float Remaining;

        public DripTime(float total)
        {
            Total = total;
            Remaining = total;
        }
    }
}
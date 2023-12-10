using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class BleederSystem : BaseSystem
    {
        private const float FrequencyRangeMin = 1f;
        private const float FrequencyRangeMax = 2f;

        //[ModOptionCategory("Multipliers", 2)]
        //[ModOption("Blood Amount",
        //	"Controls how often blood droplets spawn from wounds.",
        //	valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
        //	defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 20)]
        private static float BloodAmountMultiplier = 1;

        //[ModOptionCategory("Multipliers", 2)]
        //[ModOption("Bleed Duration",
        //	"Controls how long wounds will continue spawning blood droplets.",
        //	valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
        //	defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 21)]
        internal static float BleedDurationMultiplier = 1;

        //[ModOptionCategory("Multipliers", 2)]
        //[ModOption("Blood Trail Width",
        //	"Controls the size of the trails left by blood droplets.",
        //	valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
        //	defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 22)]
        internal static float BloodStreakWidthMultiplier = 1;

        private readonly FastList<Bleeder> _bleeders;
        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;

        public BleederSystem(FastList<Bleeder> bleeders, FastList<SurfaceBloodDrop> surfaceBloodDrops)
        {
            _bleeders = bleeders;
            _surfaceBloodDrops = surfaceBloodDrops;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            for (var index = 0; index < _bleeders.Count; index++)
            {
                ref var bleeder = ref _bleeders[index];

                bleeder.LifetimeRemaining -= deltaTime;

                if (bleeder.LifetimeRemaining < 0)
                {
                    _bleeders.RemoveAtSwapBack(index);

                    continue;
                }

                bleeder.NextBleedTime -= deltaTime;

                if (bleeder.NextBleedTime <= 0)
                {
                    bleeder.NextBleedTime = Random.Range(FrequencyRangeMin, FrequencyRangeMax) /
                                            (bleeder.FrequencyMultiplier * BloodAmountMultiplier);

                    ref var dimensions = ref bleeder.Dimensions;

                    var randomOffset = new Vector3(Random.Range(-dimensions.x, dimensions.x), 0,
                        Random.Range(-dimensions.y, dimensions.y));
                    randomOffset *= 0.5f;

                    var dropPosition = bleeder.WorldPosition + bleeder.WorldRotation * randomOffset;

                    var randomVelocity = Random.insideUnitSphere * 0.75f;
                    var gravityDir = Physics.gravity.normalized;

                    if (Vector3.Dot(randomVelocity.normalized, gravityDir) > 0)
                    {
                        randomVelocity = Vector3.ProjectOnPlane(randomVelocity, gravityDir);
                    }

                    var bloodDrop =
                        new SurfaceBloodDrop(
                            new FallingBloodDrop(dropPosition, randomVelocity, 0.01f * bleeder.SizeMultiplier,
                                Random.Range(5f, 7f)), bleeder.Collider);

                    _surfaceBloodDrops.TryAdd(in bloodDrop);
                }
            }
        }
    }
}
using DefaultEcs;
using DefaultEcs.System;
using RealisticBleeding.Components;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class BleederSystem : AEntitySetSystem<float>
	{
		private const float FrequencyRangeMin = 1f;
		private const float FrequencyRangeMax = 2f;

		[ModOptionCategory("Multipliers", 2)]
		[ModOption("Blood Amount",
			"Controls how often blood droplets spawn from wounds.",
			valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
			defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 20)]
		private static float BloodAmountMultiplier { get; set; }
		
		[ModOptionCategory("Multipliers", 2)]
		[ModOption("Bleed Duration",
			"Controls how long wounds will continue spawning blood droplets.",
			valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
			defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 21)]
		internal static float BleedDurationMultiplier { get; set; }
		
		[ModOptionCategory("Multipliers", 2)]
		[ModOption("Blood Trail Width",
			"Controls the size of the trails left by blood droplets.",
			valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
			defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 22)]
		internal static float BloodStreakWidthMultiplier { get; set; }

		public BleederSystem(World world) : base(world.GetEntities().With<Bleeder>().AsSet())
		{
		}

		protected override void Update(float deltaTime, in Entity entity)
		{
			ref var bleeder = ref entity.Get<Bleeder>();

			if (entity.Has<NextBleedTime>())
			{
				ref var nextTime = ref entity.Get<NextBleedTime>();
				nextTime.Value -= deltaTime;

				if (nextTime.Value <= 0)
				{
					entity.Remove<NextBleedTime>();
				}
			}

			if (!entity.Has<NextBleedTime>())
			{
				entity.Set(new NextBleedTime(Random.Range(FrequencyRangeMin, FrequencyRangeMax) /
				                             (bleeder.FrequencyMultiplier * BloodAmountMultiplier)));

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

				BloodDrop.Spawn(dropPosition, randomVelocity, 0.01f * bleeder.SizeMultiplier);
			}
		}

		private struct NextBleedTime
		{
			public float Value;

			public NextBleedTime(float value)
			{
				Value = value;
			}
		}
	}
}
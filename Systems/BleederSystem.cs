using DefaultEcs;
using DefaultEcs.System;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class BleederSystem : AEntitySetSystem<float>
	{
		private const float FrequencyRangeMin = 0.75f;
		private const float FrequencyRangeMax = 1.75f;
		
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
				entity.Set(new NextBleedTime(Random.Range(FrequencyRangeMin, FrequencyRangeMax) / bleeder.FrequencyMultiplier));

				ref var dimensions = ref bleeder.Dimensions;
				
				var randomOffset = new Vector3(Random.Range(-dimensions.x, dimensions.x), 0, Random.Range(-dimensions.y, dimensions.y));
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
using DefaultEcs;
using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Components
{
	public struct Bleeder
	{
		public Transform Parent;
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector2 Dimensions;

		public float FrequencyMultiplier;
		public float SizeMultiplier;

		public Vector3 WorldPosition => Parent ? Parent.TransformPoint(Position) : Position;
		public Quaternion WorldRotation => Parent ? Parent.rotation * Rotation : Rotation;
		
		[ModOption(category = "Multipliers", name = "Bleed Duration",
			tooltip = "Controls how long wounds will continue spawning blood droplets.",
			valueSourceType = typeof(ModOptionPercentage), valueSourceName = nameof(ModOptionPercentage.GetDefaults),
			defaultValueIndex = ModOptionPercentage.DefaultIndex, order = 21)]
		private static float BleedDurationMultiplier { get; set; }
		
		public static Entity Spawn(Transform parent, Vector3 position, Quaternion rotation, Vector2 dimensions, float frequencyMultiplier, float sizeMultiplier,
			float durationMultiplier)
		{
			durationMultiplier *= BleedDurationMultiplier;
			
			var entity = EntryPoint.World.CreateEntity();
			entity.Set(new Lifetime(Random.Range(3, 8f) * durationMultiplier));
			entity.Set(new Bleeder
			{
				Parent = parent,
				Position = parent ? parent.InverseTransformPoint(position) : position,
				Rotation = parent ? Quaternion.Inverse(parent.rotation) * rotation : rotation,
				Dimensions = dimensions,
				FrequencyMultiplier = frequencyMultiplier,
				SizeMultiplier = sizeMultiplier
			});

			return entity;
		}
	}
}
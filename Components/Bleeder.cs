using DefaultEcs;
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
		
		public static Entity Spawn(Transform parent, Vector3 position, Quaternion rotation, Vector2 dimensions, float frequencyMultiplier, float sizeMultiplier,
			float durationMultiplier)
		{
			durationMultiplier *= EntryPoint.Configuration.BleedDurationMultiplier;
			
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
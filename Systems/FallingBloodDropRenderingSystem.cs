using System;
using DefaultEcs;
using DefaultEcs.System;
using RealisticBleeding.Components;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class FallingBloodDropRenderingSystem : AEntitySetSystem<float>
	{
		private readonly Mesh _mesh;
		private readonly Material _material;

		public FallingBloodDropRenderingSystem(EntitySet set, Mesh mesh, Material material) : base(set)
		{
			_mesh = mesh;
			_material = material;
		}

		protected override void Update(float state, ReadOnlySpan<Entity> entities)
		{
			foreach (var entity in entities)
			{
				ref var bloodDrop = ref entity.Get<BloodDrop>();

				var magnitude = bloodDrop.Velocity.magnitude;
				var size = Mathf.Lerp(1, 3.5f, Mathf.InverseLerp(0, 4, magnitude));

				var rotation = magnitude > 0.01f ? Quaternion.LookRotation(bloodDrop.Velocity.normalized) : Quaternion.identity;

				var matrix = Matrix4x4.TRS(bloodDrop.Position, rotation, new Vector3(1, 1, size) * 0.007f);

				Graphics.DrawMesh(_mesh, matrix, _material, 0);
			}
		}
	}
}
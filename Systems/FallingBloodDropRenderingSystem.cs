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
				
				var size = Mathf.Lerp(1, 3.5f, Mathf.InverseLerp(0, 4, bloodDrop.Velocity.magnitude));
				var matrix = Matrix4x4.TRS(bloodDrop.Position, Quaternion.LookRotation(bloodDrop.Velocity), new Vector3(1, 1, size) * bloodDrop.Size);

				Graphics.DrawMesh(_mesh, matrix, _material, 0);
			}
		}
	}
}
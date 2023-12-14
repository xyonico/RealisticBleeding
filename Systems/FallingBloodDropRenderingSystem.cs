using ThunderRoad;
using UnityEngine;

namespace RealisticBleeding.Systems
{
	public class FallingBloodDropRenderingSystem : BaseSystem
	{
		private readonly FastList<FallingBloodDrop> _fallingBloodDrops;
		private readonly Mesh _mesh;
		
		private Material _material;

		private bool _firstFrame = true;

		public FallingBloodDropRenderingSystem(FastList<FallingBloodDrop> fallingBloodDrops, Mesh mesh)
		{
			_fallingBloodDrops = fallingBloodDrops;
			_mesh = mesh;
		}

		protected override void UpdateInternal(float deltaTime)
		{
			if (_firstFrame)
			{
				_firstFrame = false;
				
				Catalog.LoadAssetAsync("RealisticBleeding/BloodDrop",
					(Material material) => { _material = material; }, null);
			}
			
			for (var index = 0; index < _fallingBloodDrops.Count; index++)
			{
				ref var bloodDrop = ref _fallingBloodDrops[index];

				var magnitude = bloodDrop.Velocity.magnitude;
				var size = Mathf.Lerp(1, 3.5f, Mathf.InverseLerp(0, 4, magnitude));

				var rotation = magnitude > 0.01f ? Quaternion.LookRotation(bloodDrop.Velocity.normalized) : Quaternion.identity;

				var matrix = Matrix4x4.TRS(bloodDrop.Position, rotation, new Vector3(1, 1, size) * 0.007f);

				Graphics.DrawMesh(_mesh, matrix, _material, 0);
			}
		}
	}
}
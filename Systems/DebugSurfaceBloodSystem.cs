using UnityEngine;

namespace RealisticBleeding.Systems
{
    public class DebugSurfaceBloodSystem : BaseSystem
    {
        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;
        private readonly Mesh _mesh;
        private readonly Material _material;

        public DebugSurfaceBloodSystem(FastList<SurfaceBloodDrop> surfaceBloodDrops, Mesh mesh, Material material)
        {
            _surfaceBloodDrops = surfaceBloodDrops;
            _mesh = mesh;
            _material = material;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            for (var index = 0; index < _surfaceBloodDrops.Count; index++)
            {
                ref var bloodDrop = ref _surfaceBloodDrops[index];

                var normal = bloodDrop.SurfaceCollider.LastNormal;

                var rotation = normal.magnitude > 0.01f ? Quaternion.LookRotation(normal) : Quaternion.identity;

                var worldPosition = bloodDrop.SurfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);

                var scale = Vector3.one * bloodDrop.Size;

                scale.z = SurfaceBloodDecalSystem.ProjectionDepth * 2;

                var matrix = Matrix4x4.TRS(worldPosition, rotation, scale);

                Graphics.DrawMesh(_mesh, matrix, _material, 0);
            }
        }
    }
}
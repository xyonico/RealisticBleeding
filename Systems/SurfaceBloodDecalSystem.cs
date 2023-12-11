using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticBleeding.Systems
{
    public class SurfaceBloodDecalSystem : BaseSystem, IDisposable
    {
        private const float ProjectionDepth = 0.05f;

        private const float CellSize = 0.12f;
        private const int MaxBoundsDimension = 20;
        private const int MaxCellCount = MaxBoundsDimension * MaxBoundsDimension * MaxBoundsDimension;
        private const int MaxTotalBloodDrops = 4096;
        private const int MaxBloodDropsPerCell = 8;

        private static readonly int BloodDropsID = Shader.PropertyToID("_BloodDrops");
        private static readonly int CellsID = Shader.PropertyToID("_Cells");
        private static readonly int BoundsMatrixID = Shader.PropertyToID("_BoundsMatrix");
        private static readonly int BoundsDimensionsID = Shader.PropertyToID("_BoundsDimensions");
        private static readonly int BoundsVolumeID = Shader.PropertyToID("_BoundsVolume");

        private readonly Dictionary<RevealMaterialController, BloodDropGrid> _bloodDrops =
            new Dictionary<RevealMaterialController, BloodDropGrid>();

        private readonly FastList<SurfaceBloodDrop> _surfaceBloodDrops;
        private readonly ComputeBuffer _bloodDropsBuffer;
        private readonly ComputeBuffer _cellsBuffer;
        private readonly CommandBuffer _commandBuffer;

        private Material _decalMaterial;

        private bool _isFirstFrame = true;

        [ModOptionCategory("Performance", 1)]
        [ModOptionButton]
        [ModOption("Update Decals When Far Away",
            "Whether decals should be updated on low LOD models too.\nDisabling this can improve performance.",
            defaultValueIndex = 1, order = 12)]
        private static bool UpdateDecalsWhenFarAway = true;

        public SurfaceBloodDecalSystem(FastList<SurfaceBloodDrop> surfaceBloodDrops)
        {
            _surfaceBloodDrops = surfaceBloodDrops;

            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;

            _bloodDropsBuffer = new ComputeBuffer(MaxTotalBloodDrops, BloodDropGPU.SizeOf);
            _cellsBuffer = new ComputeBuffer(MaxCellCount, CellGPU.SizeOf);
            _commandBuffer = new CommandBuffer { name = "Realistic Blood - Decal Drawing" };
        }

        public void Dispose()
        {
            _bloodDropsBuffer.Dispose();
            _cellsBuffer.Dispose();
            _commandBuffer.Dispose();

            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
        }

        protected override void UpdateInternal(float deltaTime)
        {
            if (_isFirstFrame)
            {
                _isFirstFrame = false;

                Catalog.LoadAssetAsync("RealisticBloodDecal",
                    (Shader shader) => { _decalMaterial = new Material(shader); }, null);
            }

            if (ReferenceEquals(_decalMaterial, null))
            {
                return;
            }

            // Prepare draw call data for later
            for (var index = 0; index < _surfaceBloodDrops.Count; index++)
            {
                ref var bloodDrop = ref _surfaceBloodDrops[index];

                if (!bloodDrop.ShouldRenderDecal) continue;

                bloodDrop.ShouldRenderDecal = false;

                ref var surfaceCollider = ref bloodDrop.SurfaceCollider;

                var col = surfaceCollider.Collider;

                RagdollPart ragdollPart;
                if ((ragdollPart = col.GetComponentInParent<RagdollPart>()) == null) continue;

                var worldPos = surfaceCollider.Collider.transform.TransformPoint(bloodDrop.Position);
                var normal = surfaceCollider.LastNormal;

                var offset = normal * ProjectionDepth;

                var startPos = worldPos + offset;
                var endPos = worldPos - offset;

                var radius = Mathf.Clamp(bloodDrop.Size * 0.5f, 0.003f, 0.02f) *
                             BleederSystem.BloodStreakWidthMultiplier;

                var bloodDropGPU = new BloodDropGPU(startPos, endPos, radius);

                foreach (var rendererData in ragdollPart.renderers)
                {
                    if (!rendererData.revealDecal) continue;

                    var revealMaterialController = rendererData.revealDecal.revealMaterialController;

                    if (revealMaterialController)
                    {
                        var renderer = rendererData.renderer;

                        var isVisible = renderer.isVisible;

                        /*
                        if (!isVisible && UpdateDecalsWhenFarAway)
                        {
                            // Check if this renderer has other LODs and check if any of those are visible.
                            if (renderer.TryGetComponent(out RevealDecalLODS revealDecalLODS))
                            {
                                foreach (var (lodRenderer, _) in revealDecalLODS.LODRenderers)
                                {
                                    if (lodRenderer.isVisible)
                                    {
                                        isVisible = true;

                                        break;
                                    }
                                }
                            }
                        }
                        */

                        if (!isVisible) continue;

                        var bounds = renderer.bounds;

                        if (!bounds.Contains(worldPos)) continue;

                        if (!_bloodDrops.TryGetValue(revealMaterialController, out var bloodDrops))
                        {
                            bloodDrops = BloodDropGrid.Get();
                            _bloodDrops[revealMaterialController] = bloodDrops;

                            bloodDrops.SetWorldBounds(bounds);
                        }

                        bloodDrops.Add(in bloodDropGPU, worldPos);
                    }
                }
            }
        }

        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            try
            {
                var projectionMatrix = Matrix4x4.Ortho(0, 1, 0, 1, -1, 100);

                foreach (var keyValuePair in _bloodDrops)
                {
                    _commandBuffer.Clear();

                    var revealMaterialController = keyValuePair.Key;
                    var bloodDropsGrid = keyValuePair.Value;

                    bloodDropsGrid.StoreIntoBuffers(_commandBuffer, _bloodDropsBuffer, _cellsBuffer);

                    var shouldClear = revealMaterialController.ActivateRevealMaterials();

                    _commandBuffer.SetProjectionMatrix(projectionMatrix);
                    _commandBuffer.SetRenderTarget(revealMaterialController.MaskTexture);
                    _commandBuffer.SetGlobalBuffer(BloodDropsID, _bloodDropsBuffer);
                    _commandBuffer.SetGlobalBuffer(CellsID, _cellsBuffer);
                    _commandBuffer.SetGlobalMatrix(BoundsMatrixID, bloodDropsGrid.Matrix);

                    Vector4 boundsDimensions = (Vector3)bloodDropsGrid.Dimensions;
                    _commandBuffer.SetGlobalVector(BoundsDimensionsID, boundsDimensions);
                    _commandBuffer.SetGlobalInt(BoundsVolumeID, bloodDropsGrid.Dimensions.GetVolume());

                    if (shouldClear)
                    {
                        _commandBuffer.ClearRenderTarget(false, true, Color.clear);
                    }

                    var renderer = revealMaterialController.GetRenderer();
                    var submeshCount = revealMaterialController.GetSubMeshCount();

                    for (var submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
                    {
                        _commandBuffer.DrawRenderer(renderer, _decalMaterial, submeshIndex);
                    }

                    context.ExecuteCommandBuffer(_commandBuffer);
                }

                // Clean up
                foreach (var bloodDropGrid in _bloodDrops.Values)
                {
                    bloodDropGrid.Dispose();
                }

                _bloodDrops.Clear();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private struct BloodDropGPU
        {
            public const int SizeOf = sizeof(float) * 8;

            public Vector4 StartPosAndRadius;
            public Vector4 EndPos;

            public BloodDropGPU(Vector3 startPos, Vector3 endPos, float radius)
            {
                StartPosAndRadius = startPos;
                EndPos = endPos;

                var inverseSquareRadius = 1 / (radius * radius);

                StartPosAndRadius.w = inverseSquareRadius;
            }
        }

        private struct CellGPU
        {
            public const int SizeOf = sizeof(float) * 2;

            public float StartIndex;
            public float Count;

            public CellGPU(int startIndex, int count)
            {
                StartIndex = startIndex;
                Count = count;
            }
        }

        private class BloodDropGrid : IDisposable
        {
            //private static readonly ObjectPool<BloodDropGrid> Pool = new ObjectPool<BloodDropGrid>(null, null);

            private static readonly Vector2[] CellArray = new Vector2[MaxCellCount];

            // I use a Matrix4x4 here instead of BloodDropGPU because Nomad doesn't support user-defined types in ComputeBuffers
            // So I use a built-in type here instead. Matrix4x4 is twice the size of BloodDropGPU, so I store two drops in each.
            private static readonly Matrix4x4[] BloodDropsArray = new Matrix4x4[MaxTotalBloodDrops / 2];

            private static readonly Vector3Int MaxGridSize =
                new Vector3Int(MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension);

            private readonly List<BloodDropGPU>[,,] _grid =
                new List<BloodDropGPU>[MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension];

            public Matrix4x4 Matrix { get; private set; }
            public Vector3Int Dimensions { get; private set; }

            public int BloodDropCount { get; private set; }

            static BloodDropGrid()
            {
                /*
                // Prewarm
                const int prewarmCount = 24;
                var list = new List<BloodDropGrid>(prewarmCount);

                for (var i = 0; i < prewarmCount; i++)
                {
                    list.Add(Pool.Get());
                }

                foreach (var bloodDropGrid in list)
                {
                    Pool.Release(bloodDropGrid);
                }

                list.Clear();
                */
            }

            public static BloodDropGrid Get()
            {
                var grid = new BloodDropGrid();

                return grid;
            }

            public void SetWorldBounds(Bounds bounds)
            {
                var size = bounds.size;
                var min = bounds.min;

                var dim = Vector3Int.CeilToInt(new Vector3(size.x / CellSize, size.y / CellSize,
                    size.z / CellSize));

                dim = Vector3Int.Min(dim,
                    new Vector3Int(MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension));

                Dimensions = dim;
                Matrix = Matrix4x4.TRS(min, Quaternion.identity, size).inverse;
            }

            public void Add(in BloodDropGPU bloodDrop, Vector3 worldPos)
            {
                var boundsStartPos = Vector3.Scale(Dimensions, Matrix.MultiplyPoint3x4(worldPos));

                var startPosCoords = Vector3Int.FloorToInt(boundsStartPos);

                startPosCoords = Vector3Int.Max(Vector3Int.zero, startPosCoords);
                startPosCoords = Vector3Int.Min(MaxGridSize, startPosCoords);

                AddToCell(in bloodDrop, startPosCoords);

                BloodDropCount++;
            }

            private void AddToCell(in BloodDropGPU bloodDropGPU, Vector3Int cell)
            {
                ref var list = ref _grid[cell.x, cell.y, cell.z];

                if (list == null)
                {
                    list = new List<BloodDropGPU>();
                }

                list.Add(bloodDropGPU);
            }

            private void Clear()
            {
                BloodDropCount = 0;

                for (var x = 0; x < Dimensions.x; x++)
                {
                    for (var y = 0; y < Dimensions.y; y++)
                    {
                        for (var z = 0; z < Dimensions.z; z++)
                        {
                            _grid[x, y, z]?.Clear();
                        }
                    }
                }
            }

            public void StoreIntoBuffers(CommandBuffer commandBuffer, ComputeBuffer bloodDropsBuffer, ComputeBuffer cellsBuffer)
            {
                var dropsIndex = 0;

                var dimensions = Dimensions;
                var totalCells = dimensions.x * dimensions.y * dimensions.z;

                var bloodDropsSpan = MemoryMarshal.Cast<Matrix4x4, BloodDropGPU>(BloodDropsArray);

                for (var z = 0; z < dimensions.z; z++)
                {
                    for (var y = 0; y < dimensions.y; y++)
                    {
                        for (var x = 0; x < dimensions.x; x++)
                        {
                            var flatIndex = GetFlattenedIndex(x, y, z);
                            var drops = _grid[x, y, z];

                            var cellGPU = new CellGPU(dropsIndex, 0);

                            if (drops != null && drops.Count > 0)
                            {
                                var maxCount = MaxTotalBloodDrops - dropsIndex;
                                var count = Mathf.Min(drops.Count, Mathf.Min(maxCount, MaxBloodDropsPerCell));

                                for (var i = 0; i < count; i++)
                                {
                                    bloodDropsSpan[dropsIndex + i] = drops[i];
                                }

                                dropsIndex += count;
                                cellGPU.Count = (uint)count;
                            }

                            CellArray[flatIndex] = new Vector2(cellGPU.StartIndex, cellGPU.Count);
                        }
                    }
                }

                commandBuffer.SetBufferData(bloodDropsBuffer, BloodDropsArray, 0, 0, dropsIndex);
                commandBuffer.SetBufferData(cellsBuffer, CellArray, 0, 0, totalCells);
            }

            private int GetFlattenedIndex(int x, int y, int z)
            {
                return z * Dimensions.x * Dimensions.y + y * Dimensions.x + x;
            }

            public void Dispose()
            {
                Clear();

                //Pool.Release(this);
            }
        }
    }
}
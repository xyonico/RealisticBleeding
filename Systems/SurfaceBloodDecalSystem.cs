using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ThunderRoad;
using ThunderRoad.Reveal;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RealisticBleeding.Systems
{
    public class SurfaceBloodDecalSystem : IDisposable
    {
        public const float ProjectionDepth = 0.05f;

        private const float CellSize = 0.12f;
        private const int MaxBoundsDimension = 10;
        private const int MaxCellCount = MaxBoundsDimension * MaxBoundsDimension * MaxBoundsDimension;
        private const int MaxTotalBloodDrops = 4096;
        private const int MaxBloodDropsPerCell = 8;

        private static readonly Vector3Int MaxDimensions =
            new Vector3Int(MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension);

        private static readonly int BloodDropsID = Shader.PropertyToID("_BloodDrops");
        private static readonly int CellsID = Shader.PropertyToID("_Cells");
        private static readonly int BoundsMinPosition = Shader.PropertyToID("_BoundsMinPosition");
        private static readonly int BoundsWorldToLocalSize = Shader.PropertyToID("_BoundsWorldToLocalSize");
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

        private bool _enableReveal;

        /*
        [ModOptionCategory("Performance", 1)]
        [ModOptionButton]
        [ModOption("Update Decals When Far Away",
            "Whether decals should be updated on low LOD models too.\nDisabling this can improve performance.",
            defaultValueIndex = 1, order = 12)]
        private static bool UpdateDecalsWhenFarAway = true;
        */

        public SurfaceBloodDecalSystem(FastList<SurfaceBloodDrop> surfaceBloodDrops)
        {
            _surfaceBloodDrops = surfaceBloodDrops;

            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;

            _bloodDropsBuffer = new ComputeBuffer(MaxTotalBloodDrops, BloodDropGPU.SizeOf);
            _cellsBuffer = new ComputeBuffer(MaxCellCount, sizeof(float) * 2);
            _commandBuffer = new CommandBuffer { name = "Realistic Blood - Decal Drawing" };
        }

        public void Dispose()
        {
            _bloodDropsBuffer.Dispose();
            _cellsBuffer.Dispose();
            _commandBuffer.Dispose();

            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
        }

        private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
        {
            try
            {
                // Prepare draw call data
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

                if (_enableReveal)
                {
                    RevealMaskProjection.EnableReveal();

                    _enableReveal = false;
                }

                for (var index = 0; index < _surfaceBloodDrops.Count; index++)
                {
                    ref var bloodDrop = ref _surfaceBloodDrops[index];

                    if (!bloodDrop.ShouldRenderDecal) continue;

                    bloodDrop.ShouldRenderDecal = false;

                    ref var surfaceCollider = ref bloodDrop.SurfaceCollider;

                    var col = surfaceCollider.Collider;

                    var rb = col.attachedRigidbody;
                    if (!rb) continue;

                    RagdollPart ragdollPart;
                    if ((ragdollPart = rb.GetComponent<RagdollPart>()) == null) continue;

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

                            bounds.extents += new Vector3(0.05f, 0.05f, 0.05f);

                            if (!bounds.Contains(startPos) && !bounds.Contains(endPos)) continue;

                            BloodDropGrid bloodDrops;
                            if (!_bloodDrops.ContainsKey(revealMaterialController))
                            {
                                bloodDrops = BloodDropGrid.Get();
                                _bloodDrops[revealMaterialController] = bloodDrops;

                                bloodDrops.SetWorldBounds(bounds);
                            }
                            else
                            {
                                bloodDrops = _bloodDrops[revealMaterialController];
                            }

                            bloodDrops.Add(in bloodDropGPU, worldPos);
                        }
                    }
                }

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
                    _commandBuffer.SetGlobalVector(BoundsMinPosition, bloodDropsGrid.BoundsMinPosition);
                    _commandBuffer.SetGlobalVector(BoundsWorldToLocalSize, bloodDropsGrid.BoundsWorldToLocalSize);

                    Vector4 boundsDimensions = (Vector3)bloodDropsGrid.Dimensions;
                    _commandBuffer.SetGlobalVector(BoundsDimensionsID, boundsDimensions);
                    _commandBuffer.SetGlobalFloat(BoundsVolumeID, bloodDropsGrid.Dimensions.GetVolume());

                    if (shouldClear)
                    {
                        _commandBuffer.ClearRenderTarget(false, true, Color.clear);

                        _enableReveal = true;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct BloodDropGPU
        {
            public const int SizeOf = sizeof(float) * 8;

            public Vector3 StartPos;
            public float InverseSquareRadius;
            public Vector3 EndPos;
            private float _padding;

            public BloodDropGPU(Vector3 startPos, Vector3 endPos, float radius)
            {
                StartPos = startPos;
                InverseSquareRadius = 1 / (radius * radius);
                EndPos = endPos;
                _padding = 0;
            }
        }

        public class BloodDropGrid : IDisposable
        {
            private static readonly Stack<BloodDropGrid> Pool = new Stack<BloodDropGrid>();

            private static readonly Vector2[] CellArray = new Vector2[MaxCellCount];

            // I use a Matrix4x4 here instead of BloodDropGPU because Nomad doesn't support user-defined types in ComputeBuffers
            // So I use a built-in type here instead. Matrix4x4 is twice the size of BloodDropGPU, so I store two drops in each.
            private static readonly Matrix4x4[] BloodDropsArray = new Matrix4x4[MaxTotalBloodDrops / 2];

            private FastList<BloodDropInCell> _bloodDropsInCells;
            private Vector3Int _maxDimensions;

            public Vector3 BoundsMinPosition { get; private set; }
            public Vector3Int Dimensions { get; private set; }
            public Vector3 BoundsWorldToLocalSize { get; private set; }

            static BloodDropGrid()
            {
                // Prewarm
                const int prewarmCount = 24;
                const int bloodDropInitialCapacity = 16;

                for (var i = 0; i < prewarmCount; i++)
                {
                    var grid = new BloodDropGrid();
                    grid._bloodDropsInCells = new FastList<BloodDropInCell>(bloodDropInitialCapacity);

                    Pool.Push(grid);
                }
            }

            public static BloodDropGrid Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new BloodDropGrid();
            }

            public void SetWorldBounds(Bounds bounds)
            {
                var size = bounds.size;

                var dim = Vector3Int.CeilToInt(new Vector3(size.x / CellSize, size.y / CellSize,
                    size.z / CellSize));

                dim = Vector3Int.Min(dim,
                    new Vector3Int(MaxBoundsDimension, MaxBoundsDimension, MaxBoundsDimension));

                Dimensions = dim;
                _maxDimensions = dim - Vector3Int.one;
                BoundsMinPosition = bounds.min;

                BoundsWorldToLocalSize = new Vector3(1 / (size.x / dim.x), 1 / (size.y / dim.y), 1 / (size.z / dim.z));
            }

            public void Add(in BloodDropGPU bloodDrop, Vector3 worldPos)
            {
                if (TryConvertWorldToGrid(worldPos, out var centerGridPos))
                {
                    AddToCell(in bloodDrop, centerGridPos);
                }

                if (TryConvertWorldToGrid(bloodDrop.StartPos, out var startGridPos))
                {
                    if (startGridPos != centerGridPos)
                    {
                        AddToCell(in bloodDrop, startGridPos);
                    }
                }

                if (TryConvertWorldToGrid(bloodDrop.EndPos, out var endGridPos))
                {
                    if (endGridPos != centerGridPos && endGridPos != startGridPos)
                    {
                        AddToCell(in bloodDrop, endGridPos);
                    }
                }
            }

            private bool TryConvertWorldToGrid(Vector3 worldPos, out Vector3Int gridPosition)
            {
                var localPos = Vector3.Scale(worldPos - BoundsMinPosition, BoundsWorldToLocalSize);

                gridPosition = Vector3Int.FloorToInt(localPos);

                gridPosition = Vector3Int.Max(Vector3Int.zero, gridPosition);
                gridPosition = Vector3Int.Min(_maxDimensions, gridPosition);

                return true;
            }

            private void AddToCell(in BloodDropGPU bloodDropGPU, Vector3Int cell)
            {
                var bloodDropInCell = new BloodDropInCell(cell, bloodDropGPU);

                if (_bloodDropsInCells == null)
                {
                    _bloodDropsInCells = new FastList<BloodDropInCell>(16);
                }

                _bloodDropsInCells.InsertIntoSortedList(bloodDropInCell);
            }

            private void Clear()
            {
                _bloodDropsInCells?.Clear();
            }

            public void StoreIntoBuffers(CommandBuffer commandBuffer, ComputeBuffer bloodDropsBuffer,
                ComputeBuffer cellsBuffer)
            {
                var dropsIndex = 0;

                var dimensions = Dimensions;
                var totalCells = dimensions.x * dimensions.y * dimensions.z;

                Array.Clear(CellArray, 0, CellArray.Length);

                var currentDropCount = 0;

                if (_bloodDropsInCells != null)
                {
                    var bloodDropsSpan = MemoryMarshal.Cast<Matrix4x4, BloodDropGPU>(BloodDropsArray);

                    var prevCellCoord = Vector3Int.zero;

                    for (var i = 0; i < _bloodDropsInCells.Count; i++)
                    {
                        var bloodDropInCell = _bloodDropsInCells[i];

                        var cell = bloodDropInCell.Cell;

                        if (cell != prevCellCoord)
                        {
                            if (currentDropCount > 0)
                            {
                                var flatIndex = GetFlattenedIndex(prevCellCoord, dimensions);
                                CellArray[flatIndex] = new Vector2(dropsIndex, currentDropCount);
                            }

                            prevCellCoord = cell;
                            dropsIndex += currentDropCount;
                            currentDropCount = 0;
                        }

                        if (currentDropCount >= MaxBloodDropsPerCell) continue;

                        bloodDropsSpan[dropsIndex + currentDropCount++] = bloodDropInCell.BloodDrop;
                    }

                    if (currentDropCount > 0)
                    {
                        var flatIndex = GetFlattenedIndex(prevCellCoord, dimensions);
                        CellArray[flatIndex] = new Vector2(dropsIndex, currentDropCount);
                    }
                }

                var totalDrops = dropsIndex + currentDropCount;

                if (totalDrops > 0)
                {
                    commandBuffer.SetBufferData(bloodDropsBuffer, BloodDropsArray, 0, 0,
                        Mathf.CeilToInt(totalDrops / 2f));
                }

                commandBuffer.SetBufferData(cellsBuffer, CellArray, 0, 0, totalCells);
            }

            private static int GetFlattenedIndex(Vector3Int coord, Vector3Int dimensions)
            {
                return coord.z * dimensions.x * dimensions.y + coord.y * dimensions.x + coord.x;
            }

            public void Dispose()
            {
                Clear();
                Pool.Push(this);
            }

            private readonly struct BloodDropInCell : IComparable<BloodDropInCell>
            {
                public readonly Vector3Int Cell;
                public readonly BloodDropGPU BloodDrop;

                public BloodDropInCell(Vector3Int cell, BloodDropGPU bloodDrop)
                {
                    Cell = cell;
                    BloodDrop = bloodDrop;
                }

                public int CompareTo(BloodDropInCell other)
                {
                    var index = GetFlattenedIndex(Cell, MaxDimensions);
                    var otherIndex = GetFlattenedIndex(other.Cell, MaxDimensions);

                    return index.CompareTo(otherIndex);
                }
            }
        }
    }
}
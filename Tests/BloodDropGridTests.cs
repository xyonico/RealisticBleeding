using RealisticBleeding.Systems;
using UnityEngine;

namespace Tests;

public class BloodDropGridTests
{
    [Test]
    public void BloodDropGrid_Works()
    {
        var grid = new SurfaceBloodDecalSystem.BloodDropGrid();
        
        grid.SetWorldBounds(new Bounds(Vector3.zero, Vector3.one * 2));

        var bloodDrop = default(SurfaceBloodDecalSystem.BloodDropGPU);
        
        grid.Add(bloodDrop, Vector3.zero);
        grid.Add(bloodDrop, Vector3.one);
        grid.Add(bloodDrop, -Vector3.one);
        grid.Add(bloodDrop, Vector3.forward);
        grid.Add(bloodDrop, Vector3.one);
        grid.Add(bloodDrop, Vector3.zero);
        grid.Add(bloodDrop, -Vector3.one);
        grid.Add(bloodDrop, Vector3.forward);
        
        grid.StoreIntoBuffers(null, null, null);
    }
}
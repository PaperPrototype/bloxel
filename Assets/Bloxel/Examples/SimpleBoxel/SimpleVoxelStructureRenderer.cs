using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bloxel;
using System;

public class SimpleVoxelStructureRenderer : VoxelStructureRenderer
{
    public VoxelTypesAsset<SimpleVoxelType> voxelTypes;

    public override Color VoxelColor(byte id)
    {
        return voxelTypes[id].color;
    }
}

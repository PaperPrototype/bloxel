using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bloxel;
using System;

[Serializable]
public class SimpleVoxelType : IVoxelType<SimpleVoxelType>
{
    public string name;
    public Color color = Color.white;
    public byte group;

    public bool MeshFaceTowardsNeighbor(SimpleVoxelType neighbor)
    {
        return this.group != neighbor.group;
    }
}

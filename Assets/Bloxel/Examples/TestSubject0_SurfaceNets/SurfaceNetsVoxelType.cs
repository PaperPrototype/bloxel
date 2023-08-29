using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Bloxel;

[Serializable]
public class SurfaceNetsVoxelType : IVoxelType<SurfaceNetsVoxelType>
{
    public SurfaceNetsVoxelType()
    {
        this.color = Color.red;
    }

    public SurfaceNetsVoxelType(Color color)
    {
        this.name = "missing voxel type";
        this.color = color;
        this.material = 0;
        this.group = 0;
    }

    public string name;
    public Color color;
    public byte material;
    public byte group;

    public float4 float4Color
    {
        get
        {
            return float4(color.r, color.g, color.b, color.a);
        }
    }

    public bool MeshFaceTowardsNeighbor(SurfaceNetsVoxelType neighbor)
    {
        return this.group != neighbor.group;
    }
}
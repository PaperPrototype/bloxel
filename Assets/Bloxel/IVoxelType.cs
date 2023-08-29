using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Bloxel
{
    public interface IVoxelType<TVoxelType>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MeshFaceTowardsNeighbor(TVoxelType neighbor);
    }
}
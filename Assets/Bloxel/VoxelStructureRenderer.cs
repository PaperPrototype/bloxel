using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Bloxel
{
    [Serializable]
    public abstract class VoxelStructureRenderer : MonoBehaviour
    {
        public abstract Color VoxelColor(byte id);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using System;

namespace Bloxel
{
    // defaults to air
    public struct Voxel
    {
        public static readonly Voxel Air = new Voxel(0);
        public static readonly Voxel Solid = new Voxel(1);

        /// <summary>
        /// defaults voxel to air (not solid)
        /// </summary>
        /// <param name="isSolid"></param>
        public Voxel(byte id = 0)
        {
            this.id = id;
        }

        public byte id;
    }
}
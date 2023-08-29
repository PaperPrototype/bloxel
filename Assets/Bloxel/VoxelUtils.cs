using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using System;

namespace Bloxel
{
    public static class VoxelUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index3D(int x, int y, int z)
        {
            return (x * VoxelTables.ChunkResolution * VoxelTables.ChunkHeight) + (y * VoxelTables.ChunkResolution) + z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBufferBounds(int x, int y, int z)
        {
            // if outside of bounds
            if (x < 0 || x > VoxelTables.ChunkResolution - 1 ||
                y < 0 || y > VoxelTables.ChunkHeight     - 1 ||
                z < 0 || z > VoxelTables.ChunkResolution - 1)
            {
                return false;
            }

            // else inside of bounds
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBufferBoundsResolution(int xORz)
        {
            // if outside of bounds
            if (xORz < 0 || xORz > VoxelTables.ChunkResolution - 1)
            {
                return false;
            }

            // else inside of bounds
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinBufferBoundsHeight(int y)
        {
            // if outside of bounds
            if (y < 0 || y > VoxelTables.ChunkHeight - 1)
            {
                return false;
            }

            // else inside of bounds
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInOuterBuffers(int x, int y, int z)
        {
            if (IsWithinBufferBoundsHeight(y))
            {
                // right
                if (x == VoxelTables.ChunkResolution && IsWithinBufferBoundsResolution(z))
                {
                    return true;
                }

                // left
                if (x == -1 && IsWithinBufferBoundsResolution(z))
                {
                    return true;
                }

                // front
                if (z == VoxelTables.ChunkResolution && IsWithinBufferBoundsResolution(x))
                {
                    return true;
                }

                // back
                if (z == -1 && IsWithinBufferBoundsResolution(x))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StringID(int3 id)
        {
            return $"{id.x}_{id.y}_{id.z}";
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Bloxel
{
    public static class VoxelTables
    {
        public static readonly int MaxDistanceFromOrigin = 100000;
        public static readonly int ChunkResolution = 16;
        public static readonly int ChunkHeight = 256;

        // all 8 possible vertices for a voxel
        public static readonly Vector3[] Vertices = new Vector3[8]
        {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 1.0f),
        };

        // vertices to build a quad for each side of a voxel
        public static readonly int[,] QuadVerticesIndex = new int[6, 4]
        {
            // quad order
            // right, left, up, down, front, back

            // 0 1 2 2 1 3 <- triangle numbers

            // quads
            {1, 2, 5, 6}, // right quad
            {4, 7, 0, 3}, // left quad

            {3, 7, 2, 6}, // up quad
            {1, 5, 0, 4}, // down quad

            {5, 6, 4, 7}, // front quad
            {0, 3, 1, 2}, // back quad
        };

        public static readonly float3[] Normals = new float3[6]
        {
            new float3( 1,  0,  0), // right
            new float3(-1,  0,  0), // left
            new float3( 0,  1,  0), // up
            new float3( 0, -1,  0), // down
            new float3( 0,  0,  1), // front
            new float3( 0,  0, -1), // back
        };

        // offset to neighboring voxel position
        public static readonly Vector3Int[] VoxelNeighborOffsets = new Vector3Int[6]
        {
            new Vector3Int( 1,  0,  0), // right
            new Vector3Int(-1,  0,  0), // left
            new Vector3Int( 0,  1,  0), // up
            new Vector3Int( 0, -1,  0), // down
            new Vector3Int( 0,  0,  1), // front
            new Vector3Int( 0,  0, -1), // back
        };

        // offset to neighboring voxel chunks
        public static readonly int3[] ChunkNeighborOffsetID = new int3[6]
        {
            new int3( 1,  0,  0), // right (0)
            new int3(-1,  0,  0), // left  (1)
            new int3( 0,  1,  0), // up    (2)
            new int3( 0, -1,  0), // down  (3)
            new int3( 0,  0,  1), // front (4)
            new int3( 0,  0, -1), // back  (5)
        };

        // offset to neighboring voxel chunks
        public static readonly int3[] ChunkNeighborOffsetIDXZ = new int3[4]
        {
            new int3( 1,  0,  0), // right (0)
            new int3(-1,  0,  0), // left  (1)
            new int3( 0,  0,  1), // front (2)
            new int3( 0,  0, -1), // back  (3)
        };

        // offset to neighboring voxel chunks
        public static readonly int[] BufferOppositeNeighborIndexXZ = new int[4]
        {
            1, // left
            0, // right
            3, // back
            2, // front
        };

        // offset to neighboring buffers
        public static readonly int[] BufferNeighborsIndexFromFaceIndex = new int[6]
        {
            0, // right
            1, // left
            0, // up (no buffer)
            0, // down (no buffer)
            2, // front
            3, // back
        };
    }
}
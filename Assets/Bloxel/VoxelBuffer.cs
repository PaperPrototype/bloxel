using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using System.Threading;
using Unity.Mathematics;

namespace Bloxel
{
    public class VoxelBuffer
    {
        protected NativeArray<Voxel> buffer;

        [ReadOnly]
        protected VoxelBuffer[] neighbors = new VoxelBuffer[4];

        private bool ready = false;
        private bool disposed = false;

        public VoxelBuffer(Allocator allocator)
        {
            this.buffer = new NativeArray<Voxel>(VoxelTables.ChunkResolution * VoxelTables.ChunkHeight * VoxelTables.ChunkResolution, allocator);
        }

        public VoxelBuffer(Allocator allocator, VoxelBuffer bufferToCopyFrom)
        {
            this.neighbors = bufferToCopyFrom.neighbors;
            this.buffer = new NativeArray<Voxel>(bufferToCopyFrom.buffer, allocator);
            this.ready = true;
        }

        public void CopyFrom(VoxelBuffer buffer)
        {
            this.buffer.CopyFrom(buffer.buffer);
            this.ready = true;
        }

        public bool Ready
        {
            get
            {
                return ready;
            }
        }

        public int Length
        {
            get
            {
                return buffer.Length;
            }
        }

        public bool Disposed
        {
            get
            {
                return disposed;
            }
        }

        public void Neighbor(VoxelBuffer buffer, int bufferNeighborIndex)
        {
            neighbors[bufferNeighborIndex] = buffer;
        }

        /// <summary>
        /// Attempts to get a voxel in a neighbor buffer
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        public Voxel Get(int x, int y, int z, int bufferNeighborIndex)
        {
            if (neighbors[bufferNeighborIndex] != null && !neighbors[bufferNeighborIndex].Disposed)
            {
                return neighbors[bufferNeighborIndex].buffer[VoxelUtils.Index3D(x, y, z)];
            }

            return Voxel.Air;
        }

        public Voxel Get(int x, int y, int z)
        {
            return this.buffer[VoxelUtils.Index3D(x, y, z)];
        }

        public void Set(int x, int y, int z, Voxel voxel)
        {
            this.buffer[VoxelUtils.Index3D(x, y, z)] = voxel;
        }

        public void Dispose()
        {
            this.buffer.Dispose();
            this.disposed = true;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using System;

namespace Bloxel
{
    public abstract class VoxelGenerator : MonoBehaviour
    {
        public bool EnableLogs = false;

        private FastNoiseLite noise = new FastNoiseLite();

        private struct StructureStart
        {
            public int3 start;
            public VoxelStructure structure;
        }

        public IEnumerator GetVoxels(int3 id, Action<VoxelBuffer> callback)
        {
            if (EnableLogs)
                Debug.Log("Generating voxels for " + VoxelUtils.StringID(id));

            VoxelBuffer buffer = new VoxelBuffer(Allocator.Persistent);

            Vector3Int offset = new Vector3Int(id.x * VoxelTables.ChunkResolution, id.y * VoxelTables.ChunkHeight, id.z * VoxelTables.ChunkResolution);

            Task t = Task.Factory.StartNew(delegate
            {
                List<StructureStart> structures = new List<StructureStart>();

                for (int x = 0; x < VoxelTables.ChunkResolution; x++)
                {
                    for (int z = 0; z < VoxelTables.ChunkResolution; z++)
                    {
                        // only calculate base height once for each (X, Z) coordinate
                        float baseHeight = BaseHeight(x + offset.x, z + offset.z);

                        float maxAdditonalHeight = Mathf.Clamp(VoxelTables.ChunkHeight - baseHeight, 0, VoxelTables.ChunkHeight);

                        byte biomeID;
                        float finalHeight = BiomeHeight(x + offset.x, z + offset.z, baseHeight, maxAdditonalHeight, out biomeID);

                        for (int y = 0; y < VoxelTables.ChunkHeight; y++)
                        {
                            Vector3 pos = new Vector3(x, y, z) + offset;
                            buffer.Set(x, y, z, new Voxel(VoxelID(pos, finalHeight, biomeID)));
                        }

                        VoxelStructure structure;
                        if (VoxelStructureAt(x + offset.x, Mathf.FloorToInt(finalHeight), z + offset.z, biomeID, finalHeight, out structure))
                        {
                            if (structure != null)
                            {
                                structures.Add(new StructureStart
                                {
                                    start = new int3 { x = x, y = Mathf.FloorToInt(finalHeight), z = z },
                                    structure = structure,
                                });
                            }
                        }
                    }
                }

                // Set voxel structure voxels
                for (int i = 0; i < structures.Count; i++)
                {
                    int3 start = structures[i].start;
                    for (int v = 0; v < structures[i].structure.voxels.Count; v++)
                    {
                        int3 index = start + structures[i].structure.voxels[v].index;
                        byte id = structures[i].structure.voxels[v].id;

                        if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
                        {
                            Voxel voxel = buffer.Get(index.x, index.y, index.z);
                            if (voxel.id == 0) // if it is air
                            {
                                buffer.Set(index.x, index.y, index.z, new Voxel { id = id });
                            }
                        }
                    }
                }
            });

            yield return new WaitUntil(() => { return t.IsCompleted; });

            if (t.Exception != null)
            {
                Debug.LogError(t.Exception);
            }

            callback(buffer);

            buffer.Dispose();
        }

        protected virtual bool VoxelStructureAt(int x,int y, int z, byte biomeID, float height, out VoxelStructure structure)
        {
            structure = null;
            return false;
        }

        protected virtual byte VoxelID(Vector3 pos, float terrainHeight, byte biomeID)
        {
            bool ground = pos.y < terrainHeight;
            if (ground)
            {
                return 1;
            }

            return 0;
        }

        protected virtual float BiomeHeight(int x, int z, float baseHeight, float maxAdditionalHeight, out byte biomeID)
        {
            biomeID = 0;
            return baseHeight;
        }

        protected virtual float BaseHeight(int x, int z)
        {
            return VoxelTables.ChunkHeight / 2;
        }

        // get noise height in range of floorHeight to maxHeight
        public float GetNoiseHeight(float scale, float maxHeight, float floorHeight, float x, float z)
        {
            return // floorHeight to maxHeight
            (
                (
                    (
                        noise.GetNoise(x / scale, z / scale)    // range -1           to 1
                        + 1                     // range  0           to 2
                    ) / 2                       // range  0           to 1
                ) * (maxHeight - floorHeight)   // range  0           to (maxHeight - floorHeight)
            ) + floorHeight;                    /* range  floorHeight to ((maxHeight - floorHeight) + floorHeight) 
                                             * simplifies ===> floorHeight to maxHeight 
                                             */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <param name="scaleZ"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>range 0 to 1</returns>
        public float GetNoise(float scaleX, float scaleY, float scaleZ, float x, float y, float z)
        {

            return (noise.GetNoise(x / scaleX, y / scaleY, z / scaleZ) + 1 ) / 2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleZ"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns>range 0 to 1</returns>
        public float GetNoise(float scaleX, float scaleZ, float x, float z)
        {

            return (noise.GetNoise(x / scaleX, z / scaleZ) + 1) / 2;
        }

        public float GetNoiseOctaves(float[] scales, float x, float z)
        {
            float result = 0;

            for (int i = 0; i < scales.Length; i++)
            {
                float offset = noise.GetNoise(scales[i] / 10, scales[i] / 10) * 100;
                result += (noise.GetNoise((x + offset) / scales[i], (z + offset) / scales[i]) + 1) / 2;
            }

            return result / scales.Length;
        }

        public float GetRidgedNoiseOctaves(float[] scales, float x, float z)
        {
            float result = 0f;

            for (int i = 0; i < scales.Length; i++)
            {
                float offset = noise.GetNoise(scales[i] / 10, scales[i] / 10) * 100;
                result += (math.abs(noise.GetNoise((x + offset) / scales[i], (z + offset) / scales[i])) * -1) + 1;
            }

            return result / (float)scales.Length;
        }

        public float GetRidgedNoiseOctavesInverse(float[] scales, float x, float z)
        {
            float result = 0f;

            for (int i = 0; i < scales.Length; i++)
            {
                float offset = noise.GetNoise(scales[i] / 10, scales[i] / 10) * 100;
                result += math.abs(noise.GetNoise((x + offset) / scales[i], (z + offset) / scales[i]));
            }

            return result / (float)scales.Length;
        }
    }
}
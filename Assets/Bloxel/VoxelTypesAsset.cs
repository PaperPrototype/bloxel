using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Bloxel
{
    public abstract class VoxelTypesAsset<TVoxelType> : ScriptableObject where TVoxelType : IVoxelType<TVoxelType>, new()
    {
        public Material[] materials;
        [SerializeField] TVoxelType[] voxelTypes;

        /// <summary>
        /// Get a voxel type in the VoxelTypesAsset
        /// </summary>
        /// <param name="id">The voxelID</param>
        /// <returns>returns a VoxelType of TVoxelType</returns>
        public TVoxelType this[int id]
        {
            get
            {
                if (id > voxelTypes.Length)
                {
                    Debug.LogWarning("No VoxelType with that id. Add more VoxelTypes in the inspector");
                }
                // 0 == air (id of 0 is taken and cannot be defined, therefore we subtract 1)
                return voxelTypes[id - 1];
            }
        }
    }
}
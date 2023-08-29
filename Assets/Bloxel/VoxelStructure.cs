using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEditor;
using Unity.VisualScripting;

namespace Bloxel
{
    [ExecuteInEditMode]
    public class VoxelStructure : MonoBehaviour
    {
        [Serializable]
        public struct StructureVoxel
        {
            public int3 index;
            public byte id;
        }

        [SerializeReference]
        public VoxelStructureRenderer structureRenderer;
        
        public List<StructureVoxel> voxels;

        private float distance = 10;
        private Vector3 focus;
        [SerializeField, Min(1)] private byte currentVoxelID = 1;

        public void Add()
        {
            int3 rounded = new int3(
                Mathf.RoundToInt(focus.x - transform.position.x),
                Mathf.RoundToInt(focus.y - transform.position.y),
                Mathf.RoundToInt(focus.z - transform.position.z)
            );

            HashSet<int3> indexes = new HashSet<int3>();

            for (int i = 0; i < voxels.Count; i++)
            {
                indexes.Add(voxels[i].index);

                // no block can be air
                if (voxels[i].id <= 0)
                {
                    StructureVoxel voxel = new StructureVoxel { id = 1, index = voxels[i].index };
                    voxels[i] = voxel;
                }
            }

            if (!indexes.Contains(rounded))
            {
                voxels.Add(new StructureVoxel { index = rounded , id = currentVoxelID });
            }

            EditorUtility.SetDirty(this);
        }

        public void Remove()
        {
            int3 rounded = new int3(
                Mathf.RoundToInt(focus.x - transform.position.x),
                Mathf.RoundToInt(focus.y - transform.position.y),
                Mathf.RoundToInt(focus.z - transform.position.z)
            );

            List<StructureVoxel> tempVoxels = new List<StructureVoxel>();
            for (int i = 0; i < voxels.Count; i++)
            {
                if (voxels[i].index.x == rounded.x &&
                    voxels[i].index.y == rounded.y &&
                    voxels[i].index.z == rounded.z)
                {
                    // do nothing
                } else
                {
                    tempVoxels.Add(voxels[i]);
                }
            }

            voxels = tempVoxels;

            EditorUtility.SetDirty(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (Input.GetKey(KeyCode.LeftShift))
                Debug.Log("Add");

            var cam = Camera.current;
            if (cam != null)
            {
                focus = cam.transform.position + (cam.transform.forward * distance);
            }

            if (structureRenderer != null)
            {
                for (int i = 0; i < voxels.Count; i++)
                {
                    Gizmos.color = structureRenderer.VoxelColor(voxels[i].id);
                    Gizmos.DrawWireCube(transform.position + new Vector3(voxels[i].index.x, voxels[i].index.y, voxels[i].index.z), Vector3.one);
                }
            } else
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < voxels.Count; i++)
                {
                    Gizmos.DrawWireCube(transform.position + new Vector3(voxels[i].index.x, voxels[i].index.y, voxels[i].index.z), Vector3.one);
                }
            }

            Gizmos.color = Color.red;
            Vector3 rounded = new Vector3(
                Mathf.RoundToInt(focus.x),
                Mathf.RoundToInt(focus.y),
                Mathf.RoundToInt(focus.z)
            );
            Gizmos.DrawCube(rounded, Vector3.one);
        }
    }
}
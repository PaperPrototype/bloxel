using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Threading.Tasks;
using System;

namespace Bloxel
{
    public class VoxelStore : MonoBehaviour
    {
        private class Storable
        {
            public Storable()
            {
                this.buffer = new VoxelBuffer(Allocator.Persistent);
            }

            public void CopyFrom(VoxelBuffer buffer)
            {
                this.buffer.CopyFrom(buffer);
            }

            // check if isRemoved == true before rendering the storable
            // (edge case)
            public bool isRemoved = false;
            public Coroutine whenNeighborsReadyCoroutine = null;
            public VoxelBuffer buffer = null;
        }

        public bool EnableLogs = false;
        public VoxelGenerator voxelGenerator;
        public VoxelRenderer voxelRenderer;
        public int chunksCount = 0;

        private Dictionary<int3, Storable> store = new Dictionary<int3, Storable>();

        private void Awake()
        {
            if (voxelGenerator == null)
            {
                Debug.LogWarning("VoxelGenerator reference in VoxelStore is null");
            }
            if (voxelRenderer == null)
            {
                Debug.LogWarning("VoxelRenderer reference in VoxelStore is null");
            }
        }

        private void Update()
        {
            chunksCount = store.Count;
        }

        public int3[] GetIDs()
        {
            var keys = new int3[store.Keys.Count];

            int i = 0;
            foreach (var key in store.Keys)
            {
                keys[i] = key;
                i++;
            }

            return keys;
        }

        #region VoxelAPI
        public bool RaycastPlaceVoxel(Camera camera, Voxel voxel)
        {
            if (EnableLogs)
                Debug.Log("RaycastPlaceVoxel...");

            RaycastHit hitInfo;

            Ray rayOrigin = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(rayOrigin, out hitInfo))
            {
                Debug.Log("Raycast hit object " + hitInfo.transform.name + " at the position " + hitInfo.point);

                Vector3 aboveSurface = hitInfo.point - (hitInfo.normal / 2);

                SetVoxel(aboveSurface, voxel);
                return true;
            }

            return false;
        }

        public bool RaycastRemoveVoxel(Camera camera)
        {
            if (EnableLogs)
                Debug.Log("RaycastRemoveVoxel...");

            RaycastHit hitInfo;

            Ray rayOrigin = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(rayOrigin, out hitInfo))
            {
                Debug.Log("Raycast hit object " + hitInfo.transform.name + " at the position " + hitInfo.point);

                Vector3 beneathSurface = hitInfo.point - (hitInfo.normal / 2);

                SetVoxel(beneathSurface, Voxel.Air);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set a voxel in the world at a given position
        /// </summary>
        /// <param name="position">The position of the voxel to edit</param>
        /// <returns>Returns the previous voxel. If no voxel exists returns default Air voxel.</returns>
        public void SetVoxel(Vector3 position, Voxel voxel)
        {
            int3 storableID = GetStorableIDFromPosition(position);

            UpdateStorable(storableID, (buffer) =>
            {
                int3 voxelIndex = GetVoxelIndexFromPosition(position);
                buffer.Set(voxelIndex.x, voxelIndex.y, voxelIndex.z, voxel);
            });
        }

        /// <summary>
        /// get a voxel in the world at a given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Voxel GetVoxel(Vector3 position)
        {
            int3 storableID = GetStorableIDFromPosition(position);

            Voxel voxel = new Voxel();
            UpdateStorable(storableID, (buffer) =>
            {
                int3 voxelIndex = GetVoxelIndexFromPosition(position);
                voxel = buffer.Get(voxelIndex.x, voxelIndex.y, voxelIndex.z);
            });

            return voxel;
        }

        private int3 GetStorableIDFromPosition(Vector3 position)
        {
            return new int3
            (
                Mathf.FloorToInt(position.x / VoxelTables.ChunkResolution),
                0,
                Mathf.FloorToInt(position.z / VoxelTables.ChunkResolution)
            );
        }

        private int3 GetVoxelIndexFromPosition(Vector3 position)
        {
            var chunkPosition = RoundToChunkPosition(position);

            return new int3
            (
                Mathf.FloorToInt(position.x - chunkPosition.x),
                Mathf.FloorToInt(position.y - chunkPosition.y),
                Mathf.FloorToInt(position.z - chunkPosition.z)
            );
        }

        private Vector3Int RoundToChunkPosition(Vector3 position)
        {
            return new Vector3Int
            (
                Mathf.FloorToInt(position.x / VoxelTables.ChunkResolution) * VoxelTables.ChunkResolution,
                0,
                Mathf.FloorToInt(position.z / VoxelTables.ChunkResolution) * VoxelTables.ChunkResolution
            );
        }
        #endregion

        #region Store API
        public void LoadStorable(int3 id)
        {
            if (EnableLogs)
                Debug.Log("VoxelStore... LoadStorable");

            if (store.ContainsKey(id))
            {
                return;
            }

            Storable storable = new Storable();

            store.Add(id, storable);

            StartCoroutine(voxelGenerator.GetVoxels(id, (tempBuffer) =>
            {
                Storable storable;
                if (store.TryGetValue(id, out storable))
                {
                    // copy buffer into storable (storable is set to: Ready = true
                    storable.CopyFrom(tempBuffer);

                    // link buffers to neighboring buffers for inter-chunk optimiziation
                    SetNeighborBuffers(id, storable.buffer);

                    // start coroutine to only render this chunk if its neighbors buffers have been completed
                    storable.whenNeighborsReadyCoroutine = StartCoroutine(RenderWhenNeighborsReady(storable, id, storable.buffer));
                }
            }));
        }

        // TODO save chunk if changes were made to it
        public void UnloadStorableUnsafe(int3 id)
        {
            if (EnableLogs)
                Debug.Log("VoxelStore... UnloadStorable");

            Storable storable;
            if (store.Remove(id, out storable))
            {
                storable.isRemoved = true;

                voxelRenderer.OffloadUnsafe(id);

                RemoveBufferFromNeighbors(id);

                storable.buffer.Dispose();
            }
        }

        public IEnumerator UnloadStorableAsync(int3 id)
        {
            if (EnableLogs)
                Debug.Log("VoxelStore... UnloadStorable");

            Storable storable;
            if (store.Remove(id, out storable))
            {
                storable.isRemoved = true;

                yield return voxelRenderer.OffloadAsync(id);

                RemoveBufferFromNeighbors(id);

                storable.buffer.Dispose();
            }
        }

        public void UpdateStorable(int3 id, Action<VoxelBuffer> callback)
        {
            if (EnableLogs)
                Debug.Log("VoxelStore... UpdateStorable");

            Storable chunk;
            if (store.TryGetValue(id, out chunk))
            {
                callback(chunk.buffer);
                voxelRenderer.RenderVoxels(id, chunk.buffer);
            }
            else
            {
                Debug.LogWarning("Attempted to update " + VoxelUtils.StringID(id) + ". Can't edit a chunk that is not loaded into the store");
            }
        }

        private IEnumerator RenderWhenNeighborsReady(Storable storable, int3 id, VoxelBuffer buffer)
        {
            yield return new WaitUntil(() => { return NeighborsReady(id); });

            if (!storable.isRemoved)
                voxelRenderer.RenderVoxels(id, buffer);
        }

        private bool NeighborsReady(int3 id)
        {
            return
                IsReady(id + VoxelTables.ChunkNeighborOffsetIDXZ[0]) &&
                IsReady(id + VoxelTables.ChunkNeighborOffsetIDXZ[1]) &&
                IsReady(id + VoxelTables.ChunkNeighborOffsetIDXZ[2]) &&
                IsReady(id + VoxelTables.ChunkNeighborOffsetIDXZ[3]);

        }

        private bool IsReady(int3 id)
        {
            Storable storable;
            if (store.TryGetValue(id, out storable))
            {
                return storable.buffer.Ready;
            }

            return false;
        }

        private void SetNeighborBuffers(int3 id, VoxelBuffer buffer)
        {
            for (int bufferNeighborIndex = 0; bufferNeighborIndex < 4; bufferNeighborIndex++)
            {
                Storable chunk;
                if (store.TryGetValue(id + VoxelTables.ChunkNeighborOffsetIDXZ[bufferNeighborIndex], out chunk))
                {
                    // new -> neighbor
                    buffer.Neighbor(chunk.buffer, bufferNeighborIndex);

                    // neighbor -> new
                    chunk.buffer.Neighbor(buffer, VoxelTables.BufferOppositeNeighborIndexXZ[bufferNeighborIndex]);
                }
            }
        }

        private void RemoveBufferFromNeighbors(int3 id)
        {
            for (int bufferNeighborIndex = 0; bufferNeighborIndex < 4; bufferNeighborIndex++)
            {
                Storable neighbor;
                if (store.TryGetValue(id + VoxelTables.ChunkNeighborOffsetID[bufferNeighborIndex], out neighbor))
                {
                    // neighbor -> this null (since we will delete ourself
                    neighbor.buffer.Neighbor(null, VoxelTables.BufferOppositeNeighborIndexXZ[bufferNeighborIndex]);
                }
            }
        }
        #endregion

        #region Methods
        private void OnDisable()
        {
            var ids = GetIDs();
            foreach (int3 id in ids)
            {
                UnloadStorableUnsafe(id);
            }
        }
        #endregion
    }
}
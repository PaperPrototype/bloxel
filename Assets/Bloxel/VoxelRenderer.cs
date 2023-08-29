using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Runtime.CompilerServices;

namespace Bloxel
{
    public abstract class VoxelRenderer : MonoBehaviour
    {
        [SerializeField] private bool EnableLogs = false;

        private Dictionary<int3, Renderable> renderables = new Dictionary<int3, Renderable>();

        private void Awake()
        {
            // make sure renderer doesn't move from world origin
            transform.position = Vector3.zero;
        }

        #region public API
        public void RenderVoxels(int3 id, VoxelBuffer buffer)
        {
            if (EnableLogs)
                Debug.Log("Starting new voxel render " + VoxelUtils.StringID(id));

            Renderable tempRenderable = GetRenderable(id);

            // SCHEDULE RENDER
            tempRenderable.meshVoxelsCoroutine = StartCoroutine(ScheduleRender(id, tempRenderable, buffer, (id, optionMeshes) => {
                // completed render if renderable hasn't been removed
                Renderable renderable;
                if (renderables.TryGetValue(id, out renderable))
                {
                    renderable.SetMeshLayers(optionMeshes);
                }

                if (EnableLogs)
                    Debug.Log("Scheduled voxel render " + VoxelUtils.StringID(renderable.id));
            }));
        }

        /// <summary>
        /// Does not wait for coroutine to finish. Could result in memory leaks if the application doesn't wait for the render task to complete
        /// </summary>
        /// <param name="id"></param>
        public void OffloadUnsafe(int3 id)
        {
            Renderable renderable;
            if (renderables.Remove(id, out renderable))
            {
                Destroy(renderable.gameObject);
            }
        }

        /// <summary>
        /// Wait for the coroutine to finish to ensure there are no memory leaks.
        /// Hint: inside of an IEnumerator do -> "yield return Offload(id)" this will wait for Offload to finish
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IEnumerator OffloadAsync(int3 id)
        {
            Renderable renderable;
            if (renderables.Remove(id, out renderable))
            {
                if (renderable.meshVoxelsCoroutine != null)
                    yield return renderable.meshVoxelsCoroutine;

                Destroy(renderable.gameObject);
            }
        }
        #endregion

        #region Modular API
        protected abstract Material[] GetMaterials();
        protected abstract IEnumerator Render(VoxelBuffer buffer, Action<OptionMesh[]> callback);
        #endregion

        #region Inherited API
        public static bool IsVoxelSolidWithinBufferBounds(VoxelBuffer buffer, Vector3Int index)
        {
            // if within chunk bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
            {
                return buffer.Get(index.x, index.y, index.z).id > 0;
            }

            // air
            return false;
        }

        public static bool IsVoxelSolidInnerAndOuterBounds(VoxelBuffer buffer, Vector3Int index)
        {
            // if within chunk bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
            {
                return buffer.Get(index.x, index.y, index.z).id > 0;
            }

            if (VoxelUtils.IsWithinBufferBoundsHeight(index.y))
            {
                // right
                if (index.x == VoxelTables.ChunkResolution && VoxelUtils.IsWithinBufferBoundsResolution(index.z))
                {
                    return buffer.Get(
                        0,
                        index.y,
                        index.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[0] // right = 0
                    ).id > 0;
                }

                // left
                if (index.x == -1 && VoxelUtils.IsWithinBufferBoundsResolution(index.z))
                {
                    return buffer.Get(
                        VoxelTables.ChunkResolution - 1,
                        index.y,
                        index.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[1] // left = 1
                    ).id > 0;
                }

                // front
                if (index.z == VoxelTables.ChunkResolution && VoxelUtils.IsWithinBufferBoundsResolution(index.x))
                {
                    return buffer.Get(
                        index.x,
                        index.y,
                        0,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[4] // front = 4
                    ).id > 0;
                }

                // back
                if (index.z == -1 && VoxelUtils.IsWithinBufferBoundsResolution(index.x))
                {
                    return buffer.Get(
                        index.x,
                        index.y,
                        VoxelTables.ChunkResolution - 1,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[5] // back = 5
                    ).id > 0;
                }
            }

            // not solid, air
            return false;
        }

        public static bool IsVoxelSolidInnerBounds(VoxelBuffer buffer, Vector3Int index)
        {
            // if within chunk bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
            {
                return buffer.Get(index.x, index.y, index.z).id > 0;
            }

            // not solid, air
            return false;
        }

        public static bool IsVoxelSolidInnerAndOuterBounds<T>(VoxelBuffer buffer, Vector3Int index, VoxelTypesAsset<T> voxelTypes, out T voxel) where T : IVoxelType<T>, new()
        {
            // if within chunk bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
            {
                byte id = buffer.Get(index.x, index.y, index.z).id;

                if (id > 0)
                    voxel = voxelTypes[id];
                else
                    voxel = new(); // air
                return id > 0;
            }

            if (VoxelUtils.IsWithinBufferBoundsHeight(index.y))
            {
                // right
                if (index.x == VoxelTables.ChunkResolution && VoxelUtils.IsWithinBufferBoundsResolution(index.z))
                {
                    byte id = buffer.Get(
                        0,
                        index.y,
                        index.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[0] // right = 0
                    ).id;

                    if (id > 0)
                        voxel = voxelTypes[id];
                    else
                        voxel = new(); // air
                    return id > 0;
                }

                // left
                if (index.x == -1 && VoxelUtils.IsWithinBufferBoundsResolution(index.z))
                {
                    byte id = buffer.Get(
                        VoxelTables.ChunkResolution - 1,
                        index.y,
                        index.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[1] // left = 1
                    ).id;

                    if (id > 0)
                        voxel = voxelTypes[id];
                    else
                        voxel = new(); // air
                    return id > 0;
                }

                // front
                if (index.z == VoxelTables.ChunkResolution && VoxelUtils.IsWithinBufferBoundsResolution(index.x))
                {
                    byte id = buffer.Get(
                        index.x,
                        index.y,
                        0,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[4] // front = 4
                    ).id;

                    if (id > 0)
                        voxel = voxelTypes[id];
                    else
                        voxel = new(); // air
                    return id > 0;
                }

                // back
                if (index.z == -1 && VoxelUtils.IsWithinBufferBoundsResolution(index.x))
                {
                    byte id = buffer.Get(
                        index.x,
                        index.y,
                        VoxelTables.ChunkResolution - 1,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[5] // back = 5
                    ).id;

                    if (id > 0)
                        voxel = voxelTypes[id];
                    else
                        voxel = new(); // air
                    return id > 0;
                }
            }

            voxel = new();

            // not solid, air
            return false;
        }

        public static bool IsVoxelSolidInnerBounds<T>(VoxelBuffer buffer, Vector3Int index, VoxelTypesAsset<T> voxelTypes, out T voxel) where T : IVoxelType<T>, new()
        {
            // if within chunk bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z))
            {
                byte id = buffer.Get(index.x, index.y, index.z).id;

                if (id > 0)
                    voxel = voxelTypes[id];
                else
                    voxel = new(); // air
                return id > 0;
            }

            voxel = new();

            // not solid, air
            return false;
        }

        private static T GetOuterBufferVoxel<T>(VoxelTypesAsset<T> voxelTypes, VoxelBuffer buffer, Vector3Int index) where T : IVoxelType<T>, new()
        {
            // right
            if (index.x == VoxelTables.ChunkResolution)
            {
                return voxelTypes[buffer.Get(
                    0,
                    index.y,
                    index.z,
                    VoxelTables.BufferNeighborsIndexFromFaceIndex[0] // right = 0
                ).id];
            }

            // left
            if (index.x == -1)
            {
                return voxelTypes[buffer.Get(
                    VoxelTables.ChunkResolution - 1,
                    index.y,
                    index.z,
                    VoxelTables.BufferNeighborsIndexFromFaceIndex[1] // left = 1
                ).id];
            }

            // front
            if (index.z == VoxelTables.ChunkResolution)
            {
                return voxelTypes[buffer.Get(
                    index.x,
                    index.y,
                    0,
                    VoxelTables.BufferNeighborsIndexFromFaceIndex[4] // front = 4
                ).id];
            }

            // back
            if (index.z == -1)
            {
                return voxelTypes[buffer.Get(
                    index.x,
                    index.y,
                    VoxelTables.ChunkResolution - 1,
                    VoxelTables.BufferNeighborsIndexFromFaceIndex[5] // back = 5
                ).id];
            }

            return new();
        }

        public static bool MeshFaceTowardsNeighbor<T>(VoxelTypesAsset<T> voxelTypes, VoxelBuffer buffer, Vector3Int index, int face) where T : IVoxelType<T>, new()
        {
            // neighbor index
            Vector3Int neighbor = new Vector3Int(index.x, index.y, index.z) + VoxelTables.VoxelNeighborOffsets[face];

            // if neighbor and current voxel is within buffer bounds
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z) && VoxelUtils.IsWithinBufferBounds(neighbor.x, neighbor.y, neighbor.z))
            {
                // this voxel
                T voxel = voxelTypes[buffer.Get(index.x, index.y, index.z).id];

                byte neighborID = buffer.Get(neighbor.x, neighbor.y, neighbor.z).id;
                return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
            }

            // if this voxel is within buffer bounds (but neighbor is from a neighbor buffer)
            if (VoxelUtils.IsWithinBufferBounds(index.x, index.y, index.z) && VoxelUtils.IsInOuterBuffers(neighbor.x, neighbor.y, neighbor.z))
            {
                T voxel = voxelTypes[buffer.Get(index.x, index.y, index.z).id];

                // right
                if (face == 0)
                {
                    byte neighborID = buffer.Get(
                        0,
                        neighbor.y,
                        neighbor.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[face]
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // left
                if (face == 1)
                {
                    byte neighborID = buffer.Get(
                        VoxelTables.ChunkResolution - 1,
                        neighbor.y,
                        neighbor.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[face]
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // front
                if (face == 4)
                {
                    byte neighborID = buffer.Get(
                        neighbor.x,
                        neighbor.y,
                        0,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[face]
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // back
                if (face == 5)
                {
                    byte neighborID = buffer.Get(
                        neighbor.x,
                        neighbor.y,
                        VoxelTables.ChunkResolution - 1,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[face]
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }
            }

            // if this voxels is in the outer buffer bounds but the neighbor is within buffer bounds
            if (VoxelUtils.IsInOuterBuffers(index.x, index.y, index.z) && VoxelUtils.IsWithinBufferBounds(neighbor.x, neighbor.y, neighbor.z))
            {
                T voxel = GetOuterBufferVoxel(voxelTypes, buffer, index);
                byte neighborID = buffer.Get(neighbor.x, neighbor.y, neighbor.z).id;
                return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
            }

            // if both voxels are in the outer buffer bounds
            if (VoxelUtils.IsInOuterBuffers(index.x, index.y, index.z) && VoxelUtils.IsInOuterBuffers(neighbor.x, neighbor.y, neighbor.z))
            {
                T voxel = GetOuterBufferVoxel(voxelTypes, buffer, index);

                // right
                if (neighbor.x == VoxelTables.ChunkResolution)
                {
                    byte neighborID = buffer.Get(
                        0,
                        neighbor.y,
                        neighbor.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[0] // right = 0
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // left
                if (neighbor.x == -1)
                {
                    byte neighborID = buffer.Get(
                        VoxelTables.ChunkResolution - 1,
                        neighbor.y,
                        neighbor.z,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[1] // left = 1
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // front
                if (neighbor.z == VoxelTables.ChunkResolution)
                {
                    byte neighborID = buffer.Get(
                        neighbor.x,
                        neighbor.y,
                        0,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[4] // front = 4
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }

                // back
                if (neighbor.z == -1)
                {
                    byte neighborID = buffer.Get(
                        neighbor.x,
                        neighbor.y,
                        VoxelTables.ChunkResolution - 1,
                        VoxelTables.BufferNeighborsIndexFromFaceIndex[5] // back = 5
                    ).id;

                    return neighborID == 0 || voxel.MeshFaceTowardsNeighbor(voxelTypes[neighborID]);
                }
            }

            // don't mesh outside of this chunk
            return false;
        }
        #endregion

        #region Internal private Methods
        private IEnumerator ScheduleRender(int3 id, Renderable renderable, VoxelBuffer tempBuffer, Action<int3, OptionMesh[]> callback)
        {
            // if the chunk had a running coroutine
            if (renderable.meshVoxelsCoroutine != null)
            {
                if (EnableLogs)
                    Debug.Log("Waiting for previous voxel render to finish " + VoxelUtils.StringID(renderable.id));

                // wait for render to finish! (prevent duplicate or undisposed NativeArrays)
                yield return renderable.meshVoxelsCoroutine;
            }

            // if by the time we finish the tempBuffer is empty then we should stop
            if (!tempBuffer.Disposed)
            {
                renderable.buffer = new VoxelBuffer(Allocator.Persistent, tempBuffer);

                yield return Render(renderable.buffer, (optionMeshes) =>
                {
                    callback(id, optionMeshes);

                    if (EnableLogs)
                        Debug.Log("Completed voxel render " + VoxelUtils.StringID(renderable.id));
                });

                renderable.buffer.Dispose();
            }
        }

        private Renderable GetRenderable(int3 id)
        {
            Renderable renderable;

            // if it exists
            if (renderables.TryGetValue(id, out renderable))
            {
                return renderable;
            }

            renderable = new Renderable(id, this.transform, GetMaterials());

            renderables.Add(id, renderable);

            return renderable;
        }
        #endregion

        private class Renderable
        {
            public Renderable(int3 id, Transform parent, Material[] materials)
            {
                // create new renderable
                GameObject g = new GameObject(VoxelUtils.StringID(id));
                g.transform.parent = parent;
                g.transform.position = new Vector3(id.x * VoxelTables.ChunkResolution, id.y * VoxelTables.ChunkHeight, id.z * VoxelTables.ChunkResolution);

                this.id = id;
                this.gameObject = g;

                /* TODO remove this ______________________________
                this.meshRenderer = g.AddComponent<MeshRenderer>();
                this.meshFilter = g.AddComponent<MeshFilter>();
                this.meshCollider = g.AddComponent<MeshCollider>();
                this.meshRenderer.materials = materials;
                //______________________________________*/

                this.materials = materials;
            }

            public int3 id;
            public GameObject gameObject;
            public VoxelBuffer buffer;
            public Coroutine meshVoxelsCoroutine;
            public Material[] materials;
            public MeshLayer[] meshLayers;

            private bool meshLayersInitialized = false;

            /* TODO move to layers system____
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
            public MeshCollider meshCollider;
            //______________________________*/

            public void SetMeshLayers(OptionMesh[] optionMeshes)
            {
                if (!meshLayersInitialized)
                {
                    meshLayers = new MeshLayer[optionMeshes.Length];

                    for (int i = 0; i < optionMeshes.Length; i++)
                    {
                        meshLayers[i] = new MeshLayer(this.gameObject.transform, optionMeshes[i], materials, i);
                    }
                    meshLayersInitialized = true;
                }

                for (int i = 0; i < optionMeshes.Length; i++)
                {
                    if (optionMeshes[i].option == MeshOptions.Collision)
                    {
                        meshLayers[i].meshCollider.sharedMesh = optionMeshes[i].mesh;
                    }
                    else if (optionMeshes[i].option == MeshOptions.Render)
                    {
                        meshLayers[i].meshFilter.mesh = optionMeshes[i].mesh;
                    }
                    else if (optionMeshes[i].option == MeshOptions.CollisionAndRender)
                    {
                        meshLayers[i].meshFilter.mesh = optionMeshes[i].mesh;
                        meshLayers[i].meshCollider.sharedMesh = optionMeshes[i].mesh;
                    }
                    else if (optionMeshes[i].option == MeshOptions.Trigger)
                    {
                        meshLayers[i].meshCollider.sharedMesh = optionMeshes[i].mesh;
                    }
                    else if (optionMeshes[i].option == MeshOptions.TriggerAndRender)
                    {
                        meshLayers[i].meshFilter.mesh = optionMeshes[i].mesh;
                        meshLayers[i].meshCollider.sharedMesh = optionMeshes[i].mesh;
                    }
                }
            }

            public class MeshLayer
            {
                public GameObject gameObject;
                public MeshFilter meshFilter;
                public MeshRenderer meshRenderer;
                public MeshCollider meshCollider;

                public MeshLayer(Transform parent, OptionMesh optionMesh, Material[] materials, int meshLayer)
                {
                    // create new renderable
                    GameObject g = new GameObject(optionMesh.option.ToString());
                    g.transform.parent = parent;
                    g.transform.localPosition = Vector3.zero;
                    this.gameObject = g;

                    if (optionMesh.option == MeshOptions.Collision)
                    {
                        this.meshCollider = g.AddComponent<MeshCollider>();
                    }
                    else if (optionMesh.option == MeshOptions.Render)
                    {
                        this.meshFilter = g.AddComponent<MeshFilter>();
                        this.meshRenderer = g.AddComponent<MeshRenderer>();
                        this.meshRenderer.materials = materials;
                    }
                    else if (optionMesh.option == MeshOptions.CollisionAndRender)
                    {
                        this.meshCollider = g.AddComponent<MeshCollider>();
                        this.meshFilter = g.AddComponent<MeshFilter>();

                        this.meshRenderer = g.AddComponent<MeshRenderer>();
                        this.meshRenderer.materials = materials;
                    }
                    else if (optionMesh.option == MeshOptions.Trigger)
                    {
                        this.meshCollider = g.AddComponent<MeshCollider>();
                        this.meshCollider.isTrigger = true;
                    }
                    else if (optionMesh.option == MeshOptions.TriggerAndRender)
                    {
                        this.meshCollider = g.AddComponent<MeshCollider>();
                        this.meshCollider.isTrigger = true;

                        this.meshFilter = g.AddComponent<MeshFilter>();
                        this.meshRenderer = g.AddComponent<MeshRenderer>();
                        this.meshRenderer.materials = materials;
                    }
                }
            }

        }
    }

    public enum MeshOptions
    {
        Collision,
        Render,
        CollisionAndRender,
        Trigger,
        TriggerAndRender,
    }

    public struct OptionMesh
    {
        public OptionMesh(MeshOptions option, Mesh mesh)
        {
            this.option = option;
            this.mesh = mesh;
        }

        public MeshOptions option;
        public Mesh mesh;
    }
}
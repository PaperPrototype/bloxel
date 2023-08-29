using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Unity.Mathematics;
using static UnityEngine.Mesh;

using Bloxel;
using UnityEngine.Rendering;
using Unity.Entities.UniversalDelegates;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class SurfaceNetsVoxelRenderer : VoxelRenderer
{
    [SerializeField] private bool enableSurfaceNets = false;
    [SerializeField] private VoxelTypesAsset<SurfaceNetsVoxelType> voxelTypes;

    protected override Material[] GetMaterials()
    {
        return voxelTypes.materials;
    }

    protected override IEnumerator Render(VoxelBuffer buffer, Action<OptionMesh[]> callback)
    {
        Submeshes submeshes = new Submeshes(voxelTypes.materials.Length, voxelTypes);

        float meshHeight = 0;
        Task t = Task.Factory.StartNew(delegate
        {
            for (int y = 0; y < VoxelTables.ChunkHeight; y++)
            {
                for (int x = 0; x < VoxelTables.ChunkResolution; x++)
                {
                    for (int z = 0; z < VoxelTables.ChunkResolution; z++)
                    {
                        var index = new Vector3Int(x, y, z);
                        SurfaceNetsVoxelType voxel;
                        if (IsVoxelSolidInnerBounds(buffer, index, voxelTypes, out voxel))
                        {
                            int materialIndex = voxel.material;
                            for (int face = 0; face < 6; face++)
                            {
                                if (MeshFaceTowardsNeighbor(voxelTypes, buffer, index, face))
                                {
                                    submeshes.CountFace(materialIndex);
                                }
                            }

                            if (meshHeight < y)
                            {
                                meshHeight = y + 1;
                            }
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

        int meshOutsideOfBounds = 0;
        if (enableSurfaceNets)
        {
            meshOutsideOfBounds = 1;
        }

        Dictionary<float3, Node> nodes = new Dictionary<float3, Node>();
        submeshes.AllocateMeshData();

        //Task t1 = Task.Factory.StartNew(delegate
        //{
            for (int y = 0; y < VoxelTables.ChunkHeight; y++)
            {
                for (int x = -meshOutsideOfBounds; x < VoxelTables.ChunkResolution + meshOutsideOfBounds; x++)
                {
                    for (int z = -meshOutsideOfBounds; z < VoxelTables.ChunkResolution + meshOutsideOfBounds; z++)
                    {
                        var index = new Vector3Int(x, y, z);
                        if (IsVoxelSolidInnerBounds(buffer, index))
                        {
                            for (int face = 0; face < 6; face++)
                            {
                                if (MeshFaceTowardsNeighbor(voxelTypes, buffer, index, face))
                                {
                                    submeshes.MeshQuad(buffer, index, face);
                                }
                            }
                        }
                        else if (enableSurfaceNets && IsVoxelSolidInnerAndOuterBounds(buffer, index))
                        {
                            for (int face = 0; face < 6; face++)
                            {
                                if (MeshFaceTowardsNeighbor(voxelTypes, buffer, index, face))
                                {
                                    nodes.TryAdd(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 0]] + index, new Node(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 0]] + index));
                                    nodes.TryAdd(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 1]] + index, new Node(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 1]] + index));
                                    nodes.TryAdd(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 2]] + index, new Node(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 2]] + index));
                                    nodes.TryAdd(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 3]] + index, new Node(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 3]] + index));
                                }
                            }
                        }
                    }
                }
            }
        //});

        //yield return new WaitUntil(() => { return t1.IsCompleted; });

        //if (t1.Exception != null)
        //{
        //    Debug.LogError(t1.Exception);
        //}

        if (enableSurfaceNets)
        {
            NativeArray<float3> positions = submeshes.GetVertexData<float3>();

            Task t2 = Task.Factory.StartNew(delegate
            {
                /* create mesh vertex nodes */
                for (int i = 0; i < positions.Length; i++)
                {
                    // there may be multiple vertices at the same positions
                    // if we failed to add it (its a duplicate position)
                    if (!nodes.TryAdd(positions[i], new Node(i, positions[i])))
                    {
                        nodes[positions[i]].AddNodeVertex(i);
                    }
                }

                /* node neighbors */
                foreach(var node in nodes)
                {
                    // check for neighbor nodes
                    for (int side = 0; side < 6; side++)
                    {
                        Node neighbor;
                        // if has a neighbor
                        if (nodes.TryGetValue(VoxelTables.Normals[side] + node.Key, out neighbor))
                        {
                            node.Value.neighbors.Add(neighbor);
                        }
                    }
                }

                float3 phalf = new float3(0.5f, 0.5f, 0.5f);
                float3 nhalf = new float3(-0.5f, -0.5f, -0.5f);

                /* relax nodes */
                NativeArray<float3> positionsCopy = new NativeArray<float3>(submeshes.GetVertexData<float3>(), Allocator.Persistent);
                foreach (var node in nodes)
                {
                    float3 relaxed = node.Value.GetRelaxedNodePosition(positionsCopy);

                    float3 originalPosition = node.Value.originalPosition;

                    // if its a corner node
                    if (
                        (originalPosition.x.Equals(0f) && originalPosition.z.Equals(0f)) ||
                        (originalPosition.x.Equals(0f) && originalPosition.z.Equals(VoxelTables.ChunkResolution)) ||
                        (originalPosition.x.Equals(VoxelTables.ChunkResolution) && originalPosition.z.Equals(0f)) ||
                        (originalPosition.x.Equals(VoxelTables.ChunkResolution) && originalPosition.z.Equals(VoxelTables.ChunkResolution))
                    )
                    {
                        relaxed.x = originalPosition.x;
                        relaxed.z = originalPosition.z;
                    }
                    // if its a flap node
                    else if (
                        originalPosition.x < 0 || originalPosition.x > VoxelTables.ChunkResolution ||
                        originalPosition.z < 0 || originalPosition.z > VoxelTables.ChunkResolution
                    )
                    {
                        relaxed.x = originalPosition.x;
                        relaxed.z = originalPosition.z;
                    }

                    // clamp inside a 1x1x1 box centered on the original position
                    float3 clamped = math.clamp(relaxed, nhalf + node.Value.originalPosition, phalf + node.Value.originalPosition);

                    node.Value.SetNodePosition(clamped, positions);
                }
                positionsCopy.Dispose();
            });

            yield return new WaitUntil(() => { return t2.IsCompleted; });

            if (t2.Exception != null)
            {
                Debug.LogError(t2.Exception);
            }
        }

        Mesh mesh = submeshes.ApplyAndDisposeMeshData(meshHeight);
        mesh.RecalculateNormals();

        callback(new[]
        {
           new OptionMesh(MeshOptions.CollisionAndRender, mesh),
        });
    }

    private class Node
    {
        public List<int> indices = new List<int>();
        public List<Node> neighbors = new List<Node>();
        public float3 originalPosition;
        public float3 position;

        public Node(int firstVertexIndex, float3 originalPosition)
        {
            this.indices.Add(firstVertexIndex);
            this.originalPosition = originalPosition;
            this.position = originalPosition;
        }

        public Node(float3 position)
        {
            this.originalPosition = position;
            this.position = position;
        }

        public float3 GetRelaxedNodePosition(NativeArray<float3> positions)
        {
            if (neighbors.Count == 0)
            {
                return positions[indices[0]];
            }

            float3 meanVector = float3.zero;

            foreach (var neighbor in neighbors)
            {
                meanVector += neighbor.GetNodePosition(positions);
            }

            return (meanVector / neighbors.Count);
        }

        public void AddNodeVertex(int vertexIndex)
        {
            indices.Add(vertexIndex);
        }

        public float3 GetNodePosition(NativeArray<float3> vertices)
        {
            if (indices.Count > 0)
                return vertices[indices[0]];
            return position;
        }

        public void SetNodePosition(float3 pos, NativeArray<float3> vertices)
        {
            position = pos;
            for (int i = 0; i < indices.Count; i++)
            {
                vertices[indices[i]] = pos;
            }
        }
    }

    private class Submeshes
    {
        private NativeArray<float3> positions;
        private NativeArray<float3> normals;
        private NativeArray<float4> colors;
        private NativeArray<uint> indices;
        private VoxelTypesAsset<SurfaceNetsVoxelType> voxelTypes;

        public Submeshes(int materialsCount, VoxelTypesAsset<SurfaceNetsVoxelType> voxelTypes)
        {
            if (materialsCount == 0)
            {
                Debug.LogWarning("No materials provided to VoxelRenderer");
            }

            this.materialsCount = materialsCount;

            this.faceCountPerMaterial = new int[materialsCount];
            this.submeshIndexStarts = new int[materialsCount];

            this.indexOffsets = new int[materialsCount];
            this.vertexOffset = 0;

            this.voxelTypes = voxelTypes;
        }

        private MeshDataArray meshDataArray;
        private MeshData meshData;

        // also the number of submeshes
        private int materialsCount;
        private int faceCountTotal;

        private int[] faceCountPerMaterial;
        private int[] submeshIndexStarts;

        private int[] indexOffsets;
        private int vertexOffset;

        public void CountFace(int materialIndex)
        {
            faceCountPerMaterial[materialIndex]++;
            faceCountTotal++;
        }

        public int GetFaceCountTotal()
        {
            return faceCountTotal;
        }

        public void AllocateMeshData()
        {
            meshDataArray = Mesh.AllocateWritableMeshData(1);
            meshData = meshDataArray[0];

            int attributeCount = 3; // position, normal, colors
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                attributeCount, Allocator.Persistent
            );
            // position
            vertexAttributes[0] = new VertexAttributeDescriptor(
                VertexAttribute.Position, dimension: 3, stream: 0
            );
            // normal
            vertexAttributes[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1
            );
            // color
            vertexAttributes[2] = new VertexAttributeDescriptor(
                VertexAttribute.Color, dimension: 4, stream: 2
            );

            // set index starts for each submesh
            int indexStart = 0;
            for (int i = 0; i < materialsCount; i++)
            {
                submeshIndexStarts[i] = indexStart;

                indexStart += faceCountPerMaterial[i] * 6;
            }

            // VERTEX
            meshData.SetVertexBufferParams(faceCountTotal * 4, vertexAttributes);
            vertexAttributes.Dispose();

            // INDEX
            meshData.SetIndexBufferParams(faceCountTotal * 6, IndexFormat.UInt32);

            // vertex.positions
            positions = meshData.GetVertexData<float3>();

            // vertex.normals
            normals = meshData.GetVertexData<float3>(1);

            // vertex.colors
            colors = meshData.GetVertexData<float4>(2);

            indices = meshData.GetIndexData<uint>();
        }

        public void MeshQuad(VoxelBuffer buffer, Vector3Int index, int face)
        {
            SurfaceNetsVoxelType voxel;
            IsVoxelSolidInnerAndOuterBounds(buffer, index, voxelTypes, out voxel);
            int materialIndex = voxel.material;

            int indexOffset = indexOffsets[materialIndex] + submeshIndexStarts[materialIndex];

            positions[vertexOffset + 0] = VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 0]] + index;
            positions[vertexOffset + 1] = VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 1]] + index;
            positions[vertexOffset + 2] = VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 2]] + index;
            positions[vertexOffset + 3] = VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 3]] + index;

            normals[vertexOffset + 0] = VoxelTables.Normals[face];
            normals[vertexOffset + 1] = VoxelTables.Normals[face];
            normals[vertexOffset + 2] = VoxelTables.Normals[face];
            normals[vertexOffset + 3] = VoxelTables.Normals[face];

            colors[vertexOffset + 0] = voxel.float4Color;
            colors[vertexOffset + 1] = voxel.float4Color;
            colors[vertexOffset + 2] = voxel.float4Color;
            colors[vertexOffset + 3] = voxel.float4Color;

            indices[indexOffset + 0] = (uint)vertexOffset + 0;
            indices[indexOffset + 1] = (uint)vertexOffset + 1;
            indices[indexOffset + 2] = (uint)vertexOffset + 2;
            indices[indexOffset + 3] = (uint)vertexOffset + 2;
            indices[indexOffset + 4] = (uint)vertexOffset + 1;
            indices[indexOffset + 5] = (uint)vertexOffset + 3;

            vertexOffset += 4;
            indexOffsets[materialIndex] += 6;
        }

        public NativeArray<T> GetVertexData<T>(int stream = 0) where T : struct
        {
            return meshData.GetVertexData<T>(stream);
        }

        public Mesh ApplyAndDisposeMeshData(float meshHeight)
        {
            // bounds
            var bounds = new Bounds(
                new Vector3((float)VoxelTables.ChunkResolution / (float)2f, meshHeight / (float)2f, (float)VoxelTables.ChunkResolution / (float)2f),
                new Vector3(VoxelTables.ChunkResolution, meshHeight, VoxelTables.ChunkResolution)
            );

            // number of submeshes
            meshData.subMeshCount = materialsCount;

            for (int i = 0; i < materialsCount; i++)
            {
                meshData.SetSubMesh(i, new SubMeshDescriptor(submeshIndexStarts[i], faceCountPerMaterial[i] * 6)
                {
                    bounds = bounds,
                    vertexCount = faceCountPerMaterial[i] * 4,
                }, MeshUpdateFlags.DontRecalculateBounds);
            }

            Mesh mesh = new Mesh
            {
                bounds = bounds,
            };
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

            return mesh;
        }
    }

}

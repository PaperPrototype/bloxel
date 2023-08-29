using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bloxel;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

public class SimpleVoxelRenderer : VoxelRenderer
{
    [SerializeField] private VoxelTypesAsset<SimpleVoxelType> voxelTypes;

    protected override Material[] GetMaterials()
    {
        return voxelTypes.materials;
    }

    protected override IEnumerator Render(VoxelBuffer buffer, Action<OptionMesh[]> callback)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        int vertexOffset = 0;
        List<int> triangles = new List<int>();
        int triangleOffset = 0;

        Task t = Task.Factory.StartNew(delegate
        {
            for (int y = 0; y < VoxelTables.ChunkHeight; y++)
            {
                for (int x = 0; x < VoxelTables.ChunkResolution; x++)
                {
                    for (int z = 0; z < VoxelTables.ChunkResolution; z++)
                    {
                        var offset = new Vector3Int(x, y, z);
                        if (IsVoxelSolidWithinBufferBounds(buffer, offset))
                        {
                            for (int face = 0; face < 6; face++)
                            {
                                if (MeshFaceTowardsNeighbor(voxelTypes, buffer, offset, face))
                                {
                                    vertices.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 0]] + offset);
                                    vertices.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 1]] + offset);
                                    vertices.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 2]] + offset);
                                    vertices.Add(VoxelTables.Vertices[VoxelTables.QuadVerticesIndex[face, 3]] + offset);

                                    colors.Add(voxelTypes[buffer.Get(offset.x, offset.y, offset.z).id].color);
                                    colors.Add(voxelTypes[buffer.Get(offset.x, offset.y, offset.z).id].color);
                                    colors.Add(voxelTypes[buffer.Get(offset.x, offset.y, offset.z).id].color);
                                    colors.Add(voxelTypes[buffer.Get(offset.x, offset.y, offset.z).id].color);

                                    triangles.Add(vertexOffset + 0);
                                    triangles.Add(vertexOffset + 1);
                                    triangles.Add(vertexOffset + 2);
                                    triangles.Add(vertexOffset + 2);
                                    triangles.Add(vertexOffset + 1);
                                    triangles.Add(vertexOffset + 3);

                                    vertexOffset += 4;
                                    triangleOffset += 6;
                                }
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

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        callback(new[]
        {
           new OptionMesh(MeshOptions.CollisionAndRender, mesh)
        });
    }
}

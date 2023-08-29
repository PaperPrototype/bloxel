using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bloxel;
using static UnityEditor.PlayerSettings;

public class ExampleVoxelGenerator : VoxelGenerator
{
    [Header("Globals")]
    public float oceanHeight = 100;

    [Header("-- Sand Dunes (Ridged Noise) --")]
    public float[] SandDunes;

    [Range(0, 1)]
    public float probability = 0.1f;
    public VoxelStructure[] voxelStructures;

    protected override bool VoxelStructureAt(int x, int y, int z, byte biomeID, float height, out VoxelStructure structure)
    {
        bool isOcean = y < oceanHeight;
        bool isPalm = GetNoise(0.01f, 0.01f, x, z) < probability;

        // if remainder is zero (when divided by 4
        if (isPalm && !isOcean)
        {
            structure = voxelStructures[0]; // palm tree
            return true;
        }

        structure = null;
        return false;
    }

    protected override float BaseHeight(int x, int z)
    {
        //float halfMaxHeight = VoxelTables.ChunkHeight / 2;

        //float continentalnessSampler = GetNoiseOctaves(continentalness, pos.x, pos.z);
        //float continentHeight = (terrainContinentalnessCurve.Evaluate(continentalnessSampler) + halfMaxHeight) * halfMaxHeight;

        //float erodedHeight = (
        //    (
        //        (GetNoiseOctaves(erosions, pos.x + 100, pos.y - 100) * -1f) + 1f
        //    )
        //) * halfMaxHeight;

        return GetRidgedNoiseOctaves(SandDunes, x, z) * 50;
    }

    protected override byte VoxelID(Vector3 pos, float terrainHeight, byte biomeID)
    {
        bool isGround = pos.y < terrainHeight;

        bool isOcean = !isGround && pos.y < oceanHeight;

        if (isGround)
            return GroundSpace(pos, terrainHeight);
        else if (isOcean)
            return OceanSpace(pos, terrainHeight);

        return AirSpace(pos, terrainHeight);
    }

    private byte GroundSpace(Vector3 pos, float baseHeight)
    {
        return 1; // sand
    }

    private byte OceanSpace(Vector3 pos, float baseHeight)
    {
        return 2; // ocean
    }

    private byte AirSpace(Vector3 pos, float baseHeight)
    {
        return 0;
    }
}

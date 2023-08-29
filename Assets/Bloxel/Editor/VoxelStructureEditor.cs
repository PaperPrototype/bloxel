using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bloxel
{
    [CustomEditor(typeof(VoxelStructure))]
    public class VoxelStructureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VoxelStructure structure = (VoxelStructure)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Add"))
                structure.Add();


            if (GUILayout.Button("Remove"))
                structure.Remove();
        }
    }
}
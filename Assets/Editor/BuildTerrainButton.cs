using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGeneratorV2))]
public class BuildTerrainButton : Editor {

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		TerrainGeneratorV2 craft = (TerrainGeneratorV2)target;
		if(GUILayout.Button("Generate Terrain"))
		{
			craft.Start();
		}
	}
}

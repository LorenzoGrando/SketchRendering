using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TAMGenerator))]
public class TAMGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        TAMGenerator editor = (TAMGenerator)target;
        
        if(GUILayout.Button("Clear Texture"))
        {
            editor.CreateOrUpdateTarget();
        }
        
        if(GUILayout.Button("Update & Show SDF"))
        {
            editor.ConfigureGeneratorData();
            editor.ApplyStrokeKernel();
        }

        if (GUILayout.Button("Apply Strokes Until Fill Rate"))
        {
            editor.ConfigureGeneratorData();
            editor.ApplyStrokesUntilFillRateAchieved();
        }

        if (GUILayout.Button("Save Current Texture"))
        {
            editor.SaveCurrentTargetTexture(false);
        }
        
        if (GUILayout.Button("Generate TAM Textures"))
        {
            editor.GenerateTAMToneTextures();
            EditorUtility.SetDirty(editor.TAMAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
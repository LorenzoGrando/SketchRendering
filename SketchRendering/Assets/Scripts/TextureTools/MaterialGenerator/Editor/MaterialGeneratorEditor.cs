using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialGenerator))]
public class MaterialGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        MaterialGenerator editor = (MaterialGenerator)target;
        
        if(GUILayout.Button("Clear Texture"))
        {
            editor.CreateOrUpdateTarget();
        }
        
        if (GUILayout.Button("Generate Albedo Texture"))
        {
            editor.UpdateMaterialAlbedoTexture();
        }
        
        if (GUILayout.Button("Generate Directional Map"))
        {
            editor.UpdateMaterialDirectionalTexture();
        }
        
        if (GUILayout.Button("Save Current Texture"))
        {
            editor.SaveCurrentTargetTexture(editor.TextureOutputType, false);
        }
    }
}
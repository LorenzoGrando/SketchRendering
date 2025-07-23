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
        
        if (GUILayout.Button("Regenerate Albedo"))
        {
            editor.UpdateMaterialAlbedoTexture();
        }
        
        if (GUILayout.Button("Save Current Texture"))
        {
            editor.SaveCurrentTargetTexture(false);
        }
    }
}
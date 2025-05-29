using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TAMGenerator))]
public class TAMGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        TAMGenerator editor = (TAMGenerator)target;
        
        if(GUILayout.Button("Update & Show SDF"))
        {
            editor.ConfigureGeneratorData();
            editor.ApplyStrokeKernel();
        }
    }
}
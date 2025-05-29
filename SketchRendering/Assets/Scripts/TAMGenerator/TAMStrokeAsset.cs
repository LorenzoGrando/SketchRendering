using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "TAMStrokeAsset", menuName = "Scriptable Objects/TAMStrokeAsset")]
public class TAMStrokeAsset : ScriptableObject
{
    public TAMStrokeData StrokeData;
    public FalloffFunction SelectedFalloffFunction;
}

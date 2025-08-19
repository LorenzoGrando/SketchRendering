using UnityEngine;

[System.Serializable]
public class SketchCompositionPassData : ISketchRenderPassData<SketchCompositionPassData>
{
    public enum DebugMode
    {
        NONE, MATERIAL_ALBEDO, MATERIAL_DIRECTION, OUTLINES, LUMINANCE
    }
    [Header("Debug")]
    public DebugMode debugMode = DebugMode.NONE;

    [Header("Composition")] 
    public Color OutlineStrokeColor = Color.black;
    public Color ShadingStrokeColor = Color.black;
    [Range(0f, 1f)] 
    public float MaterialAccumulationStrength;
    public BlendingOperations StrokeBlendMode;
    [Range(0f, 1f)]
    public float BlendStrength;
    
    public bool IsAllPassDataValid()
    {
        return true;
    }

    public SketchCompositionPassData GetPassDataByVolume()
    {
        return this;
    }
}

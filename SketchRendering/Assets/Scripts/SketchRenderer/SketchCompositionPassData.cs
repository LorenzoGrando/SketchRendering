using UnityEngine;

[System.Serializable]
public class SketchCompositionPassData : ISketchRenderPassData<SketchCompositionPassData>
{
    public enum DebugMode
    {
        NONE, OUTLINES, LUMINANCE
    }
    [Header("Debug")]
    public DebugMode debugMode = DebugMode.NONE;

    [Header("Composition")] 
    public Color OutlineStrokeColor = Color.black;
    public Color ShadingStrokeColor = Color.black;
    
    public bool IsAllPassDataValid()
    {
        return true;
    }

    public SketchCompositionPassData GetPassDataByVolume()
    {
        return this;
    }
}

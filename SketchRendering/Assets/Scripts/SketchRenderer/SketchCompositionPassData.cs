[System.Serializable]
public class SketchCompositionPassData : ISketchRenderPassData
{
    public enum DebugMode
    {
        NONE, OUTLINES, LUMINANCE
    }
    
    public DebugMode debugMode = DebugMode.NONE;
}

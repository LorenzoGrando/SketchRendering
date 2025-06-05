[System.Serializable]
public class SketchCompositionPassData : ISketchRenderPassData<SketchCompositionPassData>
{
    public enum DebugMode
    {
        NONE, OUTLINES, LUMINANCE
    }
    
    public DebugMode debugMode = DebugMode.NONE;

    public bool IsAllPassDataValid()
    {
        return true;
    }

    public SketchCompositionPassData GetPassDataByVolume()
    {
        return this;
    }
}

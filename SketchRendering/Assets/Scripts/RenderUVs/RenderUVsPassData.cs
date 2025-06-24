public class RenderUVsPassData : ISketchRenderPassData<RenderUVsPassData>
{
    public bool IsAllPassDataValid()
    {
        return true;
    }

    public RenderUVsPassData GetPassDataByVolume()
    {
        return this;
    }
}

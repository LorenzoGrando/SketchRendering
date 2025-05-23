[System.Serializable]
public class LuminancePassData : ISketchRenderPassData
{
    public TonalArtMapAsset ActiveTonalMap;

    public bool IsAllPassDataValid()
    {
        return ActiveTonalMap != null;
    }
}

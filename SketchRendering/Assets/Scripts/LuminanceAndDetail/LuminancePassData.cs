using UnityEngine;

[System.Serializable]
public class LuminancePassData : ISketchRenderPassData
{
    public TonalArtMapAsset ActiveTonalMap;
    public bool SmoothTransitions;
    public Vector2 ToneScales = Vector2.one;

    public bool IsAllPassDataValid()
    {
        return ActiveTonalMap != null;
    }
}

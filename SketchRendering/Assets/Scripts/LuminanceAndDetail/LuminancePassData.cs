using UnityEngine;

[System.Serializable]
public class LuminancePassData : ISketchRenderPassData
{
    public TonalArtMapAsset ActiveTonalMap;
    public bool SmoothTransitions;
    public Vector2 ToneScales = Vector2.one;
    [Range(-1f, 1f)]
    [SerializeField] private float luminanceOffset = 0;
    public float LuminanceOffset
    {
        get
        {
            if (luminanceOffset < 0)
                return Mathf.Lerp(1, 0, Mathf.Abs(luminanceOffset));
            else
                return Mathf.Lerp(1, 9, luminanceOffset);
        }
    }

    public bool IsAllPassDataValid()
    {
        return ActiveTonalMap != null && ActiveTonalMap.IsPacked;
    }
}

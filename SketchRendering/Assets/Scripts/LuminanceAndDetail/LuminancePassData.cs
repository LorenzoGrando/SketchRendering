using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class LuminancePassData : ISketchRenderPassData<LuminancePassData>
{
    public TonalArtMapAsset ActiveTonalMap;
    public StrokeProjectionMethod ProjectionMethod;
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
        set => luminanceOffset = Mathf.Clamp(value, -1f, 1f);
    }

    public bool IsAllPassDataValid()
    {
        return ActiveTonalMap != null && ActiveTonalMap.IsPacked;
    }

    public LuminancePassData GetPassDataByVolume()
    {
        QuantizeLuminanceVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<QuantizeLuminanceVolumeComponent>();
        if (volumeComponent == null)
            return this;
        LuminancePassData overrideData = new LuminancePassData();
        
        overrideData.ProjectionMethod = volumeComponent.ProjectionMethod.overrideState ? volumeComponent.ProjectionMethod.value : ProjectionMethod;
        overrideData.SmoothTransitions = volumeComponent.SmoothTransitions.overrideState
            ? volumeComponent.SmoothTransitions.value : SmoothTransitions;
        overrideData.ToneScales = volumeComponent.ToneScales.overrideState ? volumeComponent.ToneScales.value : ToneScales;
        overrideData.LuminanceOffset = volumeComponent.LuminanceOffset.overrideState ? volumeComponent.LuminanceOffset.value : luminanceOffset;
        
        overrideData.ActiveTonalMap = ActiveTonalMap;
        
        return overrideData;
    }
}

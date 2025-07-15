using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class SketchStrokesPassData : ISketchRenderPassData<SketchStrokesPassData>
{
    public TAMStrokeAsset OutlineStrokeData;
    public ComputeData.KernelSize2D SampleArea;
    public bool DoDownscale;
    [Range(2, 4)]
    public int DownscaleFactor;
    [Range(0f, 1f)] 
    public float StrokeThreshold;
    [Range(1f, 4f)] 
    public int StrokeSampleScale;

    [HideInInspector] 
    public bool UsePerpendicularDirection;
    
    public SketchStrokesPassData()
    {
        StrokeThreshold = 0;
    }

    public bool IsAllPassDataValid()
    {
        return OutlineStrokeData != null;
    }

    public void ConfigurePerpendicularDirection(EdgeDetectionGlobalData.EdgeDetectionMethod method)
    {
        switch (method)
        {
            case EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3:
                UsePerpendicularDirection = false;
                break;
            case EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_1X3:
                UsePerpendicularDirection = true;
                break;
        }
    }
    

    public SketchStrokesPassData GetPassDataByVolume()
    {
        SketchOutlineVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<SketchOutlineVolumeComponent>();
        if (volumeComponent == null)
            return this;
        
        SketchStrokesPassData overrideData = new SketchStrokesPassData();
        overrideData.OutlineStrokeData = OutlineStrokeData;
        overrideData.SampleArea = volumeComponent.StrokeArea.overrideState ? volumeComponent.StrokeArea.value : SampleArea;
        overrideData.StrokeSampleScale = volumeComponent.StrokeScale.overrideState ? volumeComponent.StrokeScale.value : StrokeSampleScale;
        overrideData.DoDownscale = volumeComponent.DoDownscale.overrideState ? volumeComponent.DoDownscale.value : DoDownscale;
        if(overrideData.DoDownscale)
            overrideData.DownscaleFactor = volumeComponent.DownscaleFactor.overrideState ? volumeComponent.DownscaleFactor.value : DownscaleFactor;
        else
            overrideData.DownscaleFactor = 1;
        overrideData.StrokeThreshold = volumeComponent.MinThresholdForStroke.overrideState ? volumeComponent.MinThresholdForStroke.value : StrokeThreshold;;
        
        return overrideData;
    }
}

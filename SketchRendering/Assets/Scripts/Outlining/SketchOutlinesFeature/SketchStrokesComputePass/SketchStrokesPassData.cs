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
        SmoothOutlineVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<SmoothOutlineVolumeComponent>();
        if (volumeComponent == null)
            return this;
        
        SketchStrokesPassData overrideData = new SketchStrokesPassData();
        overrideData.SampleArea = SampleArea;
        overrideData.DoDownscale = DoDownscale;
        overrideData.DownscaleFactor = DoDownscale ? DownscaleFactor : 1;
        overrideData.StrokeThreshold = StrokeThreshold;
        overrideData.OutlineStrokeData = OutlineStrokeData;
        
        return overrideData;
    }
}

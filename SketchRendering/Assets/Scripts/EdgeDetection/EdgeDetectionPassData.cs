using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class EdgeDetectionPassData : ISketchRenderPassData<EdgeDetectionPassData>
{
    public EdgeDetectionGlobalData.EdgeDetectionMethod Method;
    public EdgeDetectionGlobalData.EdgeDetectionSource Source;
    [Range(0,1)]
    public float OutlineThreshold;
    [Range(0,1)]
    public float OutlineAngleSensitivity;
    [Range(0,1)]
    public float OutlineAngleConstraint;
    
    public EdgeDetectionPassData()
    {
        this.Method = EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3;
        this.Source = EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH;
        this.OutlineThreshold = 0.5f;
        this.OutlineAngleSensitivity = 1;
        this.OutlineAngleConstraint = 1;
    }

    public EdgeDetectionPassData(EdgeDetectionGlobalData.EdgeDetectionMethod method, EdgeDetectionGlobalData.EdgeDetectionSource source, float outlineThreshold, float outlineAngleSensitivity, float outlineAngleConstraint)
    {
        this.Method = method;
        this.Source = source;
        this.OutlineThreshold = outlineThreshold;
        this.OutlineAngleSensitivity = outlineAngleSensitivity;
        this.OutlineAngleConstraint = outlineAngleConstraint;
    }

    public bool IsAllPassDataValid()
    {
        return true;
    }

    public EdgeDetectionPassData GetPassDataByVolume()
    {
        SmoothOutlineVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<SmoothOutlineVolumeComponent>();
        if (volumeComponent == null)
            return this;
        EdgeDetectionPassData overrideData = new EdgeDetectionPassData();
        
        
        overrideData.Method = volumeComponent.Method.overrideState
            ? volumeComponent.Method.value : Method;
        overrideData.Source = volumeComponent.Source.overrideState ? volumeComponent.Source.value : Source;
        overrideData.OutlineThreshold = volumeComponent.Threshold.overrideState ? volumeComponent.Threshold.value : OutlineThreshold;
        overrideData.OutlineAngleSensitivity = volumeComponent.AngleSensitivity.overrideState ? volumeComponent.AngleSensitivity.value : OutlineAngleSensitivity;
        overrideData.OutlineAngleConstraint = volumeComponent.AngleConstraint.overrideState ? volumeComponent.AngleConstraint.value : OutlineAngleConstraint;
        
        return overrideData;
    }
}

using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class EdgeDetectionPassData : ISketchRenderPassData<EdgeDetectionPassData>
{
    public EdgeDetectionGlobalData.EdgeDetectionMethod Method;
    public EdgeDetectionGlobalData.EdgeDetectionSource Source;
    [Range(0, 1)]
    public float OutlineThreshold;
    [Range(0, 3)]
    public int OutlineOffset;
    [Range(0, 1)] 
    public float OutlineDistanceFalloff;
    [Range(0,1)]
    public float OutlineAngleSensitivity;
    [Range(0,1)]
    public float OutlineAngleConstraint;
    [Range(0,1)]
    public float OutlineNormalSensitivity;
    
    [HideInInspector]
    public EdgeDetectionGlobalData.EdgeDetectionOutputType OutputType;
    public void CopyFrom(EdgeDetectionPassData passData)
    {
        Method = passData.Method;
        Source = passData.Source;
        OutlineThreshold = passData.OutlineThreshold;
        OutlineDistanceFalloff = passData.OutlineDistanceFalloff;
        OutlineAngleSensitivity = passData.OutlineAngleSensitivity;
        OutlineAngleConstraint = passData.OutlineAngleConstraint;
        OutlineNormalSensitivity = passData.OutlineNormalSensitivity;
        OutputType = passData.OutputType;
    }
    public bool IsAllPassDataValid()
    {
        return true;
    }

    private OutlineVolumeComponent UpdateTargetVolume()
    {
        //This honestly feels really bad to do,
        //but i found it ideal over needing two add two different components to a stack, one for edge detection and one for outline specifics.
        //So each class inherits from a common base, and we choose here based on the stack
        //TODO: This just fires a warning, ideally select the correct volume based on universal data?

        SmoothOutlineVolumeComponent smoothComponent = VolumeManager.instance.stack.GetComponent<SmoothOutlineVolumeComponent>();
        SketchOutlineVolumeComponent sketchComponent = VolumeManager.instance.stack.GetComponent<SketchOutlineVolumeComponent>();
        bool hasSmooth = smoothComponent != null && smoothComponent.AnyPropertiesIsOverridden();
        bool hasSketch = sketchComponent != null && sketchComponent.AnyPropertiesIsOverridden();
        if (hasSmooth && hasSketch)
        {
            Debug.LogWarning("Multiple edge detection outline volumes detected in scene, defaulting to Settings values. Please remove or disable one of the overrides.");
            return null;
        }
        else if (hasSmooth)
            return smoothComponent;
        else if (hasSketch)
            return sketchComponent;
        else return null;
    }

    public EdgeDetectionPassData GetPassDataByVolume()
    {
        if(VolumeManager.instance == null || VolumeManager.instance.stack == null)
            return this;
        OutlineVolumeComponent volumeComponent = UpdateTargetVolume();
        if (volumeComponent == null || !volumeComponent.active)
            return this;

        EdgeDetectionPassData overrideData = new EdgeDetectionPassData();
        
        overrideData.Method = volumeComponent.Method.overrideState
            ? volumeComponent.Method.value : Method;
        overrideData.Source = volumeComponent.Source.overrideState ? volumeComponent.Source.value : Source;
        overrideData.OutlineThreshold = volumeComponent.Threshold.overrideState ? volumeComponent.Threshold.value : OutlineThreshold;
        overrideData.OutlineDistanceFalloff = volumeComponent.DistanceFalloff.overrideState ? volumeComponent.DistanceFalloff.value : OutlineDistanceFalloff;
        overrideData.OutlineOffset = volumeComponent.Offset.overrideState ? volumeComponent.Offset.value : OutlineOffset;
        overrideData.OutlineAngleSensitivity = volumeComponent.AngleSensitivity.overrideState ? volumeComponent.AngleSensitivity.value : OutlineAngleSensitivity;
        overrideData.OutlineAngleConstraint = volumeComponent.AngleConstraint.overrideState ? volumeComponent.AngleConstraint.value : OutlineAngleConstraint;
        overrideData.OutlineNormalSensitivity = volumeComponent.NormalSensitivity.overrideState ? volumeComponent.NormalSensitivity.value : OutlineNormalSensitivity;
        overrideData.OutputType = OutputType;
        
        return overrideData;
    }
}

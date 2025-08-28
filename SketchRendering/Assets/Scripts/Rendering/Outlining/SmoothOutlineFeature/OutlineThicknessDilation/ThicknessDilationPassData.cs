using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class ThicknessDilationPassData : ISketchRenderPassData<ThicknessDilationPassData>
{
    [Range(0, 5)]
    public int ThicknessRange;
    [Range(0f, 1f)]
    public float ThicknessStrength;

    public ThicknessDilationPassData()
    {
        ThicknessRange = 1;
        ThicknessStrength = 1;
    }
    
    public bool IsAllPassDataValid()
    {
        return ThicknessRange > 0;
    }

    public ThicknessDilationPassData GetPassDataByVolume()
    {
        SmoothOutlineVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<SmoothOutlineVolumeComponent>();
        if (volumeComponent == null)
            return this;
        ThicknessDilationPassData overrideData = new ThicknessDilationPassData();
        
        overrideData.ThicknessRange = volumeComponent.ThicknessRange.overrideState ? volumeComponent.ThicknessRange.value : ThicknessRange;
        overrideData.ThicknessStrength = volumeComponent.ThicknessStrength.overrideState ? volumeComponent.ThicknessStrength.value : ThicknessStrength;
        
        return overrideData;
    }
}

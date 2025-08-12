using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class AccentedOutlinePassData : ISketchRenderPassData<AccentedOutlinePassData>
{
    [Header("Distortion Settings")] 
    public bool BakeDistortionDuringRuntime;
    public float Rate;
    [Range(0f, 1f)]
    public float Strength;

    [Header("Additional Lines")] 
    [Range(0, 3)]
    public int AdditionalLines;
    [Range(0, 1f)]
    public float AdditionalLineTintPersistence;
    [Range(0, 1f)]
    public float AdditionalLineDistortionJitter;

    [Header("Outline Masking")] 
    public Texture2D PencilOutlineMask;
    public Vector2 MaskScale;
    
    public AccentedOutlinePassData()
    {
        Rate = 20.0f;
        Strength = 1.0f;
    }
    
    public bool RequireMultipleTextures => AdditionalLines > 1;
    
    public bool IsAllPassDataValid()
    {
        return Strength > 0 || PencilOutlineMask != null;
    }

    public AccentedOutlinePassData GetPassDataByVolume()
    {
        SmoothOutlineVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<SmoothOutlineVolumeComponent>();
        if (volumeComponent == null)
            return this;
        AccentedOutlinePassData overrideData = new AccentedOutlinePassData();

        overrideData.BakeDistortionDuringRuntime = volumeComponent.BakeDistortion.overrideState
            ? volumeComponent.BakeDistortion.value
            : BakeDistortionDuringRuntime;
        overrideData.Rate = volumeComponent.DistortionRate.overrideState ? volumeComponent.DistortionRate.value : Rate;
        overrideData.Strength = volumeComponent.DistortionStrength.overrideState ? volumeComponent.DistortionStrength.value : Strength;
        overrideData.AdditionalLines = volumeComponent.AdditionalDistortionLines.overrideState ? volumeComponent.AdditionalDistortionLines.value : AdditionalLines;
        overrideData.AdditionalLineTintPersistence = volumeComponent.AdditionalLineTintPersistence.overrideState ? volumeComponent.AdditionalLineTintPersistence.value : AdditionalLineTintPersistence;
        overrideData.AdditionalLineDistortionJitter = volumeComponent.AdditionalLinesDistortionJitter.overrideState ? volumeComponent.AdditionalLinesDistortionJitter.value : AdditionalLineDistortionJitter;
        overrideData.PencilOutlineMask = PencilOutlineMask;
        overrideData.MaskScale = MaskScale;
        
        return overrideData;
    }
}
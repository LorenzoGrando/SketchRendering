using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class AccentedOutlinePassData : ISketchRenderPassData<AccentedOutlinePassData>
{
    [Header("Distortion Settings")]
    public float Rate;
    [Range(0f, 1f)]
    public float Strength;

    [Header("Outline Masking")] 
    public Texture2D PencilOutlineMask;
    public Vector2 MaskScale;
    
    public AccentedOutlinePassData()
    {
        Rate = 20.0f;
        Strength = 1.0f;
    }
    
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

        overrideData.Rate = volumeComponent.DistortionRate.overrideState ? volumeComponent.DistortionRate.value : Rate;
        overrideData.Strength = volumeComponent.DistortionStrength.overrideState ? volumeComponent.DistortionStrength.value : Strength;
        overrideData.PencilOutlineMask = PencilOutlineMask;
        overrideData.MaskScale = MaskScale;
        
        return overrideData;
    }
}
using UnityEngine;
using UnityEngine.Rendering;

public class RendererVolumeUIController : MonoBehaviour
{
    [SerializeField] private GameObject RootTransform;
    public virtual void EnableUI()
    {
        RootTransform.SetActive(true);
    }
    public virtual void DisableUI()
    {
        RootTransform.SetActive(false);
    }
    
    public virtual void ConfigureVolume() {}

    protected float GetLerpedValue(ClampedFloatParameter param)
    {
        return Mathf.InverseLerp(param.min, param.max, param.value);
    }

    protected string GetFormattedSliderValue(ClampedFloatParameter param)
    {
        return param.value.ToString("0.##");
    }
    
    protected string GetFormattedSliderValue(float param)
    {
        return param.ToString("0.##");
    }
}

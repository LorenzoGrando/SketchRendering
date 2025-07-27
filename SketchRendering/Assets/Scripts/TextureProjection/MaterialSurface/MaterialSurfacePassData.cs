using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class MaterialSurfacePassData : ISketchRenderPassData<MaterialSurfacePassData>
{
    public TextureProjectionMethod ProjectionMethod;
    public Texture2D AlbedoTexture;
    public Texture2D NormalTexture;
    public Vector2Int Scale;
    [Range(0f, 1f)]
    public float BaseColorBlendFactor;
    
    public bool IsAllPassDataValid()
    {
        return AlbedoTexture != null && NormalTexture != null;
    }

    public MaterialSurfacePassData GetPassDataByVolume()
    {
        QuantizeLuminanceVolumeComponent volumeComponent = VolumeManager.instance.stack.GetComponent<QuantizeLuminanceVolumeComponent>();
        if (volumeComponent == null)
            return this;
        MaterialSurfacePassData overrideData = new MaterialSurfacePassData();
        
        overrideData.ProjectionMethod = ProjectionMethod;
        overrideData.AlbedoTexture = AlbedoTexture;
        overrideData.NormalTexture = NormalTexture;
        overrideData.Scale = Scale;
        overrideData.BaseColorBlendFactor = BaseColorBlendFactor;
        
        return overrideData;
    }
}
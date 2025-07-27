using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class SketchRendererContext : ContextItem
{
    public bool PrebakedDistortedUVs;
    public TextureHandle MaterialTexture;
    public TextureHandle OutlinesTexture;
    public TextureHandle LuminanceTexture;
    
    public override void Reset()
    {
        MaterialTexture = TextureHandle.nullHandle;
        OutlinesTexture = TextureHandle.nullHandle;
        LuminanceTexture = TextureHandle.nullHandle;
    }
}

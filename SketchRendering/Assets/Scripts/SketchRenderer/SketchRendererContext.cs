using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class SketchRendererContext : ContextItem
{
    public bool PrebakedDistortedUVs;
    public TextureHandle OutlinesTexture;
    public TextureHandle LuminanceTexture;
    
    public override void Reset()
    {
        OutlinesTexture = TextureHandle.nullHandle;
        LuminanceTexture = TextureHandle.nullHandle;
    }
}

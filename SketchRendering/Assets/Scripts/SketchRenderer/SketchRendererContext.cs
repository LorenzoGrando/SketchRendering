using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class SketchRendererContext : ContextItem
{
    public bool PrebakedDistortedUVs;
    public bool PrebakedDistortedMultipleUVs;
    public Vector4 LuminanceBasisDirection;
    public TextureHandle MaterialTexture;
    public TextureHandle DirectionalTexture;
    public TextureHandle OutlinesTexture;
    public TextureHandle LuminanceTexture;
    
    public override void Reset()
    {
        LuminanceBasisDirection = Vector4.zero;
        MaterialTexture = TextureHandle.nullHandle;
        DirectionalTexture = TextureHandle.nullHandle;
        OutlinesTexture = TextureHandle.nullHandle;
        LuminanceTexture = TextureHandle.nullHandle;
    }
}

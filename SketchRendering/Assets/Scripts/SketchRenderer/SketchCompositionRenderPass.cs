using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SketchCompositionRenderPass : ScriptableRenderPass, ISketchRenderPass<SketchCompositionPassData>
{ 
    public string PassName
    {
        get { return "SketchCompositionRenderPass"; }
    }
    
    protected static readonly int outlinesShaderID = Shader.PropertyToID("_OutlineTex");
    protected static readonly int luminanceShaderID = Shader.PropertyToID("_LuminanceTex");
    
    public static readonly string DEBUG_OUTLINES = "DEBUG_OUTLINES";
    public static readonly string DEBUG_LUMINANCE = "DEBUG_LUMINANCE";

    private LocalKeyword debugOutlinesKeyword;
    private LocalKeyword debugLuminanceKeyword;
    
    // Scale bias is used to control how the blit operation is done. The x and y parameter controls the scale
    // and z and w controls the offset. TAKEN FROM URPSAMPLES
    static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);

    private Material mat;
    private SketchCompositionPassData passData;

    private class PassData
    {
        public TextureHandle outlineTexture;
        public TextureHandle luminanceTexture;
        public Material material;
        public TextureHandle src;
    }

    public void Setup(SketchCompositionPassData passData, Material material)
    {
        this.mat = material;
        this.passData = passData;
        
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture = true;
        
        ConfigureMaterial();
    }

    public void ConfigureMaterial()
    {
        debugOutlinesKeyword = new LocalKeyword(mat.shader, DEBUG_OUTLINES);
        debugLuminanceKeyword = new LocalKeyword(mat.shader, DEBUG_LUMINANCE);
        
        switch (passData.debugMode)
        {
            case SketchCompositionPassData.DebugMode.NONE:
                mat.DisableKeyword(debugOutlinesKeyword);
                mat.DisableKeyword(debugLuminanceKeyword);
                break;
            case SketchCompositionPassData.DebugMode.OUTLINES:
                mat.EnableKeyword(debugOutlinesKeyword);
                mat.DisableKeyword(debugLuminanceKeyword);
                break;
            case SketchCompositionPassData.DebugMode.LUMINANCE:
                mat.DisableKeyword(debugOutlinesKeyword);
                mat.EnableKeyword(debugLuminanceKeyword);
                break;
        }
    }

    public void Dispose()
    {
        
    }

    private static void ExecuteCompositionPass(PassData passData, RasterGraphContext context)
    {
        passData.material.SetTexture(outlinesShaderID, passData.outlineTexture);
        passData.material.SetTexture(luminanceShaderID, passData.luminanceTexture);
        Blitter.BlitTexture(context.cmd, passData.src, scaleBias, passData.material, 0);
    }
    
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var sketchData = frameData.Get<SketchRendererContext>();
        if(sketchData == null)
            return;
        
        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = "FinalSketchColor";
        dstDesc.clearBuffer = true;
        dstDesc.msaaSamples = MSAASamples.None;
            
        TextureHandle dst = renderGraph.CreateTexture(dstDesc);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            passData.material = mat;
            if (sketchData.OutlinesTexture.IsValid())
            {
                builder.UseTexture(sketchData.OutlinesTexture);
                passData.outlineTexture = sketchData.OutlinesTexture;
            }
            if (sketchData.LuminanceTexture.IsValid())
            {
                builder.UseTexture(sketchData.LuminanceTexture);
                passData.luminanceTexture = sketchData.LuminanceTexture;
            }

            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
            passData.src = dst;
            
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteCompositionPass(data, context));
        }
        
        resourceData.cameraColor = dst;
    }
}

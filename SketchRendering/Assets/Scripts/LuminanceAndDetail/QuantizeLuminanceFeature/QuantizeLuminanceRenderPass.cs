
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class QuantizeLuminanceRenderPass : ScriptableRenderPass, ISketchRenderPass<LuminancePassData>
{
    public string PassName
    {
        get { return "QuantizeLuminancePass"; }
    }

    private Material luminanceMat;
    private LuminancePassData passData;
    
    public void Setup(LuminancePassData passData, Material mat)
    {
        luminanceMat = mat;
        this.passData = passData;
        
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture = true;
        
        ConfigureMaterial();
    }

    public void ConfigureMaterial()
    {
        
    }

    public void Dispose()
    {
        
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if(resourceData.isActiveTargetBackBuffer)
            return;

        var sketchData = frameData.GetOrCreate<SketchRendererContext>();
        
        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = "LuminanceTexture";
        dstDesc.clearBuffer = true;
        dstDesc.msaaSamples = MSAASamples.None;

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        
        RenderGraphUtils.BlitMaterialParameters luminanceParams = new RenderGraphUtils.BlitMaterialParameters(resourceData.activeColorTexture, dst, luminanceMat, 0);
        renderGraph.AddBlitPass(luminanceParams, PassName);
        sketchData.LuminanceTexture = dst;
    }
}

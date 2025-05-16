using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SobelEdgeDetectionRenderPass : EdgeDetectionRenderPass
{
    protected override string PassName => "SobelEdgeDetectionPass";

    public override void Setup(EdgeDetectionMethod method, Material mat, float outlineThreshold)
    {
        base.Setup(method, mat, outlineThreshold);
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if(resourceData.isActiveTargetBackBuffer)
            return;
        
        //First, create a texture to mimic the color buffer
        //Pass the depth as a shader property and blit to created texture using edge material
        //Then, set color to blitted texture to see depth

        var src = resourceData.activeDepthTexture;

        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = PassName;
        dstDesc.clearBuffer = false;

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        
        RenderGraphUtils.BlitMaterialParameters parameters = new (src, dst, edgeDetectionMaterial, 0);
        renderGraph.AddBlitPass(parameters, passName: PassName);

        resourceData.cameraColor = dst;
    }
}

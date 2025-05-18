using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class DepthNormalsSilhouetteRenderPass : EdgeDetectionRenderPass
{
    protected override string PassName => "DepthNormalsSilhouette";

    private const string DEPTH_KEYWORD = "SOURCE_DEPTH";
    private const string DEPTH_NORMALS_KEYWORD = "SOURCE_DEPTH_NORMALS";
    
    public override void Setup(EdgeDetectionMethod method, EdgeDetectionSource source, Material mat, float outlineThreshold)
    {
        base.Setup(method, source, mat, outlineThreshold);

        switch (source)
        {
            case EdgeDetectionSource.COLOR:
                break;
            case EdgeDetectionSource.DEPTH:
                ConfigureInput(ScriptableRenderPassInput.Depth);
                mat.EnableKeyword(DEPTH_KEYWORD);
                mat.DisableKeyword(DEPTH_NORMALS_KEYWORD);
                break;
            case EdgeDetectionSource.DEPTH_NORMALS:
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
                mat.DisableKeyword(DEPTH_KEYWORD);
                mat.EnableKeyword(DEPTH_NORMALS_KEYWORD);
                break;
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = "OutlineTexture";
        dstDesc.clearBuffer = true;
        dstDesc.format = GraphicsFormat.R8G8B8A8_SRGB;
        dstDesc.msaaSamples = MSAASamples.None;

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        TextureHandle ping = renderGraph.CreateTexture(dstDesc);

        if (method == EdgeDetectionMethod.SOBEL)
        {
            //Pass 0 = Sobel Horizontal Pass
            RenderGraphUtils.BlitMaterialParameters horParams = new(dst, ping, edgeDetectionMaterial, 0);
            renderGraph.AddBlitPass(horParams, passName: PassName + "_SobelHorizontal");
            //Pass 1 = Sobel Vertical Pass
            RenderGraphUtils.BlitMaterialParameters verParams = new(ping, dst, edgeDetectionMaterial, 1);
            renderGraph.AddBlitPass(verParams, passName: PassName + "_SobelVertical");
        }

        resourceData.cameraColor = dst;
    }
}

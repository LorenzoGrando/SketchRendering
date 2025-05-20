using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class DepthNormalsSilhouetteRenderPass : EdgeDetectionRenderPass
{
    public override string PassName => "DepthNormalsSilhouette";
    
    
    public override void Setup(EdgeDetectionPassData passData, Material mat)
    {
        base.Setup(passData, mat);

        switch (passData.Method)
        {
            case EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3:
                mat.EnableKeyword(EdgeDetectionGlobalData.SOBEL_3X3_KEYWORD);
                mat.DisableKeyword(EdgeDetectionGlobalData.SOBEL_1X3_KEYWORD);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_1X3:
                mat.DisableKeyword(EdgeDetectionGlobalData.SOBEL_3X3_KEYWORD);
                mat.EnableKeyword(EdgeDetectionGlobalData.SOBEL_1X3_KEYWORD);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionMethod.ROBERTS_CROSS:
                mat.DisableKeyword(EdgeDetectionGlobalData.SOBEL_3X3_KEYWORD);
                mat.DisableKeyword(EdgeDetectionGlobalData.SOBEL_1X3_KEYWORD);
                break;
        }

        switch (passData.Source)
        {
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                break;
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
                ConfigureInput(ScriptableRenderPassInput.Depth);
                mat.EnableKeyword(EdgeDetectionGlobalData.DEPTH_KEYWORD);
                mat.DisableKeyword(EdgeDetectionGlobalData.DEPTH_NORMALS_KEYWORD);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
                mat.DisableKeyword(EdgeDetectionGlobalData.DEPTH_KEYWORD);
                mat.EnableKeyword(EdgeDetectionGlobalData.DEPTH_NORMALS_KEYWORD);
                break;
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;

        var sketchData = frameData.GetOrCreate<SketchRendererContext>();
        
        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = "OutlineTexture";
        dstDesc.clearBuffer = true;
        dstDesc.msaaSamples = MSAASamples.None;

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        
        //Ensure A has same precision for ping in sobel
        dstDesc.format = GraphicsFormat.R8G8B8A8_SRGB;
        TextureHandle ping = renderGraph.CreateTexture(dstDesc);

        if (passData.Method == EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_1X3 || passData.Method == EdgeDetectionGlobalData.EdgeDetectionMethod.SOBEL_3X3)
        {
            //Pass 0 = Sobel Horizontal Pass
            RenderGraphUtils.BlitMaterialParameters horParams = new(dst, ping, edgeDetectionMaterial, 0);
            renderGraph.AddBlitPass(horParams, passName: PassName + "_SobelHorizontal");
            //Pass 1 = Sobel Vertical Pass
            RenderGraphUtils.BlitMaterialParameters verParams = new(ping, dst, edgeDetectionMaterial, 1);
            renderGraph.AddBlitPass(verParams, passName: PassName + "_SobelVertical");
        }

        sketchData.OutlinesTexture = dst;
    }
}

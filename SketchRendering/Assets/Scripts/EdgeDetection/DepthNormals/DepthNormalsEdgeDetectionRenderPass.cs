using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class DepthNormalsEdgeDetectionRenderPass : EdgeDetectionRenderPass
{
    protected override string PassName => "DepthNormalsEdgeDetection";
    
    protected static readonly int depthOutlinesShaderID = Shader.PropertyToID("_DepthOutlinesTexture");
    protected static readonly int normalsOutlinesShaderID = Shader.PropertyToID("_NormalsOutlinesTexture");
    
    // Scale bias is used to control how the blit operation is done. The x and y parameter controls the scale
    // and z and w controls the offset. TAKEN FROM UNITY URP SAMPLES
    static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);

    class PassData
    {
        public TextureHandle src;
        public TextureHandle DepthOutlinesTexture;
        public TextureHandle NormalsOutlineTexture;
        public Material mat;
    }
    
    public override void Setup(EdgeDetectionMethod method, EdgeDetectionSource source, Material mat, float outlineThreshold)
    {
        base.Setup(method, source, mat, outlineThreshold);

        switch (source)
        {
            case EdgeDetectionSource.COLOR:
                break;
            case EdgeDetectionSource.DEPTH:
                ConfigureInput(ScriptableRenderPassInput.Depth); 
                break;
            case EdgeDetectionSource.DEPTH_NORMALS:
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal); 
                break;
        }
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        TextureHandle srcDepth = resourceData.activeDepthTexture;

        var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        dstDesc.name = "DepthOutlines";
        dstDesc.clearBuffer = false;

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        TextureHandle dstDepth = source == EdgeDetectionSource.DEPTH_NORMALS ? renderGraph.CreateTexture(dstDesc) : dst;

        //Pass 0 = Depth Outlines
        RenderGraphUtils.BlitMaterialParameters depthParams = new(srcDepth, dstDepth, edgeDetectionMaterial, 0);
        renderGraph.AddBlitPass(depthParams, passName: PassName);

        if (source == EdgeDetectionSource.DEPTH_NORMALS)
        {
            TextureHandle srcNormals = resourceData.cameraNormalsTexture;
            dstDesc.name = "NormalOutlines";
            TextureHandle dstNormals = renderGraph.CreateTexture(dstDesc);
            
            //Pass 1 = Normal Outlines
            RenderGraphUtils.BlitMaterialParameters normalParams = new(srcNormals, dstNormals, edgeDetectionMaterial, 1);
            renderGraph.AddBlitPass(normalParams, passName: PassName);
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName + "_Combine", out var passData))
            {
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);
                passData.src = dst;
                builder.UseTexture(dstDepth);
                passData.DepthOutlinesTexture = dstDepth;
                builder.UseTexture(dstNormals);
                passData.NormalsOutlineTexture = dstNormals;
                passData.mat = edgeDetectionMaterial;

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => CombineEdgesPass(data, context));
            }
            resourceData.cameraColor = dst;
        }
        else
        {
            resourceData.cameraColor = dstDepth;
        }
    }

    static void CombineEdgesPass(PassData data, RasterGraphContext context)
    {
        data.mat.SetTexture(depthOutlinesShaderID, data.DepthOutlinesTexture);
        data.mat.SetTexture(normalsOutlinesShaderID, data.NormalsOutlineTexture);
        
        //Pass 2 = Combine Outlines
        Blitter.BlitTexture(context.cmd, data.src, scaleBias, data.mat, 2);
    }
}

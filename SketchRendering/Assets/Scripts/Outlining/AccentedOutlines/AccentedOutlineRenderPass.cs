using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class AccentedOutlineRenderPass : ScriptableRenderPass, ISketchRenderPass<AccentedOutlinePassData>
{
    public string PassName => "AccentedOutlinesPass";
    
    private Material accentedMaterial;
    private AccentedOutlinePassData passData;

    protected readonly int distortionRateShaderID = Shader.PropertyToID("_DistortionRate");
    protected readonly int distortionStrengthShaderID = Shader.PropertyToID("_DistortionStrength");
    protected readonly int outlineMaskShaderID = Shader.PropertyToID("_OutlineMaskTex");
    
    protected readonly string DISTORT_OUTLINE_KEYWORD = "DISTORT_OUTLINES";
    protected readonly string MASK_OUTLINE_KEYWORD = "MASK_OUTLINES";
    
    private LocalKeyword DistortionKeyword;
    private LocalKeyword MaskKeyword;

    public void Setup(AccentedOutlinePassData passData, Material mat)
    {
        this.passData = passData;
        accentedMaterial = mat;
        
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture = false;
        
        ConfigureMaterial();
    }

    public void ConfigureMaterial()
    {
        DistortionKeyword = new LocalKeyword(accentedMaterial.shader, DISTORT_OUTLINE_KEYWORD);
        MaskKeyword = new LocalKeyword(accentedMaterial.shader, MASK_OUTLINE_KEYWORD);
        
        
        accentedMaterial.SetFloat(distortionRateShaderID, passData.Rate);
        //here we interpret 0 as disabled, and 1 as a clamped value (since high values destroy the effect)
        accentedMaterial.SetFloat(distortionStrengthShaderID, Mathf.Lerp(0f, 0.01f, passData.Strength));
        accentedMaterial.SetTexture(outlineMaskShaderID, passData.PencilOutlineMask);
        accentedMaterial.SetTextureScale(outlineMaskShaderID, passData.MaskScale);
        
        accentedMaterial.SetKeyword(DistortionKeyword, passData.Strength > 0);
        accentedMaterial.SetKeyword(MaskKeyword, passData.PencilOutlineMask != null);
    }

    public void Dispose() {}

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var sketchData = frameData.Get<SketchRendererContext>();
        if(sketchData == null)
            return;
        
        var dstDesc = renderGraph.GetTextureDesc(sketchData.OutlinesTexture);
        dstDesc.name = "AccentedOutlines";
        dstDesc.clearBuffer = true;
        dstDesc.msaaSamples = MSAASamples.None;
            
        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        //This shader has a single pass that handles all behaviours (distortion, outline brightness and texture masking) by compile keywords
        RenderGraphUtils.BlitMaterialParameters thickenParams = new RenderGraphUtils.BlitMaterialParameters(sketchData.OutlinesTexture, dst, accentedMaterial, 0);
        renderGraph.AddBlitPass(thickenParams, PassName);
        sketchData.OutlinesTexture = dst;
    }
}
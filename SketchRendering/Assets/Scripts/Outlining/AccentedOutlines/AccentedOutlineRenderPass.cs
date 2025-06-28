using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class AccentedOutlineRenderPass : ScriptableRenderPass, ISketchRenderPass<AccentedOutlinePassData>
{
    public string PassName => "AccentedOutlinesPass";
    
    private Material accentedMaterial;
    private AccentedOutlinePassData passData;
    
    private RTHandle bakedDistortionTexture;

    protected static readonly int bakedDistortionTexShaderID = Shader.PropertyToID("_BakedUVDistortionTex");
    protected static readonly int distortionRateShaderID = Shader.PropertyToID("_DistortionRate");
    protected static readonly int distortionStrengthShaderID = Shader.PropertyToID("_DistortionStrength");
    protected static readonly int outlineMaskShaderID = Shader.PropertyToID("_OutlineMaskTex");
    
    protected static readonly string DISTORT_OUTLINE_KEYWORD = "DISTORT_OUTLINES";
    protected static readonly string BAKE_DISTORT_OUTLINE_KEYWORD = "BAKED_DISTORT_OUTLINES";
    protected static readonly string MASK_OUTLINE_KEYWORD = "MASK_OUTLINES";
    
    private LocalKeyword DistortionKeyword;
    private LocalKeyword BakeDistortionKeyword;
    private LocalKeyword MaskKeyword;

    public void Setup(AccentedOutlinePassData passData, Material mat)
    {
        this.passData = passData;
        accentedMaterial = mat;
        
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture = false;

        if (this.passData.BakeDistortionDuringRuntime && bakedDistortionTexture == null)
        {
            //Declare with fixed size, since what we really want is to only ever have a single reference image
            //If a new calculation is declared, disabling the bake property and reenabling it will recreate the texture
            //TODO: Add native request to have the texture be rebaked
            Vector2Int dimensions = new Vector2Int(RTHandles.maxWidth, RTHandles.maxHeight);
            bakedDistortionTexture = RTHandles.Alloc(dimensions.x, dimensions.y, GraphicsFormat.R32G32_SFloat, enableRandomWrite: true, name: "_BakedUVDistortionTex");
        }
        else if (!this.passData.BakeDistortionDuringRuntime && bakedDistortionTexture != null)
        {
            bakedDistortionTexture.Release();
            bakedDistortionTexture = null;
        }
        
        ConfigureMaterial();
    }

    public void ConfigureMaterial()
    {
        DistortionKeyword = new LocalKeyword(accentedMaterial.shader, DISTORT_OUTLINE_KEYWORD);
        BakeDistortionKeyword = new LocalKeyword(accentedMaterial.shader, BAKE_DISTORT_OUTLINE_KEYWORD);
        MaskKeyword = new LocalKeyword(accentedMaterial.shader, MASK_OUTLINE_KEYWORD);
        
        
        accentedMaterial.SetFloat(distortionRateShaderID, passData.Rate);
        //here we interpret 0 as disabled, and 1 as a clamped value (since high values destroy the effect)
        accentedMaterial.SetFloat(distortionStrengthShaderID, Mathf.Lerp(0f, 0.01f, passData.Strength));
        accentedMaterial.SetTexture(outlineMaskShaderID, passData.PencilOutlineMask);
        accentedMaterial.SetTextureScale(outlineMaskShaderID, passData.MaskScale);
        
        accentedMaterial.SetKeyword(BakeDistortionKeyword, passData.BakeDistortionDuringRuntime);
        accentedMaterial.SetKeyword(DistortionKeyword, passData.Strength > 0 && !passData.BakeDistortionDuringRuntime);
        
        if(bakedDistortionTexture != null)
            accentedMaterial.SetTexture(bakedDistortionTexShaderID, bakedDistortionTexture);

        accentedMaterial.SetKeyword(MaskKeyword, passData.PencilOutlineMask != null);
    }

    public void Dispose()
    {
        if (bakedDistortionTexture != null)
        {
            bakedDistortionTexture.Release();
            bakedDistortionTexture = null;
        }
    }
    
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var sketchData = frameData.Get<SketchRendererContext>();
        if(sketchData == null)
            return;
        
        TextureDesc dstDesc = renderGraph.GetTextureDesc(sketchData.OutlinesTexture);
        dstDesc.name = "AccentedOutlines";
        dstDesc.clearBuffer = true;
        dstDesc.msaaSamples = MSAASamples.None;
        
        if (passData.BakeDistortionDuringRuntime && !sketchData.PrebakedDistortedUVs)
        {
            TextureHandle distortedDst = renderGraph.ImportTexture(bakedDistortionTexture);
            RenderGraphUtils.BlitMaterialParameters distortParams = new RenderGraphUtils.BlitMaterialParameters(sketchData.OutlinesTexture, distortedDst, accentedMaterial, 1);
            renderGraph.AddBlitPass(distortParams, PassName + "_BakeUVDistortion");
            sketchData.PrebakedDistortedUVs = true;
        }
        else if (!passData.BakeDistortionDuringRuntime && sketchData.PrebakedDistortedUVs)
        {
            sketchData.PrebakedDistortedUVs = false;
        }

        TextureHandle dst = renderGraph.CreateTexture(dstDesc);
        //This shader has a single pass that handles all behaviours (distortion, outline brightness and texture masking) by compile keywords
        RenderGraphUtils.BlitMaterialParameters thickenParams =
            new RenderGraphUtils.BlitMaterialParameters(sketchData.OutlinesTexture, dst, accentedMaterial, 0);
        renderGraph.AddBlitPass(thickenParams, PassName);
        sketchData.OutlinesTexture = dst;
    }
}
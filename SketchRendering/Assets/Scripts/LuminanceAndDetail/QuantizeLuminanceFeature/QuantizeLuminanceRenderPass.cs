
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
    
    private readonly int numTonesShaderID = Shader.PropertyToID("_NumTones");
    private readonly int tamFirstShaderID = Shader.PropertyToID("_Tam0_2");
    private readonly int tamFirstTexelShaderID = Shader.PropertyToID("_Tam0_2_TexelSize");
    private readonly int tamSecondShaderID = Shader.PropertyToID("_Tam3_5");
    private readonly int tamSecondTexelShaderID = Shader.PropertyToID("_Tam3_5_TexelSize");
    private readonly int tamThirdShaderID = Shader.PropertyToID("_Tam6_8");
    private readonly int tamThirdTexelShaderID = Shader.PropertyToID("_Tam6_8_TexelSize");
    private readonly int tamScalesShaderID = Shader.PropertyToID("_TamScales");
    private readonly int luminanceOffsetShaderID = Shader.PropertyToID("_LuminanceOffset");

    private readonly string SINGLE_TAM_KEYWORD = "TAM_SINGLE";
    private readonly string DOUBLE_TAM_KEYWORD = "TAM_DOUBLE";
    private readonly string TRIPLE_TAM_KEYWORD = "TAM_TRIPLE";
    private readonly string QUANTIZE_KEYWORD = "QUANTIZE";
    private readonly string UVS_SCREEN_SPACE_KEYWORD = "UVS_SCREEN_SPACE";
    private readonly string UVS_OBJECT_SPACE_KEYWORD = "UVS_OBJECT_SPACE";
    private readonly string SCREEN_SIZE_KEYWORD = "CONSTANT_SCREEN_SIZE";
    
    private LocalKeyword SingleKeyword;
    private LocalKeyword DoubleKeyword;
    private LocalKeyword TripleKeyword;
    private LocalKeyword QuantizeKeyword;
    private LocalKeyword UVsScreenSpaceKeyword;
    private LocalKeyword UVsObjectSpaceKeyword;
    
    // Scale bias is used to control how the blit operation is done. The x and y parameter controls the scale
    // and z and w controls the offset.
    static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);
    
    public void Setup(LuminancePassData passData, Material mat)
    {
        luminanceMat = mat;
        this.passData = passData;
        
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        requiresIntermediateTexture = true;
        
        ConfigureInput(ScriptableRenderPassInput.Color);
        
        ConfigureMaterial();
    }

    public void ConfigureMaterial()
    {
        luminanceMat.SetInt(numTonesShaderID, passData.ActiveTonalMap.ExpectedTones);
        luminanceMat.SetFloat(luminanceOffsetShaderID, passData.LuminanceOffset);
        luminanceMat.SetVector(tamScalesShaderID, new Vector4(passData.ToneScales.x, passData.ToneScales.y, 0, 0));
        
        SingleKeyword = new LocalKeyword(luminanceMat.shader, SINGLE_TAM_KEYWORD);
        DoubleKeyword = new LocalKeyword(luminanceMat.shader, DOUBLE_TAM_KEYWORD);
        TripleKeyword = new LocalKeyword(luminanceMat.shader, TRIPLE_TAM_KEYWORD);
        QuantizeKeyword = new LocalKeyword(luminanceMat.shader, QUANTIZE_KEYWORD);
        UVsScreenSpaceKeyword = new LocalKeyword(luminanceMat.shader, UVS_SCREEN_SPACE_KEYWORD);
        UVsObjectSpaceKeyword = new LocalKeyword(luminanceMat.shader, UVS_OBJECT_SPACE_KEYWORD);

        switch (passData.ActiveTonalMap.Tones.Length)
        {
            case var _ when passData.ActiveTonalMap.Tones.Length == 1:
                luminanceMat.SetKeyword(SingleKeyword, true);
                luminanceMat.SetKeyword(DoubleKeyword, false);
                luminanceMat.SetKeyword(TripleKeyword, false);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                luminanceMat.SetVector(tamFirstTexelShaderID, passData.ActiveTonalMap.Tones[0].GetTexelSize());
                break;
            case var _ when passData.ActiveTonalMap.Tones.Length == 2:
                luminanceMat.SetKeyword(SingleKeyword, false);
                luminanceMat.SetKeyword(DoubleKeyword, true);
                luminanceMat.SetKeyword(TripleKeyword, false);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                luminanceMat.SetVector(tamFirstTexelShaderID, passData.ActiveTonalMap.Tones[0].GetTexelSize());
                luminanceMat.SetTexture(tamSecondShaderID, passData.ActiveTonalMap.Tones[1]);
                luminanceMat.SetVector(tamSecondTexelShaderID, passData.ActiveTonalMap.Tones[1].GetTexelSize());
                break;
            case var _ when passData.ActiveTonalMap.Tones.Length == 3:
                luminanceMat.SetKeyword(SingleKeyword, false);
                luminanceMat.SetKeyword(DoubleKeyword, false);
                luminanceMat.SetKeyword(TripleKeyword, true);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                luminanceMat.SetVector(tamFirstTexelShaderID, passData.ActiveTonalMap.Tones[0].GetTexelSize());
                luminanceMat.SetTexture(tamSecondShaderID, passData.ActiveTonalMap.Tones[1]);
                luminanceMat.SetVector(tamSecondTexelShaderID, passData.ActiveTonalMap.Tones[1].GetTexelSize());
                luminanceMat.SetTexture(tamThirdShaderID, passData.ActiveTonalMap.Tones[2]);
                luminanceMat.SetVector(tamThirdTexelShaderID, passData.ActiveTonalMap.Tones[2].GetTexelSize());
                break;
        }

        switch (passData.ProjectionMethod)
        {
            case StrokeProjectionMethod.SCREEN_SPACE:
                luminanceMat.SetKeyword(UVsScreenSpaceKeyword, true);
                luminanceMat.SetKeyword(UVsObjectSpaceKeyword, false);
                break;
            case StrokeProjectionMethod.OBJECT_SPACE_TEXTURE:
                luminanceMat.SetKeyword(UVsScreenSpaceKeyword, false);
                luminanceMat.SetKeyword(UVsObjectSpaceKeyword, true);
                break;
        }
        luminanceMat.SetKeyword(QuantizeKeyword, !passData.SmoothTransitions);
    }

    public void Dispose()
    {
        
    }

    private class PassData
    {
        public TextureHandle src;
        public TextureHandle dst;
        public Material mat;
    }

    private static void ExecuteLuminance(PassData data, RasterGraphContext context)
    {
        Blitter.BlitTexture(context.cmd, data.src, scaleBias, data.mat, 0);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if(resourceData.isActiveTargetBackBuffer)
                return;
            
            if(this.passData.ProjectionMethod == StrokeProjectionMethod.OBJECT_SPACE_TEXTURE)
                builder.UseGlobalTexture(ScreenUVRenderUtils.GetUVTextureID, AccessFlags.Read);
            
            var sketchData = frameData.GetOrCreate<SketchRendererContext>();

            var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            dstDesc.name = "LuminanceTexture";
            dstDesc.format = GraphicsFormat.R8G8B8A8_UNorm;
            dstDesc.clearBuffer = true;
            dstDesc.msaaSamples = MSAASamples.None;

            TextureHandle dst = renderGraph.CreateTexture(dstDesc);
            
            builder.UseTexture(resourceData.activeColorTexture, AccessFlags.ReadWrite);
            passData.src = resourceData.activeColorTexture;
            builder.SetRenderAttachment(dst, 0, AccessFlags.ReadWrite);
            passData.dst = dst;

            passData.mat = luminanceMat;

            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteLuminance(data, context));
            sketchData.LuminanceTexture = dst;
        }
    }
}

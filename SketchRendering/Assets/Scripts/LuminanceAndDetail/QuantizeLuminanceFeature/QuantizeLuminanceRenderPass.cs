
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
    
    protected readonly int numTonesShaderID = Shader.PropertyToID("_NumTones");
    protected readonly int tamFirstShaderID = Shader.PropertyToID("_Tam0_2");
    protected readonly int tamSecondShaderID = Shader.PropertyToID("_Tam3_5");
    protected readonly int tamThirdShaderID = Shader.PropertyToID("_Tam6_8");
    protected readonly int tamScalesShaderID = Shader.PropertyToID("_TamScales");
    protected readonly int luminanceOffsetShaderID = Shader.PropertyToID("_LuminanceOffset");

    protected readonly string SINGLE_TAM_KEYWORD = "TAM_SINGLE";
    protected readonly string DOUBLE_TAM_KEYWORD = "TAM_DOUBLE";
    protected readonly string TRIPLE_TAM_KEYWORD = "TAM_TRIPLE";
    protected readonly string QUANTIZE_KEYWORD = "QUANTIZE";
    
    protected LocalKeyword SingleKeyword;
    protected LocalKeyword DoubleKeyword;
    protected LocalKeyword TripleKeyword;
    protected LocalKeyword QuantizeKeyword;
    
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

        switch (passData.ActiveTonalMap.Tones.Length)
        {
            case var _ when passData.ActiveTonalMap.Tones.Length == 1:
                luminanceMat.SetKeyword(SingleKeyword, true);
                luminanceMat.SetKeyword(DoubleKeyword, false);
                luminanceMat.SetKeyword(TripleKeyword, false);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                break;
            case var _ when passData.ActiveTonalMap.Tones.Length == 2:
                luminanceMat.SetKeyword(SingleKeyword, false);
                luminanceMat.SetKeyword(DoubleKeyword, true);
                luminanceMat.SetKeyword(TripleKeyword, false);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                luminanceMat.SetTexture(tamSecondShaderID, passData.ActiveTonalMap.Tones[1]);
                break;
            case var _ when passData.ActiveTonalMap.Tones.Length == 3:
                luminanceMat.SetKeyword(SingleKeyword, false);
                luminanceMat.SetKeyword(DoubleKeyword, false);
                luminanceMat.SetKeyword(TripleKeyword, true);
                luminanceMat.SetTexture(tamFirstShaderID, passData.ActiveTonalMap.Tones[0]);
                luminanceMat.SetTexture(tamSecondShaderID, passData.ActiveTonalMap.Tones[1]);
                luminanceMat.SetTexture(tamThirdShaderID, passData.ActiveTonalMap.Tones[2]);
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
            
            builder.UseGlobalTexture(ScreenUVRenderUtils.GetUVTextureID, AccessFlags.Read);
            
            var sketchData = frameData.GetOrCreate<SketchRendererContext>();

            var dstDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            dstDesc.name = "LuminanceTexture";
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

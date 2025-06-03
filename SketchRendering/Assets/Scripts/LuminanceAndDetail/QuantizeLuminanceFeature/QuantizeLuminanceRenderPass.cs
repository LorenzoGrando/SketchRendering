
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

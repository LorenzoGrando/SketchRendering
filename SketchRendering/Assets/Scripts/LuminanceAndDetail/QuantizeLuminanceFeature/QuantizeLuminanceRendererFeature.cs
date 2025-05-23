using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QuantizeLuminanceRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")] [Space(5)] [SerializeField]
    public LuminancePassData LuminanceData = new LuminancePassData();
    
    [HideInInspector]
    private Material luminanceMaterial;
    [SerializeField]
    private Shader quantizeLuminanceShader;
    
    private QuantizeLuminanceRenderPass luminanceRenderPass;

    public override void Create()
    {
        if(quantizeLuminanceShader == null)
            return;
        
        luminanceMaterial = CreateLuminanceMaterial();
        luminanceRenderPass = new QuantizeLuminanceRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.SceneView)
            return;

        if (!renderingData.postProcessingEnabled)
            return;

        if (!AreAllMaterialsValid())
            return;
        
        if(!LuminanceData.IsAllPassDataValid())
            return;
        

        luminanceRenderPass.Setup(LuminanceData, luminanceMaterial);
        renderer.EnqueuePass(luminanceRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        luminanceRenderPass?.Dispose();
        luminanceRenderPass = null;

        if (Application.isPlaying)
        {
            if (luminanceMaterial)
                Destroy(luminanceMaterial);
        }
    }

    private bool AreAllMaterialsValid()
    {
        return luminanceMaterial != null;
    }

    private Material CreateLuminanceMaterial()
    {
        Material mat = new Material(quantizeLuminanceShader);

        return mat;
    }
}
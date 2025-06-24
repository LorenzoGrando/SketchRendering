using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderUVsRendererFeature : ScriptableRendererFeature
{
    [Header("Base Parameters")] 
    [Space(5)] 
    public RenderUVsPassData uvsPassData;
    [SerializeField] private Shader renderUVsShader;
    
    private Material renderUVsMaterial;
    
    private RenderUVsRenderPass renderUVsRenderPass;
    
    public override void Create()
    {
        renderUVsMaterial = new Material(renderUVsShader);
        renderUVsRenderPass = new RenderUVsRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType == CameraType.SceneView)
            return;
        
        if(!renderingData.postProcessingEnabled)
            return;
        
        if(!renderingData.cameraData.postProcessEnabled)
            return;
        
        if(!AreAllMaterialsValid())
            return;

        renderUVsRenderPass.Setup(uvsPassData, renderUVsMaterial);
        renderer.EnqueuePass(renderUVsRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        renderUVsRenderPass?.Dispose();
        renderUVsRenderPass = null;
        
        if (Application.isPlaying)
        {
            if (renderUVsMaterial)
                Destroy(renderUVsMaterial);
        }
    }

    private bool AreAllMaterialsValid()
    {
        return renderUVsMaterial != null;
    }
}
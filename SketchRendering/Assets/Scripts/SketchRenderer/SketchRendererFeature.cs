using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SketchRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")] [Space(5)] [SerializeField]
    public SketchCompositionPassData passData = new SketchCompositionPassData();
    
    [SerializeField]
    private Shader sketchCompositionShader;
    
    [HideInInspector]
    private Material sketchMaterial;
    private SketchCompositionRenderPass sketchRenderPass;

    public override void Create()
    {
        sketchMaterial = CreateSketchMaterial();
        sketchRenderPass = new SketchCompositionRenderPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.SceneView)
            return;

        if (!renderingData.postProcessingEnabled)
            return;

        if (!AreAllMaterialsValid())
           return;
        
        if(!passData.IsAllPassDataValid())
            return;

        sketchRenderPass.Setup(passData, sketchMaterial);
        renderer.EnqueuePass(sketchRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        sketchRenderPass?.Dispose();
        sketchRenderPass = null;

        if (Application.isPlaying)
        {
            if (sketchMaterial)
                Destroy(sketchMaterial);
        }
    }

    private Material CreateSketchMaterial()
    {
        if(sketchCompositionShader == null)
            return null;
        
        Material material = new Material(sketchCompositionShader);
        return material;
    }

    private bool AreAllMaterialsValid()
    {
        return sketchMaterial != null;
    }
}
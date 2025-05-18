using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")]
    [Space(5)]
    public EdgeDetectionMethod EdgeDetection = EdgeDetectionMethod.SOBEL;
    [Range(0, 1)]
    public float OutlineThreshold = 0f;

    [SerializeField] [HideInInspector] [Reload("Scripts/EdgeDetection/Sobel/SobelEdgeDetection.shader")]
    private Shader sobelEdgeDetectionShader;
    [SerializeField] [HideInInspector] [Reload("Scripts/EdgeDetection/DepthNormals/DepthNormalsEdgeDetection.shader")]
    private Shader depthNormalsEdgeDetectionShader;
    private Shader outlineShader;
    
    private Material edgeDetectionMaterial;
    private Material outlineMaterial;
    
    private SobelEdgeDetectionRenderPass sobelEdgeDetectionPass;
    private DepthNormalsEdgeDetectionRenderPass depthNormalsEdgeDetectionPass;
    private SmoothOutlineRenderPass smoothOutlineRenderPass;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    private ScriptableRenderPass outlinePass;
    
    public override void Create()
    {
        edgeDetectionMaterial = CreateEdgeDetectionMaterial(EdgeDetection);
        edgeDetectionPass = CreateEdgeDetectionPass(EdgeDetection);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType == CameraType.SceneView)
            return;
        
        if(!renderingData.postProcessingEnabled)
            return;
        
        if(!AreAllMaterialsValid())
            return;
        
        edgeDetectionPass.Setup(EdgeDetection, edgeDetectionMaterial, OutlineThreshold);
        renderer.EnqueuePass(edgeDetectionPass);
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass?.Dispose();
        edgeDetectionPass = null;

        if (Application.isPlaying)
        {
            if (edgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            if (outlineMaterial)
                Destroy(outlineMaterial);
        }
    }

    private bool AreAllMaterialsValid()
    {
        return edgeDetectionMaterial != null; //&& outlineMaterial != null;
    }

    private Material CreateEdgeDetectionMaterial(EdgeDetectionMethod edgeDetectionMethod)
    {
        Material mat;
        switch (edgeDetectionMethod)
        {
            case EdgeDetectionMethod.SOBEL:
                mat =  new Material(sobelEdgeDetectionShader);
                break;
            case EdgeDetectionMethod.DEPTH:
            case EdgeDetectionMethod.DEPTH_NORMALS:
                mat =  new Material(depthNormalsEdgeDetectionShader);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edgeDetectionMethod), edgeDetectionMethod, null);
        }
        return mat;
    }

    private EdgeDetectionRenderPass CreateEdgeDetectionPass(EdgeDetectionMethod edgeDetectionMethod)
    {
        switch (edgeDetectionMethod)
        {
            case EdgeDetectionMethod.SOBEL:
                return new SobelEdgeDetectionRenderPass();
            case EdgeDetectionMethod.DEPTH:
            case EdgeDetectionMethod.DEPTH_NORMALS:
                return new DepthNormalsEdgeDetectionRenderPass();
            default:
                throw new ArgumentOutOfRangeException(nameof(edgeDetectionMethod), edgeDetectionMethod, null);
        }
    }
}

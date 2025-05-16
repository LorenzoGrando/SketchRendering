using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")]
    [Space(5)]
    public EdgeDetectionMethod EdgeDetection = EdgeDetectionMethod.SOBEL;
    [Range(0, 1)]
    public float OutlineThreshold = 0f;
    
    
    [Header("Components")]
    [Space(5)]
    [SerializeField] private Shader sobelEdgeDetectionShader;
    [SerializeField] private Shader depthNormalsEdgeDetectionShader;
    [SerializeField] private Shader outlineShader;
    
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
        
        edgeDetectionPass.Setup(EdgeDetection, edgeDetectionMaterial, OutlineThreshold);
        renderer.EnqueuePass(edgeDetectionPass);
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

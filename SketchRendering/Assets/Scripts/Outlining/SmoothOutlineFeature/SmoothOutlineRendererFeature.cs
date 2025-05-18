using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")]
    [Space(5)]
    public EdgeDetectionMethod EdgeDetectionMethod = EdgeDetectionMethod.SOBEL;
    public EdgeDetectionSource EdgeDetectionSource = EdgeDetectionSource.DEPTH_NORMALS;
    [Range(0, 1)]
    public float OutlineThreshold = 0f;

    [SerializeField]
    private Shader sobelEdgeDetectionShader;
    [SerializeField]
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
        edgeDetectionMaterial = CreateEdgeDetectionMaterial(EdgeDetectionSource);
        edgeDetectionPass = CreateEdgeDetectionPass(EdgeDetectionSource);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType == CameraType.SceneView)
            return;
        
        if(!renderingData.postProcessingEnabled)
            return;
        
        if(!AreAllMaterialsValid())
            return;
        
        edgeDetectionPass.Setup(EdgeDetectionMethod, EdgeDetectionSource, edgeDetectionMaterial, OutlineThreshold);
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

    private Material CreateEdgeDetectionMaterial(EdgeDetectionSource edgeDetectionMethod)
    {
        Material mat;
        switch (edgeDetectionMethod)
        {
            case EdgeDetectionSource.COLOR:
                mat = null;
                break;
            case EdgeDetectionSource.DEPTH:
            case EdgeDetectionSource.DEPTH_NORMALS:
                mat =  new Material(depthNormalsEdgeDetectionShader);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edgeDetectionMethod), edgeDetectionMethod, null);
        }
        return mat;
    }

    private EdgeDetectionRenderPass CreateEdgeDetectionPass(EdgeDetectionSource source)
    {
        switch (source)
        {
            case EdgeDetectionSource.COLOR:
                //return new SobelEdgeDetectionRenderPass();
            case EdgeDetectionSource.DEPTH:
            case EdgeDetectionSource.DEPTH_NORMALS:
                return new DepthNormalsSilhouetteRenderPass();
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}

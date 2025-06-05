using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")]
    [Space(5)]
    public EdgeDetectionPassData EdgeDetectionPassData = new EdgeDetectionPassData();
    private EdgeDetectionPassData CurrentPassData { get { return EdgeDetectionPassData.GetPassDataByVolume(); } }

    [SerializeField]
    private Shader sobelEdgeDetectionShader;
    [SerializeField]
    private Shader depthNormalsEdgeDetectionShader;
    
    private Material edgeDetectionMaterial;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    
    public override void Create()
    {
        edgeDetectionMaterial = CreateEdgeDetectionMaterial(CurrentPassData.Source);
        edgeDetectionPass = CreateEdgeDetectionPass(CurrentPassData.Source);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType == CameraType.SceneView)
            return;
        
        if(!renderingData.postProcessingEnabled)
            return;
        
        if(!AreAllMaterialsValid())
            return;
        
        if(!CurrentPassData.IsAllPassDataValid())
            return;
        
        edgeDetectionPass.Setup(CurrentPassData, edgeDetectionMaterial);
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
        }
    }

    private bool AreAllMaterialsValid()
    {
        return edgeDetectionMaterial != null; //&& outlineMaterial != null;
    }

    private Material CreateEdgeDetectionMaterial(EdgeDetectionGlobalData.EdgeDetectionSource edgeDetectionMethod)
    {
        Material mat = null;
        switch (edgeDetectionMethod)
        {
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                if(sobelEdgeDetectionShader != null)
                    mat = new Material(sobelEdgeDetectionShader);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                if(depthNormalsEdgeDetectionShader != null)
                    mat =  new Material(depthNormalsEdgeDetectionShader);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edgeDetectionMethod), edgeDetectionMethod, null);
        }
        return mat;
    }

    private EdgeDetectionRenderPass CreateEdgeDetectionPass(EdgeDetectionGlobalData.EdgeDetectionSource source)
    {
        switch (source)
        {
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                //return new SobelEdgeDetectionRenderPass();
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                return new DepthNormalsSilhouetteRenderPass();
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}

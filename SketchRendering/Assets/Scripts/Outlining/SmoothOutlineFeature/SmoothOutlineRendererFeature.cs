using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Parameters")]
    [Space(5)]
    public EdgeDetectionPassData EdgeDetectionPassData = new EdgeDetectionPassData();
    private EdgeDetectionPassData CurrentEdgeDetectionPassData { get { return EdgeDetectionPassData.GetPassDataByVolume(); } }
    
    public ThicknessDilationPassData ThicknessPassData = new ThicknessDilationPassData();
    private ThicknessDilationPassData CurrentThicknessPassData { get { return ThicknessPassData.GetPassDataByVolume(); } }

    [SerializeField]
    private Shader sobelEdgeDetectionShader;
    [SerializeField]
    private Shader depthNormalsEdgeDetectionShader;

    [SerializeField] private Shader thicknessDilationDetectionShader;
    
    private Material edgeDetectionMaterial;
    private Material thicknessDilationMaterial;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    private ThicknessDilationRenderPass thicknessDilationPass;
    
    public override void Create()
    {
        edgeDetectionMaterial = CreateEdgeDetectionMaterial(CurrentEdgeDetectionPassData.Source);
        edgeDetectionPass = CreateEdgeDetectionPass(CurrentEdgeDetectionPassData.Source);
        
        thicknessDilationMaterial = new Material(thicknessDilationDetectionShader);
        thicknessDilationPass = new ThicknessDilationRenderPass();
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

        if (CurrentEdgeDetectionPassData.IsAllPassDataValid())
        {
            edgeDetectionPass.Setup(CurrentEdgeDetectionPassData, edgeDetectionMaterial);
            renderer.EnqueuePass(edgeDetectionPass);
        }

        if (CurrentThicknessPassData.IsAllPassDataValid())
        {
            thicknessDilationPass.Setup(CurrentThicknessPassData, thicknessDilationMaterial);
            renderer.EnqueuePass(thicknessDilationPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass?.Dispose();
        edgeDetectionPass = null;
        
        thicknessDilationPass?.Dispose();
        thicknessDilationPass = null;

        if (Application.isPlaying)
        {
            if (edgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            if(thicknessDilationMaterial)
                Destroy(thicknessDilationMaterial);
        }
    }

    private bool AreAllMaterialsValid()
    {
        return edgeDetectionMaterial != null && thicknessDilationMaterial != null;
    }

    private Material CreateEdgeDetectionMaterial(EdgeDetectionGlobalData.EdgeDetectionSource edgeDetectionMethod)
    {
        Material mat = null;
        switch (edgeDetectionMethod)
        {
            /*
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                if(sobelEdgeDetectionShader != null)
                    mat = new Material(sobelEdgeDetectionShader);
                break;
                */
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
            //case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                //return new SobelEdgeDetectionRenderPass();
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                return new DepthNormalsSilhouetteRenderPass();
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SmoothOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Base Parameters")]
    [Space(5)]
    public EdgeDetectionPassData EdgeDetectionPassData = new EdgeDetectionPassData();
    private EdgeDetectionPassData CurrentEdgeDetectionPassData { get { return EdgeDetectionPassData.GetPassDataByVolume(); } }
    
    public ThicknessDilationPassData ThicknessPassData = new ThicknessDilationPassData();
    private ThicknessDilationPassData CurrentThicknessPassData { get { return ThicknessPassData.GetPassDataByVolume(); } }

    [Header("Accented Effects")] 
    [Space(5)]
    public bool UseAccentedOutlines;
    public AccentedOutlinePassData AccentedOutlinePassData = new AccentedOutlinePassData();
    private AccentedOutlinePassData CurrentAccentOutlinePassData { get { return AccentedOutlinePassData.GetPassDataByVolume(); } }

    [SerializeField] private Shader sobelEdgeDetectionShader;
    [SerializeField] private Shader depthNormalsEdgeDetectionShader;

    [SerializeField] private Shader thicknessDilationDetectionShader;
    [SerializeField] private Shader accentedOutlinesShader;
    
    private Material edgeDetectionMaterial;
    private Material thicknessDilationMaterial;
    private Material accentedOutlinesMaterial;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    private ThicknessDilationRenderPass thicknessDilationPass;
    private AccentedOutlineRenderPass accentedOutlinePass;
    
    public override void Create()
    {
        edgeDetectionMaterial = CreateEdgeDetectionMaterial(CurrentEdgeDetectionPassData.Source);
        edgeDetectionPass = CreateEdgeDetectionPass(CurrentEdgeDetectionPassData.Source);
        
        thicknessDilationMaterial = new Material(thicknessDilationDetectionShader);
        thicknessDilationPass = new ThicknessDilationRenderPass();
        
        accentedOutlinesMaterial = new Material(accentedOutlinesShader);
        accentedOutlinePass = new AccentedOutlineRenderPass();
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

        if (UseAccentedOutlines && CurrentAccentOutlinePassData.IsAllPassDataValid())
        {
            accentedOutlinePass.Setup(CurrentAccentOutlinePassData, accentedOutlinesMaterial);
            renderer.EnqueuePass(accentedOutlinePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass?.Dispose();
        edgeDetectionPass = null;
        
        thicknessDilationPass?.Dispose();
        thicknessDilationPass = null;
        
        accentedOutlinePass?.Dispose();
        accentedOutlinePass = null;

        if (Application.isPlaying)
        {
            if (edgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            if(thicknessDilationMaterial)
                Destroy(thicknessDilationMaterial);
            if(accentedOutlinesMaterial)
                Destroy(accentedOutlinesMaterial);
        }
    }

    private bool AreAllMaterialsValid()
    {
        return edgeDetectionMaterial != null && thicknessDilationMaterial != null && accentedOutlinesMaterial != null;
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

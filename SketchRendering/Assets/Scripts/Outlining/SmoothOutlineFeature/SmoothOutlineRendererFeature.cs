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
    public AccentedOutlinePassData AccentedOutlinePassData = new AccentedOutlinePassData();
    public AccentedOutlinePassData CurrentAccentOutlinePassData { get { return AccentedOutlinePassData.GetPassDataByVolume(); } }

    [SerializeField] private Shader sobelEdgeDetectionShader;
    [SerializeField] private Shader depthNormalsEdgeDetectionShader;
    [SerializeField] private Shader colorEdgeDetectionShader;
    [SerializeField] private Shader edgeDetectionCompositorShader;

    [SerializeField] private Shader thicknessDilationDetectionShader;
    [SerializeField] private Shader accentedOutlinesShader;
    
    private Material edgeDetectionMaterial;
    private Material secondaryEdgeDetectionMaterial;
    private Material edgeCompositorMaterial;
    private Material thicknessDilationMaterial;
    private Material accentedOutlinesMaterial;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    private EdgeDetectionRenderPass secondaryEdgeDetectionPass;
    private EdgeCompositorRenderPass edgeCompositorPass;
    private ThicknessDilationRenderPass thicknessDilationPass;
    private AccentedOutlineRenderPass accentedOutlinePass;
    
    public override void Create()
    {
        //Material accumulation still requires relevant direction data, so ensure this is the case
        EdgeDetectionPassData.OutputType = EdgeDetectionGlobalData.EdgeDetectionOutputType.OUTPUT_DIRECTION_DATA_VECTOR;

        if (CurrentEdgeDetectionPassData.Source != EdgeDetectionGlobalData.EdgeDetectionSource.ALL)
        {
            edgeDetectionMaterial = CreateEdgeDetectionMaterial(CurrentEdgeDetectionPassData.Source);
            edgeDetectionPass = CreateEdgeDetectionPass(CurrentEdgeDetectionPassData.Source);
            ReleaseSecondaryEdgeDetectionComponents();
        }
        else
        {
            edgeDetectionMaterial = CreateEdgeDetectionMaterial(EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS);
            edgeDetectionPass = CreateEdgeDetectionPass(EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS);
            secondaryEdgeDetectionMaterial = CreateEdgeDetectionMaterial(EdgeDetectionGlobalData.EdgeDetectionSource.COLOR);
            secondaryEdgeDetectionPass = CreateEdgeDetectionPass(EdgeDetectionGlobalData.EdgeDetectionSource.COLOR);
        }

        edgeCompositorMaterial = new Material(edgeDetectionCompositorShader);
        edgeCompositorPass = new EdgeCompositorRenderPass();

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
        
        if(!AreCurrentDynamicsValid())
            Create();
        
        if(!AreAllMaterialsValid())
            return;

        if (CurrentEdgeDetectionPassData.IsAllPassDataValid())
        {
            if (CurrentEdgeDetectionPassData.Source != EdgeDetectionGlobalData.EdgeDetectionSource.ALL)
            {
                edgeDetectionPass.Setup(CurrentEdgeDetectionPassData, edgeDetectionMaterial);
                edgeDetectionPass.SetSecondary(false);
                renderer.EnqueuePass(edgeDetectionPass);
            }
            else
            {
                EdgeDetectionPassData primaryData = CurrentEdgeDetectionPassData;
                primaryData.Source = EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS;
                edgeDetectionPass.Setup(primaryData, edgeDetectionMaterial);
                edgeDetectionPass.SetSecondary(false);
                
                EdgeDetectionPassData secondaryData = CurrentEdgeDetectionPassData;
                secondaryData.Source = EdgeDetectionGlobalData.EdgeDetectionSource.COLOR;
                renderer.EnqueuePass(edgeDetectionPass);
                secondaryEdgeDetectionPass.Setup(secondaryData, secondaryEdgeDetectionMaterial);
                secondaryEdgeDetectionPass.SetSecondary(true);
                renderer.EnqueuePass(secondaryEdgeDetectionPass);
                
                edgeCompositorPass.Setup(edgeCompositorMaterial);
                renderer.EnqueuePass(edgeCompositorPass);
            }
        }

        if (CurrentThicknessPassData.IsAllPassDataValid())
        {
            thicknessDilationPass.Setup(CurrentThicknessPassData, thicknessDilationMaterial);
            renderer.EnqueuePass(thicknessDilationPass);
        }

        if (CurrentAccentOutlinePassData.IsAllPassDataValid())
        {
            accentedOutlinePass.Setup(CurrentAccentOutlinePassData, accentedOutlinesMaterial);
            renderer.EnqueuePass(accentedOutlinePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass?.Dispose();
        edgeDetectionPass = null;
        
        secondaryEdgeDetectionPass?.Dispose();
        secondaryEdgeDetectionPass = null;

        edgeCompositorPass = null;
        
        thicknessDilationPass?.Dispose();
        thicknessDilationPass = null;
        
        accentedOutlinePass?.Dispose();
        accentedOutlinePass = null;

        if (Application.isPlaying)
        {
            if (edgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            if(secondaryEdgeDetectionMaterial)
                Destroy(secondaryEdgeDetectionMaterial);
            if(edgeCompositorMaterial)
                Destroy(edgeCompositorMaterial);
            if(thicknessDilationMaterial)
                Destroy(thicknessDilationMaterial);
            if(accentedOutlinesMaterial)
                Destroy(accentedOutlinesMaterial);
        }
    }

    private void ReleaseSecondaryEdgeDetectionComponents()
    {
        secondaryEdgeDetectionPass?.Dispose();
        secondaryEdgeDetectionPass = null;
        
        if (Application.isPlaying)
        {
            if (secondaryEdgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            secondaryEdgeDetectionMaterial = null;
        }
    }

    private bool AreAllMaterialsValid()
    {
        return edgeDetectionMaterial != null && edgeCompositorMaterial != null && thicknessDilationMaterial != null && accentedOutlinesMaterial != null;
    }

    private bool AreCurrentDynamicsValid()
    {
        switch (CurrentEdgeDetectionPassData.Source)
        {
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                return (edgeDetectionMaterial != null && edgeDetectionMaterial.shader == colorEdgeDetectionShader);
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                return (edgeDetectionMaterial != null && edgeDetectionMaterial.shader == depthNormalsEdgeDetectionShader);
            case EdgeDetectionGlobalData.EdgeDetectionSource.ALL:
                return (edgeDetectionMaterial != null && edgeDetectionMaterial.shader == depthNormalsEdgeDetectionShader) && 
                       (secondaryEdgeDetectionMaterial != null && secondaryEdgeDetectionMaterial.shader == colorEdgeDetectionShader);
        }
        
        return false;
    }

    private Material CreateEdgeDetectionMaterial(EdgeDetectionGlobalData.EdgeDetectionSource edgeDetectionMethod)
    {
        Material mat = null;
        switch (edgeDetectionMethod)
        {
            case EdgeDetectionGlobalData.EdgeDetectionSource.COLOR:
                if(colorEdgeDetectionShader != null)
                    mat = new Material(colorEdgeDetectionShader);
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
                return new ColorSilhouetteRenderPass();
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH:
            case EdgeDetectionGlobalData.EdgeDetectionSource.DEPTH_NORMALS:
                return new DepthNormalsSilhouetteRenderPass();
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }
}

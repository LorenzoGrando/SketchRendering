using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SketchOutlineRendererFeature : ScriptableRendererFeature
{
    [Header("Base Parameters")]
    [Space(5)]
    public EdgeDetectionPassData EdgeDetectionPassData = new EdgeDetectionPassData();
    private EdgeDetectionPassData CurrentEdgeDetectionPassData { get { return EdgeDetectionPassData.GetPassDataByVolume(); } }
    public SketchStrokesPassData SketchStrokesPassData = new SketchStrokesPassData();
    private SketchStrokesPassData CurrentSketchStrokesPassData { get { return SketchStrokesPassData.GetPassDataByVolume(); } }
    
    [SerializeField] private Shader sobelEdgeDetectionShader;
    [SerializeField] private Shader depthNormalsEdgeDetectionShader;
    [SerializeField] private Shader colorEdgeDetectionShader;
    [SerializeField] private Shader edgeDetectionCompositorShader;
    [SerializeField] private ComputeShader sketchStrokesComputeShader;
    
    private Material edgeDetectionMaterial;
    private Material secondaryEdgeDetectionMaterial;
    private Material edgeCompositorMaterial;
    
    private EdgeDetectionRenderPass edgeDetectionPass;
    private EdgeDetectionRenderPass secondaryEdgeDetectionPass;
    private EdgeCompositorRenderPass edgeCompositorPass;
    private SketchStrokesComputeRenderPass strokesComputePass;
    
    public override void Create()
    {
        //This pass needs angles to calculate stroke directins, so set this here
        EdgeDetectionPassData.OutputType = EdgeDetectionGlobalData.EdgeDetectionOutputType.OUTPUT_DIRECTION_DATA_ANGLE;

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

        strokesComputePass = new SketchStrokesComputeRenderPass();
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

        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("[SketchOutlineRendererFeature] Compute Shader Support is not available on this platform.");
            return;
        }

        if (CurrentEdgeDetectionPassData.IsAllPassDataValid())
        {
            if (CurrentEdgeDetectionPassData.Source != EdgeDetectionGlobalData.EdgeDetectionSource.ALL)
            {
                edgeDetectionPass.Setup(CurrentEdgeDetectionPassData, edgeDetectionMaterial);
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

        if (SketchStrokesPassData.IsAllPassDataValid())
        {
            //TODO: The variant sobel kernel produces directions perpendicular to the sillhouete direction
            //Honestly, i couldn`t quite figure out why, and ideally this wouldnt even be necessary, but its the solution i found for now.
            SketchStrokesPassData sketchStrokesPassData = CurrentSketchStrokesPassData;
            sketchStrokesPassData.ConfigurePerpendicularDirection(CurrentEdgeDetectionPassData.Method);
            strokesComputePass.Setup(sketchStrokesPassData, edgeDetectionMaterial, sketchStrokesComputeShader);
            renderer.EnqueuePass(strokesComputePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass?.Dispose();
        edgeDetectionPass = null;
        
        secondaryEdgeDetectionPass?.Dispose();
        secondaryEdgeDetectionPass = null;
        
        edgeCompositorPass = null;

        strokesComputePass?.Dispose();
        strokesComputePass = null;

        if (Application.isPlaying)
        {
            if (edgeDetectionMaterial)
                Destroy(edgeDetectionMaterial);
            if(secondaryEdgeDetectionMaterial)
                Destroy(secondaryEdgeDetectionMaterial);
            if(edgeCompositorMaterial)
                Destroy(edgeCompositorMaterial);
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
        return edgeDetectionMaterial != null && edgeCompositorMaterial != null && (CurrentEdgeDetectionPassData.Source != EdgeDetectionGlobalData.EdgeDetectionSource.ALL || secondaryEdgeDetectionMaterial != null);
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
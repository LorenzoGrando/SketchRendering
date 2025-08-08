using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public abstract class EdgeDetectionRenderPass : ScriptableRenderPass, ISketchRenderPass<EdgeDetectionPassData>
{
    public abstract string PassName { get; }
    
    protected Material edgeDetectionMaterial;
    protected EdgeDetectionPassData passData;
    
    protected static readonly int outlineOffsetShaderID = Shader.PropertyToID("_OutlineOffset");
    protected static readonly int outlineThresholdShaderID = Shader.PropertyToID("_OutlineThreshold");
    protected static readonly int outlineDistanceFalloffShaderID = Shader.PropertyToID("_OutlineDistanceFalloff");
    protected static readonly int outlineAngleSensitivityShaderID = Shader.PropertyToID("_OutlineShallowThresholdSensitivity");
    protected static readonly int outlineAngleConstraintShaderID = Shader.PropertyToID("_OutlineShallowThresholdStrength");
    protected static readonly int outlineNormalSensitivityShaderID = Shader.PropertyToID("_OutlineNormalDistanceSensitivity");
    
    protected LocalKeyword outputGreyscaleKeyword;
    protected LocalKeyword outputDirectionAngleDataKeyword;
    protected LocalKeyword outputDirectionVectorDataKeyword;


    public virtual void Setup(EdgeDetectionPassData passData, Material mat)
    {
        this.passData = passData;
        edgeDetectionMaterial = mat;
        
        //inherited
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        ConfigureMaterial();
    }

    public virtual void ConfigureMaterial()
    {
        outputGreyscaleKeyword = new LocalKeyword(edgeDetectionMaterial.shader, EdgeDetectionGlobalData.OUTPUT_GREYSCALE_KEYWORD);
        outputDirectionAngleDataKeyword = new LocalKeyword(edgeDetectionMaterial.shader, EdgeDetectionGlobalData.OUPUT_DIRECTION_ANGLE_KEYWORD);
        outputDirectionVectorDataKeyword = new LocalKeyword(edgeDetectionMaterial.shader, EdgeDetectionGlobalData.OUPUT_DIRECTION_VECTOR_KEYWORD);
        
        edgeDetectionMaterial.SetInteger(outlineOffsetShaderID, passData.OutlineOffset);
        edgeDetectionMaterial.SetFloat(outlineThresholdShaderID, passData.OutlineThreshold);
        edgeDetectionMaterial.SetFloat(outlineDistanceFalloffShaderID, passData.OutlineDistanceFalloff);
        edgeDetectionMaterial.SetFloat(outlineAngleSensitivityShaderID, passData.OutlineAngleSensitivity);
        edgeDetectionMaterial.SetFloat(outlineAngleConstraintShaderID, passData.OutlineAngleConstraint);
        edgeDetectionMaterial.SetFloat(outlineNormalSensitivityShaderID, passData.OutlineNormalSensitivity);

        switch (passData.OutputType)
        {
            case EdgeDetectionGlobalData.EdgeDetectionOutputType.OUTPUT_GREYSCALE:
                edgeDetectionMaterial.SetKeyword(outputGreyscaleKeyword, true);
                edgeDetectionMaterial.SetKeyword(outputDirectionAngleDataKeyword, false);
                edgeDetectionMaterial.SetKeyword(outputDirectionVectorDataKeyword, false);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionOutputType.OUTPUT_DIRECTION_DATA_ANGLE:
                edgeDetectionMaterial.SetKeyword(outputGreyscaleKeyword, false);
                edgeDetectionMaterial.SetKeyword(outputDirectionAngleDataKeyword, true);
                edgeDetectionMaterial.SetKeyword(outputDirectionVectorDataKeyword, false);
                break;
            case EdgeDetectionGlobalData.EdgeDetectionOutputType.OUTPUT_DIRECTION_DATA_VECTOR:
                edgeDetectionMaterial.SetKeyword(outputGreyscaleKeyword, false);
                edgeDetectionMaterial.SetKeyword(outputDirectionAngleDataKeyword, false);
                edgeDetectionMaterial.SetKeyword(outputDirectionVectorDataKeyword, true);
                break;
        }
    }

    public virtual void Dispose() {}
    
    public abstract override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData);
}

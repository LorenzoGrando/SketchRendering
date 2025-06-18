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
    protected static readonly int outlineThicknessShaderID = Shader.PropertyToID("_OutlineThickness");
    protected static readonly int outlineAngleSensitivityShaderID = Shader.PropertyToID("_OutlineShallowThresholdSensitivity");
    protected static readonly int outlineAngleConstraintShaderID = Shader.PropertyToID("_OutlineShallowThresholdStrength");

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
        edgeDetectionMaterial.SetFloat(outlineOffsetShaderID, passData.OutlineOffset);
        edgeDetectionMaterial.SetFloat(outlineThresholdShaderID, passData.OutlineThreshold);
        edgeDetectionMaterial.SetFloat(outlineAngleSensitivityShaderID, passData.OutlineAngleSensitivity);
        edgeDetectionMaterial.SetFloat(outlineAngleConstraintShaderID, passData.OutlineAngleConstraint);
    }

    public virtual void Dispose() {}
    
    public abstract override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData);
}

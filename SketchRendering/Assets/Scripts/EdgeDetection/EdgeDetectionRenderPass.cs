using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public abstract class EdgeDetectionRenderPass : ScriptableRenderPass
{
    protected abstract string PassName { get; }

    protected EdgeDetectionMethod method;
    protected Material edgeDetectionMaterial;
    protected float outlineThreshold;
    
    protected static readonly int outlineThresholdShaderID = Shader.PropertyToID("_OutlineThreshold");

    public virtual void Setup(EdgeDetectionMethod method, Material mat, float outlineThreshold)
    {
        this.method = method;
        edgeDetectionMaterial = mat;
        this.outlineThreshold = outlineThreshold;
        ConfigureMaterial();
    }

    public virtual void ConfigureMaterial()
    {
        edgeDetectionMaterial.SetFloat(outlineThresholdShaderID, outlineThreshold);
    }
    
    public abstract override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData);
}

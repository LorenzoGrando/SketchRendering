using UnityEngine;

public interface ISketchRenderPass<T> where T : ISketchRenderPassData
{
    public string PassName { get; }
    
    public void Setup(T passData, Material mat);
    
    public void ConfigureMaterial();

    public void Dispose();
}

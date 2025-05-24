using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
[ExecuteAlways]
public class TAMGenerator : MonoBehaviour
{
    //TODO: Hide later
    public ComputeShader TAMGeneratorShader;
    public Shader TAMShader;
    [Range(1, 4096)]
    public int Dimension;
    public TAMStrokeData StrokeData;
    
    //Editor assets
    private RenderTexture targetRT;
    private Material material;
    
    //Compute Data
    private readonly string STROKE_KERNEL = "ApplyStroke";
    private int csStrokeKernelID;
    private Vector3Int csStrokeKernelThreads;
    private ComputeBuffer strokeDataBuffer;
    
    private readonly int RENDER_TEXTURE_ID = Shader.PropertyToID("_StrokeResult");
    private readonly int STROKE_DATA_ID = Shader.PropertyToID("_StrokeData");
    private readonly int DIMENSION_ID = Shader.PropertyToID("_Dimension");
    
    public void OnEnable()
    {
        if(TAMShader == null)
            return;
        CreateMaterial();
    }

    public void OnDisable()
    {
        ReleaseBuffers();
    }
    
    public void OnValidate()
    {
        if(TAMGeneratorShader == null)
            return;
        
        CreateOrUpdateTarget();
        ConfigureBuffers();
        PrepareComputeData();
        ApplyStrokeKernel();
        
        //TEMP
        if (targetRT && material)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            material.mainTexture = targetRT;
            mr.material = material;
        }
    }
    
    #region Asset Prep
    private void CreateOrUpdateTarget()
    {
        if (targetRT != null)
        {
            targetRT.Release();
            targetRT = null;
        }

        targetRT = CreateRT(Dimension);
    }

    private void CreateMaterial()
    {
        material = new Material(TAMShader);
        material.hideFlags = HideFlags.HideAndDontSave;
    }
    
    private RenderTexture CreateRT(int dimension)
    {
        RenderTexture rt = new RenderTexture(dimension, dimension, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.None);
        rt.enableRandomWrite = true;
        rt.hideFlags = HideFlags.HideAndDontSave;
        Graphics.Blit(Texture2D.whiteTexture, rt);
        return rt;
    }
    
    #endregion
    
    #region Compute Prep

    private void PrepareComputeData()
    {
        if (TAMGeneratorShader.HasKernel(STROKE_KERNEL))
        {
            csStrokeKernelID = TAMGeneratorShader.FindKernel(STROKE_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csStrokeKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csStrokeKernelThreads = new Vector3Int(
                Mathf.CeilToInt((float)Dimension / groupsX), 
                Mathf.CeilToInt((float)Dimension / groupsY), 
                1);
        }
        
        if(targetRT != null)
            TAMGeneratorShader.SetTexture(csStrokeKernelID, RENDER_TEXTURE_ID, targetRT);

        if (strokeDataBuffer != null)
            TAMGeneratorShader.SetBuffer(csStrokeKernelID, STROKE_DATA_ID, strokeDataBuffer);
        
        TAMGeneratorShader.SetInt(DIMENSION_ID, Dimension);
    }

    private void ConfigureBuffers()
    {
        ReleaseBuffers();

        strokeDataBuffer = new ComputeBuffer(1, StrokeData.GetStrideLength(), ComputeBufferType.Default,
            ComputeBufferMode.Immutable);
        strokeDataBuffer.SetData(new TAMStrokeData[] {StrokeData});
    }

    private void ReleaseBuffers()
    {
        if (strokeDataBuffer != null)
        {
            strokeDataBuffer.Release();
            strokeDataBuffer = null;
        }
    }
    
    
    private void ApplyStrokeKernel()
    {
        TAMGeneratorShader.Dispatch(csStrokeKernelID, csStrokeKernelThreads.x, csStrokeKernelThreads.y, csStrokeKernelThreads.z);
    }
    
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[ExecuteAlways]
public class TAMGenerator : MonoBehaviour
{
    //TODO: Hide later
    public ComputeShader TAMGeneratorShader;
    public Shader TAMShader;
    [Range(1, 4096)]
    public int Dimension;
    [Range(1, 100)]
    public int IterationsPerStroke;
    public TAMStrokeAsset StrokeDataAsset;
    
    //Editor assets
    private RenderTexture targetRT;
    private Material material;
    
    //Compute Data
    private readonly string APPLY_STROKE_KERNEL = "ApplyStrokeIterated";
    private readonly string TONE_FILL_RATE_KERNEL = "FindAverageTextureFillRate";
    private readonly string BLIT_STROKE_KERNEL = "BlitFinalSelectedStroke";
    private int csApplyStrokeKernelID;
    private int csFillRateKernelID;
    private int csBlitStrokeKernelID;
    private Vector3Int csApplyStrokeKernelThreads;
    private Vector3Int csFillRateKernelThreads;
    private Vector3Int csBlitStrokeKernelThreads;
    private ComputeBuffer strokeDataBuffers;
    private ComputeBuffer[] strokeIterationTextureBuffers;
    private ComputeBuffer strokeTextureTonesBuffer;
    private ComputeBuffer strokeReducedSource;
    
    private readonly int RENDER_TEXTURE_ID = Shader.PropertyToID("_OriginalSource");
    private readonly int REDUCED_SOURCE_ID = Shader.PropertyToID("_ReducedSource");
    private readonly int STROKE_DATA_ID = Shader.PropertyToID("_StrokeData");
    private readonly int ITERATION_STEP_TEXTURE_ID = Shader.PropertyToID("_IterationOutputs");
    private readonly int TONE_RESULTS_ID = Shader.PropertyToID("_ToneResults");
    private readonly int ITERATIONS_ID = Shader.PropertyToID("_Iteration");
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
        ConfigureGeneratorData();
    }

    public void ConfigureGeneratorData()
    {
        if(TAMGeneratorShader == null || StrokeDataAsset == null)
            return;
        
        if(Dimension != targetRT.width)
        CreateOrUpdateTarget();
        ConfigureBuffers();
        PrepareComputeData();
        
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
    
    private RenderTexture CreateTempTargetCopy()
    {
        if (targetRT == null)
            return null;

        return CopyRT(targetRT);
    }
    
    private RenderTexture CopyRT(RenderTexture copy)
    {
        RenderTexture rt = new RenderTexture(copy);
        rt.enableRandomWrite = true;
        rt.hideFlags = HideFlags.HideAndDontSave;
        
        return rt;
    }
    
    #endregion
    
    #region Compute Prep

    private void ManageStrokeDataKeywords()
    {
        TAMGeneratorShader.EnableKeyword("BASE_STROKE_SDF");
        string[] falloffs = Enum.GetNames(typeof(FalloffFunction));
        string selected = StrokeDataAsset.SelectedFalloffFunction.ToString();
        for (int i = 0; i < falloffs.Length; i++)
        {
            if(falloffs[i] == selected)
                TAMGeneratorShader.EnableKeyword(falloffs[i]);
            else
                TAMGeneratorShader.DisableKeyword(falloffs[i]);
        }
    }

    private void PrepareComputeData()
    {
        ManageStrokeDataKeywords();
        if (TAMGeneratorShader.HasKernel(APPLY_STROKE_KERNEL))
        {
            csApplyStrokeKernelID = TAMGeneratorShader.FindKernel(APPLY_STROKE_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csApplyStrokeKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csApplyStrokeKernelThreads = new Vector3Int(
                Mathf.CeilToInt((float)Dimension / groupsX), 
                Mathf.CeilToInt((float)Dimension / groupsY), 
                1);
            
            TAMGeneratorShader.SetTexture(csApplyStrokeKernelID, RENDER_TEXTURE_ID, targetRT);
            TAMGeneratorShader.SetBuffer(csApplyStrokeKernelID, REDUCED_SOURCE_ID, strokeReducedSource);
            TAMGeneratorShader.SetBuffer(csApplyStrokeKernelID, STROKE_DATA_ID, strokeDataBuffers);
            TAMGeneratorShader.SetInt(DIMENSION_ID, Dimension);
        }

        if (TAMGeneratorShader.HasKernel(TONE_FILL_RATE_KERNEL))
        {
            csFillRateKernelID = TAMGeneratorShader.FindKernel(TONE_FILL_RATE_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csFillRateKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csFillRateKernelThreads = new Vector3Int(
                Mathf.CeilToInt(groupsX), 
                Mathf.CeilToInt(groupsY), 
                Mathf.CeilToInt(groupsZ));
            TAMGeneratorShader.SetBuffer(csFillRateKernelID, TONE_RESULTS_ID, strokeTextureTonesBuffer);
        }

        if (TAMGeneratorShader.HasKernel(BLIT_STROKE_KERNEL))
        {
            csBlitStrokeKernelID = TAMGeneratorShader.FindKernel(BLIT_STROKE_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csBlitStrokeKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csBlitStrokeKernelThreads = new Vector3Int(
                Mathf.CeilToInt((float)Dimension / groupsX), 
                Mathf.CeilToInt((float)Dimension / groupsY), 
                1);
            
            TAMGeneratorShader.SetTexture(csBlitStrokeKernelID, RENDER_TEXTURE_ID, targetRT);
        }
    }

    private void ConfigureBuffers()
    {
        ReleaseBuffers();
        
        strokeIterationTextureBuffers = new ComputeBuffer[IterationsPerStroke];
        TAMStrokeData[] strokeDatas = new TAMStrokeData[IterationsPerStroke];    
        
        for (int i = 0; i < IterationsPerStroke; i++)
        {
            TAMStrokeData iterationData = StrokeDataAsset.StrokeData.Randomize();
            strokeDatas[i] = iterationData;

            strokeIterationTextureBuffers[i] = new ComputeBuffer(Dimension * Dimension, sizeof(uint));
        }
        strokeDataBuffers = new ComputeBuffer(IterationsPerStroke, StrokeDataAsset.StrokeData.GetStrideLength());
        strokeDataBuffers.SetData(strokeDatas);

        strokeTextureTonesBuffer = new ComputeBuffer(IterationsPerStroke, sizeof(uint));
        strokeReducedSource = new ComputeBuffer(Dimension*Dimension, sizeof(uint));
    }

    private void ReleaseBuffers()
    {
        if (strokeDataBuffers != null)
        {
            strokeDataBuffers.Release();
            strokeDataBuffers = null;
        }

        if (strokeIterationTextureBuffers != null)
        {
            for (int i = 0; i < strokeIterationTextureBuffers.Length; i++)
            {
                if(strokeIterationTextureBuffers[i] == null)
                    continue;
                strokeIterationTextureBuffers[i].Release();
                strokeIterationTextureBuffers[i] = null;
            }
            strokeIterationTextureBuffers = null;
        }

        if (strokeTextureTonesBuffer != null)
        {
            strokeTextureTonesBuffer.Release();
            strokeTextureTonesBuffer = null;
        }
        
        if (strokeReducedSource != null)
        {
            strokeReducedSource.Release();
            strokeReducedSource = null;
        }
    }
    
    public void ApplyStrokeKernel()
    {
        StartCoroutine(ExecuteIteratedStrokeKernel());
    }

    private IEnumerator ExecuteIteratedStrokeKernel()
    {
        for (int i = 0; i < IterationsPerStroke; i++)
        {
            if(i == 0)
                TAMGeneratorShader.EnableKeyword("IS_FIRST_ITERATION");
            else
                TAMGeneratorShader.DisableKeyword("IS_FIRST_ITERATION");
            //DISPATCH INDIVIDUAL STROKE APPLICATION ITERATIONS
            TAMGeneratorShader.SetInt(ITERATIONS_ID, i);
            TAMGeneratorShader.SetBuffer(csApplyStrokeKernelID, ITERATION_STEP_TEXTURE_ID, strokeIterationTextureBuffers[i]);
            TAMGeneratorShader.Dispatch(csApplyStrokeKernelID, csApplyStrokeKernelThreads.x, csApplyStrokeKernelThreads.y, csApplyStrokeKernelThreads.z);
        }
        
        
        for (int j = 0; j < IterationsPerStroke; j++)
        {
            TAMGeneratorShader.SetBuffer(csFillRateKernelID, ITERATION_STEP_TEXTURE_ID, strokeIterationTextureBuffers[j]);
            TAMGeneratorShader.SetInt(ITERATIONS_ID, j);
            for (int textureSize = Dimension; textureSize > 1; textureSize /= 2)
            {
                int reductionGroupSize = Mathf.CeilToInt((float)(textureSize * 2) / (float)csFillRateKernelThreads.x);
                if (reductionGroupSize > 1)
                    TAMGeneratorShader.DisableKeyword("IS_LAST_REDUCTION");
                else
                    TAMGeneratorShader.EnableKeyword("IS_LAST_REDUCTION");
                
                TAMGeneratorShader.Dispatch(csFillRateKernelID, reductionGroupSize, csFillRateKernelThreads.y, csFillRateKernelThreads.z);
            }
        }
        
        //Here we unfortunately call back from GPU memory to decide which value to choose in an O(N) operations loop.
        //At the worst case, this is a call back on sizeof(uint) * MaxPossibleValueOf(_IterationsPerStroke) of data
        //TODO: Surely theres a better way to do this?
        uint[] fillRates = new uint[IterationsPerStroke];
        strokeTextureTonesBuffer.GetData(fillRates);

        //maxFillrate will be equal to 1 - the found tone
        int maxToneIndex = -1;
        float maxFillRateFound = -1;
        for (int i = 0; i < fillRates.Length; i++)
        {
            float fillRate = 1 - (float)fillRates[i]/(float)(Dimension*Dimension);
            Debug.Log("checking fill rate at stroke iteration " + i);
            if (fillRate > maxFillRateFound)
            {
                Debug.Log("Found new max fillrate: " + fillRate);
                maxFillRateFound = fillRates[i];
                maxToneIndex = i;
            }
        }
        int index = Shader.PropertyToID("_TempDebug");
        TAMGeneratorShader.SetBuffer(csBlitStrokeKernelID, index, strokeIterationTextureBuffers[maxToneIndex]);
        TAMGeneratorShader.Dispatch(csBlitStrokeKernelID, csBlitStrokeKernelThreads.x, csBlitStrokeKernelThreads.y, csBlitStrokeKernelThreads.z);
        yield return null;
    }
    
    #endregion
}

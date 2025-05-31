using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [Range(0, 1)] 
    public float TargetFillRate;
    public TAMStrokeAsset StrokeDataAsset;
    public TonalArtMapAsset TAMAsset;
    public string OverwriteTexturesOutputPath;
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
    private ComputeBuffer fillRateBuffer;
    
    //Shader Properties
    private readonly int RENDER_TEXTURE_ID = Shader.PropertyToID("_OriginalSource");
    private readonly int REDUCED_SOURCE_ID = Shader.PropertyToID("_ReducedSource");
    private readonly int STROKE_DATA_ID = Shader.PropertyToID("_StrokeData");
    private readonly int ITERATION_STEP_TEXTURE_ID = Shader.PropertyToID("_IterationOutputs");
    private readonly int TONE_RESULTS_ID = Shader.PropertyToID("_ToneResults");
    private readonly int ITERATIONS_ID = Shader.PropertyToID("_Iteration");
    private readonly int DIMENSION_ID = Shader.PropertyToID("_Dimension");
    private readonly int FILL_RATE_ID = Shader.PropertyToID("_Tone_GlobalCache");
    private readonly int FILL_RATE_BUFFER_SIZE_ID = Shader.PropertyToID("_BufferSize");
    private readonly int FILL_RATE_SPLIT_BUFFER_SIZE_ID = Shader.PropertyToID("_SplitBufferSize");
    private readonly int FILL_RATE_BUFFER_OFFSET_ID = Shader.PropertyToID("_BufferOffset");
    
    //Keywords
    private readonly string FIRST_ITERATION_KEYWORD = "IS_FIRST_ITERATION";
    private readonly string LAST_REDUCTION_KEYWORD = "IS_LAST_REDUCTION";
    private readonly string STROKE_SDF_KEYWORD = "BASE_STROKE_SDF";
    
    private LocalKeyword firstIterationLocalKeyword;
    private LocalKeyword lastReductionLocalKeyword;
    private LocalKeyword strokeSDFLocalKeyword;

    private const int MAX_THREADS_PER_DISPATCH = 65535;
    
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
        if(TAMGeneratorShader == null || StrokeDataAsset == null || TAMAsset == null)
            return;
        
        if(targetRT == null || Dimension != targetRT.width)
            CreateOrUpdateTarget();
        ConfigureBuffers();
        PrepareComputeData();
    }
    
    #region Asset Prep
    public void CreateOrUpdateTarget()
    {
        if (targetRT != null)
        {
            targetRT.Release();
            targetRT = null;
        }

        targetRT = CreateRT(Dimension);
        
        //TEMP
        if (targetRT && material)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            material.mainTexture = targetRT;
            mr.material = material;
        }
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
    
    #region Compute

    private void ManageStrokeDataKeywords()
    {
        strokeSDFLocalKeyword = new LocalKeyword(TAMGeneratorShader, STROKE_SDF_KEYWORD);
        firstIterationLocalKeyword = new LocalKeyword(TAMGeneratorShader, FIRST_ITERATION_KEYWORD);
        lastReductionLocalKeyword = new LocalKeyword(TAMGeneratorShader, LAST_REDUCTION_KEYWORD);
        
        TAMGeneratorShader.EnableKeyword(strokeSDFLocalKeyword);
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
        fillRateBuffer = new ComputeBuffer(Dimension*Dimension, sizeof(uint));
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

        if (fillRateBuffer != null)
        {
            fillRateBuffer.Release();
            fillRateBuffer = null;
        }
    }
    
    public float ApplyStrokeKernel()
    {
        ConfigureGeneratorData();
        return ExecuteIteratedStrokeKernel();
    }
    private float ExecuteIteratedStrokeKernel()
    {
        for (int i = 0; i < IterationsPerStroke; i++)
        {
            if(i == 0)
                TAMGeneratorShader.EnableKeyword(firstIterationLocalKeyword);
            else
                TAMGeneratorShader.DisableKeyword(firstIterationLocalKeyword);
            //DISPATCH INDIVIDUAL STROKE APPLICATION ITERATIONS
            TAMGeneratorShader.SetInt(ITERATIONS_ID, i);
            TAMGeneratorShader.SetBuffer(csApplyStrokeKernelID, ITERATION_STEP_TEXTURE_ID, strokeIterationTextureBuffers[i]);
            TAMGeneratorShader.Dispatch(csApplyStrokeKernelID, csApplyStrokeKernelThreads.x, csApplyStrokeKernelThreads.y, csApplyStrokeKernelThreads.z);
        }
        
        for (int j = 0; j < IterationsPerStroke; j++)
        {
            TAMGeneratorShader.SetBuffer(csFillRateKernelID, ITERATION_STEP_TEXTURE_ID, strokeIterationTextureBuffers[j]);
            TAMGeneratorShader.SetBuffer(csFillRateKernelID, FILL_RATE_ID, fillRateBuffer);
            TAMGeneratorShader.SetInt(ITERATIONS_ID, j);
            int expectedBufferSize = Dimension * Dimension;
            
            for (int bufferSize = expectedBufferSize; bufferSize > 1; bufferSize = Mathf.CeilToInt((float)bufferSize/(float)csFillRateKernelThreads.x))
            {
                if (bufferSize == Dimension * Dimension)
                    TAMGeneratorShader.EnableKeyword(firstIterationLocalKeyword);
                else
                    TAMGeneratorShader.DisableKeyword(firstIterationLocalKeyword);
                
                int reductionGroupSize = Mathf.CeilToInt((float)(bufferSize) / (float)csFillRateKernelThreads.x);
                
                if (reductionGroupSize > 1)
                    TAMGeneratorShader.DisableKeyword(lastReductionLocalKeyword);
                else
                    TAMGeneratorShader.EnableKeyword(lastReductionLocalKeyword);
                int amountToSplitBuffer = 1;
                if (reductionGroupSize > MAX_THREADS_PER_DISPATCH)
                {
                    amountToSplitBuffer = Mathf.CeilToInt((float)reductionGroupSize / (float)MAX_THREADS_PER_DISPATCH);
                }
                
                TAMGeneratorShader.SetInt(FILL_RATE_BUFFER_SIZE_ID,bufferSize);

                int amountDispatched = 0;
                int groupsDispatched = 0;
                for (int s = 0; s < amountToSplitBuffer; s++)
                {
                    bool isUnderflow = amountDispatched + MAX_THREADS_PER_DISPATCH > bufferSize;
                    int splitBufferSize = isUnderflow ? bufferSize - amountDispatched : MAX_THREADS_PER_DISPATCH * csFillRateKernelThreads.x;
                    
                    TAMGeneratorShader.SetInt(FILL_RATE_SPLIT_BUFFER_SIZE_ID,amountDispatched);
                    TAMGeneratorShader.SetInt(FILL_RATE_BUFFER_OFFSET_ID, groupsDispatched);
                    
                    int reductionGroups = Mathf.CeilToInt((float)splitBufferSize / (float)csFillRateKernelThreads.x);
                    TAMGeneratorShader.Dispatch(csFillRateKernelID, reductionGroups, csFillRateKernelThreads.y,
                        csFillRateKernelThreads.z);
                    
                    amountDispatched += splitBufferSize;
                    groupsDispatched += reductionGroups;
                }
                if(bufferSize == 1)
                    break;
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
            float fillRate = 1f - ((float)fillRates[i]/(float)(Dimension*Dimension*255));
            if (fillRate > maxFillRateFound)
            {
                maxFillRateFound = fillRate;
                maxToneIndex = i;
            }
        }
        Debug.Log("fillrate: " + maxFillRateFound);
        int index = Shader.PropertyToID("_TempDebug");
        TAMGeneratorShader.SetBuffer(csBlitStrokeKernelID, index, strokeIterationTextureBuffers[maxToneIndex]);
        TAMGeneratorShader.Dispatch(csBlitStrokeKernelID, csBlitStrokeKernelThreads.x, csBlitStrokeKernelThreads.y, csBlitStrokeKernelThreads.z);
        return maxFillRateFound;
    }

    public void ApplyStrokesUntilFillRateAchieved()
    {
        StartCoroutine(ApplyStrokesUntilFillRateRoutine(TargetFillRate));
    }

    private IEnumerator ApplyStrokesUntilFillRateRoutine(float targetFillRate)
    {
        float achievedFillRate = 0;
        int maxStrokesPerFrame = 10;
        while (achievedFillRate < targetFillRate)
        {
            if (Application.isPlaying)
            {
                int strokesApplied = 0;
                while (strokesApplied < maxStrokesPerFrame)
                {
                    achievedFillRate = ApplyStrokeKernel();
                    Debug.Log("achievedFillRate: " + achievedFillRate);
                    if (achievedFillRate > targetFillRate)
                        break;
                    strokesApplied++;
                }
                yield return null;
            }
            else
                achievedFillRate = ApplyStrokeKernel();
        }
    }
    
    #endregion
    
    #region TAM Asset
    public void GenerateTAMToneTextures()
    {
        if(TAMAsset == null)
            return;
        
        CreateOrUpdateTarget();
        ClearAndReleaseTAMTones();
        StartCoroutine(GenerateTAMTonesRoutine());
    }

    private void ClearAndReleaseTAMTones()
    {
        for (int i = 0; i < TAMAsset.Tones.Length; i++)
        {
            if(TAMAsset.Tones[i] != null)
                TextureAssetManager.ClearTexture(TAMAsset.Tones[i]);
        }
    }

    private IEnumerator GenerateTAMTonesRoutine()
    {
        float currentFillRate = 0;
        float expectedFillRateThreshold = TAMAsset.GetHomogenousFillRateThreshold();

        for (int i = 0; i < TAMAsset.ExpectedTones; i++)
        {
            yield return ApplyStrokesUntilFillRateRoutine(currentFillRate);
            Texture2D output = SaveCurrentTargetTexture(true, $"Tone_{i}");
            if (output == null)
            {
                Debug.LogException(new Exception("Failed to generate Tam texture"));
                yield break;
            }
            TAMAsset.Tones[i] = output;
            currentFillRate += expectedFillRateThreshold;
            if(Application.isPlaying)
                yield return null;
        }
    }
    
    #endregion
    
    #region Editor Asset Management
    public Texture2D SaveCurrentTargetTexture(bool overwrite, string fileName = null)
    {
        if (targetRT == null)
            return null;

        string path = GetTextureOutputPath();
        
        if(fileName == null)
            fileName = "StrokeTexture";
        
        return TextureAssetManager.OutputToAssetTexture(targetRT, path, fileName, overwrite);
    }
    private string GetTextureOutputPath()
    {
        if (!string.IsNullOrEmpty(OverwriteTexturesOutputPath))
            return OverwriteTexturesOutputPath;
        else return Path.Combine(TextureAssetManager.GetAssetPath(TAMAsset), "ToneTextures");
    }
    #endregion
}

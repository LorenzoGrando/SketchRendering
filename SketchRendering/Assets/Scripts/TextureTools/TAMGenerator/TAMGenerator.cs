using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEditor;

[ExecuteAlways]
public class TAMGenerator : TextureGenerator
{
    protected override string DefaultFileOutputName => "StrokesTexture";
    protected override string DefaultFileOutputPath
    {
        get
        {
#if UNITY_EDITOR
            if (TAMAsset != null)
                return Path.Combine(TextureAssetManager.GetAssetPath(TAMAsset).Split('.')[0], "ToneTextures");
#endif
            
            return base.DefaultFileOutputPath;
        }
    }
    
    //TODO: Hide later
    [Header("TAMGenerator Settings")]
    public ComputeShader TAMGeneratorShader;
    [Range(1, 100)]
    public int IterationsPerStroke;
    [Range(0, 1)] 
    public float TargetFillRate;
    public TAMStrokeAsset StrokeDataAsset;
    public TonalArtMapAsset TAMAsset;
    public bool PackTAMTextures;
    
    //Compute Data
    private readonly string GENERATE_STROKE_BUFFER_KERNEL = "GenerateRandomStrokeBuffer";
    private readonly string APPLY_STROKE_KERNEL = "ApplyStrokeIterated";
    private readonly string TONE_FILL_RATE_KERNEL = "FindAverageTextureFillRate";
    private readonly string BLIT_STROKE_KERNEL = "BlitFinalSelectedStroke";
    private readonly string PACK_STROKES_KERNEL = "PackStrokeTextures";
    private int csGenerateStrokeBufferKernelID;
    private int csApplyStrokeKernelID;
    private int csFillRateKernelID;
    private int csBlitStrokeKernelID;
    private int csPackStrokesKernelID;
    private Vector3Int csGenerateStrokeBufferKernelThreads;
    private Vector3Int csApplyStrokeKernelThreads;
    private Vector3Int csFillRateKernelThreads;
    private Vector3Int csBlitStrokeKernelThreads;
    private Vector3Int csPackStrokesKernelThreads;
    private ComputeBuffer strokeDataBuffer;
    private ComputeBuffer variationDataBuffer;
    private ComputeBuffer[] strokeIterationTextureBuffers;
    private ComputeBuffer strokeTextureTonesBuffer;
    private ComputeBuffer strokeReducedSource;
    private ComputeBuffer fillRateBuffer;
    
    private const int MAX_THREADS_PER_DISPATCH = 65535;
    
    //Shader Properties
    private readonly int RENDER_TEXTURE_ID = Shader.PropertyToID("_OriginalSource");
    private readonly int REDUCED_SOURCE_ID = Shader.PropertyToID("_ReducedSource");
    private readonly int STROKE_DATA_ID = Shader.PropertyToID("_StrokeData");
    private readonly int VARIATION_DATA_ID = Shader.PropertyToID("_VariationData");    
    private readonly int ITERATION_STEP_TEXTURE_ID = Shader.PropertyToID("_IterationOutputs");
    private readonly int TONE_RESULTS_ID = Shader.PropertyToID("_ToneResults");
    private readonly int ITERATIONS_ID = Shader.PropertyToID("_Iteration");
    private readonly int DIMENSION_ID = Shader.PropertyToID("_Dimension");
    private readonly int SEED_ID = Shader.PropertyToID("_Seed");
    private readonly int FILL_RATE_ID = Shader.PropertyToID("_Tone_GlobalCache");
    private readonly int FILL_RATE_BUFFER_SIZE_ID = Shader.PropertyToID("_BufferSize");
    private readonly int FILL_RATE_SPLIT_BUFFER_SIZE_ID = Shader.PropertyToID("_SplitBufferSize");
    private readonly int FILL_RATE_BUFFER_OFFSET_ID = Shader.PropertyToID("_BufferOffset");
    private readonly int BLIT_RESULT_ID = Shader.PropertyToID("_BlitResult");
    private readonly int PACK_R_TEXTURE = Shader.PropertyToID("_PackTextR");
    private readonly int PACK_G_TEXTURE = Shader.PropertyToID("_PackTextG");
    private readonly int PACK_B_TEXTURE = Shader.PropertyToID("_PackTextB");
    
    //Keywords
    private readonly string FIRST_ITERATION_KEYWORD = "IS_FIRST_ITERATION";
    private readonly string LAST_REDUCTION_KEYWORD = "IS_LAST_REDUCTION";
    private readonly string PACK_TEXTURES_2_KEYWORD = "PACK_TEXTURES_2";
    private readonly string PACK_TEXTURES_3_KEYWORD = "PACK_TEXTURES_3";
    
    private LocalKeyword firstIterationLocalKeyword;
    private LocalKeyword lastReductionLocalKeyword;
    private LocalKeyword[] falloffLocalKeywords;
    private LocalKeyword[] strokeTypeLocalKeywords;
    private LocalKeyword packTextures2LocalKeyword;
    private LocalKeyword packTextures3LocalKeyword;
    
    private bool generating = false;
    public bool CanRequest { get { return !generating; } }

    public void OnDisable()
    {
        ReleaseBuffers();
    }

    public override void ConfigureGeneratorData()
    {
        if(TAMGeneratorShader == null || StrokeDataAsset == null || TAMAsset == null)
            return;
        
        base.ConfigureGeneratorData();
        
        ConfigureBuffers();
        PrepareComputeData();
    }
    
    #region Compute

    private void ManageStrokeDataKeywords()
    {
        firstIterationLocalKeyword = new LocalKeyword(TAMGeneratorShader, FIRST_ITERATION_KEYWORD);
        lastReductionLocalKeyword = new LocalKeyword(TAMGeneratorShader, LAST_REDUCTION_KEYWORD);

        string[] falloffs = Enum.GetNames(typeof(FalloffFunction));
        falloffLocalKeywords = new LocalKeyword[falloffs.Length];
        string selected = StrokeDataAsset.SelectedFalloffFunction.ToString();
        for (int i = 0; i < falloffs.Length; i++)
        {
            falloffLocalKeywords[i] = new LocalKeyword(TAMGeneratorShader, falloffs[i]);
            if (falloffs[i] == selected)
                TAMGeneratorShader.EnableKeyword(falloffLocalKeywords[i]);
            else
                TAMGeneratorShader.DisableKeyword(falloffLocalKeywords[i]);
        }
        
        
        string[] sdfTypes = Enum.GetNames(typeof(StrokeSDFType));
        strokeTypeLocalKeywords = new LocalKeyword[sdfTypes.Length];
        string selectedType = StrokeDataAsset.PatternType.ToString();
        for (int t = 0; t < sdfTypes.Length; t++)
        {
            strokeTypeLocalKeywords[t] = new LocalKeyword(TAMGeneratorShader, sdfTypes[t]);
            if (sdfTypes[t] == selectedType)
            {
                TAMGeneratorShader.EnableKeyword(strokeTypeLocalKeywords[t]);
            }
            else
                TAMGeneratorShader.DisableKeyword(strokeTypeLocalKeywords[t]);
        }

        packTextures2LocalKeyword = new LocalKeyword(TAMGeneratorShader, PACK_TEXTURES_2_KEYWORD);
        packTextures3LocalKeyword = new LocalKeyword(TAMGeneratorShader, PACK_TEXTURES_3_KEYWORD);
    }

    private void PrepareComputeData()
    {
        ManageStrokeDataKeywords();

        if (TAMGeneratorShader.HasKernel(GENERATE_STROKE_BUFFER_KERNEL))
        {
            csGenerateStrokeBufferKernelID = TAMGeneratorShader.FindKernel(GENERATE_STROKE_BUFFER_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csGenerateStrokeBufferKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csGenerateStrokeBufferKernelThreads = new Vector3Int(
                Mathf.CeilToInt((float)IterationsPerStroke / groupsX), 
                1, 
                1);
            
            TAMGeneratorShader.SetInt(DIMENSION_ID, Dimension);
            TAMGeneratorShader.SetBuffer(csGenerateStrokeBufferKernelID, STROKE_DATA_ID, strokeDataBuffer);      
            TAMGeneratorShader.SetBuffer(csGenerateStrokeBufferKernelID, VARIATION_DATA_ID, variationDataBuffer);      
        }
        
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
            TAMGeneratorShader.SetBuffer(csApplyStrokeKernelID, STROKE_DATA_ID, strokeDataBuffer);
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

        if (TAMGeneratorShader.HasKernel(PACK_STROKES_KERNEL))
        {
            csPackStrokesKernelID = TAMGeneratorShader.FindKernel(PACK_STROKES_KERNEL);
            TAMGeneratorShader.GetKernelThreadGroupSizes(csPackStrokesKernelID, out uint groupsX, out uint groupsY, out uint groupsZ);
            csPackStrokesKernelThreads = new Vector3Int(
                Mathf.CeilToInt((float)Dimension / groupsX),
                Mathf.CeilToInt((float)Dimension / groupsY),
                1);
            
            TAMGeneratorShader.SetTexture(csPackStrokesKernelID, RENDER_TEXTURE_ID, targetRT);
        }
    }

    private void ConfigureBuffers()
    {
        ReleaseBuffers();
        
        ConfigureStrokesBuffer(0f);
        
        strokeIterationTextureBuffers = new ComputeBuffer[IterationsPerStroke];
        for (int i = 0; i < IterationsPerStroke; i++)
        {
            strokeIterationTextureBuffers[i] = new ComputeBuffer(Dimension * Dimension, sizeof(uint));
        }
        
        strokeTextureTonesBuffer = new ComputeBuffer(IterationsPerStroke, sizeof(uint));
        strokeReducedSource = new ComputeBuffer(Dimension*Dimension, sizeof(uint));
        fillRateBuffer = new ComputeBuffer(Dimension*Dimension, sizeof(uint));
    }
    
    private void ConfigureStrokesBuffer(float fillRate)
    {
        TAMStrokeData[] strokeDatas = new TAMStrokeData[IterationsPerStroke];
        TAMVariationData[] variationDatas = new TAMVariationData[IterationsPerStroke];
        TAMStrokeData expectedStrokeData = StrokeDataAsset.UpdatedDataByFillRate(fillRate);
        for (int i = 0; i < IterationsPerStroke; i++)
        {
            //TAMStrokeData iterationData = StrokeDataAsset.Randomize(fillRate);
            strokeDatas[i] = expectedStrokeData;
            variationDatas[i] = StrokeDataAsset.VariationData;
        }
        if(strokeDataBuffer == null)
            strokeDataBuffer = new ComputeBuffer(IterationsPerStroke, StrokeDataAsset.StrokeData.GetStrideLength());
        if (variationDataBuffer == null)
            variationDataBuffer = new ComputeBuffer(IterationsPerStroke, StrokeDataAsset.VariationData.GetStrideLength());

        strokeDataBuffer.SetData(strokeDatas);
        variationDataBuffer.SetData(variationDatas);
    }

    private void ReleaseBuffers()
    {
        if (strokeDataBuffer != null)
        {
            strokeDataBuffer.Release();
            strokeDataBuffer = null;
        }

        if (variationDataBuffer != null)
        {
            variationDataBuffer.Release();
            variationDataBuffer = null;
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
    
    public float ApplyStrokeKernel(int iteration)
    {
        //Randomize stroke positions
        TAMGeneratorShader.SetInt(SEED_ID, (int)Time.realtimeSinceStartup + Mathf.RoundToInt(UnityEngine.Random.value * iteration * 997));
        TAMGeneratorShader.Dispatch(csGenerateStrokeBufferKernelID, csGenerateStrokeBufferKernelThreads.x, csGenerateStrokeBufferKernelThreads.y, csGenerateStrokeBufferKernelThreads.z);
        return ExecuteIteratedStrokeKernel();
    }

    public void DisplaySDF()
    {
        CreateOrUpdateTarget();
        int prevIterations = IterationsPerStroke;
        IterationsPerStroke = 1;
        ConfigureGeneratorData();
        strokeDataBuffer.SetData(new [] {StrokeDataAsset.PreviewDisplay()});
        ExecuteIteratedStrokeKernel();
        IterationsPerStroke = prevIterations;
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
        int maxToneIndex = 0;
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
        
        TAMGeneratorShader.SetBuffer(csBlitStrokeKernelID, BLIT_RESULT_ID, strokeIterationTextureBuffers[maxToneIndex]);
        TAMGeneratorShader.Dispatch(csBlitStrokeKernelID, csBlitStrokeKernelThreads.x, csBlitStrokeKernelThreads.y, csBlitStrokeKernelThreads.z);
        return maxFillRateFound;
    }

    public void CombineStrokeTextures(Texture2D texture1, Texture2D texture2 = null, Texture2D texture3 = null)
    {
        TAMGeneratorShader.SetKeyword(packTextures2LocalKeyword, texture2 != null && texture3 == null);
        TAMGeneratorShader.SetKeyword(packTextures3LocalKeyword, texture2 != null && texture3 != null);
        
        RenderTexture tmp1 = CopyToRT(texture1);
        TAMGeneratorShader.SetTexture(csPackStrokesKernelID, PACK_R_TEXTURE, tmp1);
        RenderTexture tmp2;
        if (texture2 != null)
            tmp2 = CopyToRT(texture2);
        else
            tmp2 = CopyToRT(Texture2D.whiteTexture);
        TAMGeneratorShader.SetTexture(csPackStrokesKernelID, PACK_G_TEXTURE, tmp2);
        RenderTexture tmp3 = null;
        if (texture3 != null)
            tmp3 = CopyToRT(texture3);
        else
            tmp3 = CopyToRT(Texture2D.whiteTexture);
        TAMGeneratorShader.SetTexture(csPackStrokesKernelID, PACK_B_TEXTURE, tmp3);

        TAMGeneratorShader.Dispatch(csPackStrokesKernelID, csPackStrokesKernelThreads.x, csPackStrokesKernelThreads.y, csPackStrokesKernelThreads.z);
        tmp1.Release();
        if(tmp2 != null)
            tmp2.Release();
        if(tmp3 != null)
            tmp3.Release();
    }
    
    #endregion
    
    #region TAM Asset
    public void GenerateTAMToneTextures()
    {
        if(TAMAsset == null)
            return;
        if(generating)
            return;
        
        //Force Clear
        CreateOrUpdateTarget();
        ConfigureGeneratorData();
        ClearAndReleaseTAMTones();
        StartCoroutine(GenerateTAMTonesRoutine());
        generating = true;
    }

    private void ClearAndReleaseTAMTones()
    {
        for (int i = 0; i < TAMAsset.Tones.Length; i++)
        {
            if(TAMAsset.Tones[i] != null)
                ClearAndDeleteTexture(TAMAsset.Tones[i]);
        }
        TAMAsset.ResetTones();
    }
    
    public void ApplyStrokesUntilFillRateAchieved()
    {
        StartCoroutine(ApplyStrokesUntilFillRateRoutine(TargetFillRate));
    }

    private IEnumerator ApplyStrokesUntilFillRateRoutine(float targetFillRate, float achievedFillRate = 0)
    {
        int maxStrokesPerFrame = 10;
        int strokesApplied = 0;
        while (achievedFillRate < targetFillRate)
        {
            ConfigureStrokesBuffer(achievedFillRate);
            if (Application.isPlaying)
            {
                strokesApplied = 0;
                while (strokesApplied < maxStrokesPerFrame)
                {
                    achievedFillRate = ApplyStrokeKernel(strokesApplied);
                    if (achievedFillRate > targetFillRate)
                        break;
                    strokesApplied++;
                }
                yield return null;
            }
            else
            {
                achievedFillRate = ApplyStrokeKernel(strokesApplied);
                strokesApplied++;
            }
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
                Debug.LogException(new Exception("Failed to generate TAM texture"));
                yield break;
            }
            TAMAsset.Tones[i] = output;
            currentFillRate += expectedFillRateThreshold;
            if(Application.isPlaying)
                yield return null;
        }
        if(PackTAMTextures)
            PackAllTAMTextures();

        TAMAsset.TAMBasisDirection = StrokeDataAsset.StrokeData.Direction;
        
#if UNITY_EDITOR
        EditorUtility.SetDirty(TAMAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
        generating = false;
    }

    private void PackAllTAMTextures()
    {
        if(TAMAsset == null || TAMAsset.Tones.Length == 0)
            return;
        
        Dimension = TAMAsset.Tones[0].width;
        List<Texture2D> packedTAMs = new List<Texture2D>();
        for (int i = 0; i < TAMAsset.Tones.Length; i += 3)
        {
            bool isReduced = i + 1 >= TAMAsset.Tones.Length;
            bool isFullReduced = i + 2 >= TAMAsset.Tones.Length;
            if (!isReduced && !isFullReduced)
                CombineStrokeTextures(TAMAsset.Tones[i], TAMAsset.Tones[i + 1], TAMAsset.Tones[i + 2]);
            else if (!isReduced && isFullReduced)
            {
                CombineStrokeTextures(TAMAsset.Tones[i], TAMAsset.Tones[i + 1]);
            }
            else
            {
                CombineStrokeTextures(TAMAsset.Tones[i]);
            }
            
            Texture2D packedTAM = SaveCurrentTargetTexture(true, $"PackedTAM_{i}_{(isFullReduced ? i + 1 : i + 2)}");
            packedTAMs.Add(packedTAM);
        }
        ClearAndReleaseTAMTones();
        TAMAsset.SetPackedTams(packedTAMs.ToArray());
    }
    
    #endregion
}
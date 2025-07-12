using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SketchStrokesComputeRenderPass : ScriptableRenderPass
{
    public string PassName => "SketchyStrokesCompute";

    private Material sketchMaterial;
    private SketchStrokesPassData passData;
    private ComputeShader sketchComputeShader;
    private ComputeBuffer gradientBuffer;
    private ComputeBuffer strokeDataBuffer;
    
    //Compute Data
    private readonly string COMPUTE_STROKE_KERNEL = "ComputeAverageStroke";
    private readonly string APPLY_STROKES_KERNEL = "ApplyStrokes";
    private int computeStrokeKernelID;
    private int computeApplyStrokeKernelID;
    private readonly int SOURCE_TEXTURE_ID = Shader.PropertyToID("_OriginalSource");
    private readonly int DIMENSION_WIDTH_ID = Shader.PropertyToID("_TextureWidth");
    private readonly int DIMENSION_HEIGHT_ID = Shader.PropertyToID("_TextureHeight");
    private readonly int GROUPS_X_ID = Shader.PropertyToID("_GroupsX");
    private readonly int GROUPS_Y_ID = Shader.PropertyToID("_GroupsY");
    private readonly int DOWNSCALE_FACTOR_ID = Shader.PropertyToID("_DownscaleFactor");
    private readonly int COMPUTE_GRADIENT_VECTORS_ID = Shader.PropertyToID("_GradientVectors");
    private readonly int THRESHOLD_ID = Shader.PropertyToID("_ThresholdForStroke");
    private readonly int STROKE_DATA_ID = Shader.PropertyToID("OutlineStrokeData");
    private readonly string PERPENDICULAR_DIRECTION_KEYWORD_ID = "USE_PERPENDICULAR_DIRECTION";
    private Vector3Int strokesKernelThreads;
    private Vector3Int applyKernelThreads;

    private LocalKeyword PerpendicularDirectionKeyword;

    private readonly int GRADIENT_VECTOR_STRIDE_LENGTH = sizeof(float) * 4;
    
    private Vector2Int GetDownscaleTargetDimension(Vector2 currentResolution, int factor)
    {
        if(currentResolution.x > 1920f || currentResolution.y > 1080f)
            currentResolution = new Vector2(1920f, 1080f);
        return new Vector2Int(Mathf.CeilToInt(currentResolution.x / factor), Mathf.CeilToInt(currentResolution.y / factor));
    }

    public void Setup(SketchStrokesPassData passData, Material mat, ComputeShader computeShader)
    {
        sketchMaterial = mat;
        this.passData = passData;
        sketchComputeShader = computeShader;
        
        requiresIntermediateTexture = false;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        ConfigureComputeShader();
    }

    public void ConfigureMaterial()
    {

    }

    private void ConfigureComputeShader()
    {
        computeStrokeKernelID = sketchComputeShader.FindKernel(Get1DAreaReliantKernelID(COMPUTE_STROKE_KERNEL, passData.SampleArea));
        sketchComputeShader.GetKernelThreadGroupSizes(computeStrokeKernelID, out uint x, out uint y, out uint z);
        strokesKernelThreads = new Vector3Int((int)x, (int)y, (int)z);

        computeApplyStrokeKernelID = sketchComputeShader.FindKernel(Get1DAreaReliantKernelID(APPLY_STROKES_KERNEL, passData.SampleArea));
        sketchComputeShader.GetKernelThreadGroupSizes(computeApplyStrokeKernelID, out uint x1, out uint y1, out uint z1);
        applyKernelThreads = new Vector3Int((int)x1, (int)y1, (int)z1);

        PerpendicularDirectionKeyword = new LocalKeyword(sketchComputeShader, PERPENDICULAR_DIRECTION_KEYWORD_ID);
        sketchComputeShader.SetKeyword(PerpendicularDirectionKeyword, passData.UsePerpendicularDirection);
    }

    private string Get1DAreaReliantKernelID(string kernelName, ComputeData.KernelSize2D kernelSize)
    {
        Vector2Int sizes = ComputeData.GetKernelSizeFromEnum(kernelSize);
        return $"{kernelName}{sizes.x}";
    }

    public void Dispose()
    {
        if (gradientBuffer != null)
        {
            gradientBuffer.Release();
            gradientBuffer = null;
        }

        if (strokeDataBuffer != null)
        {
            strokeDataBuffer.Release();
            strokeDataBuffer = null;
        }
    }

    class ComputePassData
    {
        public ComputeShader computeShader;
        public int kernelID;
        public int groupsXID;
        public int groupsYID;
        public Vector3Int threadGroupSize;
        public int widthID;
        public int heightID;
        public Vector2Int dimensions;
        public int texturePropertyID;
        public TextureHandle outlineTex;
        public int thresholdID;
        public float threshold;
        public int computeOutputID;
        public ComputeBuffer outputBuffer;
    }

    class DownscalePassData
    {
        public TextureHandle source;
        public TextureHandle dest;
    }

    class StrokesPassData
    {
        public ComputeShader computeShader;
        public int kernelID;
        public int groupsXID;
        public int groupsYID;
        public Vector3Int threadGroupSize;
        public int widthID;
        public int heightID;
        public Vector2Int dimensions;
        public int texturePropertyID;
        public TextureHandle outlineTex;
        public int computeInputID;
        public ComputeBuffer inputBuffer;
        public int strokeDataID;
        public ComputeBuffer strokeDataBuffer;
        public int downscaleFactorID;
        public int downscaleFactor;
    }

    static void ExecuteDownscale(DownscalePassData passData, UnsafeGraphContext context)
    {
        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
        cmd.SetRenderTarget(passData.dest);
        Blitter.BlitTexture(cmd, passData.source, new Vector4(1f, 1f, 0f, 0f), 0f, false);
    }
    static void ExecuteFindStrokesCompute(ComputePassData passData, UnsafeGraphContext context)
    {
        context.cmd.SetRenderTarget(passData.outlineTex);
        context.cmd.SetComputeTextureParam(passData.computeShader, passData.kernelID, passData.texturePropertyID, passData.outlineTex);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.widthID, passData.dimensions.x);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.heightID, passData.dimensions.y);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.groupsXID, passData.threadGroupSize.x);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.groupsYID, passData.threadGroupSize.y);
        context.cmd.SetComputeBufferParam(passData.computeShader, passData.kernelID, passData.computeOutputID, passData.outputBuffer);
        context.cmd.SetComputeFloatParam(passData.computeShader, passData.thresholdID, passData.threshold);
        context.cmd.DispatchCompute(passData.computeShader, passData.kernelID, passData.threadGroupSize.x, passData.threadGroupSize.y, passData.threadGroupSize.z);
    }

    static void ExecuteApplyStrokesCompute(StrokesPassData passData, UnsafeGraphContext context)
    {
        context.cmd.SetRenderTarget(passData.outlineTex);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.groupsXID, passData.threadGroupSize.x);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.groupsYID, passData.threadGroupSize.y);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.downscaleFactorID, passData.downscaleFactor);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.widthID, passData.dimensions.x);
        context.cmd.SetComputeIntParam(passData.computeShader, passData.heightID, passData.dimensions.y);
        context.cmd.SetComputeTextureParam(passData.computeShader, passData.kernelID, passData.texturePropertyID, passData.outlineTex);
        context.cmd.SetComputeBufferParam(passData.computeShader, passData.kernelID, passData.computeInputID, passData.inputBuffer);
        context.cmd.SetComputeBufferParam(passData.computeShader, passData.kernelID, passData.strokeDataID, passData.strokeDataBuffer);
        context.cmd.DispatchCompute(passData.computeShader, passData.kernelID, passData.threadGroupSize.x, passData.threadGroupSize.y, passData.threadGroupSize.z);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (resourceData.isActiveTargetBackBuffer)
            return;
        
        var sketchData = frameData.GetOrCreate<SketchRendererContext>();
        if(!sketchData.OutlinesTexture.IsValid())
            return;
        
        bool isDoingDownscale = false;
        var desc = renderGraph.GetTextureDesc(sketchData.OutlinesTexture);
        TextureHandle downscaleTex = TextureHandle.nullHandle;
        TextureHandle originalOutlines = sketchData.OutlinesTexture;
        if (passData.DoDownscale && (GetDownscaleTargetDimension(new Vector2(desc.width, desc.height), passData.DownscaleFactor) is Vector2Int downscaleRes && (desc.width > downscaleRes.x || desc.height > downscaleRes.y)))
        {
            //If we are downscaling, for some reason this has always been culled even when the texture is assigned back
            //so prevent it from ever culling
            using (var downBuilder = renderGraph.AddUnsafePass(PassName + "_Downscale", out DownscalePassData passData))
            {
                downBuilder.AllowPassCulling(false);
                downBuilder.UseTexture(originalOutlines);
                //create a temporary downscaled version to blit to
                var downDesc = desc;
                downDesc.width = downscaleRes.x;
                downDesc.height = downscaleRes.y;
                downscaleTex = renderGraph.CreateTexture(downDesc);
                downBuilder.UseTexture(downscaleTex);

                passData.source = sketchData.OutlinesTexture;
                passData.dest = downscaleTex;
                
                downBuilder.SetRenderFunc((DownscalePassData passData, UnsafeGraphContext context) => ExecuteDownscale(passData, context));

                isDoingDownscale = true;
                sketchData.OutlinesTexture = downscaleTex;
            }
        }

        using (var builder = renderGraph.AddUnsafePass(PassName, out ComputePassData computePassData))
        {
            //since this dosent assign back to the texture, if it exists, make sure the pass cant be culled
            builder.AllowPassCulling(false);
      
            builder.UseTexture(sketchData.OutlinesTexture);
            computePassData.outlineTex = sketchData.OutlinesTexture;
            computePassData.texturePropertyID = SOURCE_TEXTURE_ID;
            
            var computeDesc = renderGraph.GetTextureDesc(sketchData.OutlinesTexture);
            Vector2Int dimensions = new Vector2Int(computeDesc.width, computeDesc.height);
            Vector3Int groups = new Vector3Int(
                Mathf.CeilToInt((float)dimensions.x / (float)strokesKernelThreads.x),
                Mathf.CeilToInt((float)dimensions.y / (float)strokesKernelThreads.y),
                1
            );
            computePassData.groupsXID = GROUPS_X_ID;
            computePassData.groupsYID = GROUPS_Y_ID;
            computePassData.threadGroupSize = groups;
            
            if (gradientBuffer == null || gradientBuffer.count != (groups.x * groups.y))
            {
                Debug.Log("Creating gradient buffer of size " + groups);
                gradientBuffer = new ComputeBuffer(groups.x * groups.y, GRADIENT_VECTOR_STRIDE_LENGTH);
            }

            computePassData.widthID = DIMENSION_WIDTH_ID;
            computePassData.heightID = DIMENSION_HEIGHT_ID;
            computePassData.dimensions = new Vector2Int(dimensions.x, dimensions.y);
            computePassData.thresholdID = THRESHOLD_ID;
            computePassData.threshold = passData.StrokeThreshold;
            computePassData.computeOutputID = COMPUTE_GRADIENT_VECTORS_ID;
            computePassData.outputBuffer = gradientBuffer;
            
            computePassData.computeShader = sketchComputeShader;
            computePassData.kernelID = computeStrokeKernelID;
            
            builder.SetRenderFunc((ComputePassData computePassData, UnsafeGraphContext context) => ExecuteFindStrokesCompute(computePassData, context));
        }
        
        if (isDoingDownscale)
        {
            using(var upBuilder = renderGraph.AddUnsafePass(PassName + "_Upscale", out DownscalePassData passData)) {
                upBuilder.AllowPassCulling(false);
                upBuilder.UseTexture(sketchData.OutlinesTexture);
                upBuilder.UseTexture(originalOutlines);
                passData.source = sketchData.OutlinesTexture;
                passData.dest = originalOutlines;
                upBuilder.SetRenderFunc((DownscalePassData passData, UnsafeGraphContext context) => ExecuteDownscale(passData, context));
                sketchData.OutlinesTexture = originalOutlines;
            }
        }

        using (var applyBuilder = renderGraph.AddUnsafePass(PassName + "_ApplyStrokes", out StrokesPassData computePassData))
        {
            applyBuilder.AllowPassCulling(false);
            applyBuilder.UseTexture(sketchData.OutlinesTexture);
            computePassData.outlineTex = sketchData.OutlinesTexture;
            computePassData.texturePropertyID = SOURCE_TEXTURE_ID;
            computePassData.computeInputID = COMPUTE_GRADIENT_VECTORS_ID;
            computePassData.inputBuffer = gradientBuffer;
            computePassData.downscaleFactorID = DOWNSCALE_FACTOR_ID;
            computePassData.downscaleFactor = passData.DownscaleFactor;

            computePassData.computeShader = sketchComputeShader;
            computePassData.kernelID = computeApplyStrokeKernelID;
            
            var computeDesc = renderGraph.GetTextureDesc(sketchData.OutlinesTexture);
            Vector2Int dimensions = new Vector2Int(computeDesc.width, computeDesc.height);
            Vector3Int groups = new Vector3Int(
                Mathf.CeilToInt((float)dimensions.x / (float)applyKernelThreads.x),
                Mathf.CeilToInt((float)dimensions.y / (float)applyKernelThreads.y),
                1
            );
            computePassData.groupsXID = GROUPS_X_ID;
            computePassData.groupsYID = GROUPS_Y_ID;
            computePassData.threadGroupSize = groups;
            
            computePassData.widthID = DIMENSION_WIDTH_ID;
            computePassData.heightID = DIMENSION_HEIGHT_ID;
            computePassData.dimensions = new Vector2Int(dimensions.x, dimensions.y);
                
            if (strokeDataBuffer == null)
            {
                strokeDataBuffer = new ComputeBuffer(1, passData.OutlineStrokeData.StrokeData.GetStrideLength());
            }
            strokeDataBuffer.SetData(new [] {passData.OutlineStrokeData.StrokeData});
            computePassData.strokeDataID = STROKE_DATA_ID;
            computePassData.strokeDataBuffer = strokeDataBuffer;
            
            applyBuilder.SetRenderFunc((StrokesPassData data, UnsafeGraphContext context) => ExecuteApplyStrokesCompute(data, context));
        }
    }
}
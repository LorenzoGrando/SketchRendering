#ifndef SKETCH_SOBEL_DEPTH_NORMALS
#define SKETCH_SOBEL_DEPTH_NORMALS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

float SobelDepthHorizontalKernel(float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
{
    float vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams).r * -1;
    float vCL = LinearEyeDepth(SampleSceneDepth(cL), _ZBufferParams).r * -2;
    float vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams).r * -1;
    float vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams).r * 1;
    float vCR = LinearEyeDepth(SampleSceneDepth(cR), _ZBufferParams).r * 2;
    float vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams).r * 1;

    return (vUL + vCL + vDL + vUR + vCR + vDR);
}

float SobelDepthVerticalKernel(float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
{
    float3 vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams) * -1;
    float3 vUC = LinearEyeDepth(SampleSceneDepth(uC), _ZBufferParams) * -2;
    float3 vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams) * -1;
    float3 vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams) * 1;
    float3 vDC = LinearEyeDepth(SampleSceneDepth(dC), _ZBufferParams) * 2;
    float3 vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams) * 1;
               
    return (vUL + vUC + vUR + vDL + vDC + vDR);
}

float3 FullRangeNormal(float3 normal)
{
    return (normal * 2) - 1;
}

float3 SobelNormalHorizontalKernel(float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
{
    float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * -1;
    float3 vCL = FullRangeNormal(SampleSceneNormals(cL)) * -2;
    float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * -1;
    float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * 1;
    float3 vCR = FullRangeNormal(SampleSceneNormals(cR)) * 2;
    float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * 1;

    float sX = (vUL.x + vCL.x + vDL.x + vUR.x + vCR.x + vDR.x);
    float sY = (vUL.y + vCL.y + vDL.y + vUR.y + vCR.y + vDR.y);
    float sZ = (vUL.z + vCL.z + vDL.z + vUR.z + vCR.z + vDR.z);
    
    return float3(sX, sY, sZ);
}

float3 SobelNormalVerticalKernel(float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
{
    float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * -1;
    float3 vUC = FullRangeNormal(SampleSceneNormals(uC)) * -2;
    float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * -1;
    float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * 1;
    float3 vDC = FullRangeNormal(SampleSceneNormals(dC)) * 2;
    float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * 1;

    float sX = (vUL.x + vUC.x + vUR.x + vDL.x + vDC.x + vDR.x);
    float sY = (vUL.y + vUC.y + vUR.y + vDL.y + vDC.y + vDR.y);
    float sZ = (vUL.z + vUC.z + vUR.z + vDL.z + vDC.z + vDR.z);
    
    return float3(sX, sY, sZ);
}

#endif
#ifndef SKETCH_SOBEL_DEPTH_NORMALS
#define SKETCH_SOBEL_DEPTH_NORMALS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

    //Matrices in HLSL are columns based, so this visual below is the transposed kernel
    static float3x3 BaseSobel3X3HorizontalKernel = {
        float3(-1, -2, -1),
        float3(0, 0, 0),
        float3(1, 2, 1)
    };

    static float3x3 BaseSobel3X3VerticalKernel = {
        float3(-1, 0, 1),
        float3(-2, 0, 2),
        float3(-1, 0, 1)
    };

    static float3x3 ModifiedSobel3X3HorizontalKernel = {
        float3(-3, -10, -3),
        float3(0, 0, 0),
        float3(3, 10, 3)
    };

    static float3x3 ModifiedSobel3X3VerticalKernel = {
        float3(-3, 0, 3),
        float3(-10, 0, 10),
        float3(-3, 0, 3)
    };

    static float3 Sobel1X3Kernel = {
        -1, 0, 1
    };

    float SobelDepthHorizontal3X3(float3x3 kernel, float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
    {
        float vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams) * kernel._11;
        float vCL = LinearEyeDepth(SampleSceneDepth(cL), _ZBufferParams) * kernel._21;
        float vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams) * kernel._31;
        float vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams) * kernel._13;
        float vCR = LinearEyeDepth(SampleSceneDepth(cR), _ZBufferParams) * kernel._23;
        float vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams) * kernel._33;

        return (vUL + vCL + vDL + vUR + vCR + vDR)/6.0;
    }

    float SobelDepthVertical3X3(float3x3 kernel, float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
    {
        float vUL = LinearEyeDepth(SampleSceneDepth(uL), _ZBufferParams) * kernel._11;
        float vUC = LinearEyeDepth(SampleSceneDepth(uC), _ZBufferParams) * kernel._12;
        float vUR = LinearEyeDepth(SampleSceneDepth(uR), _ZBufferParams) * kernel._13;
        float vDL = LinearEyeDepth(SampleSceneDepth(dL), _ZBufferParams) * kernel._31;
        float vDC = LinearEyeDepth(SampleSceneDepth(dC), _ZBufferParams) * kernel._32;
        float vDR = LinearEyeDepth(SampleSceneDepth(dR), _ZBufferParams) * kernel._33;
                   
        return (vUL + vUC + vUR + vDL + vDC + vDR)/6.0;
    }

    float SobelDepth1X3(float3 kernel, float2 uv0, float2 uv1, float2 uv2)
    {
        float v0 = LinearEyeDepth(SampleSceneDepth(uv0), _ZBufferParams) * kernel.r;
        float v1 = LinearEyeDepth(SampleSceneDepth(uv1), _ZBufferParams) * kernel.g;
        float v2 = LinearEyeDepth(SampleSceneDepth(uv2), _ZBufferParams) * kernel.b;

        return (v0 + v1 + v2)/3.0;
    }

    float3 FullRangeNormal(float3 normal)
    {
        return (normal * 2) - 1;
    }

    float SobelNormalHorizontal3x3(float3x3 kernel, float2 c, float2 uL, float2 cL, float2 dL, float2 uR, float2 cR, float2 dR)
    {
        float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * kernel._11;
        float3 vCL = FullRangeNormal(SampleSceneNormals(cL)) * kernel._21;
        float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * kernel._31;
        float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * kernel._13;
        float3 vCR = FullRangeNormal(SampleSceneNormals(cR)) * kernel._23;
        float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * kernel._33;
        float3 vC = FullRangeNormal(SampleSceneNormals(c));

        float dUL = dot(vUL, vC);
        float dCL = dot(vCL, vC);
        float dDL = dot(vDL, vC);
        float dUR = dot(vUR, vC);
        float dCR = dot(vCR, vC);
        float dDR = dot(vDR, vC);

        float d = (dUL + dCL + dDL + dUR + dCR + dDR)/6.0;
        
        return d;
    }

    float SobelNormalVertical3x3(float3x3 kernel, float2 c, float2 uL, float2 uC, float2 uR, float2 dL, float2 dC, float2 dR)
    {
        float3 vUL = FullRangeNormal(SampleSceneNormals(uL)) * kernel._11;
        float3 vUC = FullRangeNormal(SampleSceneNormals(uC)) * kernel._12;
        float3 vUR = FullRangeNormal(SampleSceneNormals(uR)) * kernel._13;
        float3 vDL = FullRangeNormal(SampleSceneNormals(dL)) * kernel._31;
        float3 vDC = FullRangeNormal(SampleSceneNormals(dC)) * kernel._32;
        float3 vDR = FullRangeNormal(SampleSceneNormals(dR)) * kernel._33;
        float3 vC = FullRangeNormal(SampleSceneNormals(c));

        float dUL = dot(vUL, vC);
        float dUC = dot(vUC, vC);
        float dUR = dot(vUR, vC);
        float dDL = dot(vDL, vC);
        float dDC = dot(vDC, vC);
        float dDR = dot(vDR, vC);

        float d = (dUL + dUC + dUR + dDL + dDC + dDR)/6.0;
        
        return d;
    }

    float SobelNormal1X3(float3 kernel, float2 uv0, float2 uv1, float2 uv2)
    {
        float3 v0 = FullRangeNormal(SampleSceneNormals(uv0)) * kernel.r;
        float3 v1 = FullRangeNormal(SampleSceneNormals(uv1));
        float3 v2 = FullRangeNormal(SampleSceneNormals(uv2)) * kernel.b;

        float d0 = dot(v0, v1);
        float d1 = dot(v2, v1);

        return (d0 + d1)/2.0;
    }

#endif
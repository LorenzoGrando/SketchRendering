Shader "Hidden/DepthNormalsSilhouette"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Horizontal Sobel"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/EdgeDetection/DepthNormals/SobelDepthNormalsInclude.hlsl"

           #pragma multi_compile_local SOURCE_DEPTH SOURCE_DEPTH_NORMALS
           #pragma multi_compile_local SOBEL_KERNEL_3X3 SOBEL_KERNEL_1X3
           
           #pragma vertex Vert
           #pragma fragment Frag
           
           float _OutlineOffset;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;
               
               //DepthSamples
               //UV positions for samples
               float2 dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR;
               Get3X3NeighborhoodPositions(uv, _OutlineOffset, _CameraDepthTexture_TexelSize.xy, dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR);

               #if defined(SOBEL_KERNEL_3X3)
               float depthEdge = SobelDepthHorizontal3X3(BaseSobel3X3VerticalKernel,dUL, dCL, dDL, dUR, dCR, dDR);
               #elif defined(SOBEL_KERNEL_1X3)
               float depthEdge = SobelDepth1X3(Sobel1X3Kernel, dCL, uv, dCR);
               #endif
               
               #if defined(SOURCE_DEPTH_NORMALS)
               //NormalsSamples
               //UV positions for samples
               float2 nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR;
               Get3X3NeighborhoodPositions(uv, _OutlineOffset, _CameraNormalsTexture_TexelSize.xy, nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR);

               #if defined(SOBEL_KERNEL_3X3)
               float normalEdge = SobelNormalHorizontal3x3(BaseSobel3X3VerticalKernel, uv, nUL, nCL, nDL, nUR, nCR, nDR);
               #elif defined(SOBEL_KERNEL_1X3)
               float normalEdge = SobelNormal1X3(Sobel1X3Kernel, nCL, uv, nCR);
               #endif
               
               #endif

               //Store for use in vertical pass
               #if defined(SOURCE_DEPTH)
               return float4(abs(depthEdge), 0, 0, 1);
               #elif defined(SOURCE_DEPTH_NORMALS)
               return float4(abs(depthEdge), normalEdge, 0.0, 1.0);
               #endif
           }

           ENDHLSL
       }

       Pass
       {
           Name "Vertical Sobel"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"
           #include "Assets/Scripts/EdgeDetection/DepthNormals/SobelDepthNormalsInclude.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local SOURCE_DEPTH SOURCE_DEPTH_NORMALS
           #pragma multi_compile_local SOBEL_KERNEL_3X3 SOBEL_KERNEL_1X3

           int _OutlineOffset;
           float _OutlineThreshold;
           float _OutlineShallowThresholdSensitivity;
           float _OutlineShallowThresholdStrength;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               //DepthSamples
               //UV positions for samples
               float2 dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR;
               Get3X3NeighborhoodPositions(uv, _OutlineOffset, _CameraDepthTexture_TexelSize.xy, dUL, dUC, dUR, dCL, dCR, dDL, dDC, dDR);

               #if defined(SOBEL_KERNEL_3X3)
               float depthEdge = SobelDepthVertical3X3(BaseSobel3X3VerticalKernel,dUL, dUC, dUR, dDL, dDC, dDR);
               #elif defined(SOBEL_KERNEL_1X3)
               float depthEdge = SobelDepth1X3(Sobel1X3Kernel, dUC, uv, dDC);
               #endif

               #if defined(SOURCE_DEPTH_NORMALS)
               //NormalsSamples
               //UV positions for samples
               float2 nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR;
               Get3X3NeighborhoodPositions(uv, _OutlineOffset, _CameraNormalsTexture_TexelSize.xy, nUL, nUC, nUR, nCL, nCR, nDL, nDC, nDR);

               #if defined(SOBEL_KERNEL_3X3)
               float3 normalEdge = SobelNormalVertical3x3(BaseSobel3X3VerticalKernel, uv, nUL, nUC, nUR, nDL, nDC, nDR);
               #elif defined(SOBEL_KERNEL_1X3)
               float normalEdge = SobelNormal1X3(Sobel1X3Kernel, dUC, uv, dDC);
               #endif
               
               #endif
               
               
               //Sample horizontal pass (passed as blit texture) and get filter results in RG (depth, normal)
               float4 horizontalValue = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, _BlitMipLevel);
               //Get gradient of each image, and threshold for silhouette)
               //If normals texture is available, modify threshold depending on viewing angle to avoid thick edges in very shallow angles
               //Specifically, if view vector is almost perpendicular to surface normal, make the threshold higher.
               #if defined(SOURCE_DEPTH)
               float depthGradient = step(_OutlineThreshold, length(float2(horizontalValue.r, depthEdge)));
               #elif defined (SOURCE_DEPTH_NORMALS)
               float3 surfaceNormal = SampleSceneNormals(uv);
               //Get view vector, see bgolus and keijiro: https://discussions.unity.com/t/help-with-view-space-normals/654031/12
               float2 perspectiveProj = float2(unity_CameraProjection._11, unity_CameraProjection._22);
               float3 viewDir = -normalize(float3((uv * 2 - 1)/perspectiveProj, -1));
               
               half isShallow = step(1 - _OutlineShallowThresholdSensitivity, 1 - dot(viewDir, surfaceNormal));
               float threshold = _OutlineThreshold + isShallow * (_OutlineThreshold * 10.0 * _OutlineShallowThresholdStrength);
               float depthGradient = step(threshold, length(float2(horizontalValue.r, depthEdge)));
               #endif
               
               #if defined(SOURCE_DEPTH_NORMALS)
               float gradN = length(float2(horizontalValue.g, normalEdge.r)/2.0);
               float normalGradient = step(_OutlineThreshold, gradN);
               #endif

               //Set alpha as outline strenght, to easy blending in composite shader
               #if defined(SOURCE_DEPTH)
               return float4(max(0, depthGradient).rrrr);
               #elif defined(SOURCE_DEPTH_NORMALS)
               return float4(max(0, max(depthGradient, normalGradient)).rrrr);
               #endif
           }

           ENDHLSL
       }

        Pass
       {
           Name "Roberts Cross"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NeighborhoodSample.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local_fragment _ SOURCE_DEPTH SOURCE_DEPTH_NORMALS

           float _OutlineThreshold;
           
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               
               return float4(1, 1, 1, 1);
           }

           ENDHLSL
       }
   }
}
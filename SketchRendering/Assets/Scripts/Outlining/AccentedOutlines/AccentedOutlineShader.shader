Shader "Hidden/AccentedOutline"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Accented Outlines"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NoiseFunctions.hlsl"
           
           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local_fragment _ DISTORT_OUTLINES BAKED_DISTORT_OUTLINES
           #pragma multi_compile_local_fragment _ MASK_OUTLINES

           float _DistortionRate;
           float _DistortionStrength;
           
           TEXTURE2D(_OutlineMaskTex);
           SAMPLER(sampler_OutlineMaskTex);

           TEXTURE2D(_BakedUVDistortionTex);

           CBUFFER_START(UnityPerMaterial)
                float4 _OutlineMaskTex_ST;
           CBUFFER_END
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;
               float4 col = (float4)0;

               #if defined MASK_OUTLINES
               float2 maskUvs = TRANSFORM_TEX(uv, _OutlineMaskTex);
               float mask = SAMPLE_TEXTURE2D_X_LOD(_OutlineMaskTex, sampler_OutlineMaskTex, maskUvs, _BlitMipLevel).r;
               #endif

               #if defined DISTORT_OUTLINES
               float displacementX = (1 - Voronoi(uv, 1.0, _DistortionRate)) * 2 - 1;
               float displacementY = (1 - Voronoi(uv, -1.0, _DistortionRate)) * 2 - 1;
               uv = saturate(uv + float2(displacementX, displacementY) * _DistortionStrength);
               #elif defined BAKED_DISTORT_OUTLINES
               float2 distUVs = (SAMPLE_TEXTURE2D_X_LOD(_BakedUVDistortionTex, sampler_PointClamp, uv, _BlitMipLevel).xy * 2.0) - 1.0;
               uv = saturate(uv + distUVs * _DistortionStrength);
               #endif
               
               col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, _BlitMipLevel);

               #if defined MASK_OUTLINES
               col *= 1 - mask;
               #endif
               
               return float4(col.rgb, 1.0);
           }

           ENDHLSL
       }

       Pass
       {
           Name "Bake Distorted UVs"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/NoiseFunctions.hlsl"
           
           #pragma vertex Vert
           #pragma fragment Frag

           float _DistortionRate;
           float _DistortionStrength;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               float displacementX = 1 - Voronoi(uv, 1.0, _DistortionRate);
               float displacementY = 1 - Voronoi(uv, -1.0, _DistortionRate);
               float2 displacementUVs = float2(displacementX, displacementY);
               
               return float4(displacementUVs, 0.0, 1.0);
           }

           ENDHLSL
       }
   }
}
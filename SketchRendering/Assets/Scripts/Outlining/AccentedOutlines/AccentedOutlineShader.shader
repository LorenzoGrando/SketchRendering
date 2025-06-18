Shader "Hidden/AccentedOutline"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Thicken Outlines"

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
               float displacementX = (1 - Voronoi(uv, 1.0, _DistortionRate)) * 2 - 1;
               float displacementY = (1 - Voronoi(uv, 1.0, _DistortionRate)) * 2 - 1;
               float2 distUV = saturate(uv + float2(displacementX, displacementY) * _DistortionStrength);
               float4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, distUV, _BlitMipLevel);

               return float4(col.rgb, 1.0);
           }

           ENDHLSL
       }
   }
}
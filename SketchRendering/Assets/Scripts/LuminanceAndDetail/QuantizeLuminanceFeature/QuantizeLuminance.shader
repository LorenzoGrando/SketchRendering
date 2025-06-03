Shader "Hidden/QuantizeLuminance"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Image Luminance"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/LuminanceAndDetail/QuantizeLuminanceFeature/TamSampleInclude.hlsl"
           
           #pragma vertex Vert
           #pragma fragment Frag

           #pragma multi_compile_local_fragment TAM_SINGLE TAM_DOUBLE TAM_TRIPLE
           #pragma multi_compile_local_fragment _ QUANTIZE

           int _NumTones;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 uv = input.texcoord;

               //get pixel luminance: https://stackoverflow.com/questions/596216/formula-to-determine-perceived-brightness-of-rgb-color
               float4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
               //simple luminance
               //float lum = (col.r * 2 + col.b + + col.g * 3)/6.0;
               //perceived luminance, updated to use dot
               float lum = dot(col, float3(0.299, 0.586, 0.114));
               #if defined(QUANTIZE)
               lum = floor(lum * _NumTones)/_NumTones;
               #endif
               
               float stroke = SampleTAM(lum, _NumTones, uv);
               
               return float4(stroke.rrr, 1);
           }

           ENDHLSL
       }
   }
}
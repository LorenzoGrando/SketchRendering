Shader "Hidden/MaterialSurface"
{
    SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "Material Surface"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
           #include "Assets/Scripts/Includes/TextureOperations.hlsl"
        
           
           #pragma vertex Vert
           #pragma fragment Frag
           
           #pragma multi_compile_local_fragment UVS_SCREEN_SPACE UVS_OBJECT_SPACE UVS_OBJECT_SPACE_CONSTANT

           TEXTURE2D(_CameraUVsTexture); 
           
           TEXTURE2D(_MaterialAlbedoTex);
           float4 _MaterialAlbedoTex_TexelSize;
           TEXTURE2D(_MaterialDirectionalTex);
           float4 _MaterialDirectional_TexelSize;
           
           float2 _TextureScales;
           float _BlendStrength;
           
           float4 Frag(Varyings input) : SV_Target0
           {
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
               float2 screenSpaceUV = input.texcoord;
               float2 uv = screenSpaceUV;
               #if defined UVS_OBJECT_SPACE || defined UVS_OBJECT_SPACE_CONSTANT
               float2 objectUVs = SAMPLE_TEXTURE2D_X_LOD(_CameraUVsTexture, sampler_PointClamp, screenSpaceUV, _BlitMipLevel).xy;
               uv = objectUVs;
               #endif
               
               float4 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, screenSpaceUV, _BlitMipLevel);
               float4 albedo = SAMPLE_TEX(_MaterialAlbedoTex, sampler_PointRepeat, _MaterialAlbedoTex_TexelSize.z, _BlitMipLevel, uv, _TextureScales);

               float4 final = lerp(albedo, col, _BlendStrength);
               return float4(final.rgba);
           }

           ENDHLSL
       }
   }
}
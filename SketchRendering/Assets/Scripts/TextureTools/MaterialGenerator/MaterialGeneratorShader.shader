Shader "Hidden/MaterialGeneratorShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            Name "Generate Albedo"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Scripts/Includes/NoiseFunctions.hlsl"
            #include "Assets/Scripts/Includes/MathUtils.hlsl"

            #pragma multi_compile_local_fragment _ USE_GRANULARITY
            #pragma multi_compile_local_fragment _ USE_LAID_LINES
            #pragma multi_compile_local_fragment _ USE_CRUMPLES
            #pragma multi_compile_local_fragment _ USE_NOTEBOOK_LINES

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            #pragma vertex Vert
            #pragma fragment Frag

            //Granularity
            float2 _GranularityScale;
            int _GranularityOctaves;
            float _GranularityLacunarity;
            float _GranularityPersistence;
            float2 _GranularityValueRange;
            float4 _GranularityTint;

            //Laid Lines
            float _LaidLineFrequency;
            float _LaidLineThickness;
            float _LaidLineStrength;
            float _LaidLineDisplacement;
            float _LaidLineMask;
            float4 _LaidLineTint;

            //Crumples
            float2 _CrumplesScale;
            float _CrumplesJitter;
            float _CrumplesStrength;
            int _CrumplesOctaves;
            float _CrumplesLacunarity;
            float _CrumplesPersistence;
            float4 _CrumplesTint;
            float _CrumplesTintSharpness;
            float _CrumplesTintStrength;

            //Notebook Lines
            float2 _NotebookLinePhase;
            float2 _NotebookLineFrequency;
            float2 _NotebookLineSize;
            float _NotebookLineGranularitySensitivity;
            float4 _NotebookLineHorizontalTint;
            float4 _NotebookLineVerticalTint;
            

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(i.vertexID);
                o.texcoord = GetFullScreenTriangleTexCoord(i.vertexID);

                return o;
            }
            
            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 unmutableUV = i.texcoord;
                float2 uv = unmutableUV;

                //Create polygonal-esque bumps in the paper, to simulate being crumpled and flattened
                float crumpleTint = 0;
                #if defined (USE_CRUMPLES)
                float crumpleFrequency = 1.0;
                float crumpleAmplitude = 1.0;
                float3 crumpleT = 0;
                for (int i = 0; i < _CrumplesOctaves; i++)
                {
                    crumpleT += cellularNoiseDirTileable(uv, _CrumplesScale * crumpleFrequency, _CrumplesJitter, 0) * crumpleAmplitude;
                    crumpleFrequency *= _CrumplesLacunarity;
                    crumpleAmplitude *= _CrumplesPersistence;
                }
                float3 crumpleSum = crumpleT * _CrumplesStrength;
                float crushDir = (crumpleSum.y * (1.0 - crumpleT.x) * 1.0/_CrumplesScale.x);
                //return float4(abs(crushDir), 0, 0, 1.0);
                crumpleTint = pow(crumpleT.x, _CrumplesTintSharpness) * _CrumplesTintStrength;
                uv.x += crushDir.x;
                #endif
                
                // Base Granularity of paper, defining a heightmap
                float granularity = 1;
                #if defined (USE_GRANULARITY)
                float frequency = 1.0;
                float amplitude = 1.0;
                float3 t = 0;
                for (int i = 0; i < _GranularityOctaves; i++)
                {
                    t += perlinNoiseDirTileable(uv, _GranularityScale * frequency, 0) * amplitude;
                    frequency *= _GranularityLacunarity;
                    amplitude *= _GranularityPersistence;
                }
                granularity = t.x;
                float2 granularityDir = t.yz;
                granularity = granularity * 0.5 + 0.5;
                granularity = lerp(_GranularityValueRange.x, _GranularityValueRange.y, granularity);
                #endif

                //Waves emulate laid lines on paper
                float laidLines = 0;
                #if defined USE_LAID_LINES
                float laidCoords = uv.y;
                #if defined(USE_GRANULARITY)
                laidCoords += granularityDir.y * _LaidLineDisplacement * 0.01;
                #endif
                
                laidLines = sin(laidCoords * TAU * _LaidLineFrequency) * 0.5 + 0.5;
                #if defined(USE_GRANULARITY)
                laidLines -= t.x * -_LaidLineMask;
                #endif
                
                laidLines = smoothstep((1.0 - _LaidLineThickness), 1.0, laidLines);
                laidLines *= _LaidLineStrength;
                #endif

                float2 notebookLines = 0;
                #if defined(USE_NOTEBOOK_LINES)
                notebookLines = unmutableUV.yx;
                notebookLines = abs(sin((notebookLines + (_NotebookLinePhase/2.0)) * _NotebookLineFrequency * PI));
                notebookLines = step(1.0 - (_NotebookLineSize), notebookLines);
                float notebookSensitivity = 1.0;
                #if defined(USE_GRANULARITY)
                notebookSensitivity = step(_NotebookLineGranularitySensitivity, 1.0 - granularity);
                #endif
                notebookLines *= notebookSensitivity;
                #endif
                
                //Combine all elements
                float4 paperColor = lerp(_GranularityTint * _GranularityValueRange.x, _GranularityTint * _GranularityValueRange.y, granularity);
                float4 laidLineColor = _LaidLineTint * laidLines;
                float4 crumpleColor = _CrumplesTint * crumpleTint;
                float4 horizontalNotebookColor = notebookLines.x * _NotebookLineHorizontalTint;
                float4 verticalNotebookColor = notebookLines.y * _NotebookLineVerticalTint;
                //float paperHeightmap = granularity + crumpleTint - laidLines;
                float4 composite = lerp(paperColor, laidLineColor, laidLineColor.a);
                composite = lerp(composite, crumpleColor, crumpleColor.a);
                composite = lerp(composite, horizontalNotebookColor, horizontalNotebookColor.a);
                composite = lerp(composite, verticalNotebookColor, verticalNotebookColor.a);
                return float4(composite.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Generate Directional Map"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Scripts/Includes/NoiseFunctions.hlsl"
            #include "Assets/Scripts/Includes/MathUtils.hlsl"

            #pragma multi_compile_local_fragment _ USE_GRANULARITY
            #pragma multi_compile_local_fragment _ USE_CRUMPLES

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            #pragma vertex Vert
            #pragma fragment Frag

            //Granularity
            float2 _GranularityScale;
            int _GranularityOctaves;
            float _GranularityLacunarity;
            float _GranularityPersistence;
            float2 _GranularityValueRange;

            //Crumples
            float2 _CrumplesScale;
            float _CrumplesJitter;
            float _CrumplesStrength;
            int _CrumplesOctaves;
            float _CrumplesLacunarity;
            float _CrumplesPersistence;

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(i.vertexID);
                o.texcoord = GetFullScreenTriangleTexCoord(i.vertexID);

                return o;
            }
            
            float4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = i.texcoord;

                //Create polygonal-esque bumps in the paper, to simulate being crumpled and flattened
                float crumpleDir = 0;
                #if defined (USE_CRUMPLES)
                float crumpleFrequency = 1.0;
                float crumpleAmplitude = 1.0;
                float3 crumpleT = 0;
                for (int i = 0; i < _CrumplesOctaves; i++)
                {
                    crumpleT += cellularNoiseDirTileable(uv, _CrumplesScale * crumpleFrequency, _CrumplesJitter, 0) * crumpleAmplitude;
                    crumpleFrequency *= _CrumplesLacunarity;
                    crumpleAmplitude *= _CrumplesPersistence;
                }
                float3 crumpleSum = crumpleT * _CrumplesStrength;
                crumpleDir = (crumpleSum.y * (1.0 - crumpleT.x));
                uv.x += crumpleDir.x;
                #endif
                
                // Base Granularity of paper, defining a heightmap
                float2 granularityDir = float2(0, 0);
                #if defined (USE_GRANULARITY)
                float frequency = 1.0;
                float amplitude = 1.0;
                float3 t = 0;
                for (int i = 0; i < _GranularityOctaves; i++)
                {
                    t += perlinNoiseDirTileable(uv, _GranularityScale * frequency, 0) * amplitude;
                    frequency *= _GranularityLacunarity;
                    amplitude *= _GranularityPersistence;
                }
                granularityDir = t.yz ;
                #endif
                
                //Combine all elements
                float2 dir = ((granularityDir.xy * 0.25) + float2(crumpleDir * 0.75, 0));
                dir = float2((dir + 1) * 0.5);
                return float4(dir.xy, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
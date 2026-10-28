
Shader "Custom/Vertex Colored Lit URP"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                half3 normalWS     : TEXCOORD1;
                float4 color       : COLOR;
                float fogFactor    : TEXCOORD2;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                SurfaceData surfaceData = (SurfaceData)0;

                surfaceData.albedo = IN.color.rgb;
                surfaceData.alpha = IN.color.a;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.5, 0.5, 0.5);
                surfaceData.smoothness = 0.5;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.emission = 0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;

                inputData.positionWS = IN.positionWS;
                inputData.normalWS = NormalizeNormalPerPixel(IN.normalWS);
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord =
                    TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting =
                    VertexLighting(IN.positionWS, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(IN.positionHCS);

                half4 color = UniversalFragmentPBR(
                    inputData, surfaceData);

                color.rgb = MixFog(color.rgb, IN.fogFactor);

                return color;
            }

            ENDHLSL
        }
    }
}

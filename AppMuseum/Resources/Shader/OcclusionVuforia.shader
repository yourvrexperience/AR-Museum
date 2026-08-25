Shader "Template6DOF/OcclusionVuforia"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" "RenderType"="Opaque" }

        // Main occluder pass — writes depth, no color
        Pass
        {
            Name "Occluder"
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionHCS : SV_POSITION; };
            Varyings vert (Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 frag (Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // DepthOnly pass — so the mask participates in URP's depth prepass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionHCS : SV_POSITION; };
            Varyings vert (Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 frag (Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
Shader "GameGraphics/DAY08/Outline Shell"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.02, 0.025, 0.08, 1.0)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry-1"
        }

        Pass
        {
            Name "OutlineShell"
            Tags { "LightMode"="UniversalForward" }

            // 앞면을 버리고 확장된 뒷면만 그려 Inverted Hull 외곽선을 만듭니다.
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 expandedOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(expandedOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

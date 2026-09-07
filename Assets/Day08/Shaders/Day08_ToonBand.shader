Shader "GameGraphics/DAY08 Toon Band"
{
    Properties
    {
        _LitColor ("Lit Color", Color) = (1.0, 0.82, 0.25, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.22, 0.10, 0.42, 1.0)
        _LightDirectionWS ("Light Direction WS", Vector) = (0.3, 0.8, 0.4, 0.0)
        _BandThreshold ("Band Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor;
                half4 _ShadowColor;
                float4 _LightDirectionWS;
                float _BandThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 l = normalize(_LightDirectionWS.xyz);
                float ndl = dot(n, l);
                float remapped = ndl * 0.5 + 0.5;
                float band = step(_BandThreshold, remapped);
                return lerp(_ShadowColor, _LitColor, band);
            }
            ENDHLSL
        }
    }
}

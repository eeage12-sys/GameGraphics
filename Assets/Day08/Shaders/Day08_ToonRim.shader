Shader "GameGraphics/DAY08 Toon Rim"
{
    Properties
    {
        _LitColor ("Lit Color", Color) = (1.0, 0.82, 0.25, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.22, 0.10, 0.42, 1.0)
        _LightDirectionWS ("Light Direction WS", Vector) = (0.3, 0.8, 0.4, 0.0)
        _BandThreshold ("Band Threshold", Range(0,1)) = 0.5
        [HDR]_RimColor ("Rim Color", Color) = (0.0, 1.0, 0.9, 1.0)
        _RimPower ("Rim Power", Range(0.1,8)) = 3
        _RimIntensity ("Rim Intensity", Range(0,5)) = 2
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
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 l = normalize(_LightDirectionWS.xyz);
                float remapped = dot(n, l) * 0.5 + 0.5;
                float band = step(_BandThreshold, remapped);
                half3 baseCol = lerp(_ShadowColor.rgb, _LitColor.rgb, band);

                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(saturate(1.0 - dot(n, v)), max(_RimPower, 0.0001));
                half3 rim = _RimColor.rgb * fresnel * _RimIntensity;
                return half4(baseCol + rim, 1.0);
            }
            ENDHLSL
        }
    }
}

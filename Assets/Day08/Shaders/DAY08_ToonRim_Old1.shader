Shader "GameGraphics/DAY08/Toon Rim"
{
    Properties
    {
        _LitColor ("Lit Color", Color) = (1.0, 0.78, 0.20, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.24, 0.08, 0.45, 1.0)
        _LightDirectionWS ("Light Direction WS", Vector) = (0.3, 0.8, 0.4, 0.0)
        _BandThreshold ("Band Threshold", Range(0,1)) = 0.5

        [HDR] _RimColor ("Rim Color", Color) = (0.0, 1.0, 0.95, 1.0)
        _RimPower ("Rim Power", Range(0.25,8)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0,5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

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
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
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

                half3 toon = lerp(_ShadowColor.rgb, _LitColor.rgb, band);

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(n, viewDir)), _RimPower);
                half3 rim = _RimColor.rgb * fresnel * _RimIntensity;

                return half4(toon + rim, 1.0);
            }
            ENDHLSL
        }
    }
}

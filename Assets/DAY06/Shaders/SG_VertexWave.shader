Shader "Shader Graphs/SG_VertexWave"
{
    Properties
    {
        _WaveTexture ("WaveTexture", 2D) = "white" {}
        _Amplitude ("Amplitude", Float) = 0.15
        _WaveFrequency ("WaveFrequency", Float) = 2.0
        _WaveSpeed ("WaveSpeed", Float) = 1.5
        _UvTiling ("UvTiling", Vector) = (1,1,0,0)
        _UvFlowDirection ("UvFlowDirection", Vector) = (0.03,0.08,0,0)
        _UvFlowSpeed ("UvFlowSpeed", Float) = 0.2
        _CrossWaveFrequency ("CrossWaveFrequency", Float) = 1.6
        _CrossWaveSpeed ("CrossWaveSpeed", Float) = 1.1
        _CrossWaveStrength ("CrossWaveStrength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "DAY06VertexWave"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WaveTexture);
            SAMPLER(sampler_WaveTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveTexture_ST;
                float _Amplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float4 _UvTiling;
                float4 _UvFlowDirection;
                float _UvFlowSpeed;
                float _CrossWaveFrequency;
                float _CrossWaveSpeed;
                float _CrossWaveStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            float WaveHeight(float3 positionOS)
            {
                float t = _Time.y;
                float waveX = sin(positionOS.x * _WaveFrequency + t * _WaveSpeed) * _Amplitude;
                float waveZ = sin(positionOS.z * _CrossWaveFrequency + t * _CrossWaveSpeed)
                            * _Amplitude * _CrossWaveStrength;
                return (waveX + waveZ) * 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = IN.positionOS.xyz;
                positionOS.y += WaveHeight(positionOS);
                OUT.positionHCS = TransformObjectToHClip(positionOS);

                float2 flowOffset = _UvFlowDirection.xy * (_Time.y * _UvFlowSpeed);
                OUT.uv = IN.uv * _UvTiling.xy + flowOffset;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_WaveTexture, sampler_WaveTexture, IN.uv);
                half3 col = MixFog(tex.rgb, IN.fogFactor);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}

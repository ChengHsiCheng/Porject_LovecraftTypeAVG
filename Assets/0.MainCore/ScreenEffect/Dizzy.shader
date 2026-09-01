Shader "Hidden/GushenCafe/Dizzy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "Dizzy"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _Intensity;
            float _Distortion;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Swirl;
            float _SwirlSpeed;
            float _Chromatic;
            float _Vignette;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // 由畫面中心往外的波紋 ＋ 整體來回旋轉
            float2 DistortUV(float2 uv, float time)
            {
                float2 offset = uv - 0.5;
                float dist = length(offset);
                float2 dir = offset / max(dist, 1e-5);

                float wave = sin(dist * _WaveFrequency - time * _WaveSpeed);
                uv += dir * wave * _Distortion * _Intensity * dist;

                float angle = _Swirl * _Intensity * sin(time * _SwirlSpeed) * (1.0 - dist);
                float s, c;
                sincos(angle, s, c);

                float2 p = uv - 0.5;
                p = float2(p.x * c - p.y * s, p.x * s + p.y * c);

                return p + 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 uv = input.uv;
                float2 distorted = DistortUV(uv, time);

                // 色散：紅藍往相反方向偏移
                float2 offset = distorted - 0.5;
                float2 dir = offset / max(length(offset), 1e-5);
                float shift = _Chromatic * _Intensity * 0.01 * length(offset);

                half4 color;
                color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distorted + dir * shift).r;
                color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distorted).g;
                color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distorted - dir * shift).b;
                color.a = 1.0;

                // 邊緣壓黑，帶呼吸節奏
                float pulse = 0.85 + 0.15 * sin(time * _WaveSpeed * 0.5);
                float vignette = length(uv - 0.5) * _Vignette * _Intensity * pulse;
                color.rgb *= saturate(1.0 - vignette);

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

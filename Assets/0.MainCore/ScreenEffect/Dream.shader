Shader "Hidden/GushenCafe/Dream"
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
            Name "Dream"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _Intensity;
            float _Blur;
            float _Glow;
            float _GlowThreshold;
            float _Desaturate;
            float4 _Tint;
            float _EdgeSoftness;
            float _BreathSpeed;

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

            // 八個方向取樣的簡易柔焦
            half3 SampleBlur(float2 uv, float radius)
            {
                float2 texel = _MainTex_TexelSize.xy * radius;

                half3 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb * 2.0;
                float weight = 2.0;

                const float2 offsets[8] =
                {
                    float2( 1,  0), float2(-1,  0), float2( 0,  1), float2( 0, -1),
                    float2( 0.7,  0.7), float2(-0.7,  0.7), float2( 0.7, -0.7), float2(-0.7, -0.7)
                };

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offsets[i] * texel).rgb;
                    weight += 1.0;
                }

                return sum / weight;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;

                // 呼吸：整體強度緩慢起伏，夢境不該是靜止的
                float breath = 1.0 + 0.15 * sin(_Time.y * _BreathSpeed);
                float amount = saturate(_Intensity * breath);

                // 邊緣比中央更糊，像視野邊界在溶解
                float distanceFromCenter = length(uv - 0.5);
                float radius = _Blur * amount * (1.0 + distanceFromCenter * _EdgeSoftness);

                half3 blurred = SampleBlur(uv, radius);

                // 亮部再取一次更大的模糊，疊回去當光暈
                half3 wide = SampleBlur(uv, radius * 2.5);
                float brightness = dot(wide, half3(0.299, 0.587, 0.114));
                half3 glow = wide * saturate((brightness - _GlowThreshold) / max(1.0 - _GlowThreshold, 1e-5));

                half3 color = lerp(baseColor, blurred, amount);
                color += glow * _Glow * amount;

                // 褪色與染色
                float gray = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(color, gray.xxx, _Desaturate * amount);
                color = lerp(color, color * _Tint.rgb, amount);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}

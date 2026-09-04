Shader "Hidden/GushenCafe/Ghost"
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
            Name "Ghost"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _Intensity;
            float2 _Offset;
            float _Count;
            float _Falloff;
            float _DriftSpeed;
            float _ColorShift;

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

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // drift 為 0 時就照 offset 的方向；大於 0 時沿著橢圓繞圈，像視線對不準焦
                float angle = _Time.y * _DriftSpeed;
                float2 direction = float2(cos(angle), sin(angle));
                float2 step = direction * _Offset * _MainTex_TexelSize.xy;

                half3 accumulated = baseColor.rgb;
                float totalWeight = 1.0;

                [unroll]
                for (int i = 1; i <= 4; i++)
                {
                    if (i > (int)_Count) break;

                    float weight = pow(_Falloff, i) * _Intensity;
                    half3 ghost = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + step * i).rgb;

                    // 每層往不同色相偏一點，重影才不會只是變模糊
                    half3 tint = lerp(half3(1, 1, 1),
                                      half3(1.0 + _ColorShift * (i % 2),
                                            1.0,
                                            1.0 + _ColorShift * ((i + 1) % 2)),
                                      _Intensity);

                    accumulated += ghost * tint * weight;
                    totalWeight += weight;
                }

                half4 color = baseColor;
                color.rgb = accumulated / totalWeight;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

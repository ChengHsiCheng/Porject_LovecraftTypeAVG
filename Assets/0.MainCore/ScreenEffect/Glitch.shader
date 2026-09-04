Shader "Hidden/GushenCafe/Glitch"
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
            Name "Glitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _Intensity;
            float _Displacement;
            float _BlockDensity;
            float _BlockAmount;
            float _ColorSplit;
            float _Noise;
            float _Speed;

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

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 時間切成一格一格，讓錯位是「跳」的而不是連續滑動
                float slice = floor(_Time.y * _Speed);

                // 依橫條分組，一部分的橫條整條往左右錯開
                float block = floor(uv.y * _BlockDensity);
                float blockRandom = Hash(float2(block, slice));
                float shouldShift = step(1.0 - _BlockAmount * _Intensity, blockRandom);
                float shift = (Hash(float2(block * 1.7, slice * 2.3)) * 2.0 - 1.0)
                              * _Displacement * _Intensity * shouldShift;

                float2 shiftedUV = float2(uv.x + shift, uv.y);

                // RGB 分離
                float split = _ColorSplit * _Intensity * _MainTex_TexelSize.x;

                half4 color;
                color.r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shiftedUV + float2(split, 0)).r;
                color.g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shiftedUV).g;
                color.b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shiftedUV - float2(split, 0)).b;
                color.a = 1.0;

                // 雜訊顆粒
                float grain = Hash(uv * 512.0 + slice) - 0.5;
                color.rgb += grain * _Noise * _Intensity;

                // 錯位的那幾條額外提亮一點，像訊號爆掉
                color.rgb += shouldShift * _Intensity * 0.08 * blockRandom;

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

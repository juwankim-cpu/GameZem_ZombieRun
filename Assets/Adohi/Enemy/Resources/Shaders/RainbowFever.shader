Shader "Unlit/RainbowFever"
{
    Properties
    {
        _Speed("Rainbow Speed", Float) = 1
        _PixelSize("Pixel Size", Float) = 4
        _Intensity("Color Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Speed;
            float _PixelSize;
            float _Intensity;
            // <- 주석: _Time 는 빌트인으로 이미 존재(헤더에 정의될 수 있음). 직접 선언하지 않음.

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float rand(float2 n)
            {
                return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                uv *= _ScreenParams.xy / _PixelSize;
                uv = floor(uv) / (_ScreenParams.xy / _PixelSize);

                // 여기서 빌트인 _Time 사용
                float t = _Time.y * _Speed;

                float noise = rand(floor(uv * 100 + t * 5));
                uv.x += sin(t * 2 + noise * 6.283) * 0.01;
                uv.y += cos(t * 3 + noise * 6.283) * 0.01;

                float hue = frac(uv.x + uv.y * 0.5 + t * 0.2);
                float3 col = hsv2rgb(float3(hue, 1, 1)) * _Intensity;

                col += sin(noise * 20 + t * 10) * 0.1;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
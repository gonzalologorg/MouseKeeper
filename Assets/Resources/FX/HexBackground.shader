Shader "MouseKeeper/Hex Background"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.14, 0.06, 0.32, 1)
        _ColorB ("Color B", Color) = (0.36, 0.18, 0.65, 1)
        _Motion ("Motion", Float) = 0
        _Flash ("Damage flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                float _Motion;
                float _Flash;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            float2 Rotate(float2 p, float angle)
            {
                float s = sin(angle), c = cos(angle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv - 0.5;
                uv.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 p = Rotate(uv * 6.0, _Motion * 0.1) + 10.0;
                const float2 grid = float2(1.7320508, 1.0);
                float2 a = frac(p / grid) * grid - grid * 0.5;
                float2 b = frac((p - grid * 0.5) / grid) * grid - grid * 0.5;
                bool useB = dot(b, b) < dot(a, a);
                float2 local = useB ? b : a;
                float2 cell = useB ? floor((p - grid * 0.5) / grid) + 0.5 : floor(p / grid);
                float id = frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
                float direction = id < 0.5 ? -1.0 : 1.0;
                float phase = frac(_Motion * direction + id);
                float turn = smoothstep(-0.6, 0.6, cos(phase * 3.14159265));
                float pulse = cos(frac(_Motion + id) * 6.2831853) * 0.20 + 1.25;
                local = Rotate(local * pulse, turn * 1.04719755);
                float hex = max(abs(local.y), abs(local.x) * 0.8660254 + abs(local.y) * 0.5);
                float tile = 1.0 - smoothstep(0.30, 0.34, hex);
                float glow = 0.5 + 0.5 * sin(_Motion * 0.5 + id * 6.2831853 + length(uv) * 3.0);
                float diagonal = saturate((input.uv.x + input.uv.y) * 0.5);
                half3 color = lerp(_ColorA.rgb, _ColorB.rgb, saturate(0.08 + diagonal * 0.7 + glow * 0.12 + tile * 0.1));
                color *= lerp(0.62, 1.0, tile);
                color = lerp(color, half3(0.72, 0.025, 0.045) * (0.55 + tile * 0.45), saturate(_Flash));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

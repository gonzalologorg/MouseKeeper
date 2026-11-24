Shader "MouseKeeper/Floor Shadow"
{
    Properties
    {
        _FloorSize ("Floor size", Vector) = (1, 1, 0, 0)
        _Softness ("Softness", Float) = 1.6
        _Opacity ("Opacity", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _FloorSize;
                float _Softness;
                float _Opacity;
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
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * (_FloorSize.xy + 2.0 * _Softness);
                float2 outside = max(abs(p) - _FloorSize.xy * 0.5, 0.0);
                float alpha = (1.0 - smoothstep(0.0, _Softness, length(outside))) * _Opacity;
                return half4(0.015, 0.01, 0.025, alpha);
            }
            ENDHLSL
        }
    }
}

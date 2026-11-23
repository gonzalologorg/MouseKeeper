Shader "MouseKeeper/Blood Stains"
{
    Properties
    {
        _BaseMap ("Coverage (R)", 2D) = "black" {}
        _BumpMap ("Surface normals", 2D) = "bump" {}
        _BloodColor ("Blood color", Color) = (0.46, 0.024, 0.035, 1)
        _SpecColor ("Wet specular color", Color) = (0.36, 0.28, 0.28, 1)
        _BumpScale ("Normal strength", Range(0, 3)) = 2
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _RoomBrightness ("Room brightness", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #define _SPECULAR_SETUP 1
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BloodColor;
                half4 _SpecColor;
                half _Smoothness;
                half _BumpScale;
                half _RoomBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = normal.normalWS;
                output.tangentWS = half4(normal.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half coverage = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).r;
                clip(coverage - 0.004h);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = _BloodColor.rgb;
                surface.specular = _SpecColor.rgb;
                surface.smoothness = _Smoothness;
                half3 wetNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv).rgb * 2 - 1;
                wetNormal.xy *= _BumpScale;
                surface.normalTS = normalize(wetNormal);
                surface.occlusion = 1;
                surface.alpha = coverage * _BloodColor.a;

                half3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = normalize(TransformTangentToWorld(surface.normalTS,
                    half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.shadowMask = 1;

                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb * _RoomBrightness, input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}

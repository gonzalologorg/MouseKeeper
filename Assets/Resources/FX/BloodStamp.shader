Shader "Hidden/MouseKeeper/BloodStamp"
{
    Properties
    {
        _MainTex ("Previous paint", 2D) = "black" {}
        _StampColor ("Blood silhouette", 2D) = "white" {}
        _StampNormal ("Blood normal", 2D) = "bump" {}
        _NormalPass ("Paint normals", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _StampColor;
            sampler2D _StampNormal;
            float4 _Stamps[5];
            float4 _TrailDirection;
            float _NormalPass;

            float4 frag(v2f_img input) : SV_Target
            {
                float4 previous = tex2D(_MainTex, input.uv);
                float2 right = _TrailDirection.xy;
                float2 forward = _TrailDirection.zw;

                [unroll] for (int i = 0; i < 5; i++)
                {
                    float4 stamp = _Stamps[i];
                    float2 delta = input.uv - stamp.xy;
                    float2 uv = float2(dot(delta, right), dot(delta, forward)) / stamp.zw + 0.5;
                    if (any(uv < 0) || any(uv > 1)) continue;

                    float coverage = saturate(tex2D(_StampColor, uv).a * 1.5);
                    if (_NormalPass > 0.5)
                    {
                        float3 normal = UnpackNormal(tex2D(_StampNormal, uv));
                        normal.xy = right * normal.x + forward * normal.y;
                        previous.rgb = lerp(previous.rgb, normal * 0.5 + 0.5, coverage);
                    }
                    else
                    {
                        previous.r += (1 - previous.r) * coverage;
                    }
                }
                return previous;
            }
            ENDCG
        }
    }
}

Shader "Custom/LightingPass"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _AlbedoRT;
            sampler2D _NormalRT;
            sampler2D _SpecRT;

            float3 _LightDir; // already in view space
            float3 _LightColor;
            float3 _ViewDir; // (0, 0, 1) in view space

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Fetch GBuffer data (all in view space)
                float3 normalVS = normalize(tex2D(_NormalRT, i.uv).rgb * 2 - 1);
                float3 albedo = tex2D(_AlbedoRT, i.uv).rgb;
                float2 spec = tex2D(_SpecRT, i.uv).rg;

                // Lighting in view space
                float3 L = normalize(_LightDir);
                float3 V = normalize(_ViewDir);
                float3 H = normalize(L + V);

                float NdotL = saturate(dot(normalVS, L));
                float NdotH = saturate(dot(normalVS, H));

                float3 diffuse = albedo * _LightColor * NdotL;
                float3 specular = _LightColor * pow(NdotH, lerp(4, 64, spec.y)) * spec.x;

                return float4(diffuse + specular, 1);
            }
            ENDCG
        }
    }
}

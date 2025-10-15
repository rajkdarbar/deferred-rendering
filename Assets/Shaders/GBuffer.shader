Shader "Custom/GBuffer"
{
    Properties
    {
        _AlbedoTex ("Albedo Texture", 2D) = "white" {}
        _Albedo ("Albedo Tint", Color) = (1, 1, 1, 1)
        _NormalTex ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            ZWrite On // store depth to the depth buffer
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _AlbedoTex;
            float4 _Albedo;
            sampler2D _NormalTex;
            float _Metallic;
            float _Smoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewNormal : TEXCOORD1; // view - space normal
                float3 viewPos : TEXCOORD2; // view - space position
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Compute view - space normal (used for deferred lighting)
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 vNormal = mul((float3x3)UNITY_MATRIX_IT_MV, worldNormal);
                o.viewNormal = normalize(vNormal);

                // Compute view - space position (before projection)
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewPos = mul(UNITY_MATRIX_V, worldPos).xyz;

                return o;
            }

            struct MRTOut
            {
                float4 albedo : SV_Target0;
                float4 normal : SV_Target1;
                float4 spec : SV_Target2;
                float4 viewPos : SV_Target3;
                float4 viewZ : SV_Target4;
            };

            MRTOut frag(v2f i)
            {
                MRTOut o;

                // Albedo
                float4 texCol = tex2D(_AlbedoTex, i.uv);
                o.albedo = texCol * _Albedo;

                // Normal (view - space, encoded [0, 1])
                float3 n = normalize(i.viewNormal);
                o.normal = float4(n * 0.5 + 0.5, 1);

                // Specular (metallic + smoothness)
                o.spec = float4(_Metallic, _Smoothness, 0, 1);

                // Full view - space position (XYZ)
                o.viewPos = float4(i.viewPos, 1);

                // Linear Z (view space forward is - Z)
                float linearZ = - i.viewPos.z;
                o.viewZ = float4(linearZ, linearZ, linearZ, 1);

                return o;
            }

            ENDCG
        }
    }
}

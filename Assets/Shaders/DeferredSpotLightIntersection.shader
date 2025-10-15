
Shader "Custom/DeferredSpotLightIntersection"
{
    SubShader
    {
        Pass
        {
            Cull Front
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4x4 _ConeViewMatrix; // cone localToView matrix (from C#)
            float _Epsilon; // small offset to prevent artifacts

            sampler2D _ViewZRT;
            float4x4 _CameraInvProjection;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 conePosVS : TEXCOORD0; // cone vertex in view - space
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);

                float4 viewPos = mul(_ConeViewMatrix, v.vertex);
                o.conePosVS = viewPos.xyz;

                o.uv = ComputeScreenPos(o.pos).xy / o.pos.w; // screen - space uv in [0, 1]

                return o;
            }

            float3 ReconstructViewPos(float2 uv, float viewZ)
            {
                // Convert UV -> clip space (- 1..1)
                float4 clip = float4(uv * 2 - 1, 1, 1);

                // Unproject to view space direction
                float4 viewDir = mul(_CameraInvProjection, clip);
                viewDir /= viewDir.w;

                // Scale by actual view - space depth (negative forward)
                return normalize(viewDir.xyz) * viewZ;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv; // screen - space UVs
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif

                float viewZ = tex2D(_ViewZRT, uv).r;
                float3 sceneVS = ReconstructViewPos(uv, viewZ);

                // Compare depths in view - space (Z is negative forward)
                float coneZ = i.conePosVS.z;
                float sceneZ = sceneVS.z;

                if (coneZ > sceneZ + _Epsilon)
                discard;

                // Intersection area in white color
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}

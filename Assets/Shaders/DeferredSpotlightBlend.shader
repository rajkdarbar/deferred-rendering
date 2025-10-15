Shader "Custom/DeferredSpotlightBlend"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; // base lighting (from LightingPass)
            sampler2D _SpotMask; // mask for spotlight influence
            sampler2D _NormalRT; // view - space normals (encoded [0, 1])
            sampler2D _AlbedoRT; // albedo
            sampler2D _SpecularRT; // specular params
            sampler2D _ViewZRT; // view - space Z

            float4x4 _CameraInvProjection;

            float4 _LightColor;
            float3 _LightPosVS;
            float3 _LightDirVS;
            float _LightRange;
            float _SpotAngleCos;

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
                float2 uv = i.uv;

                float3 baseColor = tex2D(_MainTex, uv).rgb;
                float mask = tex2D(_SpotMask, uv).r;

                if (mask < 0.001) return float4(baseColor, 1);

                float3 albedo = tex2D(_AlbedoRT, uv).rgb;
                float3 normalVS = normalize(tex2D(_NormalRT, uv).rgb * 2 - 1);
                normalVS = normalize(normalVS);
                float3 specData = tex2D(_SpecularRT, uv).rgb;

                float viewZ = tex2D(_ViewZRT, uv).r;
                float3 posVS = ReconstructViewPos(uv, viewZ);

                // Compute light vector
                float3 Lp = _LightPosVS - posVS;
                float dist = length(Lp);
                float3 L = normalize(Lp);

                // Spotlight cone check (inside cone & within range)
                float coneFactor = dot(- L, normalize(_LightDirVS)); // L negated

                // View and half vectors in view - space
                float3 V = normalize(- posVS);
                float3 H = normalize(L + V); // camera is at (0, 0, 0)

                // Diffuse shading
                float diff = saturate(dot(normalVS, L));
                float3 diffColor = albedo * _LightColor.rgb * diff;

                // Specular shading
                float metallic = specData.r;
                float smoothness = specData.g;
                float shininess = lerp(8.0, 128.0, smoothness); // smoother surface → sharper, tighter specular
                float NdotH = saturate(dot(normalVS, H));
                float spec = pow(NdotH, shininess) * metallic;
                float3 specColor = _LightColor.rgb * spec;

                // Distance attenuation (smooth inverse - square)
                float distAtten = saturate(1.0 / (1.0 + ((dist * dist) / (_LightRange * _LightRange))));

                // Spotlight cone attenuation
                float spotAtten = smoothstep(_SpotAngleCos, _SpotAngleCos * 1.1, coneFactor);

                // Final attenuation
                float atten = distAtten * spotAtten * mask;

                // Spotlight contribution
                float3 light = (diffColor + specColor) * atten;

                // Additive blending adds it to base
                float3 finalColor = baseColor + light;
                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
}

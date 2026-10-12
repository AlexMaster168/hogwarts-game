// Непрозрачный материал с мировой триплонарной текстурой шума — не тянется на скейленных примитивах.
Shader "HM/Triplanar"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Noise", 2D) = "white" {}
        _Scale ("World Scale", Float) = 0.3
        _TexStrength ("Tex Strength", Range(0,1)) = 0.45
        _Glossiness ("Smoothness", Range(0,1)) = 0.1
        _EmissionColor ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        fixed4 _Color;
        half _Scale;
        half _TexStrength;
        half _Glossiness;
        fixed4 _EmissionColor;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = abs(IN.worldNormal);
            n = n / (n.x + n.y + n.z + 1e-5);
            float3 p = IN.worldPos * _Scale;
            half tx = tex2D(_MainTex, p.zy).r;
            half ty = tex2D(_MainTex, p.xz).r;
            half tz = tex2D(_MainTex, p.xy).r;
            half t = tx * n.x + ty * n.y + tz * n.z;
            half shade = lerp(1.0, 0.65 + 0.7 * t, _TexStrength);
            o.Albedo = _Color.rgb * shade;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

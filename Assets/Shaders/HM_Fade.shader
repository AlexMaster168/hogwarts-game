// Полупрозрачный материал с ободком (вода, щит Протего, круги-предупреждения).
Shader "HM/Fade"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.5)
        _Rim ("Rim", Range(0,4)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _EmissionColor ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        fixed4 _Color;
        half _Rim;
        half _Glossiness;
        fixed4 _EmissionColor;

        struct Input
        {
            float3 viewDir;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half rim = 1 - saturate(abs(dot(normalize(IN.viewDir), normalize(IN.worldNormal))));
            half r2 = rim * rim;
            o.Albedo = _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb + _Color.rgb * r2 * _Rim;
            o.Alpha = saturate(_Color.a + r2 * _Rim * 0.5);
        }
        ENDCG
    }
}

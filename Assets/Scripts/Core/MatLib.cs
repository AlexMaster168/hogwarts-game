using System.Collections.Generic;
using UnityEngine;

// Материалы и процедурные текстуры. Шаблоны лежат в Resources (так шейдеры гарантированно попадут в билд),
// если их нет — откатываемся на Shader.Find.
public static class MatLib
{
    static Material triT, addT, fadeT, skyT;
    static readonly Dictionary<string, Material> cache = new();
    static Texture2D noise, dot;

    public static Texture2D Noise { get { if (noise == null) noise = MakeNoise(256); return noise; } }
    public static Texture2D Dot { get { if (dot == null) dot = MakeDot(64); return dot; } }

    static Material Template(ref Material field, string res, string shaderName)
    {
        if (field != null) return field;
        field = Resources.Load<Material>(res);
        if (field == null)
        {
            var sh = Shader.Find(shaderName);
            if (sh == null) sh = Shader.Find("Standard");
            field = new Material(sh);
        }
        return field;
    }

    public static Material Solid(Color c, float smooth = 0.1f, float scale = 0.3f, float strength = 0.45f)
    {
        string key = "S" + c + smooth + "_" + scale + "_" + strength;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Template(ref triT, "HM_Tri", "HM/Triplanar"));
        m.SetColor("_Color", c);
        m.SetTexture("_MainTex", Noise);
        m.SetFloat("_Scale", scale);
        m.SetFloat("_TexStrength", strength);
        m.SetFloat("_Glossiness", smooth);
        m.SetColor("_EmissionColor", Color.black);
        m.enableInstancing = true;
        cache[key] = m;
        return m;
    }

    public static Material Glow(Color c, float intensity = 2f)
    {
        string key = "G" + c + intensity;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Template(ref triT, "HM_Tri", "HM/Triplanar"));
        m.SetColor("_Color", c * 0.4f);
        m.SetTexture("_MainTex", Noise);
        m.SetFloat("_Scale", 1f);
        m.SetFloat("_TexStrength", 0.1f);
        m.SetFloat("_Glossiness", 0.5f);
        m.SetColor("_EmissionColor", c * intensity);
        m.EnableKeyword("_EMISSION");
        m.enableInstancing = true;
        cache[key] = m;
        return m;
    }

    public static Material Additive(Color c)
    {
        string key = "A" + c;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Template(ref addT, "HM_Add", "HM/Additive"));
        m.SetColor("_Color", c);
        m.SetTexture("_MainTex", Dot);
        cache[key] = m;
        return m;
    }

    public static Material Fade(Color c, float rim = 1f, float smooth = 0.8f)
    {
        string key = "F" + c + rim + "_" + smooth;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        m = new Material(Template(ref fadeT, "HM_Fade", "HM/Fade"));
        m.SetColor("_Color", c);
        m.SetFloat("_Rim", rim);
        m.SetFloat("_Glossiness", smooth);
        cache[key] = m;
        return m;
    }

    public static Material Sky()
    {
        var m = new Material(Template(ref skyT, "HM_Sky", "Skybox/Procedural"));
        if (m.HasProperty("_SkyTint")) m.SetColor("_SkyTint", new Color(0.45f, 0.55f, 0.7f));
        if (m.HasProperty("_GroundColor")) m.SetColor("_GroundColor", new Color(0.35f, 0.38f, 0.33f));
        if (m.HasProperty("_AtmosphereThickness")) m.SetFloat("_AtmosphereThickness", 0.85f);
        if (m.HasProperty("_Exposure")) m.SetFloat("_Exposure", 1.25f);
        if (m.HasProperty("_SunSize")) m.SetFloat("_SunSize", 0.035f);
        return m;
    }

    // Тайлящийся value-noise из нескольких октав.
    static Texture2D MakeNoise(int n)
    {
        var rng = new System.Random(42);
        var px = new Color32[n * n];
        var acc = new float[n * n];
        float amp = 0.5f, total = 0f;
        for (int g = 4; g <= 64; g *= 2)
        {
            var grid = new float[g, g];
            for (int y = 0; y < g; y++)
                for (int x = 0; x < g; x++)
                    grid[x, y] = (float)rng.NextDouble();
            for (int y = 0; y < n; y++)
            {
                float fy = (float)y / n * g;
                int y0 = Mathf.FloorToInt(fy) % g, y1 = (y0 + 1) % g;
                float ty = Mathf.SmoothStep(0, 1, fy - Mathf.Floor(fy));
                for (int x = 0; x < n; x++)
                {
                    float fx = (float)x / n * g;
                    int x0 = Mathf.FloorToInt(fx) % g, x1 = (x0 + 1) % g;
                    float tx = Mathf.SmoothStep(0, 1, fx - Mathf.Floor(fx));
                    float a = Mathf.Lerp(grid[x0, y0], grid[x1, y0], tx);
                    float b = Mathf.Lerp(grid[x0, y1], grid[x1, y1], tx);
                    acc[y * n + x] += Mathf.Lerp(a, b, ty) * amp;
                }
            }
            total += amp;
            amp *= 0.55f;
        }
        for (int i = 0; i < acc.Length; i++)
        {
            byte v = (byte)Mathf.Clamp(acc[i] / total * 255f, 0, 255);
            px[i] = new Color32(v, v, v, 255);
        }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, name = "HM_Noise" };
        t.SetPixels32(px);
        t.Apply(true);
        return t;
    }

    static Texture2D MakeDot(int n)
    {
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                float r = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(r * r * 255));
            }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "HM_Dot" };
        t.SetPixels32(px);
        t.Apply();
        return t;
    }
}

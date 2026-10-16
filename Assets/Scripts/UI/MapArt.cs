using UnityEngine;

// Картинки для карты: снимок мира сверху (днём, без тумана) и стрелка «Вы здесь».
public static class MapArt
{
    // Область снимка совпадает с вкладкой «Окрестности».
    public static readonly Rect World = new Rect(-300, -250, 600, 600);
    static Texture2D aerial, arrow, ring;

    public static Texture2D Aerial
    {
        get { if (aerial == null) aerial = RenderAerial(2048); return aerial; }
    }

    public static void Prepare() { _ = Aerial; }

    static Texture2D RenderAerial(int size)
    {
        var go = new GameObject("MapCam");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = World.height / 2f;
        cam.aspect = 1f;
        cam.transform.SetPositionAndRotation(new Vector3(World.center.x, 260f, World.center.y), Quaternion.Euler(90, 0, 0));
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 600f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.22f, 0.32f, 0.18f);
        cam.cullingMask = ~(1 << 30);
        cam.enabled = false;
        var rt = new RenderTexture(size, size, 24) { antiAliasing = 4 };
        cam.targetTexture = rt;
        // Днём и без тумана, даже если сейчас ночь: ровный свет сверху.
        bool fog = RenderSettings.fog;
        var ambMode = RenderSettings.ambientMode;
        var amb = RenderSettings.ambientLight;
        float ambI = RenderSettings.ambientIntensity;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.6f);
        var sunGo = new GameObject("MapSun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.05f;
        sun.color = new Color(1f, 0.97f, 0.9f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.55f;
        sunGo.transform.rotation = Quaternion.Euler(62, 35, 0);
        float shadowDist = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance = 700f;
        cam.Render();
        QualitySettings.shadowDistance = shadowDist;
        RenderSettings.fog = fog;
        RenderSettings.ambientMode = ambMode;
        RenderSettings.ambientLight = amb;
        RenderSettings.ambientIntensity = ambI;
        Object.Destroy(sunGo);
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        RenderTexture.active = null;
        // Чуть насыщеннее и теплее — карта, а не скриншот.
        var px = tex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
            float l = (r + g + b) / 3f;
            r = Mathf.Clamp01(l + (r - l) * 1.25f + 0.025f);
            g = Mathf.Clamp01(l + (g - l) * 1.25f + 0.01f);
            b = Mathf.Clamp01(l + (b - l) * 1.25f - 0.015f);
            px[i] = new Color(r, g, b);
        }
        tex.SetPixels32(px);
        tex.Apply(true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        Object.Destroy(cam.gameObject);
        rt.Release();
        Object.Destroy(rt);
        return tex;
    }

    // Рисует часть снимка, попадающую в окно world, в прямоугольник area.
    public static void DrawAerial(Rect area, Rect world)
    {
        // За краем снимка — тёмная хвоя, сам снимок только в своих границах.
        GUI.color = new Color(0.17f, 0.24f, 0.13f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var w = Rect.MinMaxRect(Mathf.Max(world.xMin, World.xMin), Mathf.Max(world.yMin, World.yMin), Mathf.Min(world.xMax, World.xMax), Mathf.Min(world.yMax, World.yMax));
        if (w.width <= 0 || w.height <= 0) return;
        var a = CastleMap.Project(new Vector3(w.xMin, 0, w.yMax), area, world);
        var b = CastleMap.Project(new Vector3(w.xMax, 0, w.yMin), area, world);
        var uv = new Rect((w.xMin - World.xMin) / World.width, (w.yMin - World.yMin) / World.height, w.width / World.width, w.height / World.height);
        GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(a.x, a.y, b.x, b.y), Aerial, uv);
    }

    public static Texture2D Arrow
    {
        get
        {
            if (arrow != null) return arrow;
            const int n = 64;
            arrow = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Наконечник стрелы вверх: треугольник с выемкой снизу.
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n;
                    float half = (1f - v) * 0.62f;
                    bool inTri = v > 0.06f && v < 0.98f && Mathf.Abs(u) < half;
                    bool notch = v < 0.36f && Mathf.Abs(u) < (0.36f - v) * 1.2f;
                    bool inside = inTri && !notch;
                    bool border = !inside && (InTri(u, v, 0.08f));
                    px[y * n + x] = inside ? new Color32(255, 255, 255, 255) : border ? new Color32(0, 0, 0, 230) : new Color32(0, 0, 0, 0);
                }
            arrow.SetPixels32(px);
            arrow.Apply();
            return arrow;
        }
    }

    static bool InTri(float u, float v, float pad)
    {
        float half = (1f - v) * 0.62f + pad;
        bool inTri = v > 0.06f - pad && v < 0.98f + pad * 0.5f && Mathf.Abs(u) < half;
        bool notch = v < 0.36f - pad && Mathf.Abs(u) < (0.36f - pad - v) * 1.2f;
        return inTri && !notch;
    }

    public static Texture2D Ring
    {
        get
        {
            if (ring != null) return ring;
            const int n = 64;
            ring = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
                    float a = d < 1f ? Mathf.Clamp01((1f - d) * 6f) * Mathf.Clamp01((d - 0.78f) * 8f) + (d < 0.8f ? 0.18f : 0f) : 0f;
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            ring.SetPixels32(px);
            ring.Apply();
            return ring;
        }
    }

    // Метка героя: пульсирующее кольцо, стрелка по направлению взгляда и подпись.
    public static void PlayerMarker(Vector2 at, float yaw, float scale, bool label, GUIStyle style)
    {
        float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.2f);
        float rr = 15f * scale * pulse;
        var col = new Color(0.35f, 0.95f, 1f);
        GUI.color = new Color(col.r, col.g, col.b, 0.9f);
        GUI.DrawTexture(new Rect(at.x - rr, at.y - rr, rr * 2, rr * 2), Ring);
        GUI.color = Color.white;
        var m = GUI.matrix;
        GUIUtility.RotateAroundPivot(yaw, at);
        float s = 13f * scale;
        GUI.color = new Color(1f, 0.92f, 0.4f);
        GUI.DrawTexture(new Rect(at.x - s, at.y - s * 1.1f, s * 2, s * 2), Arrow);
        GUI.color = Color.white;
        GUI.matrix = m;
        if (!label) return;
        var c = new GUIContent("Вы здесь");
        var sz = style.CalcSize(c);
        var r = new Rect(at.x - sz.x / 2 - 4, at.y + rr + 2, sz.x + 8, sz.y);
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var prev = style.normal.textColor;
        style.normal.textColor = col;
        GUI.Label(r, c, style);
        style.normal.textColor = prev;
    }
}

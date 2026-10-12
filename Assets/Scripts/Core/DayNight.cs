using System.Collections.Generic;
using UnityEngine;

// Смена дня и ночи, погода и атмосферные частицы.
// Сутки = 12 минут реального времени. Ночью загораются фонари, видны звёзды, в лесу летают светлячки,
// у озера появляются дементоры (см. NightSpawns). Время можно промотать на T вне боя.
public class DayNight : MonoBehaviour
{
    public static DayNight I;
    public const float DaySeconds = 720f;

    public float hour = 9f;
    public int day = 1;
    public bool raining;
    public bool frozen;
    public float Daylight { get; private set; } = 1f;
    public bool IsNight => hour >= 20.5f || hour < 5f;

    Light sun, moon;
    Material sky, starMat, rainMat, water;
    Transform stars;
    ParticleSystem rain, fireflies;
    readonly List<ParticleSystem> dust = new();
    readonly List<(Light light, Renderer bulb)> lamps = new();
    Material bulbOn, bulbOff;
    float nextWeatherRoll = 3f, rainLeft, lightningT = 6f, rainBlend;

    static readonly Color DaySky = new(0.55f, 0.62f, 0.75f), DayEq = new(0.45f, 0.45f, 0.42f), DayGround = new(0.22f, 0.2f, 0.18f);
    static readonly Color NightSky = new(0.11f, 0.14f, 0.25f), NightEq = new(0.08f, 0.08f, 0.12f), NightGround = new(0.04f, 0.04f, 0.05f);
    static readonly Color DayFog = new(0.62f, 0.68f, 0.76f), DuskFog = new(0.75f, 0.52f, 0.4f), NightFog = new(0.05f, 0.06f, 0.11f), RainFog = new(0.42f, 0.45f, 0.5f);

    void Awake()
    {
        I = this;
        frozen = SmokeTest.Requested;
        sun = RenderSettings.sun;
        sky = RenderSettings.skybox;
        // Тот же экземпляр, что у озера и реки (MatLib кэширует по параметрам).
        water = MatLib.Fade(new Color(0.12f, 0.3f, 0.42f, 0.78f), 0.5f, 0.95f);
        var mg = new GameObject("Moon");
        moon = mg.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.55f, 0.65f, 1f);
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = 0.6f;
        moon.transform.rotation = Quaternion.Euler(50f, 150f, 0f);
        moon.intensity = 0f;
        BuildStars();
        BuildRain();
        BuildFireflies();
        BuildDust();
        BuildLamps();
        Apply();
    }

    // ---------------- построение ----------------
    void BuildStars()
    {
        var rng = new System.Random(7);
        int n = 700;
        var v = new Vector3[n * 4];
        var uv = new Vector2[n * 4];
        var col = new Color[n * 4];
        var tri = new int[n * 6];
        for (int i = 0; i < n; i++)
        {
            // Равномерно по верхней полусфере.
            float y = (float)rng.NextDouble() * 0.92f + 0.08f, a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(1f - y * y);
            var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            var c = dir * 380f;
            var right = Vector3.Cross(dir, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            var up = Vector3.Cross(right, dir);
            float s = 0.6f + (float)rng.NextDouble() * (rng.NextDouble() < 0.08 ? 2.4f : 1.1f);
            v[i * 4] = c + (-right - up) * s; v[i * 4 + 1] = c + (right - up) * s;
            v[i * 4 + 2] = c + (right + up) * s; v[i * 4 + 3] = c + (-right + up) * s;
            uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(1, 0); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(0, 1);
            float warm = (float)rng.NextDouble();
            var sc = Color.Lerp(new Color(0.7f, 0.8f, 1f), new Color(1f, 0.9f, 0.75f), warm);
            sc.a = 0.5f + (float)rng.NextDouble() * 0.5f;
            for (int k = 0; k < 4; k++) col[i * 4 + k] = sc;
            tri[i * 6] = i * 4; tri[i * 6 + 1] = i * 4 + 1; tri[i * 6 + 2] = i * 4 + 2;
            tri[i * 6 + 3] = i * 4; tri[i * 6 + 4] = i * 4 + 2; tri[i * 6 + 5] = i * 4 + 3;
        }
        var mesh = new Mesh { name = "Stars", vertices = v, uv = uv, colors = col, triangles = tri };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 900f);
        var go = new GameObject("Stars");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        starMat = new Material(MatLib.Additive(Color.white));
        mr.sharedMaterial = starMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        stars = go.transform;
    }

    void BuildRain()
    {
        var go = new GameObject("Rain");
        rain = go.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = rain.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 28f;
        main.startSize = 0.09f;
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.9f);
        var em = rain.emission;
        em.rateOverTime = 3200f;
        // Бокс-эмиттер стреляет вдоль своей оси Z — поворачиваем её вниз.
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sh = rain.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(60f, 60f, 1f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        rainMat = MatLib.Additive(new Color(0.75f, 0.85f, 1f, 0.6f));
        r.sharedMaterial = rainMat;
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.05f;
        r.lengthScale = 1f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    ParticleSystem Motes(string name, Vector3 pos, Vector3 box, Color c, float rate, float size, float life)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = 0.15f;
        main.startSize = size;
        main.startColor = c;
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = rate;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = box;
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.25f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
        fade.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = MatLib.Additive(Color.white);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    void BuildFireflies()
    {
        fireflies = Motes("Fireflies", WorldBuilder.ForestCenter + Vector3.up * 2f, new Vector3(140f, 4f, 140f), new Color(0.85f, 1f, 0.4f), 110f, 0.38f, 6f);
    }

    // Золотистая пыль в лучах света внутри помещений замка.
    void BuildDust()
    {
        foreach (var l in CastleLocations.All)
        {
            float y = l.level switch { -1 => -4.5f, 1 => 9f, 2 => 26f, _ => 3f };
            dust.Add(Motes("Dust " + l.name, new Vector3(l.area.center.x, y, l.area.center.y), new Vector3(l.area.width * 0.8f, 3f, l.area.height * 0.8f),
                new Color(1f, 0.85f, 0.55f, 0.45f), Mathf.Clamp(l.area.width * l.area.height / 60f, 4f, 16f), 0.06f, 7f));
        }
    }

    // Фонари на мосту, во дворе и в Хогсмиде: горят ночью и в дождь.
    void BuildLamps()
    {
        bulbOn = MatLib.Glow(new Color(1f, 0.75f, 0.4f), 3.5f);
        bulbOff = MatLib.Solid(new Color(0.35f, 0.32f, 0.26f), 0.4f);
        var pole = MatLib.Solid(new Color(0.12f, 0.12f, 0.13f), 0.3f);
        var root = new GameObject("Lamps").transform;
        var spots = new List<Vector3>();
        for (float z = -96; z <= -30; z += 11) { spots.Add(new Vector3(-4.6f, 0, z)); spots.Add(new Vector3(4.6f, 0, z)); }
        foreach (var p in new[] { new Vector3(-8, 0, -20), new Vector3(8, 0, -20), new Vector3(-20, 0, 0), new Vector3(20, 0, 0), new Vector3(-8, 0, 20), new Vector3(8, 0, 20) })
            spots.Add(p);
        for (int i = 0; i < 8; i++) spots.Add(WorldBuilder.HogsmeadeCenter + new Vector3(-40 + i * 11.5f, 0, (i % 2 == 0 ? 7f : -7f)));
        foreach (var p in spots)
        {
            Prims.Cyl(root, p, 0.09f, 3.6f, pole);
            Prims.Box(root, p + Vector3.up * 3.65f, new Vector3(0.5f, 0.12f, 0.5f), pole, false);
            var bulb = Prims.Sphere(root, p + Vector3.up * 3.35f, 0.42f, bulbOff);
            var lg = new GameObject("LampLight");
            lg.transform.SetParent(root, false);
            lg.transform.position = p + Vector3.up * 3.2f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.7f, 0.4f);
            l.range = 11f;
            l.intensity = 1.6f;
            l.enabled = false;
            lamps.Add((l, bulb.GetComponent<Renderer>()));
        }
    }

    // ---------------- обновление ----------------
    void Update()
    {
        if (!frozen && GameState.Playing)
        {
            hour += Time.deltaTime * 24f / DaySeconds;
            if (hour >= 24f) { hour -= 24f; day++; }
            UpdateWeather(Time.deltaTime);
        }
        if (GameState.Playing && Input.GetKeyDown(KeyCode.T)) SkipTime();
        Apply();
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        stars.position = cam.transform.position;
        var cp = cam.transform.position;
        rain.transform.position = new Vector3(cp.x, cp.y + 16f, cp.z) + cam.transform.forward * 8f;
    }

    void UpdateWeather(float dt)
    {
        if (raining)
        {
            rainLeft -= dt;
            if (rainLeft <= 0f) SetRain(false);
            lightningT -= dt;
            if (lightningT <= 0f)
            {
                lightningT = Random.Range(7f, 18f);
                var cam = Camera.main;
                if (cam != null && !Indoors(cam.transform.position))
                {
                    var p = cam.transform.position + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * 120f + Vector3.up * 90f;
                    FX.Flash(p, new Color(0.75f, 0.8f, 1f), 8f, 600f, 0.18f);
                    SFX.Play("boom", 0.35f);
                }
            }
        }
        else
        {
            nextWeatherRoll -= dt;
            if (nextWeatherRoll <= 0f)
            {
                nextWeatherRoll = DaySeconds / 6f;
                if (Random.value < 0.3f) SetRain(true, Random.Range(80f, 160f));
            }
        }
    }

    public void SetRain(bool on, float seconds = 120f)
    {
        raining = on;
        rainLeft = seconds;
        if (on) HUD.ShowToast("Начинается дождь", new Color(0.7f, 0.8f, 1f));
    }

    public void SetHour(float h)
    {
        hour = Mathf.Repeat(h, 24f);
        Apply();
    }

    public void SkipTime()
    {
        if (RoomServices.Threatened) { HUD.ShowToast("Нельзя ждать во время боя"); return; }
        float target = IsNight ? 7f : 21f;
        if (target < hour) day++;
        hour = target;
        HUD.ShowToast(IsNight ? "Наступила ночь…" : "Наступило утро", new Color(0.85f, 0.85f, 1f));
        Apply();
    }

    public static bool Indoors(Vector3 p) => p.y < -2f || CastleLocations.Current(p) != "Окрестности Хогвартса";

    void Apply()
    {
        // Высота солнца: 0° в 6:00 и 18:00, 62° в полдень.
        float elev = Mathf.Sin((hour - 6f) / 12f * Mathf.PI) * 62f;
        float yaw = -35f + (hour - 12f) * 15f;
        float d = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-6f, 14f, elev));
        Daylight = d;
        rainBlend = Mathf.MoveTowards(rainBlend, raining ? 1f : 0f, Time.unscaledDeltaTime * 0.25f);
        float dusk = Mathf.Clamp01(1f - Mathf.Abs(elev - 4f) / 14f) * (elev > -8f ? 1f : 0f);

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, -20f), yaw, 0f);
            sun.intensity = 1.25f * d * Mathf.Lerp(1f, 0.45f, rainBlend);
            sun.color = Color.Lerp(new Color(1f, 0.92f, 0.8f), new Color(1f, 0.55f, 0.3f), dusk);
            sun.shadows = d > 0.05f ? LightShadows.Soft : LightShadows.None;
        }
        moon.intensity = 0.38f * (1f - d) * Mathf.Lerp(1f, 0.5f, rainBlend);
        moon.shadows = d < 0.05f ? LightShadows.Soft : LightShadows.None;
        moon.enabled = moon.intensity > 0.01f;

        float amb = Mathf.Lerp(1f, 0.7f, rainBlend);
        RenderSettings.ambientSkyColor = Color.Lerp(NightSky, DaySky, d) * amb;
        RenderSettings.ambientEquatorColor = Color.Lerp(NightEq, DayEq, d) * amb;
        RenderSettings.ambientGroundColor = Color.Lerp(NightGround, DayGround, d) * amb;
        var fog = Color.Lerp(Color.Lerp(NightFog, DayFog, d), DuskFog, dusk * 0.6f);
        RenderSettings.fogColor = Color.Lerp(fog, RainFog * Mathf.Lerp(0.25f, 1f, d), rainBlend);
        RenderSettings.fogDensity = Mathf.Lerp(0.0028f, 0.0045f, rainBlend) * Mathf.Lerp(0.8f, 1f, d);
        if (sky != null && sky.HasProperty("_SkyTint"))
            sky.SetColor("_SkyTint", Color.Lerp(new Color(0.45f, 0.55f, 0.7f), new Color(0.5f, 0.5f, 0.52f), rainBlend));
        if (sky != null && sky.HasProperty("_Exposure"))
            sky.SetFloat("_Exposure", Mathf.Lerp(0.18f, 1.25f, d) * Mathf.Lerp(1f, 0.55f, rainBlend));

        // Отражения запечены с дневного неба — ночью их гасим, иначе вода светится.
        RenderSettings.reflectionIntensity = Mathf.Lerp(0.12f, 1f, d);
        water.SetFloat("_Rim", Mathf.Lerp(0.08f, 0.5f, d));

        var cam = Camera.main;
        bool indoors = cam != null && Indoors(cam.transform.position);
        starMat.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * 1.4f) * (1f - rainBlend)));
        bool rainVisible = rainBlend > 0.3f && !indoors && GameState.Mode != GameMode.Title;
        if (rainVisible && !rain.isPlaying) rain.Play();
        else if (!rainVisible && rain.isPlaying) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        var ffe = fireflies.emission;
        ffe.enabled = d < 0.35f && !raining;
        bool lit = d < 0.45f || rainBlend > 0.6f;
        foreach (var (light, bulb) in lamps)
        {
            if (light.enabled == lit) continue;
            light.enabled = lit;
            bulb.sharedMaterial = lit ? bulbOn : bulbOff;
        }
        if (PostFX.Grading != null) PostFX.Grading.postExposure.value = Mathf.Lerp(0.75f, 0.45f, d);
    }

    public string ClockText()
    {
        int h = Mathf.FloorToInt(hour), m = Mathf.FloorToInt((hour - h) * 60f);
        string phase = hour < 5 ? "ночь" : hour < 11 ? "утро" : hour < 17 ? "день" : hour < 21 ? "вечер" : "ночь";
        return $"День {day} · {h:00}:{m:00} · {phase}" + (raining ? " · дождь" : "");
    }
}

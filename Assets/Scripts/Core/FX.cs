using UnityEngine;

// Одноразовые эффекты на частицах + вспышки света.
public static class FX
{
    static ParticleSystem MakePS(Vector3 pos, Quaternion rot, string name)
    {
        var go = new GameObject(name);
        go.transform.SetPositionAndRotation(pos, rot);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        var em = ps.emission;
        em.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = MatLib.Additive(Color.white);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.15f));
        return ps;
    }

    static void Setup(ParticleSystem ps, Color c, float speed, float size, float life, float gravity)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = c;
        main.gravityModifier = gravity;
    }

    public static void Burst(Vector3 pos, Color c, int count = 24, float speed = 6f, float size = 0.35f, float life = 0.5f, float gravity = 0.2f)
    {
        var ps = MakePS(pos, Quaternion.identity, "FX_Burst");
        Setup(ps, c, speed, size, life, gravity);
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.1f;
        ps.Play();
        ps.Emit(count);
        Object.Destroy(ps.gameObject, life + 0.5f);
    }

    public static void Cone(Vector3 pos, Vector3 dir, Color c, int count = 70, float speed = 16f, float angle = 22f)
    {
        var ps = MakePS(pos, Quaternion.LookRotation(dir), "FX_Cone");
        Setup(ps, c, speed, 0.9f, 0.55f, -0.3f);
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = angle;
        sh.radius = 0.15f;
        ps.Play();
        ps.Emit(count);
        Object.Destroy(ps.gameObject, 1.2f);
    }

    public static void Shockwave(Vector3 pos, Color c, float radius)
    {
        var ps = MakePS(pos + Vector3.up * 0.3f, Quaternion.Euler(90, 0, 0), "FX_Wave");
        Setup(ps, c, radius * 2.2f, 0.9f, 0.45f, 0f);
        var main = ps.main;
        main.startSpeed = radius * 2.2f;
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.3f;
        ps.Play();
        ps.Emit(Mathf.RoundToInt(30 + radius * 8));
        Object.Destroy(ps.gameObject, 1f);
        Burst(pos + Vector3.up * 0.5f, c, 25, 4f, 0.8f, 0.6f, -0.5f);
    }

    // Зацикленный огонь/свечение (жаровни, факелы).
    public static ParticleSystem Loop(Transform parent, Vector3 localPos, Color c, float rate = 25f, float size = 0.6f, float speed = 1.5f)
    {
        var ps = MakePS(Vector3.zero, Quaternion.identity, "FX_Loop");
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = localPos;
        ps.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        Setup(ps, c, speed, size, 1.0f, -0.1f);
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 12;
        sh.radius = 0.2f;
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = rate;
        ps.Play();
        return ps;
    }

    public static void Flash(Vector3 pos, Color c, float intensity = 4f, float range = 12f, float life = 0.3f)
    {
        var go = new GameObject("FX_Flash");
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        go.AddComponent<FadeLight>().life = life;
    }
}

public class FadeLight : MonoBehaviour
{
    public float life = 0.3f;
    Light l;
    float t, i0;

    void Start() { l = GetComponent<Light>(); i0 = l.intensity; }

    void Update()
    {
        t += Time.deltaTime;
        l.intensity = i0 * Mathf.Clamp01(1 - t / life);
        if (t >= life) Destroy(gameObject);
    }
}

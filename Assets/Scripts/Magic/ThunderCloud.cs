using UnityEngine;

// Грозовое зелье: туча над точкой, 8 секунд бьёт молниями ближайших врагов.
public class ThunderCloud : MonoBehaviour
{
    PlayerController owner;
    float life = 8f, strikeT = 0.3f;
    const float Radius = 10f;
    static readonly Color Bolt = new(0.65f, 0.6f, 1f);

    public static void Spawn(Vector3 at, PlayerController owner)
    {
        var go = new GameObject("ThunderCloud");
        go.transform.position = new Vector3(at.x, at.y + 9f, at.z);
        var cloud = MatLib.Solid(new Color(0.18f, 0.18f, 0.24f), 0.1f, 0.5f, 0.6f);
        for (int i = 0; i < 7; i++)
        {
            var off = Random.insideUnitSphere * 3.5f;
            off.y *= 0.3f;
            Prims.Make(PrimitiveType.Sphere, go.transform, off, new Vector3(4f, 2.2f, 4f) * Random.Range(0.8f, 1.3f), cloud, false);
        }
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Bolt;
        l.range = 25f;
        l.intensity = 0f;
        go.AddComponent<ThunderCloud>().owner = owner;
        SFX.Play("boom", 0.6f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        life -= dt;
        strikeT -= dt;
        var l = GetComponent<Light>();
        l.intensity = Mathf.MoveTowards(l.intensity, 0f, dt * 30f);
        transform.Rotate(0, 10f * dt, 0);
        if (life <= 0f)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.zero, dt * 2f);
            if (transform.localScale.x <= 0.01f) Destroy(gameObject);
            return;
        }
        if (strikeT > 0f) return;
        strikeT = 0.6f;

        Enemy best = null;
        float bd = float.MaxValue;
        var ground = transform.position - Vector3.up * 9f;
        foreach (var e in Enemy.All)
        {
            if (e == null || e.IsDead || e.Charmed) continue;
            float d = (e.transform.position - ground).sqrMagnitude;
            if (d < Radius * Radius && d < bd && Random.value < 0.85f) { bd = d; best = e; }
        }
        Vector3 hit = best != null ? best.targetable.AimPoint : ground + new Vector3(Random.Range(-Radius, Radius), 0, Random.Range(-Radius, Radius));
        DrawBolt(transform.position, hit);
        l.intensity = 8f;
        FX.Burst(hit, Bolt, 25, 7f, 0.5f, 0.4f);
        SFX.Play("boom", 0.35f);
        if (best != null)
            best.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = 25f * owner.inventory.DamageMult, point = hit, dir = Vector3.down, source = owner.gameObject });
    }

    static void DrawBolt(Vector3 a, Vector3 b)
    {
        var go = new GameObject("Lightning");
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = MatLib.Additive(Color.white);
        lr.startColor = lr.endColor = Bolt;
        lr.startWidth = 0.35f;
        lr.endWidth = 0.15f;
        int n = 9;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float t = i / (n - 1f);
            var p = Vector3.Lerp(a, b, t);
            if (i > 0 && i < n - 1) p += new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
            lr.SetPosition(i, p);
        }
        Destroy(go, 0.15f);
    }
}

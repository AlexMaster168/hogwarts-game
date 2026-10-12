using System.Collections.Generic;
using UnityEngine;

// Экспекто Патронум: серебристый олень мчится вперёд и сметает врагов. Дементоров изгоняет.
public class PatronusSpirit : MonoBehaviour
{
    public static readonly Color Silver = new(0.75f, 0.9f, 1f);
    Vector3 dir;
    float t, dmg;
    GameObject owner;
    readonly HashSet<Enemy> hit = new();
    Transform body;
    Transform[] legs;

    public static void Spawn(Vector3 from, Vector3 dir, float dmg, GameObject owner)
    {
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
        var go = new GameObject("Patronus");
        go.transform.position = new Vector3(from.x, from.y, from.z);
        go.transform.rotation = Quaternion.LookRotation(dir.normalized);
        var p = go.AddComponent<PatronusSpirit>();
        p.dir = dir.normalized;
        p.dmg = dmg;
        p.owner = owner;
        p.Build();
    }

    void Build()
    {
        var m = MatLib.Glow(Silver, 3.2f);
        body = Prims.Group(transform, "Stag");
        Prims.Make(PrimitiveType.Capsule, body, new Vector3(0, 1.25f, 0), new Vector3(0.6f, 0.75f, 0.6f), m, false, Quaternion.Euler(90, 0, 0));
        Prims.Make(PrimitiveType.Capsule, body, new Vector3(0, 1.75f, 0.75f), new Vector3(0.28f, 0.45f, 0.28f), m, false, Quaternion.Euler(35, 0, 0));
        Prims.Make(PrimitiveType.Capsule, body, new Vector3(0, 2.05f, 1.05f), new Vector3(0.26f, 0.22f, 0.4f), m, false, Quaternion.Euler(90, 0, 0));
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Box(body, new Vector3(s * 0.22f, 2.55f, 0.95f), new Vector3(0.05f, 0.7f, 0.05f), m, false, Quaternion.Euler(-15, 0, s * -25));
            Prims.Box(body, new Vector3(s * 0.42f, 2.75f, 0.95f), new Vector3(0.05f, 0.4f, 0.05f), m, false, Quaternion.Euler(0, 0, s * -60));
            Prims.Box(body, new Vector3(s * 0.3f, 2.7f, 1.1f), new Vector3(0.05f, 0.35f, 0.05f), m, false, Quaternion.Euler(-40, 0, s * -10));
        }
        legs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var pivot = Prims.Group(body, "Leg");
            pivot.localPosition = new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 1.0f, i < 2 ? 0.45f : -0.45f);
            Prims.Box(pivot, Vector3.down * 0.5f, new Vector3(0.1f, 1f, 0.1f), m, false);
            legs[i] = pivot;
        }
        var l = gameObject.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Silver;
        l.range = 14f;
        l.intensity = 3f;
        FX.Loop(transform, new Vector3(0, 1.2f, -0.4f), Silver, 60, 0.35f, 0.6f);
        FX.Flash(transform.position + Vector3.up, Silver, 6f, 16f, 0.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        transform.position += dir * 20f * dt;
        for (int i = 0; i < legs.Length; i++)
            legs[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 16f + (i == 0 || i == 3 ? 0 : Mathf.PI)) * 35f, 0, 0);
        body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(t * 8f)) * 0.25f;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || e.Charmed || hit.Contains(e)) continue;
            var d = e.transform.position - transform.position;
            d.y = 0;
            if (d.magnitude > 3.2f) continue;
            hit.Add(e);
            e.OnSpellHit(new SpellHit { type = SpellType.Patronus, damage = dmg, point = e.targetable.AimPoint, dir = dir, source = owner });
        }
        if (t > 1.8f)
        {
            FX.Burst(transform.position + Vector3.up, Silver, 60, 6f, 0.4f, 0.8f, -0.4f);
            Destroy(gameObject);
        }
    }
}

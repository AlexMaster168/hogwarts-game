using System.Collections.Generic;
using UnityEngine;

// Акцио-тир на тренировочном дворе: за минуту притянуть Акцио как можно больше шаров.
// Синий — 10 очков, золотой (быстрый) — 25, красный проклятый — минус 15.
public class AccioRange : MonoBehaviour
{
    public static AccioRange Active;
    public const float Duration = 60f;
    public static readonly Vector3 Stand = new(40, 0, 15);
    static readonly Vector3 Zone = new(60, 0, 0);
    public int score, pulled, cursed;
    public float timeLeft = Duration;
    float spawnT;
    public readonly List<AccioOrb> orbs = new();

    public string Status => $"Акцио-тир: {score} очков · {Mathf.CeilToInt(timeLeft)} с";

    void Awake()
    {
        Active = this;
        SFX.Play("quest", 0.5f);
        HUD.ShowToast("Акцио-тир! ЛКМ притягивает шар. Синий +10, золотой +25, красный −15. Минута пошла!", new Color(0.95f, 0.5f, 1f));
        for (int i = 0; i < 4; i++) Spawn();
    }

    void Spawn()
    {
        float r = Random.value;
        int kind = r < 0.2f ? 1 : r < 0.42f ? 2 : 0;
        var pos = Zone + new Vector3(Random.Range(-12f, 12f), Random.Range(1.5f, 6f), Random.Range(-16f, 16f));
        orbs.Add(AccioOrb.Spawn(this, kind, pos));
    }

    void Update()
    {
        if (!GameState.Playing) return;
        float dt = Time.deltaTime;
        timeLeft -= dt;
        orbs.RemoveAll(o => o == null);
        spawnT -= dt;
        if (spawnT <= 0f && orbs.Count < 7) { spawnT = 0.9f; Spawn(); }
        if (timeLeft <= 0f || (GameManager.I.player.transform.position - Stand).magnitude > 60f) Finish();
    }

    public void Collected(AccioOrb o)
    {
        int pts = o.kind == 1 ? 25 : o.kind == 2 ? -15 : 10;
        score = Mathf.Max(0, score + pts);
        if (pts > 0) pulled++; else cursed++;
        var at = GameManager.I.player.transform.position + Vector3.up * 2.4f;
        HUD.Float(at, (pts > 0 ? "+" : "") + pts, o.Color);
        SFX.Play(pts > 0 ? "pickup" : "hurt", 0.6f);
        if (pts < 0) HUD.ShowToast("Проклятый шар! −15", new Color(1f, 0.4f, 0.4f));
    }

    public void Abort()
    {
        Active = null;
        foreach (var o in orbs) if (o != null) Destroy(o.gameObject);
        Destroy(gameObject);
    }

    public void Finish()
    {
        if (Active != this) return;
        Active = null;
        foreach (var o in orbs) if (o != null) Destroy(o.gameObject);
        Activities.I.AccioFinished(score, pulled);
        Destroy(gameObject);
    }
}

public class AccioOrb : MonoBehaviour, ISpellTarget
{
    public int kind;
    AccioRange game;
    Vector3 basePos, drift;
    float life, phase, pullT = -1f;
    Vector3 pullFrom;
    public Color Color => kind == 1 ? new Color(1f, 0.8f, 0.2f) : kind == 2 ? new Color(1f, 0.15f, 0.15f) : new Color(0.35f, 0.65f, 1f);

    public static AccioOrb Spawn(AccioRange game, int kind, Vector3 pos)
    {
        var go = new GameObject("AccioOrb");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var o = go.AddComponent<AccioOrb>();
        o.game = game; o.kind = kind; o.basePos = pos; o.phase = Random.value * 10f;
        o.life = kind == 1 ? 5f : 8f;
        o.drift = Random.insideUnitSphere * (kind == 1 ? 4f : 1.6f);
        Prims.Sphere(go.transform, Vector3.zero, kind == 1 ? 0.55f : 0.75f, MatLib.Glow(o.Color, 3f));
        if (kind == 2) for (int i = 0; i < 4; i++) Prims.Cone(go.transform, Quaternion.Euler(0, i * 90, 60) * Vector3.up * 0.3f, 0.1f, 0.35f, MatLib.Glow(new Color(0.4f, 0f, 0f), 1f), false, Quaternion.Euler(0, i * 90, 60));
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        var h = go.AddComponent<Health>();
        h.Init(1f);
        h.invulnerable = true;
        var t = go.AddComponent<Targetable>();
        t.health = h;
        t.aimHeight = 0f;
        t.displayName = kind == 1 ? "Золотой шар (+25)" : kind == 2 ? "Проклятый шар (−15)" : "Шар (+10)";
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = o.Color; l.range = 4f; l.intensity = 1.5f;
        return o;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var p = GameManager.I.player;
        if (pullT >= 0f)
        {
            pullT += dt / 0.35f;
            transform.position = Vector3.Lerp(pullFrom, p.transform.position + Vector3.up * 1.3f, pullT);
            if (pullT >= 1f)
            {
                FX.Burst(transform.position, Color, 30, 4f, 0.3f, 0.5f);
                if (game != null) game.Collected(this);
                Destroy(gameObject);
            }
            return;
        }
        life -= dt;
        if (life <= 0f) { FX.Burst(transform.position, Color, 12, 2f, 0.25f, 0.4f); Destroy(gameObject); return; }
        float tt = Time.time + phase;
        float spd = kind == 1 ? 1.6f : 0.7f;
        transform.position = basePos + new Vector3(Mathf.Sin(tt * spd) * drift.x, Mathf.Sin(tt * spd * 1.3f) * 0.6f, Mathf.Cos(tt * spd * 0.8f) * drift.z);
    }

    public void OnSpellHit(SpellHit h)
    {
        if (pullT >= 0f) return;
        if (h.type == SpellType.Accio)
        {
            pullT = 0f;
            pullFrom = transform.position;
            GetComponent<Targetable>().enabled = false;
            return;
        }
        HUD.Float(transform.position + Vector3.up, "Только Акцио!", new Color(0.95f, 0.5f, 1f));
    }

    public void ForcePull() => OnSpellHit(new SpellHit { type = SpellType.Accio });
}

using System.Collections.Generic;
using UnityEngine;

// ---------------- Карточка волшебника ----------------
public class WizardCard : MonoBehaviour
{
    public int id;
    Vector3 basePos;

    public static WizardCard Spawn(int id, Vector3 pos)
    {
        var go = new GameObject("WizardCard " + id);
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var c = go.AddComponent<WizardCard>();
        c.id = id;
        c.basePos = pos;
        Prims.Box(go.transform, Vector3.zero, new Vector3(0.55f, 0.78f, 0.04f), MatLib.Glow(new Color(0.75f, 0.45f, 1f), 1.8f), false);
        Prims.Box(go.transform, Vector3.zero, new Vector3(0.42f, 0.62f, 0.05f), MatLib.Glow(new Color(1f, 0.85f, 0.45f), 1.4f), false);
        Prims.Sphere(go.transform, new Vector3(0, 0.08f, 0), 0.22f, MatLib.Solid(new Color(0.95f, 0.85f, 0.7f)));
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.8f, 0.55f, 1f);
        l.range = 5f;
        l.intensity = 1.6f;
        FX.Loop(go.transform, Vector3.zero, new Color(0.8f, 0.6f, 1f, 0.6f), 8, 0.12f, 0.4f);
        return c;
    }

    void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f + id) * 0.15f;
        transform.Rotate(0, 120 * Time.deltaTime, 0);
        var gm = GameManager.I;
        if (gm == null || gm.player == null || !GameState.Playing) return;
        if ((gm.player.transform.position + Vector3.up - transform.position).sqrMagnitude < 3.2f) Collect();
    }

    public void Collect()
    {
        FX.Burst(transform.position, new Color(0.85f, 0.6f, 1f), 40, 4f, 0.3f, 0.7f, -0.3f);
        Activities.I.CollectCard(id);
        Activities.I.cardObjs.Remove(this);
        Destroy(gameObject);
    }
}

// ---------------- Испытание Мерлина ----------------
// Каменный круг. Чётные — сбить 4 парящие сферы за 25 с, нечётные — зажечь 3 жаровни огнём за 30 с.
public class MerlinStone : MonoBehaviour
{
    public int index;
    public bool done, running;
    public readonly List<MerlinTarget> targets = new();
    float timeLeft;
    Renderer rune;
    Light glow;
    public bool Orbs => index % 2 == 0;
    public string Title => Orbs ? "сферы" : "жаровни";
    public string Status => running ? $"Испытание Мерлина: {(Orbs ? "сферы" : "жаровни")} {targets.FindAll(t => t.hit).Count}/{targets.Count} · {Mathf.CeilToInt(timeLeft)} с" : null;

    public static MerlinStone Spawn(int index, Vector3 pos)
    {
        var go = new GameObject("MerlinTrial " + index);
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var s = go.AddComponent<MerlinStone>();
        s.index = index;
        var stone = MatLib.Solid(new Color(0.5f, 0.5f, 0.48f), 0.1f, 0.3f, 0.6f);
        for (int i = 0; i < 7; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / 7f, 0) * Vector3.forward;
            float h = 1.6f + (i % 3) * 0.5f;
            Prims.Box(go.transform, d * 6.5f + Vector3.up * h * 0.5f, new Vector3(0.9f, h, 0.6f), stone, true, Quaternion.LookRotation(d));
        }
        Prims.Box(go.transform, Vector3.up * 0.9f, new Vector3(1.1f, 1.8f, 0.6f), stone);
        s.rune = Prims.Box(go.transform, new Vector3(0, 1.2f, -0.31f), new Vector3(0.6f, 0.6f, 0.02f), MatLib.Glow(new Color(0.4f, 1f, 0.7f), 2.5f), false).GetComponent<Renderer>();
        s.glow = go.AddComponent<Light>();
        s.glow.type = LightType.Point;
        s.glow.color = new Color(0.4f, 1f, 0.7f);
        s.glow.range = 8f;
        s.glow.intensity = 1.4f;
        return s;
    }

    public void SetDone(bool d)
    {
        done = d;
        if (rune != null) rune.sharedMaterial = d ? MatLib.Glow(new Color(1f, 0.85f, 0.4f), 2.5f) : MatLib.Glow(new Color(0.4f, 1f, 0.7f), 2.5f);
        if (glow != null) glow.color = d ? new Color(1f, 0.85f, 0.4f) : new Color(0.4f, 1f, 0.7f);
    }

    public void Begin()
    {
        if (done || running) return;
        running = true;
        timeLeft = Orbs ? 25f : 30f;
        int n = Orbs ? 4 : 3;
        for (int i = 0; i < n; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / n + 20f, 0) * Vector3.forward;
            targets.Add(MerlinTarget.Spawn(this, transform.position + d * 4.5f, Orbs, i));
        }
        SFX.Play("quest", 0.5f);
        HUD.ShowToast(Orbs ? "Собейте 4 сферы любыми заклинаниями!" : "Зажгите 3 жаровни огнём (Инсендио, Конфринго)!", new Color(0.5f, 1f, 0.75f));
    }

    void Update()
    {
        if (!running || !GameState.Playing) return;
        timeLeft -= Time.deltaTime;
        if (targets.TrueForAll(t => t.hit))
        {
            running = false;
            ClearTargets();
            SetDone(true);
            FX.Burst(transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.4f), 80, 7f, 0.5f, 1f, -0.4f);
            Activities.I.CompleteMerlin(index);
        }
        else if (timeLeft <= 0f || (GameManager.I.player.transform.position - transform.position).magnitude > 45f)
        {
            Abort();
            HUD.ShowToast("Испытание Мерлина провалено — попробуйте снова", new Color(1f, 0.6f, 0.5f));
        }
    }

    public void Abort()
    {
        running = false;
        ClearTargets();
    }

    void ClearTargets()
    {
        foreach (var t in targets) if (t != null) Destroy(t.gameObject);
        targets.Clear();
    }
}

public class MerlinTarget : MonoBehaviour, ISpellTarget
{
    public bool hit, orb;
    public Targetable target;
    MerlinStone owner;
    Vector3 center;
    float phase;
    Light glow;
    Renderer body;

    public static MerlinTarget Spawn(MerlinStone owner, Vector3 pos, bool orb, int i)
    {
        var go = new GameObject(orb ? "MerlinOrb" : "MerlinBrazier");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos + (orb ? Vector3.up * (2f + i * 0.6f) : Vector3.zero);
        var t = go.AddComponent<MerlinTarget>();
        t.owner = owner;
        t.orb = orb;
        t.center = owner.transform.position;
        t.phase = i * 1.3f;
        var h = go.AddComponent<Health>();
        h.Init(1);
        t.target = go.AddComponent<Targetable>();
        t.target.health = h;
        t.target.displayName = orb ? "Сфера Мерлина" : "Жаровня";
        t.target.aimHeight = orb ? 0f : 1.3f;
        if (orb)
        {
            t.body = Prims.Sphere(go.transform, Vector3.zero, 0.9f, MatLib.Glow(new Color(0.4f, 1f, 0.7f), 2.4f), true).GetComponent<Renderer>();
        }
        else
        {
            Prims.Cyl(go.transform, Vector3.zero, 0.55f, 1.1f, MatLib.Solid(new Color(0.3f, 0.28f, 0.27f), 0.2f));
            Prims.Cyl(go.transform, Vector3.up * 1.1f, 0.75f, 0.2f, MatLib.Solid(new Color(0.2f, 0.18f, 0.17f), 0.2f));
        }
        t.glow = go.AddComponent<Light>();
        t.glow.type = LightType.Point;
        t.glow.color = orb ? new Color(0.4f, 1f, 0.7f) : new Color(1f, 0.55f, 0.2f);
        t.glow.range = 6f;
        t.glow.intensity = orb ? 1.5f : 0f;
        return t;
    }

    void Update()
    {
        if (!orb || hit) return;
        phase += Time.deltaTime * 0.6f;
        var off = transform.position - center;
        off.y = 0;
        var d = Quaternion.Euler(0, Time.deltaTime * 35f, 0) * off;
        transform.position = new Vector3(center.x + d.x, center.y + 2.4f + Mathf.Sin(phase * 2f) * 0.8f, center.z + d.z);
    }

    public void OnSpellHit(SpellHit h)
    {
        if (hit) return;
        var p = GameManager.I.player;
        if (p == null || h.source != p.gameObject) return;
        if (!orb && h.type != SpellType.Incendio && h.type != SpellType.Confringo && h.type != SpellType.Bombarda)
        {
            HUD.Float(transform.position + Vector3.up * 2f, "Нужен огонь", new Color(1f, 0.6f, 0.4f));
            return;
        }
        hit = true;
        target.enabled = false;
        SFX.Play(orb ? "ring" : "fire", 0.6f);
        if (orb)
        {
            FX.Burst(transform.position, new Color(0.4f, 1f, 0.7f), 40, 5f, 0.35f, 0.6f);
            if (body != null) body.enabled = false;
            glow.enabled = false;
        }
        else
        {
            FX.Loop(transform, Vector3.up * 1.3f, new Color(1f, 0.5f, 0.15f), 30, 0.6f, 1.6f);
            glow.intensity = 2.2f;
        }
    }
}

// ---------------- Фантастические звери ----------------
public class Beast : MonoBehaviour
{
    public int index, species;
    Vector3 home, goal;
    float wait, flee, animT;
    Transform body;

    public static Beast Spawn(int index, int species, Vector3 home)
    {
        var go = new GameObject(Activities.BeastNames[species]);
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        var b = go.AddComponent<Beast>();
        b.index = index;
        b.species = species;
        b.home = home;
        go.transform.position = Ground(home);
        b.goal = go.transform.position;
        b.body = Prims.Group(go.transform, "Body");
        var black = MatLib.Solid(new Color(0.08f, 0.07f, 0.07f), 0.3f);
        var eye = MatLib.Solid(new Color(0.02f, 0.02f, 0.02f), 0.9f);
        switch (species)
        {
            case 0: // нюхлер: чёрный, с утиным клювом и монеткой
                Prims.Make(PrimitiveType.Sphere, b.body, new Vector3(0, 0.32f, 0), new Vector3(0.55f, 0.45f, 0.75f), black, false);
                Prims.Make(PrimitiveType.Sphere, b.body, new Vector3(0, 0.38f, 0.42f), new Vector3(0.22f, 0.12f, 0.3f), MatLib.Solid(new Color(0.85f, 0.6f, 0.3f)), false);
                Prims.Sphere(b.body, new Vector3(0.09f, 0.48f, 0.3f), 0.07f, eye);
                Prims.Sphere(b.body, new Vector3(-0.09f, 0.48f, 0.3f), 0.07f, eye);
                Prims.Make(PrimitiveType.Cylinder, b.body, new Vector3(0.25f, 0.45f, 0.05f), new Vector3(0.22f, 0.01f, 0.22f), MatLib.Glow(new Color(1f, 0.8f, 0.3f), 1.5f), false, Quaternion.Euler(0, 0, 80));
                break;
            case 1: // пушишка: жёлтый пушистый шарик
                Prims.Sphere(b.body, new Vector3(0, 0.35f, 0), 0.7f, MatLib.Solid(new Color(0.95f, 0.8f, 0.35f), 0.05f, 2f, 0.9f));
                Prims.Sphere(b.body, new Vector3(0.12f, 0.45f, 0.3f), 0.1f, eye);
                Prims.Sphere(b.body, new Vector3(-0.12f, 0.45f, 0.3f), 0.1f, eye);
                Prims.Make(PrimitiveType.Sphere, b.body, new Vector3(0, 0.3f, 0.34f), new Vector3(0.12f, 0.04f, 0.04f), MatLib.Solid(new Color(0.8f, 0.3f, 0.4f)), false);
                break;
            default: // лунный телёнок: бледно-голубой, длинные ноги и шея, огромные глаза
                var pale = MatLib.Solid(new Color(0.7f, 0.8f, 0.95f), 0.3f);
                Prims.Make(PrimitiveType.Sphere, b.body, new Vector3(0, 1.0f, 0), new Vector3(0.55f, 0.45f, 0.8f), pale, false);
                Prims.Make(PrimitiveType.Capsule, b.body, new Vector3(0, 1.35f, 0.35f), new Vector3(0.14f, 0.3f, 0.14f), pale, false, Quaternion.Euler(30, 0, 0));
                Prims.Sphere(b.body, new Vector3(0, 1.62f, 0.5f), 0.32f, pale);
                Prims.Sphere(b.body, new Vector3(0.12f, 1.66f, 0.62f), 0.16f, MatLib.Solid(new Color(0.1f, 0.12f, 0.25f), 0.9f));
                Prims.Sphere(b.body, new Vector3(-0.12f, 1.66f, 0.62f), 0.16f, MatLib.Solid(new Color(0.1f, 0.12f, 0.25f), 0.9f));
                for (int i = 0; i < 4; i++)
                    Prims.Box(b.body, new Vector3(i % 2 == 0 ? -0.15f : 0.15f, 0.42f, i < 2 ? 0.22f : -0.22f), new Vector3(0.07f, 0.85f, 0.07f), pale, false);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.6f, 0.75f, 1f);
                l.range = 4f;
                l.intensity = 0.8f;
                break;
        }
        return b;
    }

    static Vector3 Ground(Vector3 p)
    {
        float y = 0f;
        foreach (var h in Physics.RaycastAll(new Vector3(p.x, 30f, p.z), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
            if (!(h.collider is CharacterController)) y = Mathf.Max(y, h.point.y);
        return new Vector3(p.x, y, p.z);
    }

    void Update()
    {
        if (!GameState.Playing) return;
        float dt = Time.deltaTime;
        animT += dt;
        var pl = GameManager.I.player.transform.position;
        var toP = pl - transform.position;
        toP.y = 0;
        // Нюхлер убегает от бегущего игрока; остальные просто пасутся.
        if (species == 0 && toP.magnitude < 6f && GameManager.I.player.Velocity.magnitude > 6f && flee <= 0f)
        {
            flee = 1.4f;
            var away = -toP.normalized * 9f;
            goal = home + Vector3.ClampMagnitude(transform.position + away - home, 14f);
        }
        if (flee > 0f) flee -= dt;
        wait -= dt;
        if (wait <= 0f && flee <= 0f && (goal - transform.position).sqrMagnitude < 0.2f)
        {
            wait = Random.Range(1.5f, 4f);
            var r = Random.insideUnitCircle * 7f;
            goal = home + new Vector3(r.x, 0, r.y);
        }
        var d = goal - transform.position;
        d.y = 0;
        float speed = flee > 0f ? 6f : species == 2 ? 1.1f : 1.6f;
        if (d.magnitude > 0.1f && wait <= 0f || flee > 0f)
        {
            var step = Vector3.ClampMagnitude(d, speed * dt);
            transform.position = Ground(transform.position + step);
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 6f * dt);
            body.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(animT * (species == 1 ? 9f : 12f))) * (species == 1 ? 0.18f : 0.05f);
        }
        else body.localPosition = Vector3.zero;
    }
}

// ---------------- Ловля снитча ----------------
public class SnitchGame : MonoBehaviour
{
    public Transform snitch, rival;
    float timeLeft = 100f, retarget, dodge;
    Vector3 snitchVel, waypoint, rivalVel;
    bool finished;
    static Vector3 Center => WorldBuilder.PitchCenter + Vector3.up * 16f;
    static readonly Vector3 Extents = new(42f, 10f, 30f);
    public string Status
    {
        get
        {
            var p = GameManager.I.player.transform.position;
            return $"Снитч: {(snitch.position - p).magnitude:0} м · соперник {(snitch.position - rival.position).magnitude:0} м · {Mathf.CeilToInt(timeLeft)} с";
        }
    }

    void Awake()
    {
        snitch = new GameObject("Snitch").transform;
        snitch.position = Center + new Vector3(0, 0, 10);
        var gold = MatLib.Glow(new Color(1f, 0.8f, 0.25f), 3.5f);
        Prims.Sphere(snitch, Vector3.zero, 0.38f, gold);
        var wing = MatLib.Glow(new Color(1f, 0.95f, 0.85f), 1.5f);
        Prims.Box(snitch, new Vector3(0.35f, 0.05f, 0), new Vector3(0.5f, 0.02f, 0.2f), wing, false, Quaternion.Euler(0, 0, 20));
        Prims.Box(snitch, new Vector3(-0.35f, 0.05f, 0), new Vector3(0.5f, 0.02f, 0.2f), wing, false, Quaternion.Euler(0, 0, -20));
        var tr = snitch.gameObject.AddComponent<TrailRenderer>();
        tr.sharedMaterial = MatLib.Additive(Color.white);
        tr.time = 0.5f;
        tr.startWidth = 0.25f;
        tr.endWidth = 0f;
        tr.startColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        tr.endColor = new Color(1f, 0.85f, 0.3f, 0f);
        var l = snitch.gameObject.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.3f);
        l.range = 8f;
        l.intensity = 2f;

        rival = new GameObject("RivalSeeker").transform;
        rival.position = WorldBuilder.PitchCenter + new Vector3(10, 10, -26);
        var model = Prims.Group(rival, "Model");
        Figures.Humanoid(model, Houses.Main[1], Figures.Skin, new Color(0.85f, 0.8f, 0.6f), Houses.Second[1]);
        Figures.Broom(model).SetActive(true);
        waypoint = snitch.position;
        HUD.ShowToast("Поймайте снитч раньше ловца Слизерина! Пробел/Ctrl — высота, Shift — ускорение", new Color(1f, 0.85f, 0.3f));
    }

    void Update()
    {
        if (finished || !GameState.Playing) return;
        float dt = Time.deltaTime;
        timeLeft -= dt;
        var player = GameManager.I.player;
        var pp = player.transform.position + Vector3.up * 1f;

        // Снитч: петляет между случайными точками и уворачивается от того, кто близко.
        retarget -= dt;
        dodge -= dt;
        float dP = (pp - snitch.position).magnitude, dR = (rival.position - snitch.position).magnitude;
        if (retarget <= 0f || (snitch.position - waypoint).magnitude < 3f)
        {
            retarget = Random.Range(1.2f, 2.4f);
            waypoint = Center + Vector3.Scale(Random.insideUnitSphere, Extents);
        }
        if (dodge <= 0f && Mathf.Min(dP, dR) < 7f)
        {
            dodge = 2.5f;
            var threat = dP < dR ? pp : rival.position;
            var away = (snitch.position - threat).normalized;
            waypoint = Center + Vector3.Scale(Vector3.ClampMagnitude((snitch.position - Center) + (away + Random.insideUnitSphere * 0.6f) * 18f, 1f), Extents);
        }
        float sSpeed = dodge > 1.5f ? 24f : 17f;
        snitchVel = Vector3.Lerp(snitchVel, (waypoint - snitch.position).normalized * sSpeed, 3f * dt);
        snitch.position += snitchVel * dt;
        snitch.Rotate(0, 900 * dt, 0);

        // Соперник: летит к снитчу чуть медленнее Нимбуса.
        var toS = snitch.position - rival.position;
        rivalVel = Vector3.Lerp(rivalVel, toS.normalized * 16.5f, 1.6f * dt);
        rival.position += rivalVel * dt;
        if (rivalVel.sqrMagnitude > 0.1f) rival.rotation = Quaternion.Slerp(rival.rotation, Quaternion.LookRotation(rivalVel), 4f * dt);

        if (dP < 2.6f) Finish(true);
        else if (dR < 1.4f || timeLeft <= 0f) Finish(false);
    }

    public void ForceCatch() => Finish(true);

    void Finish(bool won)
    {
        if (finished) return;
        finished = true;
        FX.Burst(snitch.position, new Color(1f, 0.85f, 0.3f), 60, 6f, 0.3f, 0.8f, -0.3f);
        Activities.I.SnitchFinished(won);
        Destroy(snitch.gameObject);
        Destroy(rival.gameObject, 0.2f);
        Destroy(gameObject, 0.3f);
    }
}

// ---------------- Выручай-комната ----------------
// Три волны: маги → гоблины с пауками и инферналом → тролль с гоблинами.
public class RoomChallenge : MonoBehaviour
{
    public readonly List<Enemy> alive = new();
    public int wave;
    float started, nextWave = -1f;
    public bool Active { get; private set; }
    static readonly EnemyKind[][] Waves =
    {
        new[] { EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.DarkWizard },
        new[] { EnemyKind.Goblin, EnemyKind.Goblin, EnemyKind.Spider, EnemyKind.Spider, EnemyKind.Inferius },
        new[] { EnemyKind.Troll, EnemyKind.Goblin, EnemyKind.Goblin },
    };
    public string Status => Active ? $"Выручай-комната: волна {wave + 1}/{Waves.Length} · врагов {alive.FindAll(e => e != null && !e.IsDead).Count} · {Time.time - started:0} с" : null;

    public void Begin()
    {
        Active = true;
        wave = 0;
        started = Time.time;
        SFX.Play("curse", 0.6f);
        HUD.ShowToast("Комната превращается в арену! Продержитесь 3 волны", new Color(0.75f, 0.6f, 1f));
        SpawnWave();
    }

    void SpawnWave()
    {
        alive.Clear();
        var c = WorldBuilder.RoomOfRequirementPos;
        var kinds = Waves[wave];
        for (int i = 0; i < kinds.Length; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / kinds.Length + wave * 25f, 0) * Vector3.forward;
            var pos = c + new Vector3(d.x * 6.5f, 0.4f, Mathf.Clamp(d.z * 6.5f, -9f, 9f));
            var e = Enemy.Spawn(kinds[i], pos, true);
            e.goldDrop /= 2;
            alive.Add(e);
        }
        nextWave = -1f;
    }

    void Update()
    {
        if (!Active) return;
        if (GameState.Mode == GameMode.Dead) { Abort(true); return; }
        if (!GameState.Playing) return;
        if ((GameManager.I.player.transform.position - WorldBuilder.RoomOfRequirementPos).magnitude > 30f) { Abort(true); return; }
        alive.RemoveAll(e => e == null || e.IsDead);
        if (alive.Count > 0) return;
        if (nextWave < 0f)
        {
            nextWave = Time.time + 2f;
            if (wave + 1 < Waves.Length) HUD.ShowToast($"Волна {wave + 1} отбита!", new Color(0.75f, 0.6f, 1f));
            return;
        }
        if (Time.time < nextWave) return;
        wave++;
        if (wave >= Waves.Length)
        {
            Active = false;
            Activities.I.RoomFinished(Time.time - started);
            return;
        }
        SpawnWave();
    }

    public void Abort(bool notify)
    {
        if (!Active) return;
        Active = false;
        foreach (var e in alive) if (e != null) Destroy(e.gameObject);
        alive.Clear();
        if (notify) HUD.ShowToast("Испытание Выручай-комнаты прервано", new Color(1f, 0.6f, 0.5f));
    }
}

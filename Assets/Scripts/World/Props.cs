using System.Collections.Generic;
using UnityEngine;

// Страница путеводителя — коллекционка.
public class FieldGuidePage : MonoBehaviour
{
    public static readonly List<FieldGuidePage> All = new();
    public static readonly HashSet<int> Collected = new();
    Vector3 basePos;

    public static FieldGuidePage Spawn(Vector3 pos)
    {
        var go = new GameObject("FieldGuidePage");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        Prims.Box(go.transform, Vector3.zero, new Vector3(0.6f, 0.8f, 0.04f), MatLib.Glow(new Color(1f, 0.85f, 0.4f), 1.5f), false);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.4f);
        l.range = 5;
        l.intensity = 1.5f;
        var p = go.AddComponent<FieldGuidePage>();
        p.basePos = pos;
        return p;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.2f;
        transform.Rotate(0, 90 * Time.deltaTime, 0);
        var gm = GameManager.I;
        if (gm == null || gm.player == null || !GameState.Playing) return;
        if ((gm.player.transform.position + Vector3.up - transform.position).sqrMagnitude < 4f) Collect();
    }

    public void Collect()
    {
        int id=System.Array.FindIndex(WorldBuilder.PageSpots,v=>Vector3.Distance(v,basePos)<.1f);
        if(id>=0 && !Collected.Add(id))return;
        QuestManager.I.pages++;
        SFX.Play("pickup");
        FX.Burst(transform.position, new Color(1f, 0.85f, 0.4f), 30, 4f);
        HUD.ShowToast($"Страница путеводителя ({QuestManager.I.pages}/{QuestManager.TotalPages})", new Color(1f, 0.85f, 0.4f));
        Destroy(gameObject);
    }
}

// Кольцо для испытания на метле.
public class BroomRing : MonoBehaviour
{
    Renderer rend;
    bool passed;
    float t;

    public static BroomRing Spawn(Vector3 pos, Vector3 facing)
    {
        var go = new GameObject("BroomRing");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(90, 0, 0);
        var ring = Prims.Torus(go.transform, Vector3.zero, 4.2f, MatLib.Glow(new Color(0.4f, 0.6f, 1f), 1.2f));
        var r = go.AddComponent<BroomRing>();
        r.rend = ring.GetComponent<Renderer>();
        return r;
    }

    public void SetActive(bool active)
    {
        rend.sharedMaterial = active ? MatLib.Glow(new Color(1f, 0.8f, 0.2f), 3f) : MatLib.Glow(new Color(0.4f, 0.6f, 1f), 1.2f);
    }

    public void Pass()
    {
        passed = true;
        rend.sharedMaterial = MatLib.Glow(new Color(0.3f, 1f, 0.4f), 3f);
        FX.Burst(transform.position, new Color(0.3f, 1f, 0.4f), 50, 10f, 0.6f, 0.7f, 0f);
    }

    void Update()
    {
        if (!passed) return;
        t += Time.deltaTime;
        transform.localScale = Vector3.one * (1 + t * 1.5f);
        if (t > 0.6f) Destroy(gameObject);
    }
}

// Древняя реликвия в лесу.
public class Relic : MonoBehaviour
{
    public bool active;
    Light glow;
    Transform crystal;

    public static Relic Spawn(Vector3 pos)
    {
        var go = new GameObject("Relic");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var r = go.AddComponent<Relic>();
        r.crystal = Prims.Group(go.transform, "Crystal");
        var m = MatLib.Glow(new Color(0.3f, 0.8f, 1f), 2.5f);
        Prims.Cone(r.crystal, Vector3.zero, 0.45f, 0.8f, m);
        Prims.Cone(r.crystal, Vector3.zero, 0.45f, 0.8f, m, false, Quaternion.Euler(180, 0, 0));
        r.glow = go.AddComponent<Light>();
        r.glow.type = LightType.Point;
        r.glow.color = new Color(0.3f, 0.8f, 1f);
        r.glow.range = 10;
        r.glow.intensity = 0.6f;
        return r;
    }

    void Update()
    {
        crystal.localPosition = Vector3.up * Mathf.Sin(Time.time * 1.5f) * 0.15f;
        crystal.Rotate(0, (active ? 120 : 30) * Time.deltaTime, 0);
        glow.intensity = active ? 3f + Mathf.Sin(Time.time * 4) : 0.6f;
    }
}

// Круг на земле — через задержку в нём взрывается огонь (атака Ранрока).
public class FireMark : MonoBehaviour
{
    float delay, radius, damage, t;
    Transform disc;

    public static void Spawn(Vector3 pos, float delay, float radius, float damage)
    {
        var go = new GameObject("FireMark");
        float y = 0.08f;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 4f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            if (!(h.collider is CharacterController)) y = Mathf.Max(y, h.point.y + 0.06f);
        go.transform.position = new Vector3(pos.x, y, pos.z);
        var m = go.AddComponent<FireMark>();
        m.delay = delay;
        m.radius = radius;
        m.damage = damage;
        m.disc = Prims.Make(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.1f, 0.02f, 0.1f),
            MatLib.Fade(new Color(1f, 0.2f, 0.05f, 0.35f), 1.5f, 0.2f), false).transform;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / delay);
        disc.localScale = new Vector3(radius * 2 * k, 0.02f, radius * 2 * k);
        if (t < delay) return;
        FX.Burst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.45f, 0.1f), 40, 9f, 1f, 0.6f, -1f);
        FX.Flash(transform.position + Vector3.up, new Color(1f, 0.5f, 0.2f), 4, 10);
        SFX.Play("fire", 0.5f);
        var p = GameManager.I.player;
        var d = p.transform.position - transform.position;
        d.y = 0;
        if (d.magnitude < radius) p.ReceiveMelee(damage, true, null, d.normalized * 6 + Vector3.up * 5);
        Destroy(gameObject);
    }
}

// Тренировочный манекен.
public class TrainingDummy : MonoBehaviour, ISpellTarget
{
    public Health health;
    public event System.Action<TrainingDummy> Broken;
    Transform visual;
    float wobble, hop, t;

    public static TrainingDummy Spawn(Vector3 pos)
    {
        var go = new GameObject("Dummy");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1, 0);
        col.height = 2.1f;
        col.radius = 0.45f;
        var d = go.AddComponent<TrainingDummy>();
        d.visual = Prims.Group(go.transform, "Visual");
        var straw = MatLib.Solid(new Color(0.78f, 0.65f, 0.38f), 0.05f, 1f, 0.6f);
        Prims.Cyl(d.visual, Vector3.zero, 0.08f, 1f, MatLib.Solid(new Color(0.3f, 0.2f, 0.1f)), false);
        Prims.Make(PrimitiveType.Capsule, d.visual, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 0.45f, 0.5f), straw, false);
        Prims.Sphere(d.visual, new Vector3(0, 1.85f, 0), 0.4f, straw);
        Prims.Make(PrimitiveType.Cylinder, d.visual, new Vector3(0, 1.25f, 0.24f), new Vector3(0.32f, 0.01f, 0.32f), MatLib.Solid(new Color(0.8f, 0.1f, 0.1f)), false,
            Quaternion.Euler(90, 0, 0));
        Prims.Make(PrimitiveType.Capsule, d.visual, new Vector3(0, 1.4f, 0), new Vector3(0.15f, 0.7f, 0.15f), straw, false, Quaternion.Euler(0, 0, 90));
        d.health = go.AddComponent<Health>();
        d.health.Init(40);
        d.health.Died += d.Break;
        var t = go.AddComponent<Targetable>();
        t.displayName = "Манекен";
        t.health = d.health;
        t.aimHeight = 1.3f;
        go.transform.rotation = Quaternion.LookRotation(-new Vector3(pos.x - WorldBuilder.HecatPos.x, 0, pos.z - WorldBuilder.HecatPos.z));
        return d;
    }

    public void OnSpellHit(SpellHit h)
    {
        if (health.Dead) return;
        wobble = 1f;
        if (h.type == SpellType.Levioso) hop = 1f;
        if (h.type == SpellType.Depulso) wobble = 2f;
        HUD.Float(transform.position + Vector3.up * 2.2f, Mathf.RoundToInt(h.damage).ToString(), new Color(1f, 0.9f, 0.6f));
        health.TakeDamage(h.damage);
    }

    void Break()
    {
        FX.Burst(transform.position + Vector3.up * 1.2f, new Color(0.9f, 0.75f, 0.4f), 40, 7f, 0.4f, 0.8f, 1f);
        SFX.Play("hit");
        Broken?.Invoke(this);
        Destroy(gameObject);
    }

    void Update()
    {
        t += Time.deltaTime;
        wobble = Mathf.MoveTowards(wobble, 0, Time.deltaTime * 1.5f);
        hop = Mathf.MoveTowards(hop, 0, Time.deltaTime * 0.5f);
        visual.localRotation = Quaternion.Euler(Mathf.Sin(t * 18) * wobble * 14, 0, 0);
        visual.localPosition = Vector3.up * Mathf.Sin(hop * Mathf.PI) * 2f;
    }
}

// Галлеон: выпадает из врагов, подскакивает, притягивается к игроку.
public class Coin : MonoBehaviour
{
    int value;
    Vector3 vel;
    float groundY, life;

    public static void Drop(Vector3 pos, int total)
    {
        int n = Mathf.Clamp(total / 10, 4, 16);
        int each = Mathf.Max(1, total / n);
        for (int i = 0; i < n; i++)
        {
            int v = i == n - 1 ? total - each * (n - 1) : each;
            Spawn(pos, v);
        }
    }

    public static void Spawn(Vector3 pos, int value)
    {
        var go = new GameObject("Coin");
        go.transform.position = pos;
        Prims.Make(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.35f, 0.03f, 0.35f), MatLib.Glow(new Color(1f, 0.78f, 0.25f), 1.2f), false,
            Quaternion.Euler(90, 0, 0));
        var c = go.AddComponent<Coin>();
        c.value = value;
        var r = Random.insideUnitCircle * 4f;
        c.vel = new Vector3(r.x, Random.Range(4f, 7f), r.y);
        c.groundY = 0.3f;
        if (Physics.Raycast(pos + Vector3.up, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore) && !(hit.collider is CharacterController))
            c.groundY = hit.point.y + 0.3f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        life += dt;
        transform.Rotate(0, 360 * dt, 0, Space.World);
        var p = GameManager.I.player;
        var to = p.transform.position + Vector3.up - transform.position;
        if (life > 0.6f && to.magnitude < 8f)
        {
            transform.position += to.normalized * Mathf.Max(8f, 20f - to.magnitude) * dt;
            if (to.magnitude < 0.9f)
            {
                p.inventory.AddGold(value, transform.position + Vector3.up);
                SFX.Play("coin", 0.35f);
                Destroy(gameObject);
            }
            return;
        }
        vel.y -= 20f * dt;
        transform.position += vel * dt;
        if (transform.position.y < groundY)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            vel = new Vector3(vel.x * 0.5f, Mathf.Abs(vel.y) * 0.3f, vel.z * 0.5f);
        }
        if (life > 90f) Destroy(gameObject);
    }
}

// Сундук: открывается на E, внутри галлеоны и иногда зелье.
public class Chest : MonoBehaviour
{
    public static readonly List<Chest> All = new();
    public static event System.Action<Chest> Opened;
    public bool opened;
    Transform lid;
    float openT;
    public void RestoreOpened(bool value)
    {
        opened=value;openT=value?1:0;
        if(lid!=null)lid.localRotation=Quaternion.Euler(value?-100:0,0,0);
    }

    public static Chest Spawn(Vector3 pos, float yaw)
    {
        var go = new GameObject("Chest");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        var wood = MatLib.Solid(new Color(0.45f, 0.28f, 0.12f), 0.2f, 0.8f, 0.5f);
        var gold = MatLib.Solid(new Color(0.9f, 0.7f, 0.25f), 0.7f, 1f, 0.2f);
        Prims.Box(go.transform, new Vector3(0, 0.35f, 0), new Vector3(1.2f, 0.7f, 0.8f), wood);
        Prims.Box(go.transform, new Vector3(0, 0.35f, 0.41f), new Vector3(0.2f, 0.25f, 0.04f), gold, false);
        var c = go.AddComponent<Chest>();
        c.lid = Prims.Group(go.transform, "Lid");
        c.lid.localPosition = new Vector3(0, 0.7f, -0.4f);
        Prims.Box(c.lid, new Vector3(0, 0.12f, 0.4f), new Vector3(1.25f, 0.25f, 0.85f), wood, false);
        Prims.Box(c.lid, new Vector3(0, 0.12f, 0.4f), new Vector3(1.27f, 0.08f, 0.87f), gold, false);
        return c;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void Open(PlayerController p)
    {
        if (opened) return;
        opened = true;
        Opened?.Invoke(this);
        SFX.Play("chest", 0.7f);
        int gold = Random.Range(120, 360);
        Coin.Drop(transform.position + Vector3.up * 1.2f, gold);
        FX.Burst(transform.position + Vector3.up, new Color(1f, 0.85f, 0.4f), 40, 4f, 0.4f, 0.8f, -0.3f);
        if (Random.value < 0.4f)
        {
            string[] ids = { "wiggenweld", "rowan", "rowan", "maxima", "edurus", "focus", "invis", "thunder", "felix" };
            var id = ids[Random.Range(0, ids.Length)];
            if (p.inventory.potions[id] < Inventory.MaxStack)
            {
                p.inventory.potions[id]++;
                HUD.ShowToast("Найдено: " + ItemDB.Get(id).name, ItemDB.Get(id).color);
            }
        }
    }

    void Update()
    {
        if (!opened || openT >= 1f) return;
        openT = Mathf.Min(1f, openT + Time.deltaTime * 2f);
        lid.localRotation = Quaternion.Euler(-100f * openT, 0, 0);
    }
}

// Лагерь врагов: после зачистки возрождается через пару минут (если игрок далеко).
public class EnemyCamp : MonoBehaviour
{
    public static readonly List<EnemyCamp> All = new();
    public EnemyKind[] kinds;
    public QuestManager.Step unlockStep = QuestManager.Step.MeetNatsai;
    readonly List<Enemy> alive = new();
    float respawnT = 1f;

    public static EnemyCamp Create(Vector3 pos, params EnemyKind[] kinds)
    {
        var go = new GameObject("EnemyCamp");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var c = go.AddComponent<EnemyCamp>();
        c.kinds = kinds;
        return c;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        var q = QuestManager.I;
        if (q == null || q.step < unlockStep || !GameState.Playing) return;
        alive.RemoveAll(e => e == null || e.IsDead);
        if (alive.Count > 0) return;
        respawnT -= Time.deltaTime;
        if (respawnT > 0f) return;
        float dist = (GameManager.I.player.transform.position - transform.position).magnitude;
        if (dist < 70f) return;
        for (int i = 0; i < kinds.Length; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / kinds.Length, 0) * Vector3.forward;
            alive.Add(Enemy.Spawn(kinds[i], transform.position + d * 6f + Vector3.up * 0.3f, false));
        }
        respawnT = 150f;
    }
}

// Подбираемая штука: травы для зелий, сферы испытания. Срабатывает при приближении игрока.
public class Pickup : MonoBehaviour
{
    public static readonly List<Pickup> All = new();
    System.Action<Pickup> onCollect;
    float radius;
    Vector3 basePos;
    Color color;

    public static Pickup Spawn(Vector3 pos, Color c, float size, float radius, System.Action<Pickup> cb, bool flower = false)
    {
        var go = new GameObject(flower ? "Herb" : "Orb");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        if (flower)
        {
            Prims.Cyl(go.transform, new Vector3(0, -0.4f, 0), 0.04f, 0.6f, MatLib.Solid(new Color(0.2f, 0.5f, 0.2f)), false);
            for (int i = 0; i < 5; i++)
                Prims.Make(PrimitiveType.Sphere, go.transform, Quaternion.Euler(0, i * 72, 0) * new Vector3(0, 0.25f, 0.15f), new Vector3(0.18f, 0.06f, 0.28f),
                    MatLib.Glow(c, 1.5f), false, Quaternion.Euler(0, i * 72, 0));
            Prims.Sphere(go.transform, new Vector3(0, 0.27f, 0), 0.12f, MatLib.Glow(Color.white, 2f));
        }
        else
            Prims.Sphere(go.transform, Vector3.zero, size, MatLib.Glow(c, 2.5f));
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = flower ? 4f : 10f;
        l.intensity = 2f;
        var p = go.AddComponent<Pickup>();
        p.onCollect = cb;
        p.radius = radius;
        p.basePos = pos;
        p.color = c;
        return p;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f + basePos.x) * 0.15f;
        transform.Rotate(0, 60 * Time.deltaTime, 0);
        var gm = GameManager.I;
        if (gm == null || gm.player == null || !GameState.Playing) return;
        if ((gm.player.transform.position + Vector3.up - transform.position).sqrMagnitude < radius * radius) Collect();
    }

    public void Collect()
    {
        SFX.Play("pickup", 0.6f);
        FX.Burst(transform.position, color, 30, 5f, 0.4f, 0.6f);
        var cb = onCollect;
        onCollect = null;
        Destroy(gameObject);
        cb?.Invoke(this);
    }
}

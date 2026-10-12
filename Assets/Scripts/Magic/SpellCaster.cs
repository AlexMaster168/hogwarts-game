using System.Collections.Generic;
using UnityEngine;

public class SpellSlot
{
    public SpellType type;
    public string name;
    public float cooldown, cd, damage, speed, size;
    public bool learned, unforgivable;
    public Color color;
}

// Заклинания игрока. Выученные раскладываются по наборам из 4 штук (Tab — следующий набор, 1–4 — каст).
public class SpellCaster : MonoBehaviour
{
    public PlayerController pc;
    public Transform wandTip;
    public Targetable target;
    public readonly List<SpellSlot> catalog = new();
    public readonly List<SpellSlot> learned = new();
    public int page;
    public bool protegoLearned;
    public bool ShieldActive { get; private set; }
    public bool IsAiming => Time.time - lastCast < 0.7f;
    public bool InParryWindow => ShieldActive && Time.time - shieldStart < 0.35f;
    public Vector3 AimFlatDir { get; private set; } = Vector3.forward;
    public float basicCd;
    public int Pages => Mathf.Max(1, (learned.Count + 3) / 4);

    public static readonly Color BasicColor = new(1f, 0.85f, 0.45f);
    static readonly KeyCode[] Keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

    float lastCast = -10f, shieldStart, armRaise;
    GameObject shield;

    void Awake()
    {
        Def(SpellType.Incendio, "Инсендио", 5f, 25f, 0f, 0f, new Color(1f, 0.5f, 0.1f));
        Def(SpellType.Levioso, "Левиосо", 8f, 8f, 40f, 0.3f, new Color(1f, 0.95f, 0.4f));
        Def(SpellType.Depulso, "Депульсо", 6f, 20f, 44f, 0.35f, new Color(0.7f, 0.35f, 1f));
        Def(SpellType.Confringo, "Конфринго", 7f, 35f, 34f, 0.4f, new Color(1f, 0.25f, 0.1f));
        Def(SpellType.Expelliarmus, "Экспеллиармус", 6f, 15f, 46f, 0.3f, new Color(1f, 0.2f, 0.25f));
        Def(SpellType.Accio, "Акцио", 6f, 5f, 42f, 0.3f, new Color(0.95f, 0.45f, 1f));
        Def(SpellType.Diffindo, "Диффиндо", 5f, 40f, 60f, 0.22f, new Color(0.75f, 1f, 1f));
        Def(SpellType.Glacius, "Глациус", 9f, 15f, 38f, 0.35f, new Color(0.5f, 0.85f, 1f));
        Def(SpellType.Bombarda, "Бомбарда", 10f, 55f, 30f, 0.5f, new Color(1f, 0.6f, 0.15f));
        Def(SpellType.Imperio, "Империо", 25f, 0f, 36f, 0.35f, new Color(0.85f, 0.85f, 0.4f), true);
        Def(SpellType.Crucio, "Круцио", 20f, 10f, 36f, 0.35f, new Color(0.95f, 0.1f, 0.45f), true);
        Def(SpellType.AvadaKedavra, "Авада Кедавра", 40f, 999f, 40f, 0.4f, new Color(0.2f, 1f, 0.25f), true);
    }

    void Def(SpellType t, string name, float cd, float dmg, float speed, float size, Color c, bool unforgivable = false)
    {
        catalog.Add(new SpellSlot { type = t, name = name, cooldown = cd, damage = dmg, speed = speed, size = size, color = c, unforgivable = unforgivable });
    }

    public void Init(PlayerController p, Transform tip)
    {
        pc = p;
        wandTip = tip;
        shield = Prims.Sphere(transform, Vector3.up * 1.0f, 2.5f, MatLib.Fade(new Color(0.45f, 0.75f, 1f, 0.12f), 1.6f, 0.9f));
        shield.SetActive(false);
    }

    public SpellSlot Slot(SpellType t) => catalog.Find(s => s.type == t);
    public bool Knows(SpellType t) { var s = Slot(t); return s != null && s.learned; }
    public SpellSlot PageSlot(int i) { int k = page * 4 + i; return k < learned.Count ? learned[k] : null; }
    public string KeyFor(SpellSlot s) { int k = learned.IndexOf(s); return k < 0 ? "" : $"{k / 4 + 1}·{k % 4 + 1}"; }

    public void Learn(SpellType t)
    {
        var s = Slot(t);
        if (s == null || s.learned) return;
        s.learned = true;
        learned.Add(s);
        page = (learned.Count - 1) / 4;
        int k = learned.Count - 1;
        HUD.ShowToast($"Новое заклинание: {s.name} — набор {k / 4 + 1}, клавиша {k % 4 + 1}", s.color);
    }

    public void LearnProtego()
    {
        if (protegoLearned) return;
        protegoLearned = true;
        HUD.ShowToast("Новое заклинание: Протего [Q]", new Color(0.5f, 0.8f, 1f));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (basicCd > 0) basicCd -= dt;
        foreach (var s in catalog) if (s.cd > 0) s.cd -= dt;
        armRaise = Mathf.MoveTowards(armRaise, 0, dt * 3f);
        if (pc != null && pc.parts.armR != null)
            pc.parts.armR.localRotation = Quaternion.Euler(Mathf.Lerp(-15f, -88f, ShieldActive ? 1f : Mathf.Clamp01(armRaise * 2f)), 0, 0);

        if (!GameState.Playing) return;
        UpdateTarget();

        if (Input.GetKeyDown(KeyCode.Tab) && Pages > 1)
        {
            page = (page + 1) % Pages;
            SFX.Play("book", 0.4f);
        }

        bool canCast = !pc.onBroom && !pc.Dodging && !pc.health.Dead;
        bool wantShield = protegoLearned && canCast && Input.GetKey(KeyCode.Q);
        if (wantShield && !ShieldActive)
        {
            shieldStart = Time.time;
            SFX.Play("shield", 0.5f);
        }
        ShieldActive = wantShield;
        shield.SetActive(ShieldActive);
        if (!canCast || ShieldActive) return;

        if (Input.GetMouseButton(0) && basicCd <= 0) CastBasic();
        for (int i = 0; i < 4; i++)
            if (Input.GetKeyDown(Keys[i]))
            {
                var s = PageSlot(i);
                if (s != null) TryCast(s);
            }
        if (Input.GetKeyDown(KeyCode.F)) CastAncient();
    }

    void UpdateTarget()
    {
        Targetable best = null;
        float bestScore = float.MaxValue;
        var cam = pc.cam.transform;
        foreach (var t in Targetable.All)
        {
            if (!t.Alive) continue;
            var e = t.GetComponent<Enemy>();
            var to = t.AimPoint - cam.position;
            float d = to.magnitude;
            if (d > 45f) continue;
            float ang = Vector3.Angle(cam.forward, to);
            if (ang > 22f) continue;
            // Своего под Империо выбираем, только если больше целиться не в кого.
            float score = ang + d * 0.3f + (e != null && e.Charmed ? 1000f : 0f);
            if (score < bestScore) { bestScore = score; best = t; }
        }
        target = best;
    }

    public Vector3 AimPoint()
    {
        if (target != null) return target.AimPoint;
        var cam = pc.cam.transform;
        var hits = Physics.RaycastAll(cam.position, cam.forward, 200f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 p = cam.position + cam.forward * 100f;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(pc.transform)) continue;
            if (h.distance < best) { best = h.distance; p = h.point; }
        }
        return p;
    }

    void BeginCast(Vector3 aim)
    {
        armRaise = 1f;
        pc.parts.armR.localRotation = Quaternion.Euler(-88f, 0, 0);
        var flat = aim - pc.transform.position;
        flat.y = 0;
        if (flat.sqrMagnitude > 0.01f)
        {
            AimFlatDir = flat.normalized;
            pc.transform.rotation = Quaternion.LookRotation(AimFlatDir);
        }
        lastCast = Time.time;
        if (pc.visual != null) pc.visual.Attack();
        if (pc.inventory != null) pc.inventory.BreakInvisibility();
    }

    float Dmg => pc.inventory != null ? pc.inventory.DamageMult : 1f;

    void CastBasic()
    {
        basicCd = 0.38f;
        var aim = AimPoint();
        BeginCast(aim);
        var from = wandTip.position;
        var p = SpellProjectile.Spawn(SpellType.Basic, from, aim - from, pc.gameObject, 10f * Dmg, 48f, BasicColor, false,
            target != null ? target.transform : null, 6f, false, 0.22f);
        if (target != null) p.homingHeight = target.aimHeight;
        SFX.Play("cast", 0.35f);
    }

    public void TryCast(SpellSlot s)
    {
        if (!s.learned || s.cd > 0) return;
        s.cd = pc.inventory != null && pc.inventory.Focused ? 0.25f : s.cooldown * (pc.inventory != null ? pc.inventory.CooldownMult : 1f);
        var aim = AimPoint();
        BeginCast(aim);
        if (s.type == SpellType.Incendio)
        {
            CastIncendio(aim, s.color);
            return;
        }
        var from = wandTip.position;
        var p = SpellProjectile.Spawn(s.type, from, aim - from, pc.gameObject, s.damage * Dmg, s.speed, s.color, false,
            target != null ? target.transform : null, 6f, false, s.size);
        if (target != null) p.homingHeight = target.aimHeight;
        if (s.unforgivable)
        {
            SFX.Play("curse", 0.7f);
            FX.Flash(from, s.color, 4f, 10f, 0.3f);
        }
        else SFX.Play(s.type == SpellType.Bombarda ? "heavy" : "cast", 0.55f);
    }

    void CastIncendio(Vector3 aim, Color c)
    {
        var origin = pc.transform.position + Vector3.up * 1.2f;
        var dir = aim - origin;
        var flat = new Vector3(dir.x, 0, dir.z).normalized;
        foreach (var t in Targetable.All.ToArray())
        {
            if (!t.Alive) continue;
            var to = t.AimPoint - origin;
            var tf = new Vector3(to.x, 0, to.z);
            if (to.magnitude > 9f || Vector3.Angle(flat, tf) > 55f) continue;
            t.Target?.OnSpellHit(new SpellHit { type = SpellType.Incendio, damage = 25f * Dmg, point = t.AimPoint, dir = to.normalized, source = pc.gameObject });
        }
        FX.Cone(wandTip.position, dir.normalized, c);
        FX.Flash(wandTip.position + flat * 3f, c, 5f, 12f, 0.4f);
        SFX.Play("fire", 0.8f);
    }

    public void CastAncient()
    {
        if (!pc.ancientUnlocked || pc.ancient < 100f) return;
        pc.ancient = 0f;
        var aim = AimPoint();
        BeginCast(aim);
        var c = new Color(0.4f, 0.8f, 1f);
        foreach (var t in Targetable.All.ToArray())
        {
            if (!t.Alive || (t.AimPoint - aim).magnitude > 7f) continue;
            t.Target?.OnSpellHit(new SpellHit { type = SpellType.Ancient, damage = 150f * Dmg, point = t.AimPoint, dir = (t.AimPoint - wandTip.position).normalized, source = pc.gameObject });
        }
        FX.Burst(aim, c, 120, 16f, 1.4f, 1.1f, -0.3f);
        FX.Burst(aim, new Color(1f, 0.85f, 0.4f), 60, 8f, 0.8f, 1.2f, -0.6f);
        FX.Shockwave(aim - Vector3.up, c, 7f);
        FX.Flash(aim, c, 8f, 25f, 0.7f);
        SFX.Play("ancient", 0.9f);
        SFX.Play("boom", 0.8f);
        pc.cam.Shake(0.7f);
        HUD.ShowToast("ДРЕВНЯЯ МАГИЯ!", c);
    }

    // Для автотеста: кастуем без ввода.
    public void DebugCast(SpellType t)
    {
        if (t == SpellType.Basic) { CastBasic(); return; }
        if (t == SpellType.Ancient) { CastAncient(); return; }
        var s = Slot(t);
        if (s != null) { s.cd = 0; TryCast(s); }
    }

    public void OnBlocked()
    {
        SFX.Play("shield", 0.6f);
        FX.Burst(transform.position + Vector3.up * 1.2f + transform.forward * 0.8f, new Color(0.5f, 0.8f, 1f), 18, 4f);
        pc.AddAncient(3f);
    }

    public void OnParry()
    {
        SFX.Play("parry", 0.7f);
        FX.Flash(transform.position + Vector3.up * 1.2f, new Color(0.5f, 0.8f, 1f), 5f, 10f, 0.3f);
        HUD.Float(transform.position + Vector3.up * 2.4f, "ОТРАЖЕНО!", new Color(0.5f, 0.85f, 1f));
        pc.AddAncient(12f);
    }
}

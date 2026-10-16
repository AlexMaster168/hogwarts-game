using System.Collections.Generic;
using UnityEngine;

// Галлеоны, зелья, купленное снаряжение и его бонусы.
public class Inventory : MonoBehaviour
{
    public PlayerController pc;
    public int galleons = 50;
    public int earnedTotal;
    public readonly Dictionary<string, int> potions = new()
    {
        ["wiggenweld"] = 3, ["rowan"] = 2, ["maxima"] = 0, ["edurus"] = 0, ["focus"] = 0, ["invis"] = 0, ["thunder"] = 0, ["felix"] = 0,
    };
    public readonly HashSet<string> owned = new();
    public readonly Dictionary<ItemKind, string> equipped = new();
    public float maximaT, edurusT, regenT, focusT, invisT, felixT;
    public const int MaxStack = 5;
    float potionCd;
    GameObject hatObj;

    public bool Invisible => invisT > 0f;
    public bool Focused => focusT > 0f;
    public bool InfinitePotions => equipped.TryGetValue(ItemKind.Artifact, out var id) && id == "bottomless_satchel";

    public static readonly (string id, KeyCode key)[] PotionKeys =
    {
        ("wiggenweld", KeyCode.H), ("rowan", KeyCode.G), ("maxima", KeyCode.J), ("edurus", KeyCode.K),
        ("focus", KeyCode.L), ("invis", KeyCode.N), ("thunder", KeyCode.M), ("felix", KeyCode.U),
    };

    ItemDef Eq(ItemKind k) => equipped.TryGetValue(k, out var id) ? ItemDB.Get(id) : null;
    float Sum(System.Func<ItemDef, float> f)
    {
        float s = 0;
        foreach (var kv in equipped) { var d = ItemDB.Get(kv.Value); if (d != null) s += f(d); }
        return s;
    }

    public float DamageMult => (1f + Sum(d => d.dmgBonus)) * (maximaT > 0 ? 1.5f : 1f) * Progression.DamageMult;
    public float DefenseMult => (1f - Mathf.Min(0.8f, Sum(d => d.defBonus))) * (edurusT > 0 ? 0.5f : 1f);
    public float CooldownMult => 1f - Mathf.Min(0.6f, Sum(d => d.cdBonus));
    public float BroomMult => 1f + Sum(d => d.broomBonus);
    public float HpBonus => Sum(d => d.hpBonus);

    void Update()
    {
        float dt = Time.deltaTime;
        if (maximaT > 0) maximaT -= dt;
        if (edurusT > 0) edurusT -= dt;
        if (potionCd > 0) potionCd -= dt;
        if (focusT > 0) focusT -= dt;
        if (felixT > 0) felixT -= dt;
        if (regenT > 0)
        {
            regenT -= dt;
            pc.health.Heal(6f * dt);
        }
        if (invisT > 0)
        {
            invisT -= dt;
            if (invisT <= 0) EndInvisibility();
        }
        if (!GameState.Playing) return;
        foreach (var (id, key) in PotionKeys)
            if (Input.GetKeyDown(key)) UsePotion(id);
    }

    public void AddGold(int n, Vector3? at = null)
    {
        if (felixT > 0) n *= 2;
        galleons += n;
        earnedTotal += n;
        if (at.HasValue) HUD.Float(at.Value, "+" + n + " г.", new Color(1f, 0.85f, 0.3f));
    }

    public bool Owns(ItemDef d) => owned.Contains(d.id) || (d.kind == ItemKind.Book && pc.caster.Knows(d.spell));

    public string CantBuyReason(ItemDef d)
    {
        if (d.kind == ItemKind.Potion && potions[d.id] >= MaxStack) return "Максимум " + MaxStack;
        if (d.kind != ItemKind.Potion && Owns(d)) return "Уже есть";
        if (d.kind == ItemKind.Broom && !pc.hasBroom) return "Сначала получи метлу";
        if (galleons < d.price) return "Не хватает галлеонов";
        return null;
    }

    public bool Buy(ItemDef d)
    {
        if (CantBuyReason(d) != null) { SFX.Play("click", 0.4f); return false; }
        galleons -= d.price;
        SFX.Play("buy", 0.7f);
        switch (d.kind)
        {
            case ItemKind.Potion:
                potions[d.id]++;
                break;
            case ItemKind.Book:
                pc.caster.Learn(d.spell);
                break;
            default:
                owned.Add(d.id);
                Equip(d);
                break;
        }
        HUD.ShowToast("Куплено: " + d.name, new Color(1f, 0.85f, 0.4f));
        return true;
    }

    public void Equip(ItemDef d)
    {
        if (d.kind == ItemKind.Potion || d.kind == ItemKind.Book) return;
        equipped[d.kind] = d.id;
        float frac = pc.health.Frac;
        pc.health.max = Progression.BaseHp + HpBonus;
        pc.health.current = Mathf.Max(1f, pc.health.max * frac);
        ApplyVisuals();
    }

    public void Unequip(ItemKind k)
    {
        if (k == ItemKind.Broom) return;
        equipped.Remove(k);
        pc.health.max = Progression.BaseHp + HpBonus;
        pc.health.current = Mathf.Min(pc.health.current, pc.health.max);
        ApplyVisuals();
    }

    public void GiveStarterBroom()
    {
        owned.Add("cleansweep");
        if (!equipped.ContainsKey(ItemKind.Broom)) Equip(ItemDB.Get("cleansweep"));
    }

    // Мантия красит тело, шляпа появляется на голове, метла меняет цвет.
    public void ApplyVisuals()
    {
        var robe = Eq(ItemKind.Robe);
        pc.SetRobeColor(robe != null ? robe.color : new Color(0.08f, 0.08f, 0.1f));
        var hat = Eq(ItemKind.Hat);
        if (hatObj != null) Destroy(hatObj);
        if (hat != null)
        {
            var g = Prims.Group(pc.model, "Hat");
            var m = MatLib.Solid(hat.color, 0.1f);
            Prims.Make(PrimitiveType.Cylinder, g, new Vector3(0, 1.86f, 0), new Vector3(0.62f, 0.015f, 0.62f), m, false);
            Prims.Cone(g, new Vector3(0, 1.86f, 0), 0.24f, 0.62f, m, false, Quaternion.Euler(-8, 0, 0));
            hatObj = g.gameObject;
            if (pc.visual != null) pc.visual.PutOnHead(g, 1.86f);
        }
        var broom = Eq(ItemKind.Broom);
        if (broom != null && pc.broom != null)
            foreach (var r in pc.broom.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Cylinder")) r.sharedMaterial = MatLib.Solid(broom.color, 0.4f);
    }

    public void UsePotion(string id)
    {
        if (potionCd > 0 || (!InfinitePotions && potions[id] <= 0)) return;
        if ((id == "wiggenweld" || id == "rowan") && pc.health.current >= pc.health.max) return;
        if (!InfinitePotions) potions[id]--;
        potionCd = 1f;
        var d = ItemDB.Get(id);
        switch (id)
        {
            case "wiggenweld":
                pc.health.Heal(70f);
                HUD.Float(pc.transform.position + Vector3.up * 2.2f, "+70", new Color(0.4f, 1f, 0.5f));
                break;
            case "maxima":
                maximaT = 20f;
                HUD.ShowToast("Зелье Максимы: урон +50%", d.color);
                break;
            case "edurus":
                edurusT = 20f;
                HUD.ShowToast("Зелье Эдурус: защита +50%", d.color);
                break;
            case "rowan":
                regenT = 10f;
                HUD.ShowToast("Отвар рябины: лечение 10 сек", d.color);
                break;
            case "focus":
                focusT = 12f;
                foreach (var s in pc.caster.catalog) s.cd = 0f;
                HUD.ShowToast("Фокусирующее зелье: 12 сек без перезарядки!", d.color);
                break;
            case "invis":
                invisT = 15f;
                pc.SetGhost(true);
                foreach (var e in Enemy.All) if (!e.Charmed) e.LoseTarget();
                HUD.ShowToast("Невидимость: 15 сек (заклинание снимет её)", d.color);
                break;
            case "thunder":
                ThunderCloud.Spawn(pc.caster.AimPoint(), pc);
                HUD.ShowToast("Грозовое зелье!", d.color);
                break;
            case "felix":
                felixT = 60f;
                HUD.ShowToast("Феликс Фелицис: двойные галлеоны 60 сек!", d.color);
                break;
        }
        SFX.Play("potion", 0.7f);
        FX.Burst(pc.transform.position + Vector3.up, d.color, 30, 3f, 0.4f, 0.8f, -0.5f);
    }

    public void BreakInvisibility()
    {
        if (invisT <= 0) return;
        invisT = 0;
        EndInvisibility();
    }

    void EndInvisibility()
    {
        pc.SetGhost(false);
        HUD.ShowToast("Невидимость закончилась", new Color(0.7f, 0.8f, 1f));
    }
}

using UnityEngine;

// Уровни и опыт. Опыт дают победы над врагами, сюжет, поручения, испытания и активности.
// Каждый уровень: +10 к здоровью, +4% к урону и полное лечение. Максимум — 30 уровень.
public class Progression : MonoBehaviour
{
    public static Progression I;
    public const int MaxLevel = 30;
    public int level = 1, xp;
    public PlayerController pc;

    public int XpToNext => 80 + level * 70;
    public float Frac => level >= MaxLevel ? 1f : (float)xp / XpToNext;
    public static float BaseHp => 150f + (I != null ? (I.level - 1) * 10f : 0f);
    public static float DamageMult => 1f + (I != null ? (I.level - 1) * 0.04f : 0f);

    void Awake()
    {
        I = this;
        Enemy.AnyDied += OnEnemyDied;
    }

    void OnDestroy() { Enemy.AnyDied -= OnEnemyDied; }

    public static int XpFor(EnemyKind k) => k switch
    {
        EnemyKind.Spider => 15,
        EnemyKind.Troll => 70,
        EnemyKind.Guardian => 90,
        EnemyKind.Boss => 400,
        EnemyKind.Duelist => 0,
        EnemyKind.Inferius => 20,
        EnemyKind.Dementor => 45,
        EnemyKind.Goblin => 22,
        EnemyKind.Werewolf => 50,
        EnemyKind.Poacher => 30,
        EnemyKind.Berserker => 55,
        EnemyKind.Necromancer => 120,
        EnemyKind.Rookwood => 600,
        EnemyKind.GraveHound => 45,
        EnemyKind.Mourner => 55,
        EnemyKind.Chorister => 90,
        EnemyKind.Bellringer or EnemyKind.Gatekeeper => 650,
        EnemyKind.NamelessKing => 900,
        EnemyKind.Vampire => 60,
        EnemyKind.Golem => 110,
        EnemyKind.IceWraith => 50,
        EnemyKind.Mrax => 1000,
        _ => 25,
    };

    void OnEnemyDied(Enemy e)
    {
        int x = XpFor(e.kind);
        if (x > 0) Gain(x, e.transform.position + Vector3.up * (e.targetable.aimHeight + 0.6f));
    }

    public void Gain(int amount, Vector3? at = null)
    {
        if (amount <= 0 || level >= MaxLevel) return;
        xp += amount;
        if (at.HasValue) HUD.Float(at.Value, "+" + amount + " опыта", new Color(0.65f, 0.85f, 1f));
        while (level < MaxLevel && xp >= XpToNext)
        {
            xp -= XpToNext;
            level++;
            OnLevelUp();
        }
        if (level >= MaxLevel) xp = 0;
    }

    void OnLevelUp()
    {
        var inv = pc.inventory;
        pc.health.max = BaseHp + inv.HpBonus;
        pc.health.current = pc.health.max;
        var p = pc.transform.position;
        FX.Burst(p + Vector3.up, new Color(1f, 0.85f, 0.4f), 80, 7f, 0.5f, 1.1f, -0.6f);
        FX.Shockwave(p, new Color(1f, 0.85f, 0.4f), 5f);
        FX.Flash(p + Vector3.up, new Color(1f, 0.85f, 0.45f), 5f, 12f, 0.5f);
        SFX.Play("ancient", 0.6f);
        HUD.ShowToast($"Новый уровень: {level}! Здоровье +10, урон +4%", new Color(1f, 0.85f, 0.4f));
    }

    // Восстановление из сохранения без фанфар.
    public void Restore(int lvl, int x)
    {
        level = Mathf.Clamp(lvl, 1, MaxLevel);
        xp = Mathf.Max(0, x);
    }
}

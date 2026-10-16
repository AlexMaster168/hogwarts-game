using UnityEngine;

// Уязвимости и телеграфируемые атаки четырёх хранителей осколков.
public class SagaEnemy : MonoBehaviour
{
    public Enemy enemy;
    float attack = 6f, shield = 0f;
    bool second, third;
    GameObject ward;
    public static string Name(EnemyKind k) => k switch
    {
        EnemyKind.Ashling => "Пепельник", EnemyKind.RuneKnight => "Рунный рыцарь",
        EnemyKind.MirrorShade => "Зеркальная тень", EnemyKind.StormWisp => "Грозовой огонёк",
        EnemyKind.AshQueen => "Королева пепла", EnemyKind.BronzeWarden => "Бронзовый часовщик",
        EnemyKind.MirrorRegent => "Зеркальный регент", _ => "Архивариус гроз"
    };
    public static Color Tint(EnemyKind k) => k switch
    {
        EnemyKind.Ashling or EnemyKind.AshQueen => new Color(1f, 0.35f, 0.1f),
        EnemyKind.RuneKnight or EnemyKind.BronzeWarden => new Color(0.8f, 0.6f, 0.2f),
        EnemyKind.MirrorShade or EnemyKind.MirrorRegent => new Color(0.75f, 0.4f, 1f),
        _ => new Color(0.3f, 0.85f, 1f)
    };
    void Start()
    {
        enemy.targetable.displayName = Name(enemy.kind);
        enemy.goldDrop = enemy.IsBossKind ? 1800 : 160;
        var ring = Prims.Torus(enemy.model, Vector3.up * 2.8f, 1f, MatLib.Glow(Tint(enemy.kind)));
        if (enemy.IsBossKind) ring.transform.localScale = Vector3.one * 1.6f;
        if (enemy.IsBossKind)
        {
            ward = Prims.Sphere(enemy.model, Vector3.up * 1.5f, 4.2f, MatLib.Fade(new Color(Tint(enemy.kind).r, Tint(enemy.kind).g, Tint(enemy.kind).b, 0.22f)), false);
            ward.SetActive(false);
        }
    }
    public bool Accept(ref SpellHit hit)
    {
        if (shield <= 0 || hit.type == SpellType.Ancient) return true;
        var breaker = enemy.kind == EnemyKind.AshQueen ? SpellType.Aguamenti : enemy.kind == EnemyKind.BronzeWarden ? SpellType.Reducto : enemy.kind == EnemyKind.MirrorRegent ? SpellType.Lumos : SpellType.Incarcerous;
        if (hit.type == breaker) { shield = 0; FX.Shockwave(transform.position, Color.white, 4f); return true; }
        hit.damage *= 0.15f;
        HUD.Float(transform.position + Vector3.up * 3, "Щит: " + GameManager.I.player.caster.Slot(breaker).name, Tint(enemy.kind));
        return true;
    }
    void Update()
    {
        if (!GameState.Playing || enemy == null || enemy.IsDead || !enemy.aggro) return;
        shield = Mathf.Max(0, shield - Time.deltaTime);
        if (!enemy.IsBossKind) return;
        if (!second && enemy.health.Frac < 0.66f) { second = true; shield = 12; attack = 1f; }
        if (!third && enemy.health.Frac < 0.33f) { third = true; shield = 12; attack = 1f; }
        if (ward != null) ward.SetActive(shield > 0);
        attack -= Time.deltaTime;
        if (attack > 0) return;
        attack = third ? 5f : 8f;
        var p = GameManager.I.player;
        if ((p.transform.position - transform.position).sqrMagnitude > 2500) return;
        if (enemy.kind == EnemyKind.AshQueen)
            for (int i = -1; i <= 1; i++) FireMark.Spawn(p.transform.position + Vector3.right * i * 3f, 1.8f, 1.5f, 24);
        else if (enemy.kind == EnemyKind.BronzeWarden)
        {
            FX.Shockwave(transform.position, Tint(enemy.kind), 10);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                FireMark.Spawn(transform.position + new Vector3(Mathf.Sin(a) * 7, 0, Mathf.Cos(a) * 7), 2.4f, 1.5f, 28);
            }
        }
        else if (enemy.kind == EnemyKind.MirrorRegent)
        {
            var clone = Enemy.Spawn(EnemyKind.MirrorShade, transform.position + Vector3.right * 5);
            Destroy(clone.gameObject, 14);
        }
        else
            for (int i = 0; i < 3; i++) FireMark.Spawn(p.transform.position + new Vector3(i * 3 - 3, 0, i * 2), 1.2f, 1.8f, 32);
    }
    public void Reveal() { shield = 0; }
}

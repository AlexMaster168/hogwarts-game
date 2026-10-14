using System.Collections.Generic;
using UnityEngine;

public enum EnemyKind
{
    DarkWizard, Duelist, Spider, Troll, Boss, Guardian, Inferius, Dementor, Goblin, Werewolf, Poacher, Berserker, Necromancer, Rookwood,
    // Акты IV–V: существа Завесы. Повадки — от базовых видов, но своя модель, имя и награды.
    GraveHound, Mourner, Chorister, Bellringer, Gatekeeper, NamelessKing,
    // Глава VI: вампиры, каменные големы, ледяные призраки и Исольда Мракс.
    Vampire, Golem, IceWraith, Mrax
}

public class Enemy : MonoBehaviour, ISpellTarget
{
    public static readonly List<Enemy> All = new();
    public static event System.Action<Enemy> AnyDied;
    // Базовый вид: по нему выбираются повадки и сопротивления.
    public EnemyKind Base => kind switch
    {
        EnemyKind.GraveHound => EnemyKind.Werewolf,
        EnemyKind.Mourner => EnemyKind.Dementor,
        EnemyKind.Chorister => EnemyKind.Necromancer,
        EnemyKind.Bellringer or EnemyKind.NamelessKing => EnemyKind.Rookwood,
        EnemyKind.Gatekeeper => EnemyKind.Boss,
        _ => kind,
    };
    public bool Heavy => Base is EnemyKind.Troll or EnemyKind.Boss or EnemyKind.Guardian or EnemyKind.Rookwood or EnemyKind.Berserker or EnemyKind.Golem or EnemyKind.Mrax;
    public bool IsBossKind => Base is EnemyKind.Boss or EnemyKind.Rookwood or EnemyKind.Mrax;
    // Ледяные снаряды и волны замедляют героя.
    public bool Chills => kind is EnemyKind.IceWraith or EnemyKind.Mrax;
    // Исольда под каменным щитом, пока стоит хотя бы один её голем.
    public bool Shielded => kind == EnemyKind.Mrax && minions.Exists(m => m != null && !m.IsDead && m.kind == EnemyKind.Golem);

    public EnemyKind kind;
    public Health health;
    public Targetable targetable;
    public CharacterController cc;
    public Transform model;
    public FigureParts parts;
    public CharacterVisual visual;
    bool wasTelegraph, anim_speedFrozen;
    public bool aggro;
    public float aggroRange = 28f;
    public event System.Action<Enemy> Died;
    public bool IsDead => dying || health.Dead;
    public bool Telegraphing => telegraphT > 0f;
    public bool TelegraphRed => telegraphT > 0f && (attackKind == 1 || attackKind == 4);

    public float levitateT, stunT, burnT, disarmT, frozenT, crucioT, charmedT, slowT, cutT;
    public bool sliced;
    float flameT, cutTick;
    Light fireLight;
    // Время последнего «холода» от дементора — HUD рисует иней по краям экрана.
    public static float ColdTime = -10f;
    float drainTick, resistHint;
    public bool Charmed => charmedT > 0f;
    public int goldDrop;
    float crucioTick, charmCd;
    GameObject iceBlock;
    Light charmLight;
    Enemy charmTarget;
    float speed, attackCd = 1.5f, telegraphT, strafeT, strafeDir = 1f, burnTick, levBaseY, animT, deathT, castPose;
    int attackKind;
    bool dying, summoned1, summoned2;
    // Сложность применяется в Start: так учитывается health.Init, который сюжет вызывает сразу после Spawn.
    float baseHp, baseSpeed, summonCd = 8f, blinkCd = 9f;
    bool started, enraged;
    readonly List<Enemy> minions = new();
    Vector3 vel, knock;
    Light aura;

    static PlayerController P => GameManager.I.player;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static Enemy Spawn(EnemyKind kind, Vector3 pos, bool aggro = true)
    {
        var go = new GameObject(kind.ToString());
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var cc = go.AddComponent<CharacterController>();
        var e = go.AddComponent<Enemy>();
        e.kind = kind;
        e.cc = cc;
        e.aggro = aggro;
        e.health = go.AddComponent<Health>();
        e.model = Prims.Group(go.transform, "Model");
        var t = go.AddComponent<Targetable>();
        e.targetable = t;
        t.health = e.health;

        string name;
        float hp, h, r, aim;
        switch (kind)
        {
            case EnemyKind.Duelist:
                hp = 160; e.speed = 4f; h = 1.8f; r = 0.4f; aim = 1.3f; name = "Себастьян Сэллоу";
                e.parts = Figures.Humanoid(e.model, new Color(0.07f, 0.07f, 0.08f), Figures.Skin, new Color(0.25f, 0.15f, 0.08f), Houses.Main[1]);
                break;
            case EnemyKind.Spider:
                hp = 45; e.speed = 7.5f; h = 1.2f; r = 0.6f; aim = 0.8f; name = "Акромантул";
                e.parts = Figures.Spider(e.model);
                break;
            case EnemyKind.Troll:
                hp = 320; e.speed = 3.2f; h = 4.4f; r = 1.1f; aim = 3.2f; name = "Горный тролль";
                e.parts = Figures.Troll(e.model);
                break;
            case EnemyKind.Guardian:
                hp = 450; e.speed = 3f; h = 4.4f; r = 1.1f; aim = 3.2f; name = "Страж Хранителей";
                e.parts = Figures.Troll(e.model);
                break;
            case EnemyKind.Boss:
                hp = 1400; e.speed = 3f; h = 5f; r = 1.2f; aim = 3.6f; name = "Ранрок";
                e.parts = Figures.Goblin(e.model);
                e.model.localScale = Vector3.one * 2.6f;
                t.isBoss = true;
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point;
                e.aura.color = new Color(1f, 0.25f, 0.1f);
                e.aura.range = 12f;
                e.aura.intensity = 1.5f;
                break;
            case EnemyKind.Inferius:
                hp = 95; e.speed = 2.5f; h = 1.8f; r = 0.4f; aim = 1.3f; name = "Инфернал";
                e.parts = Figures.Humanoid(e.model, new Color(0.25f, 0.27f, 0.25f), new Color(0.6f, 0.68f, 0.6f), new Color(0.2f, 0.2f, 0.18f), new Color(0.2f, 0.25f, 0.2f));
                break;
            case EnemyKind.Dementor:
                hp = 260; e.speed = 4.6f; h = 2.6f; r = 0.5f; aim = 2.3f; name = "Дементор";
                BuildDementor(e);
                break;
            case EnemyKind.Goblin:
                hp = 80; e.speed = 4.4f; h = 1.3f; r = 0.35f; aim = 0.95f; name = "Гоблин Ранрока";
                e.parts = Figures.Goblin(e.model);
                break;
            case EnemyKind.Werewolf:
                hp = 180; e.speed = 7f; h = 2.1f; r = 0.5f; aim = 1.5f; name = "Оборотень";
                e.parts = Figures.Humanoid(e.model, new Color(0.3f, 0.25f, 0.2f), new Color(0.35f, 0.28f, 0.22f), new Color(0.2f, 0.16f, 0.12f), new Color(0.4f, 0.3f, 0.2f));
                e.aggroRange = 34f;
                break;
            case EnemyKind.Poacher:
                hp = 110; e.speed = 3.8f; h = 1.8f; r = 0.4f; aim = 1.3f; name = "Браконьер Руквуда";
                e.parts = Figures.Humanoid(e.model, new Color(0.35f, 0.28f, 0.18f), Figures.Skin, new Color(0.3f, 0.2f, 0.1f), new Color(0.55f, 0.4f, 0.2f));
                e.aggroRange = 36f;
                break;
            case EnemyKind.Berserker:
                hp = 240; e.speed = 5.2f; h = 1.7f; r = 0.5f; aim = 1.2f; name = "Гоблин-берсерк";
                e.parts = Figures.Goblin(e.model);
                break;
            case EnemyKind.Necromancer:
                hp = 280; e.speed = 3.2f; h = 1.9f; r = 0.4f; aim = 1.4f; name = "Некромант";
                e.parts = Figures.Humanoid(e.model, new Color(0.08f, 0.14f, 0.1f), new Color(0.7f, 0.75f, 0.7f), new Color(0.05f, 0.05f, 0.05f), new Color(0.2f, 0.8f, 0.4f), true, new Color(0.06f, 0.1f, 0.07f), new Color(0.3f, 1f, 0.5f));
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = new Color(0.3f, 1f, 0.45f); e.aura.range = 7f; e.aura.intensity = 1.6f;
                break;
            case EnemyKind.Rookwood:
                hp = 1800; e.speed = 4.4f; h = 2.2f; r = 0.5f; aim = 1.6f; name = "Виктор Руквуд";
                e.parts = Figures.Humanoid(e.model, new Color(0.25f, 0.03f, 0.05f), Figures.Skin, new Color(0.15f, 0.12f, 0.1f), new Color(0.8f, 0.6f, 0.15f), true, new Color(0.2f, 0.02f, 0.04f), new Color(1f, 0.2f, 0.2f));
                t.isBoss = true;
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = new Color(1f, 0.15f, 0.2f); e.aura.range = 10f; e.aura.intensity = 1.8f;
                break;
            case EnemyKind.GraveHound:
                hp = 150; e.speed = 7.6f; h = 1.9f; r = 0.5f; aim = 1.3f; name = "Могильная гончая";
                e.parts = Figures.Humanoid(e.model, new Color(0.18f, 0.24f, 0.2f), new Color(0.4f, 0.48f, 0.42f), new Color(0.1f, 0.12f, 0.1f), new Color(0.5f, 0.9f, 0.6f));
                e.aggroRange = 34f;
                break;
            case EnemyKind.Mourner:
                hp = 220; e.speed = 4.8f; h = 2.6f; r = 0.5f; aim = 2.3f; name = "Плакальщица Завесы";
                BuildDementor(e, new Color(0.3f, 0.2f, 0.4f, 0.85f), new Color(0.75f, 0.45f, 1f));
                break;
            case EnemyKind.Chorister:
                hp = 210; e.speed = 3.2f; h = 1.9f; r = 0.4f; aim = 1.4f; name = "Костяной певчий";
                e.parts = Figures.Humanoid(e.model, new Color(0.75f, 0.72f, 0.62f), new Color(0.85f, 0.83f, 0.75f), new Color(0.2f, 0.2f, 0.2f), new Color(0.6f, 0.5f, 0.9f), true, new Color(0.7f, 0.68f, 0.6f), new Color(0.7f, 0.55f, 1f));
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = new Color(0.7f, 0.55f, 1f); e.aura.range = 6f; e.aura.intensity = 1.4f;
                break;
            case EnemyKind.Bellringer:
            case EnemyKind.NamelessKing:
            {
                bool king = kind == EnemyKind.NamelessKing;
                hp = king ? 1900 : 1300; e.speed = king ? 4.6f : 4f; h = king ? 2.7f : 2.4f; r = 0.55f; aim = king ? 2f : 1.8f;
                name = king ? "Безымянный король" : "Звонарь пустоты";
                var robe = king ? new Color(0.12f, 0.05f, 0.2f) : new Color(0.4f, 0.28f, 0.12f);
                var glow = king ? new Color(0.75f, 0.2f, 1f) : new Color(1f, 0.7f, 0.3f);
                e.parts = Figures.Humanoid(e.model, robe, new Color(0.7f, 0.7f, 0.75f), new Color(0.1f, 0.1f, 0.1f), glow, true, robe, glow);
                t.isBoss = true;
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = glow; e.aura.range = 11f; e.aura.intensity = 2f;
                break;
            }
            case EnemyKind.Gatekeeper:
                hp = 1300; e.speed = 3f; h = 4.8f; r = 1.1f; aim = 3.4f; name = "Слепой привратник";
                e.parts = Figures.Troll(e.model);
                t.isBoss = true;
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = new Color(0.55f, 0.3f, 1f); e.aura.range = 12f; e.aura.intensity = 1.8f;
                break;
            case EnemyKind.Vampire:
                hp = 200; e.speed = 6f; h = 1.85f; r = 0.4f; aim = 1.35f; name = "Вампир";
                e.parts = Figures.Humanoid(e.model, new Color(0.08f, 0.02f, 0.04f), new Color(0.9f, 0.86f, 0.9f), new Color(0.05f, 0.05f, 0.05f), new Color(0.7f, 0.05f, 0.1f));
                e.aggroRange = 32f;
                break;
            case EnemyKind.Golem:
                hp = 520; e.speed = 2.7f; h = 3.9f; r = 1f; aim = 2.8f; name = "Каменный голем";
                e.parts = Figures.Troll(e.model);
                break;
            case EnemyKind.IceWraith:
                hp = 170; e.speed = 4.2f; h = 2.4f; r = 0.5f; aim = 2.1f; name = "Ледяной призрак";
                BuildDementor(e, new Color(0.55f, 0.8f, 0.95f, 0.75f), new Color(0.5f, 0.85f, 1f));
                break;
            case EnemyKind.Mrax:
                hp = 2000; e.speed = 4.3f; h = 2.2f; r = 0.5f; aim = 1.6f; name = "Исольда Мракс";
                e.parts = Figures.Humanoid(e.model, new Color(0.1f, 0.12f, 0.22f), Figures.Skin, new Color(0.85f, 0.85f, 0.9f), new Color(0.4f, 0.8f, 1f), true, new Color(0.08f, 0.1f, 0.2f), new Color(0.5f, 0.85f, 1f));
                t.isBoss = true;
                e.aura = go.AddComponent<Light>();
                e.aura.type = LightType.Point; e.aura.color = new Color(0.45f, 0.75f, 1f); e.aura.range = 11f; e.aura.intensity = 2f;
                break;
            default:
                hp = 70; e.speed = 3.6f; h = 1.8f; r = 0.4f; aim = 1.3f; name = "Тёмный маг";
                e.parts = Figures.Humanoid(e.model, new Color(0.06f, 0.06f, 0.07f), new Color(0.78f, 0.78f, 0.76f), new Color(0.05f, 0.05f, 0.05f),
                    new Color(0.45f, 0.05f, 0.05f), true, new Color(0.06f, 0.06f, 0.07f), new Color(1f, 0.15f, 0.1f));
                break;
        }
        e.goldDrop = kind switch
        {
            EnemyKind.Spider => Random.Range(32, 57),
            EnemyKind.Troll => 280,
            EnemyKind.Guardian => 360,
            EnemyKind.Boss => 1600,
            EnemyKind.Duelist => 0,
            EnemyKind.Inferius => Random.Range(20, 41),
            EnemyKind.Dementor => Random.Range(90, 141),
            EnemyKind.Goblin => Random.Range(40, 71),
            EnemyKind.Werewolf => Random.Range(70, 111),
            EnemyKind.Poacher => Random.Range(60, 96),
            EnemyKind.Berserker => Random.Range(90, 131),
            EnemyKind.Necromancer => 220,
            EnemyKind.Rookwood => 2500,
            EnemyKind.GraveHound => Random.Range(60, 96),
            EnemyKind.Mourner => Random.Range(90, 131),
            EnemyKind.Chorister => Random.Range(120, 161),
            EnemyKind.Bellringer or EnemyKind.Gatekeeper => 1800,
            EnemyKind.NamelessKing => 3000,
            EnemyKind.Vampire => Random.Range(90, 131),
            EnemyKind.Golem => 320,
            EnemyKind.IceWraith => Random.Range(80, 121),
            EnemyKind.Mrax => 3500,
            _ => Random.Range(60, 105),
        };
        e.visual = kind switch
        {
            EnemyKind.Spider => CharacterVisual.Attach(e.model, "spider", 1.3f),
            EnemyKind.Troll => CharacterVisual.Attach(e.model, "giant", 4.4f),
            EnemyKind.Guardian => CharacterVisual.Attach(e.model, "giant", 4.6f, null, false, new Color(0.55f, 0.65f, 0.9f)),
            EnemyKind.Boss => CharacterVisual.Attach(e.model, "goblin_male", 1.9f, e.parts, true),
            EnemyKind.Duelist => CharacterVisual.Attach(e.model, "casual_male", 1.8f, e.parts, true),
            EnemyKind.Inferius => CharacterVisual.Attach(e.model, "worker_male", 1.8f, e.parts, false, new Color(0.55f, 0.66f, 0.58f)),
            EnemyKind.Dementor => null,
            EnemyKind.Goblin => CharacterVisual.Attach(e.model, "goblin_male", 1.25f, e.parts, true),
            EnemyKind.Werewolf => CharacterVisual.Attach(e.model, "ninja_male", 2.1f, e.parts, false, new Color(0.5f, 0.4f, 0.32f)),
            EnemyKind.Poacher => CharacterVisual.Attach(e.model, "worker_male", 1.8f, e.parts, true, new Color(0.75f, 0.6f, 0.45f)),
            EnemyKind.Berserker => CharacterVisual.Attach(e.model, "goblin_male", 1.7f, e.parts, false, new Color(1f, 0.55f, 0.5f)),
            EnemyKind.Necromancer => CharacterVisual.Attach(e.model, "rpg_wizard", 1.9f, e.parts, true, new Color(0.45f, 0.75f, 0.5f)),
            EnemyKind.Rookwood => CharacterVisual.Attach(e.model, "rpg_rogue", 2.2f, e.parts, true, new Color(0.85f, 0.35f, 0.35f)),
            EnemyKind.GraveHound => CharacterVisual.Attach(e.model, "ninja_male", 1.9f, e.parts, false, new Color(0.45f, 0.6f, 0.5f)),
            EnemyKind.Mourner or EnemyKind.IceWraith => null,
            EnemyKind.Chorister => CharacterVisual.Attach(e.model, "rpg_wizard", 1.9f, e.parts, true, new Color(0.95f, 0.92f, 0.82f)),
            EnemyKind.Bellringer => CharacterVisual.Attach(e.model, "kimono_male", 2.4f, e.parts, true, new Color(0.8f, 0.55f, 0.3f)),
            EnemyKind.NamelessKing => CharacterVisual.Attach(e.model, "rpg_wizard", 2.7f, e.parts, true, new Color(0.45f, 0.3f, 0.65f)),
            EnemyKind.Gatekeeper => CharacterVisual.Attach(e.model, "giant", 4.8f, null, false, new Color(0.5f, 0.4f, 0.75f)),
            EnemyKind.Vampire => CharacterVisual.Attach(e.model, "suit_male", 1.85f, e.parts, false, new Color(0.85f, 0.75f, 0.85f)),
            EnemyKind.Golem => CharacterVisual.Attach(e.model, "giant", 3.9f, null, false, new Color(0.55f, 0.55f, 0.58f)),
            EnemyKind.Mrax => CharacterVisual.Attach(e.model, "witch", 2.2f, e.parts, true, new Color(0.6f, 0.75f, 1f)),
            _ => CharacterVisual.Attach(e.model, "rpg_rogue", 1.8f, e.parts, true, new Color(0.45f, 0.4f, 0.5f)),
        };
        cc.height = h;
        cc.radius = r;
        cc.center = new Vector3(0, h * 0.5f, 0);
        cc.stepOffset = Mathf.Min(0.45f, h * 0.3f);
        cc.slopeLimit = 50f;
        e.health.Init(hp);
        if (kind == EnemyKind.Duelist) e.health.minHealth = 1f;
        e.health.Died += e.Die;
        t.displayName = name;
        t.aimHeight = aim;
        e.attackCd = Random.Range(1f, 2.5f);
        if (kind == EnemyKind.Inferius && e.visual != null) e.visual.anim.speed = 0.75f;
        Decorate(e);
        FX.Burst(pos + Vector3.up, new Color(0.6f, 0.2f, 0.9f), 35, 5f, 0.6f, 0.6f, -0.3f);
        return e;
    }

    // Дементор из примитивов: рваная мантия, капюшон с пустотой вместо лица, серый шлейф и холодный свет.
    // Отличительные детали новых видов: корона короля, колокол Звонаря, светящиеся трещины голема.
    static void Decorate(Enemy e)
    {
        var top = Vector3.up * (e.cc.height + 0.05f);
        switch (e.kind)
        {
            case EnemyKind.NamelessKing:
                for (int i = 0; i < 7; i++)
                {
                    var d = Quaternion.Euler(0, i * 360f / 7, 0) * Vector3.forward * 0.22f;
                    Prims.Cone(e.model, top + d - Vector3.up * 0.15f, 0.06f, 0.45f, MatLib.Glow(new Color(0.75f, 0.2f, 1f), 3f));
                }
                FX.Loop(e.model, Vector3.up * 0.2f, new Color(0.45f, 0.1f, 0.7f, 0.4f), 22, 1.1f, 0.6f);
                break;
            case EnemyKind.Bellringer:
                Prims.Cone(e.model, new Vector3(0.45f, 0.9f, 0.1f), 0.32f, 0.5f, MatLib.Solid(new Color(0.7f, 0.5f, 0.2f), 0.7f));
                Prims.Sphere(e.model, new Vector3(0.45f, 0.85f, 0.1f), 0.14f, MatLib.Glow(new Color(1f, 0.7f, 0.3f), 3f));
                break;
            case EnemyKind.Gatekeeper:
                Prims.Box(e.model, top - Vector3.up * 1.1f + Vector3.forward * 0.55f, new Vector3(1.4f, 0.25f, 0.2f), MatLib.Solid(new Color(0.05f, 0.05f, 0.08f)), false);
                break;
            case EnemyKind.Golem:
                for (int i = 0; i < 4; i++)
                    Prims.Sphere(e.model, new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.2f, 3f), 0.75f), 0.22f, MatLib.Glow(new Color(1f, 0.55f, 0.15f), 2.5f));
                break;
            case EnemyKind.Vampire:
                FX.Loop(e.model, Vector3.up * 1.2f, new Color(0.35f, 0.02f, 0.06f, 0.35f), 6, 0.35f, 1.2f);
                break;
            case EnemyKind.Mrax:
                for (int i = 0; i < 3; i++)
                    Prims.Cone(e.model, new Vector3((i - 1) * 0.2f, e.cc.height - 0.05f, 0), 0.07f, 0.5f, MatLib.Glow(new Color(0.5f, 0.85f, 1f), 3f));
                break;
        }
    }

    static void BuildDementor(Enemy e) => BuildDementor(e, new Color(0.13f, 0.14f, 0.18f, 0.9f), new Color(0.45f, 0.6f, 1f));

    static void BuildDementor(Enemy e, Color robeCol, Color lightCol)
    {
        var robe = MatLib.Fade(robeCol, 1.6f, 0.15f);
        var g = Prims.Group(e.model, "Dementor");
        Prims.Cone(g, new Vector3(0, 0.25f, 0), 0.75f, 2.4f, robe);
        Prims.Sphere(g, new Vector3(0, 2.45f, 0), 0.7f, robe);
        Prims.Sphere(g, new Vector3(0, 2.4f, 0.2f), 0.42f, MatLib.Solid(new Color(0.01f, 0.01f, 0.012f), 0f));
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Box(g, new Vector3(s * 0.55f, 1.75f, 0.25f), new Vector3(0.14f, 0.9f, 0.14f), robe, false, Quaternion.Euler(-55, 0, s * 25));
            Prims.Box(g, new Vector3(s * 0.75f, 1.35f, 0.6f), new Vector3(0.06f, 0.35f, 0.06f), MatLib.Solid(new Color(0.45f, 0.48f, 0.5f)), false, Quaternion.Euler(-70, 0, 0));
        }
        for (int i = 0; i < 6; i++)
        {
            var d = Quaternion.Euler(0, i * 60, 0) * Vector3.forward;
            Prims.Box(g, d * 0.55f + Vector3.up * 0.15f, new Vector3(0.25f, 0.6f, 0.04f), robe, false, Quaternion.LookRotation(d) * Quaternion.Euler(15, 0, 0));
        }
        FX.Loop(g, new Vector3(0, 0.3f, 0), new Color(0.35f, 0.4f, 0.5f, 0.4f), 18, 0.9f, 0.5f);
        var lg = new GameObject("ColdLight");
        lg.transform.SetParent(g, false);
        lg.transform.localPosition = new Vector3(0, 1.5f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightCol;
        l.range = 8f;
        l.intensity = 2.2f;
    }

    Vector3 CastPos => parts != null && parts.wandTip != null
        ? parts.wandTip.position
        : transform.position + Vector3.up * (cc.height * 0.7f) + transform.forward * (cc.radius + 0.3f);

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        // Арресто Моментум: враг живёт в замедленном времени.
        if (slowT > 0f)
        {
            slowT -= dt;
            dt *= 0.35f;
            if (visual != null && frozenT <= 0f) visual.anim.speed = slowT > 0f ? 0.35f : 1f;
        }
        animT += dt;
        if (dying)
        {
            deathT += dt;
            float k = Mathf.Clamp01(deathT * 2.5f);
            if (visual != null) { if (deathT > 1.2f) model.localPosition += Vector3.down * dt * 1.2f; return; }
            model.localRotation = kind == EnemyKind.Spider ? Quaternion.Euler(0, 0, 180 * k) : Quaternion.Euler(-85 * k, 0, 0);
            if (deathT > 0.8f) model.localPosition += Vector3.down * dt * 1.2f;
            return;
        }

        if (fireLight != null) fireLight.enabled = burnT > 0f;
        if (burnT > 0f)
        {
            burnT -= dt;
            flameT -= dt;
            if (flameT <= 0f)
            {
                flameT = 0.05f;
                float hgt = targetable.aimHeight * 1.3f;
                var at = transform.position + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.2f, hgt), Random.Range(-0.35f, 0.35f));
                FX.Burst(at, Random.value < 0.5f ? new Color(1f, 0.45f, 0.08f) : new Color(1f, 0.8f, 0.2f), 7, 1.4f, 0.8f, 0.55f, -3.5f);
            }
            if (fireLight == null)
            {
                fireLight = new GameObject("BurnLight").AddComponent<Light>();
                fireLight.transform.SetParent(transform, false);
                fireLight.transform.localPosition = Vector3.up * targetable.aimHeight;
                fireLight.type = LightType.Point;
                fireLight.color = new Color(1f, 0.55f, 0.2f);
                fireLight.range = 6f;
            }
            fireLight.intensity = 1.6f + Mathf.PerlinNoise(Time.time * 9f, 0f) * 1.4f;
            burnTick -= dt;
            if (burnTick <= 0f)
            {
                burnTick = 0.5f;
                FX.Burst(transform.position + Vector3.up * targetable.aimHeight, new Color(1f, 0.5f, 0.1f), 8, 2f, 0.5f, 0.5f, -1f);
                health.TakeDamage(3f);
                if (IsDead) return;
            }
        }
        if (cutT > 0f)
        {
            cutT -= dt;
            cutTick -= dt;
            if (cutTick <= 0f)
            {
                cutTick = 0.5f;
                FX.Burst(transform.position + Vector3.up * targetable.aimHeight, new Color(0.75f, 0.05f, 0.08f), 6, 2f, 0.22f, 0.5f, 2f);
                health.TakeDamage(2.5f);
                if (IsDead) return;
            }
        }
        if (stunT > 0f) stunT -= dt;
        if (disarmT > 0f) disarmT -= dt;
        if (frozenT > 0f) { frozenT -= dt; if (frozenT <= 0f) Unfreeze(); }
        if (crucioT > 0f)
        {
            crucioT -= dt;
            crucioTick -= dt;
            if (crucioTick <= 0f)
            {
                crucioTick = 0.4f;
                FX.Burst(transform.position + Vector3.up * targetable.aimHeight, new Color(0.95f, 0.1f, 0.45f), 10, 3f, 0.4f, 0.4f);
                health.TakeDamage(IsBossKind ? 10f : 7f);
                if (IsDead) return;
            }
        }
        if (charmedT > 0f)
        {
            charmedT -= dt;
            if (charmedT <= 0f) EndCharm();
        }
        bool lev = levitateT > 0f;
        if (lev) levitateT -= dt;

        var player = P;
        var toP = player.transform.position - transform.position;
        toP.y = 0;
        float dist = toP.magnitude;
        bool hidden = player.inventory != null && player.inventory.Invisible;
        if (!aggro && dist < aggroRange && !hidden) aggro = true;
        else if (aggro && !Charmed && !IsBossKind && dist > LeashRange) LoseTarget();
        if (Base == EnemyKind.Dementor && aggro && !Charmed && dist < 7f && !hidden && GameState.Playing && !player.health.Dead)
        {
            // Холод дементора вытягивает силы, щит не помогает.
            ColdTime = Time.time;
            drainTick -= dt;
            if (drainTick <= 0f)
            {
                drainTick = 0.5f;
                player.health.TakeDamage(2.5f * player.inventory.DefenseMult);
            }
        }

        var desired = Vector3.zero;
        bool incapacitated = lev || stunT > 0f || frozenT > 0f || crucioT > 0f;
        bool canAct = !incapacitated && aggro && !hidden && !player.health.Dead && GameState.Playing;
        if (!incapacitated && Charmed && GameState.Playing)
        {
            ThinkCharmed(dt, ref desired, out var face);
            canAct = false;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 8f * dt);
        }
        else if (canAct) Think(dt, toP, dist, ref desired);
        else telegraphT = 0f;

        var hz = Vector3.MoveTowards(new Vector3(vel.x, 0, vel.z), desired, 20f * dt);
        vel.x = hz.x;
        vel.z = hz.z;
        if (lev)
            vel.y = (levBaseY + 2.6f - transform.position.y) * 3f;
        else
        {
            if (cc.isGrounded && vel.y < 0f) vel.y = -2f;
            vel.y -= 22f * dt;
        }
        knock = Vector3.MoveTowards(knock, Vector3.zero, 25f * dt);
        cc.Move((vel + knock) * dt);

        if (canAct && dist > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toP), 8f * dt);
        Animate(dt, hz.magnitude, lev);
    }

    void Think(float dt, Vector3 toP, float dist, ref Vector3 desired)
    {
        var dirTo = dist > 0.01f ? toP / dist : transform.forward;
        if (telegraphT > 0f)
        {
            telegraphT -= dt * Difficulty.Cur.tele;
            castPose = 1f;
            if (telegraphT <= 0f) Execute(attackKind, dirTo, dist);
            return;
        }
        attackCd -= dt * Difficulty.Cur.attack;
        switch (Base)
        {
            case EnemyKind.Vampire:
                blinkCd -= dt;
                if (blinkCd <= 0f && dist > 7f && dist < 26f)
                {
                    // Вампир растворяется в стае летучих мышей и возникает рядом с героем.
                    blinkCd = 5.5f;
                    var side = new Vector3(dirTo.z, 0, -dirTo.x) * Random.Range(-2f, 2f);
                    BlinkTo(P.transform.position - dirTo * 2.4f + side, new Color(0.45f, 0.02f, 0.08f));
                    attackCd = Mathf.Min(attackCd, 0.5f);
                    break;
                }
                if (dist > 2.2f) desired = dirTo * speed;
                if (attackCd <= 0f && dist < 3.2f)
                {
                    attackKind = 13;
                    telegraphT = 0.45f;
                    attackCd = 1.4f;
                    ThreatSense.Warn(false, 0.5f);
                }
                break;
            case EnemyKind.Golem:
                if (dist > 3.8f) desired = dirTo * speed;
                if (attackCd <= 0f && dist < 18f)
                {
                    bool rock = dist > 6f;
                    attackKind = rock ? 14 : 4;
                    telegraphT = rock ? 1.2f : 1.1f;
                    attackCd = rock ? 3.4f : 2.6f;
                    ThreatSense.Warn(!rock, telegraphT + 0.1f);
                }
                break;
            case EnemyKind.IceWraith:
            {
                strafeT -= dt;
                if (strafeT <= 0f) { strafeT = Random.Range(1.5f, 3f); strafeDir = Random.value < 0.5f ? -1f : 1f; }
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.6f;
                if (dist > 18f) desired += dirTo * speed;
                else if (dist < 9f) desired -= dirTo * speed * 0.7f;
                if (attackCd <= 0f)
                {
                    if (dist < 6f) { attackKind = 16; telegraphT = 0.9f; attackCd = 3f; ThreatSense.Warn(true, 1f); }
                    else if (dist < 36f) { attackKind = 15; telegraphT = 0.6f; attackCd = Random.Range(2.2f, 3f); ThreatSense.Warn(false, 0.6f + dist / 20f); }
                }
                break;
            }
            case EnemyKind.Mrax:
            {
                int phase = health.Frac > 0.66f ? 1 : health.Frac > 0.33f ? 2 : 3;
                minions.RemoveAll(m => m == null || m.IsDead);
                if (phase >= 2 && !summoned1)
                {
                    summoned1 = true;
                    SummonOwn(EnemyKind.Golem, 2);
                    HUD.ShowToast("Мракс поднимает каменных стражей! Пока они стоят, она под щитом", new Color(0.5f, 0.8f, 1f));
                }
                if (phase >= 3 && !summoned2)
                {
                    summoned2 = true;
                    SummonOwn(EnemyKind.Vampire, 2 + Difficulty.Cur.extra);
                    SummonOwn(EnemyKind.IceWraith, 1);
                    speed *= 1.25f;
                    if (aura != null) aura.intensity = 4f;
                    HUD.ShowToast("Мракс зовёт вампиров и вьюгу!", new Color(0.6f, 0.85f, 1f));
                }
                UpdateShield();
                blinkCd -= dt;
                if (phase >= 3 && blinkCd <= 0f && dist < 12f)
                {
                    blinkCd = 6f;
                    BlinkTo(P.transform.position + Quaternion.Euler(0, Random.Range(100f, 260f), 0) * (-dirTo) * Random.Range(9f, 12f), new Color(0.5f, 0.8f, 1f));
                }
                strafeT -= dt;
                if (strafeT <= 0f) { strafeT = Random.Range(1.2f, 2.5f); strafeDir = Random.value < 0.5f ? -1f : 1f; }
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.8f;
                if (dist > 16f) desired += dirTo * speed;
                else if (dist < 8f) desired -= dirTo * speed * 0.8f;
                if (attackCd <= 0f)
                {
                    float rv = Random.value;
                    if (dist < 6f) { attackKind = 16; telegraphT = 0.8f; ThreatSense.Warn(true, 0.9f); }
                    else if (phase >= 2 && rv < 0.3f) { attackKind = 6; telegraphT = 0.8f; ThreatSense.Warn(true, 2.2f); }
                    else if (rv < 0.7f) { attackKind = 15; telegraphT = 0.6f; ThreatSense.Warn(false, 0.6f + dist / 20f); }
                    else { attackKind = 1; telegraphT = 0.55f; ThreatSense.Warn(true, 0.6f + dist / 20f); }
                    attackCd = phase == 3 ? 1.4f : phase == 2 ? 1.9f : 2.4f;
                }
                break;
            }
            case EnemyKind.Werewolf:
                if (!enraged && health.Frac < 0.4f)
                {
                    enraged = true;
                    speed *= 1.35f;
                    HUD.Float(transform.position + Vector3.up * 2.6f, "Ярость!", new Color(1f, 0.4f, 0.2f));
                    SFX.Play("heavy", 0.5f);
                }
                if (dist > 2.6f) desired = dirTo * speed;
                if (attackCd <= 0f && dist < 7f)
                {
                    attackKind = 9;
                    telegraphT = 0.5f;
                    attackCd = enraged ? 1.1f : 1.5f;
                    ThreatSense.Warn(false, 0.55f);
                }
                break;
            case EnemyKind.Berserker:
                if (dist > 2.2f) desired = dirTo * speed;
                if (attackCd <= 0f && dist < 3.8f)
                {
                    attackKind = 11;
                    telegraphT = 0.8f;
                    attackCd = 2f;
                    ThreatSense.Warn(true, 0.9f);
                }
                break;
            case EnemyKind.Necromancer:
            {
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.5f;
                if (dist > 20f) desired += dirTo * speed;
                else if (dist < 11f) desired -= dirTo * speed;
                summonCd -= dt * Difficulty.Cur.attack;
                minions.RemoveAll(m => m == null || m.IsDead);
                if (summonCd <= 0f && minions.Count < 3 + Difficulty.Cur.extra && kind == EnemyKind.Necromancer)
                {
                    attackKind = 12;
                    telegraphT = 1f;
                    summonCd = 13f;
                    HUD.Float(transform.position + Vector3.up * 2.6f, "Восстаньте!", new Color(0.4f, 1f, 0.5f));
                }
                else if (attackCd <= 0f && dist < 34f && disarmT <= 0f)
                {
                    attackKind = Random.value < 0.4f ? 1 : 5;
                    telegraphT = 0.7f;
                    attackCd = Random.Range(2.2f, 3.2f);
                    ThreatSense.Warn(attackKind == 1, 0.7f + dist / 20f);
                }
                break;
            }
            case EnemyKind.Rookwood:
            {
                int phase = health.Frac > 0.66f ? 1 : health.Frac > 0.33f ? 2 : 3;
                if (phase >= 2 && !summoned1 && kind == EnemyKind.Rookwood)
                {
                    summoned1 = true;
                    Summon(EnemyKind.Poacher, 2 + Difficulty.Cur.extra);
                    HUD.ShowToast("Руквуд зовёт браконьеров!", new Color(1f, 0.4f, 0.3f));
                }
                if (phase >= 3 && !summoned2 && kind == EnemyKind.Rookwood)
                {
                    summoned2 = true;
                    Summon(EnemyKind.Werewolf, 2 + Difficulty.Cur.extra);
                    speed *= 1.3f;
                    if (aura != null) aura.intensity = 4f;
                    HUD.ShowToast("Руквуд выпускает оборотней!", new Color(1f, 0.3f, 0.2f));
                }
                // Аппарация: в третьей фазе прыгает за спину / вбок.
                blinkCd -= dt;
                if (phase >= 2 && blinkCd <= 0f && dist < 14f)
                {
                    blinkCd = phase == 3 ? 5f : 8f;
                    var off = Quaternion.Euler(0, Random.Range(90f, 270f), 0) * (-dirTo) * Random.Range(9f, 13f);
                    BlinkTo(P.transform.position + off, aura != null ? aura.color : new Color(0.8f, 0.1f, 0.2f));
                }
                strafeT -= dt;
                if (strafeT <= 0f) { strafeT = Random.Range(1.2f, 2.5f); strafeDir = Random.value < 0.5f ? -1f : 1f; }
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.8f;
                if (dist > 15f) desired += dirTo * speed;
                else if (dist < 7f) desired -= dirTo * speed;
                if (attackCd <= 0f)
                {
                    float rv = Random.value;
                    if (phase >= 2 && rv < 0.3f) { attackKind = 6; telegraphT = 0.8f; ThreatSense.Warn(true, 2.2f); }
                    else if (rv < 0.6f) { attackKind = 5; telegraphT = 0.6f; ThreatSense.Warn(false, 0.6f + dist / 20f); }
                    else { attackKind = 1; telegraphT = 0.55f; ThreatSense.Warn(true, 0.6f + dist / 20f); }
                    attackCd = phase == 3 ? 1.3f : phase == 2 ? 1.8f : 2.3f;
                }
                break;
            }
            case EnemyKind.Poacher:
            {
                strafeT -= dt;
                if (strafeT <= 0f) { strafeT = Random.Range(1.5f, 3f); strafeDir = Random.value < 0.5f ? -1f : 1f; }
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.6f;
                if (dist > 26f) desired += dirTo * speed;
                else if (dist < 13f) desired -= dirTo * speed;
                if (attackCd <= 0f && dist < 40f && disarmT <= 0f)
                {
                    attackKind = 10;
                    telegraphT = 0.55f;
                    attackCd = Random.Range(2f, 3f);
                    ThreatSense.Warn(false, 0.55f + dist / 22f);
                }
                break;
            }
            case EnemyKind.DarkWizard:
            case EnemyKind.Duelist:
            case EnemyKind.Goblin:
            {
                strafeT -= dt;
                if (strafeT <= 0f)
                {
                    strafeT = Random.Range(1.5f, 3.5f);
                    strafeDir = Random.value < 0.5f ? -1f : 1f;
                }
                var side = new Vector3(dirTo.z, 0, -dirTo.x);
                desired = side * strafeDir * speed * 0.7f;
                if (dist > 16f) desired += dirTo * speed;
                else if (dist < 8f) desired -= dirTo * speed;
                if (attackCd <= 0f && dist < 32f && disarmT <= 0f)
                {
                    bool red = kind == EnemyKind.DarkWizard && Random.value < 0.25f;
                    attackKind = red ? 1 : 0;
                    telegraphT = kind == EnemyKind.Goblin ? 0.45f : 0.6f;
                    attackCd = kind == EnemyKind.Duelist ? Random.Range(1.6f, 2.6f) : kind == EnemyKind.Goblin ? Random.Range(1.6f, 2.8f) : Random.Range(2.4f, 4f);
                    ThreatSense.Warn(red, 0.6f + dist / 20f);
                }
                break;
            }
            case EnemyKind.Inferius:
                if (dist > 2.1f) desired = dirTo * speed;
                else if (attackCd <= 0f)
                {
                    attackKind = 7;
                    telegraphT = 0.7f;
                    attackCd = 1.9f;
                    ThreatSense.Warn(false, 0.75f);
                }
                break;
            case EnemyKind.Dementor:
                if (dist > 4.5f) desired = dirTo * speed;
                else desired = new Vector3(dirTo.z, 0, -dirTo.x) * speed * 0.5f;
                if (attackCd <= 0f && dist < 9f)
                {
                    attackKind = 8;
                    telegraphT = 0.9f;
                    attackCd = 3.2f;
                    ThreatSense.Warn(true, 1f);
                }
                break;
            case EnemyKind.Spider:
                if (dist > 2.4f) desired = dirTo * speed;
                else if (attackCd <= 0f)
                {
                    attackKind = 2;
                    telegraphT = 0.45f;
                    attackCd = 1.6f;
                    ThreatSense.Warn(false, 0.5f);
                }
                break;
            case EnemyKind.Guardian:
            case EnemyKind.Troll:
                if (dist > 3.5f) desired = dirTo * speed;
                if (attackCd <= 0f && dist < 5.5f)
                {
                    bool slam = Random.value < 0.45f;
                    attackKind = slam ? 4 : 3;
                    telegraphT = slam ? 1.1f : 0.75f;
                    attackCd = 2.4f;
                    ThreatSense.Warn(slam, telegraphT + 0.1f);
                }
                break;
            case EnemyKind.Boss:
            {
                int phase = health.Frac > 0.6f ? 1 : health.Frac > 0.3f ? 2 : 3;
                if (phase >= 2 && !summoned1 && kind == EnemyKind.Boss)
                {
                    summoned1 = true;
                    Summon(EnemyKind.DarkWizard, 2);
                    HUD.ShowToast("Ранрок призывает приспешников!", new Color(1f, 0.4f, 0.3f));
                }
                if (phase >= 3 && !summoned2 && kind == EnemyKind.Boss)
                {
                    summoned2 = true;
                    Summon(EnemyKind.Spider, 3);
                    speed *= 1.4f;
                    aura.intensity = 4f;
                    HUD.ShowToast("Ранрок в ярости!", new Color(1f, 0.3f, 0.2f));
                }
                if (dist > 14f) desired = dirTo * speed;
                else if (dist < 6f) desired = -dirTo * speed * 0.6f;
                if (attackCd <= 0f)
                {
                    float rv = Random.value;
                    if (dist < 8f && rv < 0.45f) { attackKind = 4; telegraphT = 1.2f; ThreatSense.Warn(true, 1.3f); }
                    else if (phase >= 2 && rv < 0.7f) { attackKind = 6; telegraphT = 0.8f; ThreatSense.Warn(true, 2.2f); }
                    else { attackKind = 5; telegraphT = 0.8f; ThreatSense.Warn(false, 0.8f + dist / 20f); }
                    attackCd = phase == 3 ? 1.8f : 2.8f;
                }
                break;
            }
        }
    }

    void Execute(int k, Vector3 dirTo, float dist)
    {
        switch (k)
        {
            case 0: FireBolt(false, 0f); break;
            case 1: FireBolt(true, 0f); break;
            case 2:
                knock = dirTo * 9f;
                if (dist < 3.4f) P.ReceiveMelee(10f, false, this, dirTo * 5f);
                SFX.Play("hit", 0.3f);
                break;
            case 3:
                Swipe(4.8f, 18f);
                break;
            case 4:
                Slam(Base == EnemyKind.Boss ? 7.5f : 5f, 30f);
                break;
            case 5:
                for (int i = -1; i <= 1; i++) FireBolt(false, i * 14f);
                break;
            case 9:
                // Прыжок оборотня: рывок к герою и удар когтями.
                knock = dirTo * 15f + Vector3.up * 3f;
                SFX.Play("heavy", 0.4f);
                if (dist < 4.2f) P.ReceiveMelee(enraged ? 26f : 20f, false, this, dirTo * 6f);
                break;
            case 10:
                // Браконьер: два быстрых выстрела с разбросом.
                FireBolt(false, -4f);
                FireBolt(false, 4f);
                break;
            case 11:
                // Берсерк крутится с топором: удар по кругу.
                FX.Shockwave(transform.position, new Color(1f, 0.4f, 0.2f), 3.6f);
                SFX.Play("heavy", 0.5f);
                if (dist < 3.8f) P.ReceiveMelee(24f, false, this, dirTo * 8f);
                break;
            case 13:
                // Укус вампира: восстанавливает ему здоровье.
                knock = dirTo * 6f;
                SFX.Play("hit", 0.4f);
                if (dist < 3.4f && P.ReceiveMelee(16f, false, this, dirTo * 4f))
                {
                    health.Heal(22f);
                    FX.Burst(P.transform.position + Vector3.up * 1.3f, new Color(0.7f, 0.02f, 0.06f), 20, 4f, 0.25f, 0.5f, 1f);
                    HUD.Float(transform.position + Vector3.up * 2.4f, "+22", new Color(0.9f, 0.15f, 0.2f));
                }
                break;
            case 14:
            {
                // Голем швыряет валун: медленный, тяжёлый, его можно отразить Протего.
                var from = transform.position + Vector3.up * (cc.height * 0.85f);
                var to = P.transform.position + Vector3.up * 0.9f;
                SpellProjectile.Spawn(SpellType.EnemyBolt, from, to - from, gameObject, 24f, 15f, new Color(0.55f, 0.5f, 0.45f), true, P.transform, 0.5f, false, 0.7f);
                SFX.Play("heavy", 0.6f);
                break;
            }
            case 15:
                for (int i = -1; i <= 1; i++) FireBolt(false, i * 9f, new Color(0.55f, 0.85f, 1f));
                break;
            case 16:
                // Ледяная волна: щит не держит, спасает только перекат.
                FX.Shockwave(transform.position, new Color(0.6f, 0.9f, 1f), 6f);
                FX.Burst(transform.position + Vector3.up, new Color(0.8f, 0.95f, 1f), 50, 8f, 0.4f, 0.7f, 0.5f);
                SFX.Play("freeze", 0.6f);
                if (dist < 6f && P.ReceiveMelee(18f, true, this, dirTo * 7f)) P.Chill(2.5f);
                break;
            case 12:
                for (int i = 0; i < 2; i++)
                {
                    var d = Quaternion.Euler(0, i * 180f + 90f, 0) * dirTo;
                    var m = Spawn(EnemyKind.Inferius, transform.position + d * 3f + Vector3.up * 0.4f);
                    minions.Add(m);
                    FX.Burst(m.transform.position + Vector3.up, new Color(0.3f, 1f, 0.45f), 30, 4f, 0.5f, 0.6f, -1f);
                }
                SFX.Play("curse", 0.6f);
                break;
            case 7:
                knock = dirTo * 5f;
                if (dist < 2.8f) P.ReceiveMelee(16f, false, this, dirTo * 4f);
                SFX.Play("hit", 0.35f);
                break;
            case 8:
                // Поцелуй дементора: рывок, щит не держит — только перекат.
                knock = dirTo * 16f;
                FX.Burst(transform.position + Vector3.up * 2f, new Color(0.4f, 0.5f, 0.7f), 30, 6f, 0.6f, 0.6f);
                SFX.Play("curse", 0.5f);
                if (dist < 5f) P.ReceiveMelee(26f, true, this, dirTo * 6f);
                break;
            case 6:
                for (int i = 0; i < 6; i++)
                {
                    var off = i == 0 ? Vector2.zero : Random.insideUnitCircle * 8f;
                    FireMark.Spawn(P.transform.position + new Vector3(off.x, 0, off.y), 1.4f, 3f, 18f);
                }
                SFX.Play("fire", 0.6f);
                break;
        }
    }

    void FireBolt(bool unblockable, float spreadYaw, Color? col = null)
    {
        var from = CastPos;
        var to = P.transform.position + Vector3.up * 1.1f;
        var dir = Quaternion.AngleAxis(spreadYaw, Vector3.up) * (to - from).normalized;
        var c = col ?? (unblockable ? new Color(1f, 0.1f, 0.1f) : Chills ? new Color(0.55f, 0.85f, 1f) : new Color(0.4f, 1f, 0.3f));
        SpellProjectile.Spawn(unblockable ? SpellType.EnemyCurse : SpellType.EnemyBolt, from, dir, gameObject,
            unblockable ? 20f : 12f, unblockable ? 17f : 22f, c, true, P.transform, 1.0f, unblockable, unblockable ? 0.38f : 0.26f);
        SFX.Play("cast", 0.3f);
    }

    void Swipe(float range, float dmg)
    {
        var to = P.transform.position - transform.position;
        to.y = 0;
        FX.Burst(transform.position + transform.forward * 2f + Vector3.up * 1.5f, new Color(0.8f, 0.7f, 0.5f), 15, 4f);
        SFX.Play("hit", 0.5f);
        if (to.magnitude < range && Vector3.Angle(transform.forward, to) < 75f)
            P.ReceiveMelee(dmg, false, this, to.normalized * 6f);
    }

    void Slam(float radius, float dmg)
    {
        var c = transform.position + transform.forward * (cc.radius + 1.2f);
        FX.Shockwave(c, new Color(1f, 0.55f, 0.25f), radius);
        FX.Flash(c + Vector3.up, new Color(1f, 0.5f, 0.2f), 4f, radius * 2f);
        SFX.Play("boom", 0.8f);
        GameManager.I.camRig.Shake(0.5f);
        var to = P.transform.position - c;
        to.y = 0;
        if (to.magnitude < radius) P.ReceiveMelee(dmg, true, this, to.normalized * 10f + Vector3.up * 6f);
    }

    void BlinkTo(Vector3 to, Color c)
    {
        FX.Burst(transform.position + Vector3.up, c, 40, 6f, 0.5f, 0.5f);
        cc.enabled = false;
        transform.position = new Vector3(to.x, P.transform.position.y + 0.3f, to.z);
        Physics.SyncTransforms();
        cc.enabled = true;
        FX.Burst(transform.position + Vector3.up, c, 40, 6f, 0.5f, 0.5f);
        SFX.Play("ancient", 0.5f);
    }

    void SummonOwn(EnemyKind k, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / n + 60f, 0) * Vector3.forward;
            var m = Spawn(k, transform.position + d * 7f + Vector3.up * 0.5f);
            minions.Add(m);
            FX.Burst(m.transform.position + Vector3.up, new Color(0.5f, 0.8f, 1f), 30, 4f, 0.5f, 0.6f, -1f);
        }
    }

    // Каменный щит Мракс: полупрозрачная сфера, пока жив хотя бы один голем.
    GameObject shieldOrb;
    void UpdateShield()
    {
        bool on = Shielded;
        if (on && shieldOrb == null)
            shieldOrb = Prims.Sphere(transform, Vector3.up * cc.height * 0.5f, cc.height * 1.6f, MatLib.Fade(new Color(0.55f, 0.75f, 1f, 0.25f), 2f, 0.9f));
        if (shieldOrb != null && shieldOrb.activeSelf != on) shieldOrb.SetActive(on);
    }

    void Summon(EnemyKind k, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / n + 30f, 0) * Vector3.forward;
            Spawn(k, transform.position + d * 8f + Vector3.up * 0.5f);
        }
    }

    public const float LeashRange = 70f;

    void Start()
    {
        started = true;
        baseHp = health.max;
        baseSpeed = speed;
        goldDrop = Mathf.RoundToInt(goldDrop * Difficulty.Cur.reward);
        ApplyDifficulty();
    }

    public void ApplyDifficulty()
    {
        if (!started || kind == EnemyKind.Duelist) return;
        var d = Difficulty.Cur;
        float frac = health.Frac;
        health.max = baseHp * d.hp;
        health.current = Mathf.Max(1f, health.max * frac);
        speed = baseSpeed * d.speed * (enraged ? 1.35f : 1f);
    }

    public void LoseTarget()
    {
        aggro = false;
        telegraphT = 0f;
    }

    public void Stun(float t)
    {
        stunT = Mathf.Max(stunT, t);
        telegraphT = 0f;
    }

    public void Knock(Vector3 v)
    {
        bool heavy = Heavy;
        knock = heavy ? v * 0.3f : v;
    }

    public void OnSpellHit(SpellHit h)
    {
        if (IsDead) return;
        aggro = true;
        float dmg = h.damage;
        var src = P;
        bool fromPlayer = src != null && h.source == src.gameObject;
        bool crit = fromPlayer && h.type != SpellType.Ancient && h.type != SpellType.AvadaKedavra && h.type != SpellType.Imperio
            && h.type != SpellType.Arresto && Random.value < 0.12f;
        if (crit) dmg *= 2f;
        if (levitateT > 0f) dmg *= 1.5f;
        bool heavy = Heavy;
        var flatDir = new Vector3(h.dir.x, 0, h.dir.z).normalized;
        switch (h.type)
        {
            case SpellType.Incendio: burnT = kind is EnemyKind.Inferius or EnemyKind.IceWraith ? 5f : 3f; break;
            case SpellType.Confringo: burnT = kind is EnemyKind.Inferius or EnemyKind.IceWraith ? 5f : 3f; break;
            case SpellType.Diffindo:
                Slicer.Slash(transform.position + Vector3.up * targetable.aimHeight, h.dir);
                cutT = 3f;
                break;
            case SpellType.Patronus:
                if (Base == EnemyKind.Dementor)
                {
                    dmg *= 2f;
                    Stun(2.5f);
                    HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.5f), "Изгнан!", PatronusSpirit.Silver);
                }
                else
                {
                    knock = flatDir * (heavy ? 4f : 14f) + Vector3.up * (heavy ? 0f : 3f);
                    Stun(heavy ? 0.5f : 1.4f);
                }
                break;
            case SpellType.Arresto:
                slowT = heavy ? 4f : 6f;
                telegraphT = 0f;
                break;
            case SpellType.Levioso:
                if (heavy) Stun(0.6f);
                else
                {
                    if (levitateT <= 0f) levBaseY = transform.position.y;
                    levitateT = 3.5f;
                    telegraphT = 0f;
                }
                break;
            case SpellType.Depulso:
                knock = flatDir * (heavy ? 5f : 18f) + Vector3.up * (heavy ? 0f : 4f);
                Stun(heavy ? 0.4f : 1.2f);
                break;
            case SpellType.Stupefy:
                Stun(heavy ? 1.2f : 2.5f);
                break;
            case SpellType.Ancient:
                if (IsBossKind) dmg = health.max * 0.12f;
                break;
            case SpellType.Expelliarmus:
                if (kind == EnemyKind.DarkWizard || kind == EnemyKind.Duelist || kind == EnemyKind.Poacher || Base == EnemyKind.Necromancer || IsBossKind)
                {
                    disarmT = IsBossKind ? 2f : 5f;
                    HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.5f), "Обезоружен!", new Color(1f, 0.5f, 0.5f));
                }
                Stun(0.6f);
                break;
            case SpellType.Accio:
                knock = -flatDir * (heavy ? 4f : 15f) + Vector3.up * (heavy ? 0f : 3f);
                Stun(heavy ? 0.3f : 0.9f);
                break;
            case SpellType.Bombarda:
            {
                var away = transform.position - h.point;
                away.y = 0;
                knock = away.normalized * (heavy ? 4f : 14f) + Vector3.up * (heavy ? 0f : 5f);
                break;
            }
            case SpellType.Glacius:
                if (Chills) { HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.5f), "Лёд не берёт — жги огнём", new Color(0.6f, 0.85f, 1f)); dmg *= 0.3f; }
                else Freeze(heavy ? 2f : 4f);
                break;
            case SpellType.Crucio:
                crucioT = IsBossKind ? 2f : 4f;
                telegraphT = 0f;
                break;
            case SpellType.Imperio:
                if (IsBossKind || kind == EnemyKind.Duelist || kind == EnemyKind.Guardian || Base == EnemyKind.Dementor || kind == EnemyKind.Golem)
                {
                    Stun(1f);
                    HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.5f), "Сопротивляется!", new Color(0.9f, 0.9f, 0.5f));
                }
                else Charm(15f);
                break;
            case SpellType.AvadaKedavra:
                dmg = IsBossKind ? health.max * 0.2f : kind == EnemyKind.Duelist ? 40f : health.current + 1f;
                FX.Flash(transform.position + Vector3.up, new Color(0.2f, 1f, 0.25f), 8f, 15f, 0.5f);
                break;
        }
        if (frozenT > 0f && h.type != SpellType.Glacius) dmg *= 1.5f;
        bool fire = h.type == SpellType.Incendio || h.type == SpellType.Confringo || h.type == SpellType.Bombarda;
        if ((kind is EnemyKind.Inferius or EnemyKind.IceWraith) && fire) dmg *= 2f;
        if (kind == EnemyKind.Vampire && fire) dmg *= 1.5f;
        // Голем: камень почти не чувствует обычных чар, но крошится от взрывов.
        if (kind == EnemyKind.Golem && h.type != SpellType.Ancient && h.type != SpellType.AvadaKedavra)
        {
            if (h.type == SpellType.Bombarda || h.type == SpellType.Depulso || h.type == SpellType.Confringo) dmg *= 1.6f;
            else
            {
                dmg *= 0.45f;
                if (fromPlayer && Time.time > resistHint)
                {
                    resistHint = Time.time + 4f;
                    HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.8f), "Камень! Нужны Бомбарда, Конфринго или Депульсо", new Color(1f, 0.7f, 0.4f));
                }
            }
        }
        if (Shielded && h.type != SpellType.Ancient)
        {
            dmg *= 0.2f;
            if (fromPlayer && Time.time > resistHint)
            {
                resistHint = Time.time + 4f;
                HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.8f), "Каменный щит! Сначала разбей големов", new Color(0.6f, 0.85f, 1f));
            }
        }
        if (Base == EnemyKind.Dementor && h.type != SpellType.Patronus && h.type != SpellType.Ancient)
        {
            dmg *= 0.25f;
            if (fromPlayer && Time.time > resistHint)
            {
                resistHint = Time.time + 4f;
                HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.8f), "Почти не действует — нужен Патронус", new Color(0.7f, 0.8f, 1f));
            }
        }
        if (h.finisher && !IsDead)
        {
            knock = flatDir * (heavy ? 3f : 10f) + Vector3.up * (heavy ? 0f : 2f);
            Stun(heavy ? 0.2f : 0.5f);
        }
        var col = crit ? new Color(1f, 0.6f, 0.15f) : h.type == SpellType.Ancient ? new Color(0.4f, 0.85f, 1f) : levitateT > 0f ? new Color(1f, 0.95f, 0.4f) : Color.white;
        HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1f), (crit ? "КРИТ " : "") + Mathf.RoundToInt(dmg), col);
        float hpBefore = health.current;
        health.TakeDamage(dmg);
        // Добивающий Диффиндо режет врага пополам (кроме Ранрока — у него своя смерть).
        if (IsDead && h.type == SpellType.Diffindo && !IsBossKind && !sliced)
            sliced = Slicer.Slice(model, transform.position + Vector3.up * targetable.aimHeight * 0.85f, Slicer.CutNormal(h.dir), h.dir, transform.position.y, cc.radius * 0.6f);
        if (fromPlayer)
        {
            if (IsDead) { HitStop.Do(0.09f); GameManager.I.camRig.Shake(0.18f); }
            else if (crit || h.finisher || dmg >= 40f) HitStop.Do(0.045f);
        }
        if (visual != null && !IsDead) visual.Hit();
        var p = P;
        if (p != null && h.source == p.gameObject && h.type != SpellType.Ancient && h.type != SpellType.AvadaKedavra) p.AddAncient(Mathf.Min(Mathf.Max(0f, hpBefore - health.current) * 0.07f, 4f) + (IsDead ? 6f : 0f));
        if (kind == EnemyKind.Duelist && !dying && health.current <= health.minHealth + 0.01f) Yield();
    }

    void Die()
    {
        if (dying) return;
        dying = true;
        targetable.enabled = false;
        cc.enabled = false;
        telegraphT = 0f;
        if (aura != null) aura.enabled = false;
        Unfreeze();
        if (visual != null) { visual.anim.speed = 1f; visual.SetDead(true); }
        if (charmLight != null) charmLight.enabled = false;
        if (goldDrop > 0) Coin.Drop(transform.position + Vector3.up, goldDrop);
        FX.Burst(transform.position + Vector3.up * targetable.aimHeight, new Color(0.6f, 0.2f, 0.9f), IsBossKind ? 150 : 40, 6f, 0.7f, 0.9f, -0.3f);
        SFX.Play(IsBossKind ? "boom" : "hit", 0.7f);
        Died?.Invoke(this);
        AnyDied?.Invoke(this);
        Destroy(gameObject, IsBossKind ? 3f : 1.6f);
    }

    // Себастьян не умирает — сдаётся.
    void Yield()
    {
        dying = true;
        targetable.enabled = false;
        cc.enabled = false;
        FX.Burst(transform.position + Vector3.up, new Color(0.5f, 0.8f, 1f), 40, 5f, 0.6f, 0.6f);
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    void Freeze(float t)
    {
        frozenT = Mathf.Max(frozenT, t);
        telegraphT = 0f;
        if (iceBlock == null)
        {
            iceBlock = Prims.Make(PrimitiveType.Cube, transform, new Vector3(0, cc.height * 0.5f, 0),
                new Vector3(cc.radius * 2.6f, cc.height * 1.1f, cc.radius * 2.6f), MatLib.Fade(new Color(0.6f, 0.85f, 1f, 0.45f), 1.2f, 0.95f), false);
        }
    }

    void Unfreeze()
    {
        frozenT = 0f;
        if (iceBlock != null)
        {
            FX.Burst(iceBlock.transform.position, new Color(0.7f, 0.9f, 1f), 30, 5f, 0.3f, 0.6f, 1f);
            Destroy(iceBlock);
            iceBlock = null;
        }
    }

    // Империо держит только одного врага: новый каст отпускает предыдущего.
    void Charm(float t)
    {
        foreach (var other in All)
            if (other != this && other.Charmed) other.EndCharm();
        charmedT = t;
        telegraphT = 0f;
        aggro = true;
        if (charmLight == null)
        {
            var go = new GameObject("CharmLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * (cc.height + 0.5f);
            charmLight = go.AddComponent<Light>();
            charmLight.type = LightType.Point;
            charmLight.color = new Color(0.9f, 0.9f, 0.4f);
            charmLight.range = 5f;
            charmLight.intensity = 3f;
        }
        charmLight.enabled = true;
        HUD.Float(transform.position + Vector3.up * (targetable.aimHeight + 1.5f), "Под Империо!", new Color(0.9f, 0.9f, 0.4f));
    }

    void EndCharm()
    {
        charmedT = 0f;
        charmTarget = null;
        if (charmLight != null) charmLight.enabled = false;
    }

    // Под Империо: нападаем на ближайшего врага, если нет — держимся рядом с игроком.
    void ThinkCharmed(float dt, ref Vector3 desired, out Vector3 face)
    {
        face = Vector3.zero;
        if (charmTarget == null || charmTarget.IsDead || charmTarget.Charmed)
        {
            charmTarget = null;
            float best = 40f * 40f;
            foreach (var e in All)
            {
                if (e == this || e.IsDead || e.Charmed) continue;
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; charmTarget = e; }
            }
        }
        charmCd -= dt;
        if (charmTarget == null)
        {
            var toP = P.transform.position - transform.position;
            toP.y = 0;
            if (toP.magnitude > 5f) desired = toP.normalized * speed;
            face = toP;
            return;
        }
        var to = charmTarget.transform.position - transform.position;
        to.y = 0;
        float dist = to.magnitude;
        face = to;
        bool ranged = kind == EnemyKind.DarkWizard || kind == EnemyKind.Goblin;
        float range = ranged ? 14f : cc.radius + charmTarget.cc.radius + 1.8f;
        if (dist > range) desired = to.normalized * speed;
        if (charmCd > 0f || dist > range + 1f) return;
        charmCd = ranged ? 1.8f : 1.3f;
        castPose = 1f;
        if (ranged)
        {
            var from = CastPos;
            var p = SpellProjectile.Spawn(SpellType.Basic, from, charmTarget.targetable.AimPoint - from, gameObject, 15f, 28f,
                new Color(0.9f, 0.9f, 0.4f), false, charmTarget.transform, 3f, false, 0.26f);
            p.homingHeight = charmTarget.targetable.aimHeight;
            SFX.Play("cast", 0.3f);
        }
        else
        {
            charmTarget.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = Heavy ? 35f : 15f, point = charmTarget.targetable.AimPoint,
                dir = to.normalized, source = gameObject });
            SFX.Play("hit", 0.4f);
        }
    }

    public void Kill()
    {
        if (!IsDead) health.TakeDamage(health.current + 1f);
    }

    void Animate(float dt, float moveMag, bool lev)
    {
        castPose = Mathf.MoveTowards(castPose, 0f, dt * 2.5f);
        if (visual != null)
        {
            visual.SetSpeed(lev || frozenT > 0f ? 0f : moveMag);
            if (anim_speedFrozen != (frozenT > 0f)) { anim_speedFrozen = frozenT > 0f; visual.anim.speed = anim_speedFrozen ? 0f : 1f; }
            bool tel = telegraphT > 0f || castPose > 0.95f;
            if (tel && !wasTelegraph) visual.Attack();
            wasTelegraph = tel;
        }
        float bob = moveMag > 0.5f ? Mathf.Abs(Mathf.Sin(animT * 8f)) * 0.06f : 0f;
        if (Base == EnemyKind.Dementor || kind == EnemyKind.IceWraith)
        {
            model.localPosition = Vector3.up * (0.6f + Mathf.Sin(animT * 1.7f) * 0.25f);
            model.localRotation = Quaternion.Euler(telegraphT > 0f ? 25f : 6f, 0, Mathf.Sin(animT * 1.3f) * 6f);
            return;
        }
        if (lev)
        {
            model.localPosition = Vector3.up * Mathf.Sin(animT * 3f) * 0.1f;
            model.localRotation = Quaternion.Euler(Mathf.Sin(animT * 2f) * 25f, animT * 120f, Mathf.Cos(animT * 2f) * 20f);
        }
        else if (frozenT > 0f)
        {
            model.localPosition = Vector3.zero;
        }
        else if (crucioT > 0f)
        {
            model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(animT * 20f)) * 0.15f;
            model.localRotation = Quaternion.Euler(Mathf.Sin(animT * 17f) * 25f, 0, Mathf.Cos(animT * 23f) * 25f);
        }
        else if (stunT > 0f)
        {
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(animT * 12f) * 12f);
        }
        else
        {
            model.localPosition = Vector3.up * bob;
            float lean = (kind == EnemyKind.Troll || kind == EnemyKind.Golem) && telegraphT > 0 ? -15f : 0f;
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(lean, 0, 0), 10f * dt);
        }
        if (parts == null) return;
        if (kind == EnemyKind.Troll || kind == EnemyKind.Golem)
        {
            float raise = telegraphT > 0f ? -150f : -10f + Mathf.Sin(animT * 4f) * 10f;
            parts.armR.localRotation = Quaternion.Slerp(parts.armR.localRotation, Quaternion.Euler(raise, 0, 0), 8f * dt);
            parts.armL.localRotation = Quaternion.Euler(Mathf.Sin(animT * 4f + 1f) * 15f, 0, 0);
        }
        else if (parts.armR != null)
        {
            parts.armR.localRotation = Quaternion.Euler(Mathf.Lerp(-15f, -95f, castPose), 0, 0);
        }
        for (int i = 0; i < parts.legs.Count; i++)
        {
            float a = Mathf.Sin(animT * 14f + i * 1.7f) * 14f * Mathf.Clamp01(moveMag);
            parts.legs[i].localRotation = Quaternion.Euler(a, parts.legYaw[i] + a * 0.5f, 0);
        }
    }
}

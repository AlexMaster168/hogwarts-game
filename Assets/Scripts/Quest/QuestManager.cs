using System.Collections.Generic;
using UnityEngine;

// Сюжет: линейная цепочка шагов. Каждый шаг задаёт цель, маркер и условие перехода.
public partial class QuestManager : MonoBehaviour
{
    public static QuestManager I;

    public enum Step
    {
        MeetFig, Sorting, MeetHecat, Dummies, MeetSebastian, Duel, MeetKogawa, Rings,
        MeetNatsai, Ambush, GoForest, ForestFight, TakeRelic, FigReveal, Trials, FigVerdict, GoRuins, Boss, Aftermath, Archive, KitchenLetter, Convoy, Rescue, Seals, Rift, FinalReport,
        Ch3Call, Werewolves, PoacherTrail, PoacherCamp, FreeBeasts, GoblinMine, Graveyard, FortressMarch, Rookwood, Epilogue, Free,
        Ch4Call, Ch4Diary, Ch4Hunt, Ch4Witness, Ch4Crypt, Ch4Bell, Ch4Warden, Ch4Return,
        Ch5Call, Ch5Gate, Ch5Shadows, Ch5Memory, Ch5Sentinel, Ch5Anchor, Ch5Procession, Ch5Sovereign, Ch5Return,
        Ch6Call, Ch6Manor, Ch6Letter, Ch6Quarry, Ch6Ice, Ch6Map, Ch6Mrax, Ch6Return
    }

    public Step step;
    public string title = "", objective = "", progress = "";
    public bool hasMarker;
    public Vector3 markerPos;
    public string talkTarget;
    public int house = -1;
    public int pages;
    public const int TotalPages = 10;
    public Vector3 checkpoint;
    public float playTime;
    public int enemiesDefeated;

    public readonly List<Enemy> encounter = new();
    public readonly List<TrainingDummy> dummies = new();
    public readonly List<BroomRing> rings = new();
    public int ringIndex;
    public Relic relic;
    public Enemy boss;
    int encounterTotal;
    bool broomHintShown;
    readonly Dictionary<string, NPC> npcs = new();

    static PlayerController P => GameManager.I.player;
    public NPC Npc(string id) => npcs.TryGetValue(id, out var n) ? n : null;

    const string Fig = "Профессор Фиг", Hat = "Распределяющая шляпа", Hecat = "Профессор Гекат", Seb = "Себастьян",
        Kog = "Мадам Когава", Nat = "Натсай Онай", Ran = "Ранрок", You = "Ты";

    void Awake() { I = this; }

    void Start()
    {
        SpawnNpcs();
        ApplyModels();
        relic = Relic.Spawn(WorldBuilder.ForestClearing + Vector3.up * 1.9f);
        SetStep(Step.MeetFig, false);
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hmHorrorOnly") >= 0)
            StartCoroutine(VerifyHorror());
    }

    void SpawnNpcs()
    {
        Add(NPC.Spawn("fig", Fig, WorldBuilder.FigPos, new Color(0.35f, 0.25f, 0.15f), Figures.Skin, new Color(0.75f, 0.75f, 0.75f),
            new Color(0.6f, 0.45f, 0.2f), true, new Color(0.3f, 0.2f, 0.12f), "Держись рядом, и всё будет хорошо.", "Древняя магия... кто бы мог подумать."));
        Add(NPC.SpawnHat(WorldBuilder.HatPos));
        Add(NPC.Spawn("hecat", Hecat, WorldBuilder.HecatPos, new Color(0.25f, 0.1f, 0.3f), Figures.Skin, new Color(0.9f, 0.9f, 0.9f),
            new Color(0.6f, 0.2f, 0.6f), true, new Color(0.25f, 0.1f, 0.3f), "Тренируйся, пока заклинания не станут рефлексом.", "Протего спасает жизни. Запомни это."));
        Add(NPC.Spawn("sebastian", "Себастьян Сэллоу", WorldBuilder.DuelCenter + new Vector3(0, 0.4f, -7), new Color(0.07f, 0.07f, 0.08f), Figures.Skin,
            new Color(0.25f, 0.15f, 0.08f), Houses.Main[1], false, default, "Ещё дуэль? Как-нибудь потом.", "В библиотеке есть Запретная секция... но ты этого не слышал."));
        Add(NPC.Spawn("kogawa", Kog, WorldBuilder.KogawaPos, new Color(0.3f, 0.35f, 0.5f), Figures.Skin, new Color(0.05f, 0.05f, 0.05f),
            new Color(0.8f, 0.8f, 0.85f), false, default, "Выше нос — и метла сама полетит!", "Ускорение — Shift. Только не врежься в башню."));
        Add(NPC.Spawn("natsai", Nat, WorldBuilder.NatsaiPos, new Color(0.08f, 0.08f, 0.09f), new Color(0.45f, 0.3f, 0.2f), new Color(0.05f, 0.05f, 0.05f),
            Houses.Main[0], false, default, "Хогсмид снова в безопасности. Почти.", "Сливочное пиво в «Трёх мётлах» — лучшее."));

        string[] studentLines = { "Говорят, в Запретном лесу водятся акромантулы...", "Ты новенький? Пятый курс — это что-то!", "Опять опаздываю на зелья!", "Видел гигантского кальмара в озере?" };
        Color[] robes = { Houses.Main[0], Houses.Main[2], Houses.Main[3], Houses.Main[1] };
        Vector3[] spots = { new(-12, 0, -12), new(12, 0, -14), new(-14, 0, 10), new(10, 0, 14) };
        for (int i = 0; i < 4; i++)
        {
            var n = NPC.Spawn("student" + i, "Студент", spots[i], new Color(0.08f, 0.08f, 0.1f), i == 2 ? new Color(0.55f, 0.38f, 0.26f) : Figures.Skin,
                i % 2 == 0 ? new Color(0.6f, 0.4f, 0.15f) : new Color(0.15f, 0.1f, 0.05f), robes[i], false, default, studentLines);
            n.wanders = true;
            Add(n);
        }
        // Студенты в комнатах замка (стоят на месте, чтобы не бродить сквозь мебель).
        (Vector3 pos, int house, string line)[] inside =
        {
            (WorldBuilder.LibraryPos + new Vector3(-1.5f, 0.08f, -6f), 2, "Тсс! Мадам Скрибнер не любит шума в библиотеке."),
            (WorldBuilder.ClassroomPos + new Vector3(4f, 0.08f, -4f), 3, "Профессор Хекат велела отработать Протего. Опять."),
            (WorldBuilder.CommonRoomPos + new Vector3(-3f, 0.08f, -4f), 0, "Огонь в камине никогда не гаснет. Уютно, да?"),
            (WorldBuilder.TrophyPos + new Vector3(2f, 0.08f, -3f), 1, "Слизерин опять впереди по очкам. Надо что-то делать."),
        };
        for (int i = 0; i < inside.Length; i++)
        {
            var it = inside[i];
            Add(NPC.Spawn("pupil" + i, "Студент", it.pos, new Color(0.08f, 0.08f, 0.1f), Figures.Skin,
                i % 2 == 0 ? new Color(0.15f, 0.1f, 0.05f) : new Color(0.6f, 0.4f, 0.15f), Houses.Main[it.house], false, default, it.line));
        }
        Add(NPC.Spawn("sharp", "Профессор Шарп", WorldBuilder.Courtyard + new Vector3(-12, 0, 28), new Color(0.15f, 0.25f, 0.2f), Figures.Skin,
            new Color(0.5f, 0.5f, 0.5f), Houses.Main[1], false, default, "Зельеварение — точная наука. Никаких махов палочкой.", "Не трогай котёл."));
        Add(NPC.Spawn("ronen", "Профессор Ронен", new Vector3(6, 0.12f, 42), new Color(0.3f, 0.15f, 0.35f), new Color(0.55f, 0.38f, 0.26f),
            new Color(0.1f, 0.1f, 0.1f), Houses.Main[2], false, default, "Заклинания — это искусство!", "Вингардиум Левиоса! Не Левиоса́, а Левио́са."));
        Add(NPC.Spawn("moon", "Смотритель Мун", WorldBuilder.Courtyard + new Vector3(-8, 0, 2), new Color(0.3f, 0.25f, 0.18f), Figures.Skin,
            new Color(0.5f, 0.5f, 0.5f), new Color(0.6f, 0.5f, 0.3f), false, default, "Не шуми в коридорах.", "Ключи? Какие ключи? Алохомора надёжнее."));
        Add(NPC.Spawn("garlick", "Профессор Гарлик", new Vector3(36, 0, -32), new Color(0.3f, 0.45f, 0.2f), Figures.Skin,
            new Color(0.6f, 0.35f, 0.15f), Houses.Main[3], false, default, "Мандрагоры сегодня особенно капризны.", "Береги уши у теплиц!"));
        for (int i = 0; i < 3; i++)
        {
            var k = NPC.Spawn("keeper_" + Trials.Ids[i], Trials.Keepers[i], Trials.Sites[i] + new Vector3(0, 0.45f, -7), Color.gray, Color.gray, Color.gray, Color.gray,
                false, default, "...");
            Add(k);
        }

        Shop("pippin", "Дж. Пиппин", WorldBuilder.HogsmeadeCenter + new Vector3(-25, 0, -7), new Color(0.2f, 0.45f, 0.25f), "Свежие зелья! Только сегодня!");
        Shop("tomes", "Хозяин «Свитков и фолиантов»", WorldBuilder.HogsmeadeCenter + new Vector3(-12, 0, 7), new Color(0.4f, 0.3f, 0.2f), "Знание — сила. А сила стоит денег.");
        Shop("spintwitches", "Продавец Спинтвитчес", WorldBuilder.HogsmeadeCenter + new Vector3(13, 0, 6), new Color(0.2f, 0.3f, 0.55f), "Новая Молния! Быстрее ветра!");
        Shop("gladrags", "Хозяйка Гладрэгс", WorldBuilder.HogsmeadeCenter + new Vector3(22, 0, -7), new Color(0.55f, 0.2f, 0.45f), "Мантии по последней моде!");
        Shop("shady", "Тёмный торговец", WorldBuilder.ShadyPos + new Vector3(0, 0, 1.5f), new Color(0.05f, 0.06f, 0.05f), "Тс-с... Не здесь.");
        // Логово тёмных магов к северу от замка: те же непростительные + тёмная экипировка.
        Shop("darkcamp", "Скупщик проклятий Морвен", WorldBuilder.DarkCampPos + new Vector3(-3.5f, 0, -2.5f), new Color(0.05f, 0.08f, 0.05f), "Пришёл за силой? Плати.");
        Shop("darkgear", "Тёмная колдунья Изольда", WorldBuilder.DarkCampPos + new Vector3(3.5f, 0, -2.5f), new Color(0.12f, 0.05f, 0.1f), "Эти вещи помнят своих хозяев.");

        string[] villageLines = { "Добро пожаловать в Хогсмид!", "Тёмные маги совсем распоясались...", "Лучшие перья — у «Писаря»." };
        for (int i = 0; i < 3; i++)
        {
            var n = NPC.Spawn("villager" + i, "Житель Хогсмида", WorldBuilder.HogsmeadeCenter + new Vector3(-25 + i * 22, 0, 3), new Color(0.35f, 0.3f, 0.25f + i * 0.1f),
                Figures.Skin, new Color(0.4f, 0.3f, 0.2f), new Color(0.6f, 0.5f, 0.3f), i == 1, new Color(0.3f, 0.25f, 0.2f), villageLines);
            n.wanders = true;
            Add(n);
        }
    }

    void Add(NPC n) { npcs[n.id] = n; }

    static readonly Dictionary<string, string> Roles = new()
    {
        ["fig"] = "oldclassy_male", ["hecat"] = "oldclassy_female", ["sebastian"] = "casual_male", ["kogawa"] = "casual2_female",
        ["natsai"] = "casual_female", ["student0"] = "casual2_male", ["student1"] = "casual3_female", ["student2"] = "casual_male",
        ["student3"] = "casual2_female", ["villager0"] = "worker_male", ["villager1"] = "chef_male", ["villager2"] = "worker_female",
        ["pippin"] = "doctor_male_old", ["tomes"] = "oldclassy_female", ["spintwitches"] = "casual2_male", ["gladrags"] = "suit_female",
        ["shady"] = "ninja_male", ["darkcamp"] = "rpg_rogue", ["darkgear"] = "witch", ["sharp"] = "doctor_male_young", ["ronen"] = "suit_male", ["garlick"] = "kimono_female", ["moon"] = "worker_male",
        ["keeper_rackham"] = "oldclassy_male", ["keeper_bakar"] = "kimono_male", ["keeper_niamh"] = "witch",
        ["pupil0"] = "casual3_female", ["pupil1"] = "casual2_male", ["pupil2"] = "casual_female", ["pupil3"] = "casual_male",
    };

    void ApplyModels()
    {
        foreach (var kv in npcs)
        {
            if (Roles.TryGetValue(kv.Key, out var role))
                kv.Value.UseModel(role, 1.8f, kv.Key == "darkcamp" ? new Color(0.35f, 0.5f, 0.35f) : kv.Key == "darkgear" ? new Color(0.45f, 0.3f, 0.45f) : null);
            if (kv.Key.StartsWith("keeper_")) kv.Value.MakeGhost(Trials.Colors[System.Array.IndexOf(Trials.Ids, kv.Key.Substring(7))]);
        }
    }

    void Shop(string id, string name, Vector3 pos, Color robe, string line)
    {
        var n = NPC.Spawn(id, name, pos, robe, Figures.Skin, new Color(0.3f, 0.25f, 0.2f), new Color(0.8f, 0.7f, 0.4f), id == "shady", new Color(0.05f, 0.05f, 0.05f), line);
        n.shopId = id;
        Add(n);
    }

    bool shadyWarned;

    void Marker(Vector3 p) { hasMarker = true; markerPos = p; }

    public void SetStep(Step s, bool announce = true)
    {
        ClearHorrorEncounter();
        step = s;
        talkTarget = null;
        progress = "";
        hasMarker = false;
        switch (s)
        {
            case Step.MeetFig:
                title = "Новый студент"; objective = "Поговори с профессором Фиг";
                talkTarget = "fig"; Marker(WorldBuilder.FigPos); checkpoint = WorldBuilder.Spawn;
                break;
            case Step.Sorting:
                title = "Распределение"; objective = "Пройди по мосту в замок и подойди к Распределяющей шляпе в Большом зале";
                talkTarget = "hat"; Marker(WorldBuilder.HatPos); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.MeetHecat:
                title = "Защита от тёмных искусств"; objective = "Найди профессора Гекат на тренировочном дворе (восточные ворота)";
                talkTarget = "hecat"; Marker(WorldBuilder.HecatPos); checkpoint = new Vector3(0, 0, 20);
                break;
            case Step.Dummies:
                title = "Тренировка"; objective = "Уничтожь тренировочные манекены (ЛКМ, Инсендио [1], Левиосо [2])";
                Marker(WorldBuilder.TrainingCenter); checkpoint = WorldBuilder.HecatPos + new Vector3(-3, 0, 0);
                dummies.Clear();
                foreach (var p in WorldBuilder.DummySpots)
                {
                    var d = TrainingDummy.Spawn(p);
                    d.Broken += x => dummies.Remove(x);
                    dummies.Add(d);
                }
                break;
            case Step.MeetSebastian:
                title = "Дуэльный клуб"; objective = "Поговори с Себастьяном на дуэльной арене";
                talkTarget = "sebastian"; Marker(Npc("sebastian").transform.position); checkpoint = WorldBuilder.DuelCenter + new Vector3(0, 0.5f, -14);
                break;
            case Step.Duel:
                title = "Дуэль"; objective = "Победи Себастьяна. Держи Q — Протего, нажми Q прямо перед попаданием — отражение!";
                Marker(WorldBuilder.DuelCenter);
                break;
            case Step.MeetKogawa:
                title = "Полёты на метле"; objective = "Найди мадам Когаву у поля для квиддича (северо-восток)";
                talkTarget = "kogawa"; Marker(WorldBuilder.KogawaPos); checkpoint = WorldBuilder.DuelCenter + new Vector3(0, 0.5f, -14);
                break;
            case Step.Rings:
                title = "Испытание полётом"; objective = "Пролети сквозь все кольца. B — метла, Пробел/Ctrl — вверх/вниз, Shift — ускорение";
                checkpoint = WorldBuilder.KogawaPos + new Vector3(3, 0, 0);
                rings.Clear();
                ringIndex = 0;
                var rp = WorldBuilder.Rings;
                for (int i = 0; i < rp.Length; i++)
                {
                    var next = i + 1 < rp.Length ? rp[i + 1] : rp[i] + (rp[i] - rp[i - 1]);
                    var prev = i > 0 ? rp[i - 1] : WorldBuilder.KogawaPos;
                    rings.Add(BroomRing.Spawn(rp[i], (next - prev).normalized));
                }
                rings[0].SetActive(true);
                Marker(rp[0]);
                break;
            case Step.MeetNatsai:
                title = "Тревога в Хогсмиде"; objective = "Лети в Хогсмид (юго-восток) и найди Натсай Онай";
                talkTarget = "natsai"; Marker(WorldBuilder.NatsaiPos); checkpoint = WorldBuilder.KogawaPos + new Vector3(3, 0, 0);
                break;
            case Step.Ambush:
                title = "Засада!"; objective = "Победи тёмных магов";
                Marker(WorldBuilder.NatsaiPos); checkpoint = WorldBuilder.HogsmeadeCenter + new Vector3(-30, 0, 0);
                StartEncounter(
                    (EnemyKind.DarkWizard, WorldBuilder.NatsaiPos + new Vector3(16, 0, 2)),
                    (EnemyKind.DarkWizard, WorldBuilder.NatsaiPos + new Vector3(13, 0, -6)),
                    (EnemyKind.DarkWizard, WorldBuilder.NatsaiPos + new Vector3(-14, 0, -3)),
                    (EnemyKind.DarkWizard, WorldBuilder.NatsaiPos + new Vector3(22, 0, 4)));
                break;
            case Step.GoForest:
                title = "Запретный лес"; objective = "Найди древнюю реликвию в Запретном лесу (северо-запад)";
                Marker(WorldBuilder.ForestClearing); checkpoint = WorldBuilder.HogsmeadeCenter + new Vector3(-30, 0, 0);
                break;
            case Step.ForestFight:
                title = "Страж реликвии"; objective = "Победи обитателей леса";
                Marker(WorldBuilder.ForestClearing); checkpoint = WorldBuilder.ForestClearing + new Vector3(30, 0, -40);
                var c = WorldBuilder.ForestClearing;
                StartEncounter(
                    (EnemyKind.Spider, c + new Vector3(10, 0, 8)), (EnemyKind.Spider, c + new Vector3(-10, 0, 6)),
                    (EnemyKind.Spider, c + new Vector3(12, 0, -6)), (EnemyKind.Spider, c + new Vector3(-8, 0, -10)),
                    (EnemyKind.Spider, c + new Vector3(0, 0, 13)), (EnemyKind.Troll, c + new Vector3(0, 0, 6)));
                break;
            case Step.TakeRelic:
                title = "Реликвия"; objective = "Возьми реликвию с постамента";
                Marker(WorldBuilder.ForestClearing); relic.active = true;
                break;
            case Step.FigReveal:
                title = "Тайна Хранителей"; objective = "Встреться с профессором Фигом во дворе замка";
                var fig = Npc("fig");
                fig.transform.position = WorldBuilder.Courtyard + new Vector3(3, 0, 6);
                talkTarget = "fig"; Marker(fig.transform.position); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.Trials:
                title = "Испытания Хранителей"; objective = "Найди призраков Хранителей и пройди их испытания: у озера (восток и юг) и на западных холмах";
                Marker(Trials.I.MarkerPos()); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.FigVerdict:
                title = "Наследник Хранителей"; objective = "Вернись к профессору Фигу во дворе замка";
                talkTarget = "fig"; Marker(Npc("fig").transform.position); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.GoRuins:
                title = "Ранрок"; objective = "Отправляйся к древним руинам на севере";
                Marker(WorldBuilder.RuinsCenter); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.Boss:
                title = "Битва с Ранроком"; objective = "Победи Ранрока! Копи Древнюю магию [F]";
                Marker(WorldBuilder.RuinsCenter); checkpoint = WorldBuilder.RuinsCenter + new Vector3(0, 0.6f, -20);
                encounter.Clear();
                boss = Enemy.Spawn(EnemyKind.Boss, WorldBuilder.RuinsCenter + new Vector3(0, 1f, 6));
                boss.Died += OnBossDefeated;
                encounterTotal = 0;
                break;
            case Step.Aftermath: case Step.Archive: case Step.KitchenLetter: case Step.Convoy:
            case Step.Rescue: case Step.Seals: case Step.Rift: case Step.FinalReport:
                ConfigureExtension(s); break;
            case Step.Ch3Call: case Step.Werewolves: case Step.PoacherTrail: case Step.PoacherCamp: case Step.FreeBeasts:
            case Step.GoblinMine: case Step.Graveyard: case Step.FortressMarch: case Step.Rookwood: case Step.Epilogue:
                ConfigureCh3(s); break;
            case Step.Ch4Call: case Step.Ch4Diary: case Step.Ch4Hunt: case Step.Ch4Witness:
            case Step.Ch4Crypt: case Step.Ch4Bell: case Step.Ch4Warden: case Step.Ch4Return:
            case Step.Ch5Call: case Step.Ch5Gate: case Step.Ch5Shadows: case Step.Ch5Memory:
            case Step.Ch5Sentinel: case Step.Ch5Anchor: case Step.Ch5Procession: case Step.Ch5Sovereign: case Step.Ch5Return:
            case Step.Ch6Call: case Step.Ch6Manor: case Step.Ch6Letter: case Step.Ch6Quarry: case Step.Ch6Ice: case Step.Ch6Map: case Step.Ch6Mrax: case Step.Ch6Return:
                ConfigureHorror(s); break;
            case Step.Free:
                title = "Свободная прогулка"; objective = "Исследуй мир и собери страницы путеводителя";
                checkpoint = WorldBuilder.Courtyard;
                break;
        }
        if (announce)
        {
            int reward = s == Step.Free ? 2000 : 100 + (int)s * 25;
            P.inventory.AddGold(reward);
            HUD.ShowToast($"Награда: {reward} галлеонов", new Color(1f, 0.8f, 0.3f));
            SFX.Play("quest", 0.6f);
            HUD.ShowToast("Новое задание: " + title, new Color(1f, 0.85f, 0.45f));
        }
    }

    void StartEncounter(params (EnemyKind kind, Vector3 pos)[] list)
    {
        encounter.Clear();
        foreach (var (kind, pos) in list)
        {
            var e = Enemy.Spawn(kind, pos + Vector3.up * 0.3f);
            e.Died += _ => enemiesDefeated++;
            encounter.Add(e);
        }
        // Сложно и Кошмар добавляют врагов в каждую сюжетную схватку.
        int extra = list.Length > 0 ? Difficulty.Cur.extra : 0;
        for (int i = 0; i < extra; i++)
        {
            var src = list[i % list.Length];
            var k = src.kind == EnemyKind.Troll || src.kind == EnemyKind.Guardian || src.kind == EnemyKind.Necromancer || src.kind == EnemyKind.Berserker
                ? EnemyKind.DarkWizard : src.kind;
            var e = Enemy.Spawn(k, src.pos + new Vector3(-3f + i * 6f, 0.3f, -5f));
            e.Died += _ => enemiesDefeated++;
            encounter.Add(e);
        }
        encounterTotal = list.Length + extra;
    }

    int AliveCount()
    {
        encounter.RemoveAll(e => e == null || e.IsDead);
        return encounter.Count;
    }

    void Update()
    {
        if (!GameState.Playing) return;
        if (UpdateHorror()) return;
        playTime += Time.deltaTime;
        if (UpdateExtension()) return;
        if (UpdateCh3()) return;
        var pp = P.transform.position;
        switch (step)
        {
            case Step.Dummies:
                progress = $"{WorldBuilder.DummySpots.Length - dummies.Count}/{WorldBuilder.DummySpots.Length}";
                if (dummies.Count == 0)
                    Dialogue.Show(() => SetStep(Step.MeetSebastian),
                        new Line(Hecat, "Превосходно! Ты схватываешь на лету."),
                        new Line(Hecat, "Но манекены не дают сдачи. Себастьян Сэллоу ждёт тебя на дуэльной арене — к северу отсюда."));
                break;
            case Step.Rings:
                progress = $"{ringIndex}/{rings.Count}";
                if (!P.onBroom && !broomHintShown)
                {
                    broomHintShown = true;
                    HUD.ShowToast("Нажми B, чтобы сесть на метлу", new Color(0.8f, 0.9f, 1f));
                }
                if (ringIndex < rings.Count && (pp + Vector3.up - rings[ringIndex].transform.position).magnitude < 5.5f)
                {
                    rings[ringIndex].Pass();
                    ringIndex++;
                    SFX.Play("ring", 0.7f);
                    if (ringIndex < rings.Count)
                    {
                        rings[ringIndex].SetActive(true);
                        Marker(rings[ringIndex].transform.position);
                    }
                    else
                        Dialogue.Show(() => SetStep(Step.MeetNatsai),
                            new Line(Kog, "Блестяще! Метла теперь твоя — летай куда хочешь."),
                            new Line(Kog, "Кстати, из Хогсмида прислали сову: там неспокойно. Натсай Онай просит помощи."));
                }
                break;
            case Step.Ambush:
                progress = $"{encounterTotal - AliveCount()}/{encounterTotal}";
                if (AliveCount() == 0)
                    Dialogue.Show(() => { P.caster.Learn(SpellType.Confringo); SetStep(Step.GoForest); },
                        new Line(Nat, "Вот это да! Ты раскидал их как кегли."),
                        new Line(Nat, "У одного из них была карта... Они ищут реликвию в Запретном лесу. Для Ранрока."),
                        new Line(Nat, "Возьми эту книгу — там заклинание Конфринго. Взрывное. Тебе пригодится."));
                break;
            case Step.GoForest:
                if ((pp - WorldBuilder.ForestClearing).magnitude < 42f)
                {
                    HUD.ShowToast("Из чащи лезут акромантулы!", new Color(1f, 0.4f, 0.3f));
                    SetStep(Step.ForestFight, false);
                }
                break;
            case Step.ForestFight:
                progress = $"{encounterTotal - AliveCount()}/{encounterTotal}";
                if (AliveCount() == 0)
                {
                    HUD.ShowToast("Реликвия пробудилась!", new Color(0.4f, 0.85f, 1f));
                    SetStep(Step.TakeRelic);
                }
                break;
            case Step.TakeRelic:
                if ((pp + Vector3.up - relic.transform.position).magnitude < 3f)
                {
                    FX.Burst(relic.transform.position, new Color(0.4f, 0.85f, 1f), 100, 10f, 0.8f, 1f);
                    FX.Flash(relic.transform.position, new Color(0.4f, 0.85f, 1f), 8f, 20f, 0.8f);
                    SFX.Play("ancient", 0.8f);
                    relic.gameObject.SetActive(false);
                    Dialogue.Show(() =>
                        {
                            P.ancientUnlocked = true;
                            P.ancient = 100f;
                            HUD.ShowToast("Открыта Древняя магия! [F]", new Color(0.4f, 0.85f, 1f));
                            SetStep(Step.FigReveal);
                        },
                        new Line("Голос реликвии", "...наследник древней магии... ты видишь то, что скрыто от других..."),
                        new Line("Голос реликвии", "Ранрок жаждет нашей силы. Но сила без мудрости погубит тебя..."),
                        new Line(Fig, "(по связному зеркалу) Я чувствую это! Магия теперь в тебе. Возвращайся в замок, во двор — нам нужно поговорить."));
                }
                break;
            case Step.Trials:
                progress = Trials.I.Status();
                Marker(Trials.I.MarkerPos());
                break;
            case Step.GoRuins:
                if ((pp - WorldBuilder.RuinsCenter).magnitude < 45f)
                    Dialogue.Show(() => SetStep(Step.Boss),
                        new Line(Ran, "Волшебник... С реликвией, что принадлежит МНЕ."),
                        new Line(You, "Эта магия не для тебя, Ранрок."),
                        new Line(Ran, "Гоблины веками служили вам. Хватит! Сила будет моей!"));
                break;
            case Step.Free:
                progress = $"Страницы: {pages}/{TotalPages}";
                break;
        }
    }

    void OnDuelWon(Enemy _)
    {
        var seb = Npc("sebastian");
        seb.gameObject.SetActive(true);
        Dialogue.Show(() => { P.caster.Learn(SpellType.Depulso); SetStep(Step.MeetKogawa); },
            new Line(Seb, "Ладно, ладно, сдаюсь! Где ты так научился?"),
            new Line(Seb, "Держи — Депульсо. Отбрасывает врагов как пушинки."),
            new Line(Seb, "А теперь иди к мадам Когаве. С метлой ты будешь непобедим."));
    }

    void OnBossDefeated(Enemy _)
    {
        foreach (var e in Enemy.All.ToArray()) if (e != null && !e.IsDead) e.Kill();
        enemiesDefeated++;
        Dialogue.Show(() =>
            {
                SetStep(Step.Aftermath);
            },
            new Line(Ran, "Нет... Эта сила... должна была... стать моей..."),
            new Line(Fig, "Ты сделал это! Ранрок повержен, и древняя магия в безопасности."),
            new Line(Fig, "Хогвартс в долгу перед тобой. А теперь — отдыхай. Учебный год только начинается!"));
    }

    // Разговор с NPC (по E).
    public void Talk(NPC npc)
    {
        if (TalkExtension(npc)) return;
        if (TalkHorror(npc)) return;
        if (TalkCh3(npc)) return;
        if (npc.id.StartsWith("keeper_"))
        {
            Trials.I.Talk(npc);
            return;
        }
        if (npc.id != talkTarget && SideQuests.I.TryTalk(npc)) return;
        if (npc.id != talkTarget && npc.shopId != null)
        {
            if ((npc.shopId == "shady" || npc.shopId.StartsWith("dark")) && !shadyWarned)
            {
                shadyWarned = true;
                Dialogue.Show(() => ShopUI.Open(npc.shopId),
                    new Line(npc.displayName, "Ищешь то, чего не найдёшь в «Свитках и фолиантах»?"),
                    new Line(npc.displayName, "Империо. Круцио. Авада Кедавра. Непростительные, малыш. Азкабан — если узнают."),
                    new Line(npc.displayName, "Но Ранрок не играет по правилам... Так что смотри товар."));
                return;
            }
            ShopUI.Open(npc.shopId);
            return;
        }
        if (npc.id != talkTarget)
        {
            Dialogue.Show(null, new Line(npc.displayName, npc.RandomIdle()));
            return;
        }
        switch (step)
        {
            case Step.MeetFig:
                Dialogue.Show(() => SetStep(Step.Sorting),
                    new Line(Fig, "А вот и ты! Добро пожаловать в Хогвартс. Я профессор Элеазар Фиг."),
                    new Line(Fig, "Ты поступаешь сразу на пятый курс — редкость. Но в тебе есть кое-что особенное..."),
                    new Line(Fig, "Древняя магия. Её следы видят лишь единицы. И не только мы её ищем: гоблин Ранрок собирает армию тёмных магов."),
                    new Line(Fig, "Но обо всём позже. Сначала — распределение! Иди по мосту в замок, в Большой зал."));
                break;
            case Step.Sorting:
                Dialogue.Choice(Hat, "Хм-м... Интересно. Отвага, хитрость, ум, верность... Куда же тебя определить?\nВыбери факультет:",
                    Houses.Names, ChooseHouse);
                break;
            case Step.MeetHecat:
                Dialogue.Show(() =>
                    {
                        P.caster.LearnProtego();
                        P.caster.Learn(SpellType.Incendio);
                        P.caster.Learn(SpellType.Levioso);
                        SetStep(Step.Dummies);
                    },
                    new Line(Hecat, "Новенький? Отлично, проверим, на что ты годен."),
                    new Line(Hecat, "ЛКМ — базовое заклинание. Наведи камеру на цель — палочка сама подправит полёт."),
                    new Line(Hecat, "Инсендио [1] — огонь вблизи, поджигает. Левиосо [2] — подбрасывает врага, и он беззащитен."),
                    new Line(Hecat, "И главное — Протего [Q]. Держи щит, а если нажать вовремя — отразишь заклятие обратно."),
                    new Line(Hecat, "Ctrl — перекат. Красные атаки не блокируются, от них только уворачиваться! Вперёд, манекены ждут."));
                break;
            case Step.MeetSebastian:
                Dialogue.Show(() =>
                    {
                        npc.gameObject.SetActive(false);
                        var duelist = Enemy.Spawn(EnemyKind.Duelist, npc.transform.position + Vector3.up * 0.2f);
                        duelist.transform.rotation = npc.transform.rotation;
                        duelist.Died += OnDuelWon;
                        encounter.Clear();
                        encounter.Add(duelist);
                        encounterTotal = 1;
                        SetStep(Step.Duel);
                    },
                    new Line(Seb, "Себастьян Сэллоу, Слизерин. Говорят, ты новенький с талантом?"),
                    new Line(Seb, "Давай проверим. Дуэль до первой сдачи. Не волнуйся — я буду почти нежен."));
                break;
            case Step.MeetKogawa:
                Dialogue.Show(() =>
                    {
                        P.hasBroom = true;
                        P.inventory.GiveStarterBroom();
                        HUD.ShowToast("Получена метла! [B]", new Color(0.9f, 0.8f, 0.5f));
                        SetStep(Step.Rings);
                    },
                    new Line(Kog, "Ты на урок полётов? Опоздал на четыре года, но ладно."),
                    new Line(Kog, "Держи метлу. B — сесть или слезть. Мышь — направление, W — вперёд."),
                    new Line(Kog, "Пробел — вверх, Ctrl — вниз, Shift — ускорение. Пролети через все золотые кольца!"));
                break;
            case Step.MeetNatsai:
                Dialogue.Show(() => SetStep(Step.Ambush),
                    new Line(Nat, "Ты из Хогвартса? Слава Мерлину! Я Натсай. Тут шныряют люди Ранрока..."),
                    new Line(Nat, "Тихо! Слышишь? Это они — засада! Палочку к бою!"));
                break;
            case Step.FigReveal:
                Dialogue.Show(() => SetStep(Step.Trials),
                    new Line(Fig, "Ты справился с реликвией... Значит, пора открыть тебе правду."),
                    new Line(Fig, "Столетия назад древнюю магию берегли Хранители — пятеро профессоров Хогвартса."),
                    new Line(Fig, "Трое из них оставили свои портреты-призраки у мест силы: у озера и на западных холмах."),
                    new Line(Fig, "Ранрок не сможет подчинить магию, если Хранители признают тебя её наследником. Пройди их испытания."),
                    new Line(Fig, "И не забывай про мелкие дела: профессора Шарп, Ронен и Гарлик просили помощи — поручения дают силы и галлеоны."));
                break;
            case Step.FigVerdict:
                Dialogue.Show(() => SetStep(Step.GoRuins),
                    new Line(Fig, "Все трое признали тебя? Невероятно... Я видел это лишь в старых летописях."),
                    new Line(Fig, "Теперь древняя магия откликнется тебе в полную силу. А Ранрок..."),
                    new Line(Fig, "Мои источники говорят: он собирает силы у древних руин на севере. Пора положить этому конец."));
                break;
        }
    }

    public void OnTrialsComplete()
    {
        HUD.ShowToast("Все испытания Хранителей пройдены!", new Color(0.5f, 0.85f, 1f));
        SetStep(Step.FigVerdict);
    }

    void ChooseHouse(int h)
    {
        house = h;
        P.SetHouse(h);
        FX.Burst(P.transform.position + Vector3.up * 2, Houses.Main[h], 80, 8f, 0.6f, 1.2f, 0.3f);
        Dialogue.Show(() => SetStep(Step.MeetHecat),
            new Line(Hat, Houses.Names[h].ToUpper() + "!"),
            new Line(Fig, $"Поздравляю! {Houses.Names[h]} — отличный выбор."),
            new Line(Fig, "Первый урок — защита от тёмных искусств. Профессор Гекат ждёт на тренировочном дворе, за восточными воротами."));
    }

    // Возрождение на последнем чекпоинте. Если умер в дуэли — Себастьян тоже восстанавливается.
    public void Respawn()
    {
        if (step >= Step.Ambush) P.inventory.galleons = Mathf.Max(0, P.inventory.galleons - 10);
        if (step == Step.Duel)
        {
            foreach (var e in encounter) if (e != null) e.health.Revive();
        }
        P.Respawn(checkpoint + Vector3.up * 0.5f);
    }
}

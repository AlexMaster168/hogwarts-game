using System;
using System.Collections.Generic;
using UnityEngine;

public enum SQState { Locked, Available, Active, Ready, Done }

public class SideQuest
{
    public string id, giver, giverName, title, desc;
    public int need = 1, have;
    public SQState state = SQState.Locked;
    public QuestManager.Step unlockAt;
    public int gold;
    public (string item, int count)[] items = Array.Empty<(string, int)>();
    public SpellType? book;
    public Line[] offer, done;
    public string Progress => state == SQState.Ready ? $"готово — вернись: {giverName}" : need > 1 ? $"{Mathf.Min(have, need)}/{need}" : "";
}

// Побочные поручения профессоров и друзей.
public class SideQuests : MonoBehaviour
{
    public static SideQuests I;
    public readonly List<SideQuest> all = new();
    // Отслеживаемое поручение; null — отслеживается сюжет. Новые поручения фокус не перехватывают.
    public SideQuest tracked;
    public List<SideQuest> Open => all.FindAll(q => q.state == SQState.Active || q.state == SQState.Ready);

    public void Focus(SideQuest q)
    {
        tracked = q != null && (q.state == SQState.Active || q.state == SQState.Ready) ? q : null;
        SFX.Play("book", 0.4f);
        HUD.ShowToast("Отслеживается: " + (tracked != null ? tracked.title : QuestManager.I.title), tracked != null ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.83f, 0.45f));
    }

    // Z: сюжет → поручение 1 → поручение 2 → … → снова сюжет.
    public void CycleFocus()
    {
        var open = Open;
        int i = tracked == null ? -1 : open.IndexOf(tracked);
        Focus(i + 1 < open.Count ? open[i + 1] : null);
    }

    public static readonly Vector3 CryptPos = new(-45, 0, 150);

    static readonly Vector3[] HerbSpots =
    {
        new(-95, 0.4f, 118), new(-170, 0.4f, 118), new(-205, 0.4f, 165), new(-120, 0.4f, 238), new(-82, 0.4f, -55), new(-215, 0.4f, -55),
    };

    static readonly Vector3[] RaceRings =
    {
        new(100, 12, 140), new(140, 15, 168), new(178, 18, 130), new(168, 14, 85), new(128, 10, 72), new(96, 12, 102),
    };

    // Лунные статуэтки для смотрителя Муна: Хогсмид, руины и берег озера.
    public static readonly Vector3[] StatueSpots =
    {
        WorldBuilder.HogsmeadeCenter + new Vector3(4, 0.6f, 9), WorldBuilder.RuinsCenter + new Vector3(8, 0.6f, -4), WorldBuilder.LakeCenter + new Vector3(88, 0.6f, -6),
    };
    public readonly List<Pickup> statues = new();
    public readonly List<Pickup> herbs = new();
    public readonly List<BroomRing> race = new();
    public int raceIdx;
    public float raceTime;
    public bool raceRunning;
    public readonly List<Enemy> crypt = new();
    public void RestoreCompleted(SaveEntry[] completed)
    {
        foreach(var h in herbs)if(h!=null)Destroy(h.gameObject);herbs.Clear();
        foreach(var h in statues)if(h!=null)Destroy(h.gameObject);statues.Clear();
        race.Clear();crypt.Clear();raceRunning=false;tracked=null;
        foreach(var q in all)
        {
            q.have=0;q.state=SQState.Locked;
            if(System.Array.Exists(completed,x=>x.id==q.id)){q.have=q.need;q.state=SQState.Done;}
        }
    }

    static PlayerController P => GameManager.I.player;

    void Awake()
    {
        I = this;
        Define();
        Enemy.AnyDied += OnEnemyDied;
        Chest.Opened += OnChest;
    }

    void OnDestroy()
    {
        Enemy.AnyDied -= OnEnemyDied;
        Chest.Opened -= OnChest;
    }

    SideQuest Q(string id) => all.Find(q => q.id == id);

    void Add(string id, string giver, string giverName, string title, string desc, QuestManager.Step unlock, int need, int gold, Line[] offer, Line[] done,
        (string, int)[] items = null, SpellType? book = null)
    {
        all.Add(new SideQuest
        {
            id = id, giver = giver, giverName = giverName, title = title, desc = desc, unlockAt = unlock, need = need, gold = gold,
            offer = offer, done = done, items = items ?? Array.Empty<(string, int)>(), book = book,
        });
    }

    void Define()
    {
        const string Moon = "Смотритель Мун";
        Add("moon_alohomora", "moon", Moon, "Лунные статуэтки", "Найди 3 лунные статуэтки: в Хогсмиде, в руинах и на берегу озера", QuestManager.Step.MeetHecat, 3, 100,
            new[] { new Line(Moon, "Смотритель Мун. Слежу, чтобы студенты не совали нос куда не надо."),
                    new Line(Moon, "По округе стоят старые хранилища на замках. Открыть их может только Алохомора."),
                    new Line(Moon, "Я научу тебя, если вернёшь мои лунные статуэтки: одна в Хогсмиде, одна в руинах, одна на берегу озера.") },
            new[] { new Line(Moon, "Все три! Ну что ж, уговор есть уговор. Смотри: палочку к замку, чётко — «Алохомора»."),
                    new Line(Moon, "Хранилища отмечены на карте оранжевым. Только не говори директору, что это я тебя научил.") });
        const string Hec = "Профессор Гекат", Sh = "Профессор Шарп", Ga = "Профессор Гарлик", Ro = "Профессор Ронен", Ko = "Мадам Когава", Fi = "Профессор Фиг", Se = "Себастьян";
        Add("hecat_duel", "hecat", Hec, "Дуэльная практика", "Победи 8 тёмных магов", QuestManager.Step.Rings, 8, 200,
            new[] { new Line(Hec, "Манекены — это детский сад. Настоящая практика — тёмные маги в окрестностях."),
                    new Line(Hec, "Победи восьмерых — и я дам тебе кое-что полезное.") },
            new[] { new Line(Hec, "Восемь! Неплохо. Возьми эти фокусирующие зелья — пригодятся в настоящей дуэли.") },
            new[] { ("focus", 2) });
        Add("sharp_herbs", "sharp", Sh, "Ингредиенты для зелий", "Собери 6 лунных цветов у озера и в лесу", QuestManager.Step.MeetSebastian, 6, 150,
            new[] { new Line(Sh, "Профессор Эзоп Шарп, зельеварение. У меня кончились лунные цветы."),
                    new Line(Sh, "Они светятся по ночам у озера и на опушке Запретного леса. Принеси шесть.") },
            new[] { new Line(Sh, "Отличные экземпляры. Держи — Феликс Фелицис и пара отваров рябины. Не злоупотребляй удачей.") },
            new[] { ("felix", 1), ("rowan", 3) });
        Add("garlick_spiders", "garlick", Ga, "Нашествие акромантулов", "Уничтожь 8 акромантулов", QuestManager.Step.GoForest, 8, 180,
            new[] { new Line(Ga, "Мирабель Гарлик, травология. Акромантулы лезут к моим теплицам!"),
                    new Line(Ga, "Уничтожь восьмерых — в лесу и в лагерях их полно.") },
            new[] { new Line(Ga, "Теплицы спасены! Вот, вигенвельд из моих запасов.") },
            new[] { ("wiggenweld", 2) });
        Add("ronen_pages", "ronen", Ro, "Утерянные страницы", "Собери 6 страниц путеводителя", QuestManager.Step.MeetHecat, 6, 250,
            new[] { new Line(Ro, "Профессор Ронен, заклинания. Мой путеводитель по Хогвартсу разлетелся по всей округе!"),
                    new Line(Ro, "Страницы светятся золотом. Одна точно на крыше Астрономической башни... Собери шесть.") },
            new[] { new Line(Ro, "Шесть страниц! Мерлинова борода, ты меня спас. Держи награду.") });
        Add("kogawa_race", "kogawa", Ko, "Гонка на время", "Пролети 6 колец за 60 секунд", QuestManager.Step.MeetNatsai, 1, 300,
            new[] { new Line(Ko, "Думаешь, ты быстрый? Шесть колец вокруг поля. Шестьдесят секунд."),
                    new Line(Ko, "Таймер пойдёт, когда пролетишь первое. Не опозорь меня!") },
            new[] { new Line(Ko, "Ха! Вот это полёт! Держи призовые.") });
        Add("fig_chests", "fig", Fi, "Охота за сокровищами", "Открой 6 сундуков", QuestManager.Step.MeetHecat, 6, 150,
            new[] { new Line(Fi, "В округе спрятаны старые сундуки. Говорят, в некоторых — древние артефакты."),
                    new Line(Fi, "Открой шесть — и расскажи, что нашёл.") },
            new[] { new Line(Fi, "Любопытно... А это от меня — грозовые зелья. Осторожнее с ними.") },
            new[] { ("thunder", 2) });
        Add("seb_crypt", "sebastian", Se, "Склеп", "Зачисти склеп к северу от замка", QuestManager.Step.GoForest, 1, 300,
            new[] { new Line(Se, "Слушай... К северу от замка есть старый склеп. Там засели тёмные маги."),
                    new Line(Se, "Внутри — кое-что, что мне нужно. Поможешь? Я в долгу не останусь.") },
            new[] { new Line(Se, "Ты сделал это! Держи — книгу я нашёл в Запретной секции. Только никому ни слова.") },
            null, SpellType.Crucio);
    }

    // Возвращает true, если NPC — квестодатель и разговор состоялся.
    public bool TryTalk(NPC npc)
    {
        SideQuest pick = null;
        foreach (var st in new[] { SQState.Ready, SQState.Available, SQState.Active })
        {
            pick = all.Find(q => q.giver == npc.id && q.state == st);
            if (pick != null) break;
        }
        if (pick == null) return false;
        switch (pick.state)
        {
            case SQState.Ready:
                Dialogue.Show(() => Complete(pick), pick.done);
                break;
            case SQState.Available:
                Dialogue.Show(() => Accept(pick), pick.offer);
                break;
            default:
                Dialogue.Show(null, new Line(npc.displayName, $"Как успехи? {pick.desc}: {pick.Progress}"));
                break;
        }
        return true;
    }

    // Значок над головой квестодателя: "?" — есть поручение, "!" — можно сдать.
    public string Indicator(string npcId)
    {
        if (all.Exists(q => q.giver == npcId && q.state == SQState.Ready)) return "!";
        if (all.Exists(q => q.giver == npcId && q.state == SQState.Available)) return "?";
        return null;
    }

    void Accept(SideQuest q)
    {
        q.state = SQState.Active;
        SFX.Play("quest", 0.5f);
        HUD.ShowToast("Поручение: " + q.title + "  ·  [Z] — отслеживать", new Color(0.5f, 0.85f, 1f));
        switch (q.id)
        {
            case "sharp_herbs":
                foreach (var p in HerbSpots)
                    herbs.Add(Pickup.Spawn(p, new Color(0.7f, 0.6f, 1f), 0.4f, 2.2f, h => { herbs.Remove(h); Progress(Q("sharp_herbs"), 1); }, true));
                break;
            case "kogawa_race":
                SpawnRace();
                break;
            case "moon_alohomora":
                foreach (var p in StatueSpots)
                    statues.Add(Pickup.Spawn(p, new Color(0.75f, 0.85f, 1f), 0.5f, 2.2f, h => { statues.Remove(h); Progress(Q("moon_alohomora"), 1); }));
                break;
            case "seb_crypt":
                crypt.Clear();
                var kinds = new[] { EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.Spider, EnemyKind.Spider, EnemyKind.DarkWizard };
                for (int i = 0; i < kinds.Length; i++)
                {
                    // Перед входом в склеп (само здание — к северу от точки).
                    var off = new Vector3(-8f + i * 4f, 0.5f, -6f - (i % 2) * 4f);
                    var e = Enemy.Spawn(kinds[i], CryptPos + off, false);
                    if (i == 4)
                    {
                        e.health.Init(220);
                        e.targetable.displayName = "Главарь склепа";
                        e.goldDrop = 120;
                    }
                    crypt.Add(e);
                }
                break;
            case "ronen_pages":
                q.have = QuestManager.I.pages;
                if (q.have >= q.need) Ready(q);
                break;
        }
    }

    void Complete(SideQuest q)
    {
        q.state = SQState.Done;
        P.inventory.AddGold(q.gold);
        var rewards = new List<string> { q.gold + " галлеонов" };
        foreach (var (item, count) in q.items)
        {
            P.inventory.potions[item] = Mathf.Min(Inventory.MaxStack, P.inventory.potions[item] + count);
            rewards.Add($"{ItemDB.Get(item).name} x{count}");
        }
        if (q.book.HasValue)
        {
            if (P.caster.Knows(q.book.Value)) P.inventory.AddGold(300);
            else P.caster.Learn(q.book.Value);
        }
        if (q.id == "moon_alohomora")
        {
            P.alohomora = true;
            rewards.Add("заклинание Алохомора");
            SFX.Voice("Alohomora");
        }
        SFX.Play("victory", 0.7f);
        HUD.ShowToast($"Поручение выполнено: {q.title}! Награда: {string.Join(", ", rewards)}", new Color(0.5f, 1f, 0.6f));
        if (tracked == q) tracked = null;
    }

    void Progress(SideQuest q, int add)
    {
        if (q == null || q.state != SQState.Active) return;
        q.have += add;
        if (q.have >= q.need) Ready(q);
    }

    void Ready(SideQuest q)
    {
        q.have = q.need;
        q.state = SQState.Ready;
        SFX.Play("pickup", 0.6f);
        HUD.ShowToast($"«{q.title}» — выполнено! Вернись: {q.giverName}", new Color(0.5f, 1f, 0.6f));
    }

    void OnEnemyDied(Enemy e)
    {
        if (e.kind == EnemyKind.DarkWizard) Progress(Q("hecat_duel"), 1);
        if (e.kind == EnemyKind.Spider) Progress(Q("garlick_spiders"), 1);
    }

    void OnChest(Chest c) { Progress(Q("fig_chests"), 1); }

    void SpawnRace()
    {
        foreach (var r in race) if (r != null) Destroy(r.gameObject);
        race.Clear();
        for (int i = 0; i < RaceRings.Length; i++)
        {
            var next = RaceRings[(i + 1) % RaceRings.Length];
            var prev = RaceRings[(i + RaceRings.Length - 1) % RaceRings.Length];
            race.Add(BroomRing.Spawn(RaceRings[i], (next - prev).normalized));
        }
        race[0].SetActive(true);
        raceIdx = 0;
        raceTime = 0f;
        raceRunning = false;
    }

    void Update()
    {
        var qm = QuestManager.I;
        if (qm == null || !GameState.Playing) return;
        if (Input.GetKeyDown(KeyCode.Z)) CycleFocus();
        if (tracked != null && tracked.state != SQState.Active && tracked.state != SQState.Ready) tracked = null;
        foreach (var q in all)
            if (q.state == SQState.Locked && qm.step >= q.unlockAt)
                q.state = SQState.Available;

        var ronen = Q("ronen_pages");
        if (ronen.state == SQState.Active)
        {
            ronen.have = qm.pages;
            if (ronen.have >= ronen.need) Ready(ronen);
        }

        var cr = Q("seb_crypt");
        if (cr.state == SQState.Active && crypt.Count > 0)
        {
            crypt.RemoveAll(e => e == null || e.IsDead);
            if (crypt.Count == 0) Progress(cr, 1);
        }

        var rq = Q("kogawa_race");
        if (rq.state == SQState.Active && race.Count > 0)
        {
            if (raceRunning)
            {
                raceTime += Time.deltaTime;
                if (raceTime > 60f)
                {
                    HUD.ShowToast("Не успел! Гонка начнётся заново с первого кольца.", new Color(1f, 0.5f, 0.4f));
                    SpawnRace();
                    return;
                }
            }
            if (raceIdx < race.Count && race[raceIdx] != null &&
                (P.transform.position + Vector3.up - race[raceIdx].transform.position).magnitude < 5.5f)
            {
                race[raceIdx].Pass();
                raceIdx++;
                raceRunning = true;
                SFX.Play("ring", 0.7f);
                if (raceIdx < race.Count) race[raceIdx].SetActive(true);
                else
                {
                    race.Clear();
                    HUD.ShowToast($"Финиш: {raceTime:0.0} с!", new Color(1f, 0.85f, 0.4f));
                    Progress(rq, 1);
                }
            }
        }
    }

    // Маркер отслеживаемого поручения (синий ромб на HUD).
    public Vector3? TrackedMarker()
    {
        var q = tracked;
        if (q == null || q.state == SQState.Done) return null;
        if (q.state == SQState.Ready)
        {
            var n = QuestManager.I.Npc(q.giver);
            return n != null ? n.transform.position : null;
        }
        switch (q.id)
        {
            case "sharp_herbs":
                Vector3? best = null;
                float bd = float.MaxValue;
                foreach (var h in herbs)
                {
                    if (h == null) continue;
                    float d = (h.transform.position - P.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; best = h.transform.position; }
                }
                return best;
            case "kogawa_race": return raceIdx < race.Count && race[raceIdx] != null ? race[raceIdx].transform.position : null;
            case "seb_crypt": return CryptPos;
            case "hecat_duel": return Nearest(EnemyKind.DarkWizard) ?? WorldBuilder.ForestCenter;
            case "garlick_spiders": return Nearest(EnemyKind.Spider) ?? WorldBuilder.ForestCenter;
            case "ronen_pages":
            {
                Vector3? bp = null; float bd2 = float.MaxValue;
                foreach (var pg in FieldGuidePage.All)
                {
                    if (pg == null) continue;
                    float d = (pg.transform.position - P.transform.position).sqrMagnitude;
                    if (d < bd2) { bd2 = d; bp = pg.transform.position; }
                }
                return bp;
            }
            case "fig_chests":
            {
                Vector3? bc = null; float bd3 = float.MaxValue;
                foreach (var c in Chest.All)
                {
                    if (c == null || c.opened) continue;
                    float d = (c.transform.position - P.transform.position).sqrMagnitude;
                    if (d < bd3) { bd3 = d; bc = c.transform.position; }
                }
                return bc;
            }
        }
        return null;
    }

    static Vector3? Nearest(EnemyKind k)
    {
        Vector3? best = null; float bd = float.MaxValue;
        foreach (var e in Enemy.All)
        {
            if (e == null || e.IsDead || e.Charmed || e.kind != k) continue;
            float d = (e.transform.position - P.transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = e.transform.position; }
        }
        return best;
    }

    public string RaceTimer() => raceRunning && race.Count > 0 ? $"Гонка: {raceTime:0.0} / 60 с" : null;
}

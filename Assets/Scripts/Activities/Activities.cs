using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Побочные активности открытого мира: карточки волшебников, испытания Мерлина, фантастические звери,
// ловля снитча, Выручай-комната и Омут памяти. Журнал всего прогресса — клавиша O.
public class Activities : MonoBehaviour
{
    public static Activities I;

    public readonly HashSet<int> cards = new();
    public readonly HashSet<int> merlin = new();
    public readonly HashSet<int> beasts = new();
    public int snitchWins, snitchGames, rorRuns;
    public float rorBest;

    public readonly List<WizardCard> cardObjs = new();
    public readonly List<MerlinStone> stones = new();
    public readonly List<Beast> beastObjs = new();
    public SnitchGame snitch;
    public RoomChallenge room;

    public static readonly string[] CardNames =
    {
        "Мерлин", "Моргана", "Годрик Гриффиндор", "Салазар Слизерин", "Ровена Когтевран", "Пенелопа Пуффендуй",
        "Николас Фламель", "Ньют Скамандер", "Парацельс", "Клиодна", "Берти Боттс", "Гиффорд Оллертон",
    };
    public static readonly string[] CardHints =
    {
        "верхняя галерея библиотеки", "обсерватория", "трофейный зал", "кухня", "Выручай-комната", "оранжерея",
        "кабинет директора", "вершина Большой лестницы", "спальня", "улица Хогсмида", "поляна Запретного леса", "крыша надвратной башни",
    };
    public static readonly Vector3[] CardSpots =
    {
        new(-31, 7.1f, 72), new(-23, 25.1f, 93), new(-7.5f, 1.1f, 90), new(6, -5.7f, 70), new(34, 7.1f, 76), new(20, 1.1f, 143),
        new(17.5f, 7.1f, 41), new(12.5f, 22.2f, 176), new(21, 1.1f, 66), new(185, 1.3f, -150), new(-140, 1.3f, 200), new(0, 10f, -25),
    };
    public static readonly Vector3[] MerlinSites =
    {
        new(-60, 0, -100), new(125, 0, 10), new(-110, 0, 60), new(-200, 0, -100), new(60, 0, 190), new(250, 0, -80),
    };
    public static readonly string[] BeastNames = { "Нюхлер", "Пушишка", "Лунный телёнок" };
    public static readonly (int species, Vector3 home)[] BeastSpots =
    {
        (0, new(155, 0, -150)), (0, new(185, 0, -147)), (0, new(205, 0, -153)),
        (1, new(-18, 0, -8)), (1, new(18, 0, -8)), (1, new(14, 0, 16)),
        (2, new(-80, 0, 40)), (2, new(-95, 0, 55)), (2, new(-68, 0, 52)),
    };
    public static Vector3 SnitchStand => WorldBuilder.PitchCenter + new Vector3(0, 0, -30);

    bool journalOpen;
    Vector2 journalScroll;
    GUIStyle sTitle, sText, sHint, sBig;

    void Awake()
    {
        I = this;
        for (int i = 0; i < MerlinSites.Length; i++) stones.Add(MerlinStone.Spawn(i, MerlinSites[i]));
        BuildSnitchStand();
        Respawn();
    }

    // Пересоздаёт карточки и зверей по текущему прогрессу (новая игра или загрузка).
    public void Respawn()
    {
        foreach (var c in cardObjs) if (c != null) Destroy(c.gameObject);
        foreach (var b in beastObjs) if (b != null) Destroy(b.gameObject);
        cardObjs.Clear();
        beastObjs.Clear();
        for (int i = 0; i < CardSpots.Length; i++) if (!cards.Contains(i)) cardObjs.Add(WizardCard.Spawn(i, CardSpots[i]));
        for (int i = 0; i < BeastSpots.Length; i++) if (!beasts.Contains(i)) beastObjs.Add(Beast.Spawn(i, BeastSpots[i].species, BeastSpots[i].home));
        foreach (var s in stones) s.SetDone(merlin.Contains(s.index));
    }

    void BuildSnitchStand()
    {
        var p = SnitchStand;
        var g = new GameObject("SnitchStand").transform;
        g.SetParent(WorldBuilder.Dynamic, false);
        g.position = p;
        Prims.Cyl(g, Vector3.zero, 0.5f, 1.2f, MatLib.Solid(new Color(0.35f, 0.25f, 0.15f), 0.3f));
        Prims.Sphere(g, Vector3.up * 1.5f, 0.45f, MatLib.Glow(new Color(1f, 0.8f, 0.25f), 3f));
        var l = g.gameObject.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.3f);
        l.range = 6f;
        l.intensity = 1.5f;
    }

    // ---------------- прогресс ----------------
    public void CollectCard(int id)
    {
        if (!cards.Add(id)) return;
        var p = GameManager.I.player;
        p.inventory.AddGold(25);
        Progression.I.Gain(40);
        SFX.Play("pickup", 0.8f);
        HUD.ShowToast($"Карточка: {CardNames[id]} ({cards.Count}/{CardNames.Length})", new Color(0.85f, 0.65f, 1f));
        if (cards.Count == CardNames.Length)
        {
            p.inventory.AddGold(500);
            Progression.I.Gain(400);
            HUD.ShowToast("Вся коллекция карточек собрана! +500 галлеонов", new Color(1f, 0.85f, 0.4f));
        }
    }

    public void CompleteMerlin(int id)
    {
        if (!merlin.Add(id)) return;
        GameManager.I.player.inventory.AddGold(90);
        Progression.I.Gain(150);
        SFX.Play("quest", 0.7f);
        HUD.ShowToast($"Испытание Мерлина пройдено ({merlin.Count}/{MerlinSites.Length})", new Color(0.55f, 1f, 0.75f));
    }

    public void CatchBeast(Beast b)
    {
        if (b == null || !beasts.Add(b.index)) return;
        GameManager.I.player.inventory.AddGold(40);
        Progression.I.Gain(60);
        SFX.Play("pickup", 0.7f);
        int same = beasts.Count(i => BeastSpots[i].species == b.species);
        HUD.ShowToast($"Спасён зверь: {BeastNames[b.species]} ({same}/3) — в виварий", new Color(0.6f, 1f, 0.6f));
        FX.Burst(b.transform.position + Vector3.up * 0.5f, new Color(0.7f, 1f, 0.6f), 40, 4f, 0.4f, 0.8f, -0.5f);
        beastObjs.Remove(b);
        Destroy(b.gameObject);
    }

    // ---------------- взаимодействие ----------------
    static PlayerController P => GameManager.I.player;

    public static string Hint(PlayerController p)
    {
        if (I == null) return null;
        var pos = p.transform.position;
        if (I.room != null && I.room.Active) return null;
        if (I.snitch != null) return null;
        if ((pos - WorldBuilder.PensievePos).sqrMagnitude < 6.5f) return "[E] Заглянуть в Омут памяти";
        if ((pos - WorldBuilder.RoomOfRequirementPos).sqrMagnitude < 7f) return "[E] Испытание Выручай-комнаты";
        if ((pos - SnitchStand).sqrMagnitude < 9f) return p.hasBroom ? "[E] Ловля снитча против ловца Слизерина" : "Снитч: сначала получите метлу";
        foreach (var s in I.stones)
            if (!s.done && !s.running && (pos - s.transform.position).sqrMagnitude < 9f) return "[E] Начать испытание Мерлина: " + s.Title;
        foreach (var b in I.beastObjs)
            if (b != null && (pos - b.transform.position).sqrMagnitude < 7f) return "[E] Поймать: " + BeastNames[b.species];
        return null;
    }

    public static bool TryInteract(PlayerController p)
    {
        if (I == null || Hint(p) == null) return false;
        var pos = p.transform.position;
        if ((pos - WorldBuilder.PensievePos).sqrMagnitude < 6.5f) { I.UsePensieve(); return true; }
        if ((pos - WorldBuilder.RoomOfRequirementPos).sqrMagnitude < 7f) { I.StartRoom(); return true; }
        if ((pos - SnitchStand).sqrMagnitude < 9f) { if (p.hasBroom) I.StartSnitch(); return true; }
        foreach (var s in I.stones)
            if (!s.done && !s.running && (pos - s.transform.position).sqrMagnitude < 9f) { s.Begin(); return true; }
        foreach (var b in I.beastObjs.ToArray())
            if (b != null && (pos - b.transform.position).sqrMagnitude < 7f) { I.CatchBeast(b); return true; }
        return false;
    }

    public void UsePensieve()
    {
        var p = P;
        if (!p.caster.Knows(SpellType.Patronus))
        {
            Dialogue.Show(() =>
            {
                p.caster.Learn(SpellType.Patronus);
                Progression.I.Gain(120);
            },
            new Line("Омут памяти", "Серебристый туман затягивает вас в чужое воспоминание…"),
            new Line("Воспоминание", "Молодой профессор стоит у замёрзшего озера. Вокруг кружат дементоры, и тепло уходит из мира."),
            new Line("Воспоминание", "«Вспомни самое счастливое мгновение. Держись за него. Экспекто Патронум!»"),
            new Line("Воспоминание", "Серебряный олень вырывается из палочки, и тьма отступает."),
            new Line("Омут памяти", "Вы выучили Экспекто Патронум. Только он по-настоящему ранит дементоров."));
        }
        else
        {
            Dialogue.Show(null,
                new Line("Омут памяти", "В серебристой глади мелькают воспоминания директоров."),
                new Line("Омут памяти", "Ночью у озера кружат дементоры. Экспекто Патронум изгоняет их."));
        }
    }

    public void StartRoom()
    {
        if (room != null && room.Active) return;
        if (room == null) room = new GameObject("RoomChallenge").AddComponent<RoomChallenge>();
        room.Begin();
    }

    public void StartSnitch()
    {
        if (snitch != null) return;
        var p = P;
        if (!p.onBroom) p.ToggleBroom();
        snitch = new GameObject("SnitchGame").AddComponent<SnitchGame>();
        snitchGames++;
    }

    public void SnitchFinished(bool won)
    {
        snitch = null;
        if (won)
        {
            snitchWins++;
            P.inventory.AddGold(300);
            Progression.I.Gain(220);
            SFX.Play("victory", 0.7f);
            HUD.ShowToast("Снитч пойман! +300 галлеонов", new Color(1f, 0.85f, 0.3f));
        }
        else
        {
            Progression.I.Gain(40);
            HUD.ShowToast("Ловец Слизерина оказался быстрее…", new Color(0.6f, 0.9f, 0.6f));
        }
    }

    public void RoomFinished(float time)
    {
        rorRuns++;
        bool first = rorRuns == 1;
        P.inventory.AddGold(first ? 300 : 120);
        Progression.I.Gain(first ? 300 : 120);
        if (rorBest <= 0f || time < rorBest) rorBest = time;
        SFX.Play("victory", 0.6f);
        HUD.ShowToast($"Выручай-комната пройдена за {time:0} с! +{(first ? 300 : 120)} галлеонов", new Color(0.75f, 0.6f, 1f));
    }

    // ---------------- журнал и интерфейс ----------------
    void Update()
    {
        if (GameState.Playing && Input.GetKeyDown(KeyCode.O)) { journalOpen = true; GameState.Set(GameMode.Journal); }
        else if (GameState.Mode == GameMode.Journal && (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Escape))) CloseJournal();
    }

    public void OpenJournal() { journalOpen = true; GameState.Set(GameMode.Journal); }
    public void CloseJournal() { journalOpen = false; GameState.Set(GameMode.Play); }

    void Styles()
    {
        if (sText != null) return;
        sTitle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        sTitle.normal.textColor = new Color(1f, 0.83f, 0.45f);
        sText = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        sText.normal.textColor = new Color(0.9f, 0.9f, 0.95f);
        sHint = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sHint.normal.textColor = new Color(1f, 0.83f, 0.45f);
        sBig = new GUIStyle(sHint) { fontSize = 34 };
    }

    void Label(Rect r, string s, GUIStyle st)
    {
        var c = st.normal.textColor;
        st.normal.textColor = new Color(0, 0, 0, 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    void OnGUI()
    {
        Styles();
        if (GameState.Mode == GameMode.Journal) { DrawJournal(); return; }
        if (!GameState.Playing || GameManager.I == null || GameManager.I.player == null) return;
        float W = Screen.width, H = Screen.height;
        var hint = Hint(GameManager.I.player);
        if (hint != null && GameManager.I.player.nearbyNpc == null) Label(new Rect(0, H * 0.6f, W, 36), hint, sHint);
        string status = null;
        if (snitch != null) status = snitch.Status;
        else if (room != null && room.Active) status = room.Status;
        else foreach (var s in stones) if (s.running) status = s.Status;
        if (status != null) Label(new Rect(0, 108, W, 44), status, sBig);
    }

    void DrawJournal()
    {
        float W = Screen.width, H = Screen.height;
        GUI.color = new Color(0.03f, 0.03f, 0.06f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = Color.white;
        var r = new Rect(W * 0.12f, 40, W * 0.76f, H - 80);
        Label(new Rect(r.x, r.y, r.width, 40), "ЖУРНАЛ ПРИКЛЮЧЕНИЙ", sTitle);
        if (GUI.Button(new Rect(r.xMax - 140, r.y, 140, 34), "Закрыть [O]")) { CloseJournal(); return; }
        var p = GameManager.I.player;
        var prog = Progression.I;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Уровень {prog.level}  ·  опыт {prog.xp}/{prog.XpToNext}  ·  здоровье {p.health.max:0}  ·  бонус урона +{(Progression.DamageMult - 1f) * 100f:0}%");
        if (DayNight.I != null) sb.AppendLine(DayNight.I.ClockText());
        sb.AppendLine();
        sb.AppendLine($"КАРТОЧКИ ВОЛШЕБНИКОВ: {cards.Count}/{CardNames.Length}");
        for (int i = 0; i < CardNames.Length; i++)
            sb.AppendLine((cards.Contains(i) ? "  [x] " + CardNames[i] : "  [  ] ??? — " + CardHints[i]));
        sb.AppendLine();
        sb.AppendLine($"ИСПЫТАНИЯ МЕРЛИНА: {merlin.Count}/{MerlinSites.Length}  (каменные круги в окрестностях; на карте окрестностей — жёлтые метки)");
        sb.AppendLine();
        sb.AppendLine("ВИВАРИЙ:");
        for (int s = 0; s < BeastNames.Length; s++)
        {
            int got = beasts.Count(i => BeastSpots[i].species == s);
            string where = s == 0 ? "Хогсмид, тащат блестящее" : s == 1 ? "двор замка" : "луга у озера";
            sb.AppendLine($"  {BeastNames[s]}: {got}/3 — {where}");
        }
        sb.AppendLine();
        sb.AppendLine($"КВИДДИЧ: поймано снитчей {snitchWins} из {snitchGames} (стойка у поля)");
        sb.AppendLine(rorRuns > 0 ? $"ВЫРУЧАЙ-КОМНАТА: пройдена {rorRuns} раз, рекорд {rorBest:0} с" : "ВЫРУЧАЙ-КОМНАТА: ещё не пройдена (2 этаж восточного крыла)");
        sb.AppendLine(p.caster.Knows(SpellType.Patronus) ? "ПАТРОНУС: изучен" : "ПАТРОНУС: не изучен — загляните в Омут памяти в кабинете директора");
        var q = QuestManager.I;
        sb.AppendLine();
        sb.AppendLine($"Сюжет: {q.title}  ·  страниц путеводителя {q.pages}/{QuestManager.TotalPages}  ·  повержено врагов {q.enemiesDefeated}");
        var text = sb.ToString();
        var inner = new Rect(0, 0, r.width - 30, sText.CalcHeight(new GUIContent(text), r.width - 30) + 20);
        journalScroll = GUI.BeginScrollView(new Rect(r.x, r.y + 52, r.width, r.height - 60), journalScroll, inner);
        GUI.Label(inner, text, sText);
        GUI.EndScrollView();
    }

    // ---------------- сохранение ----------------
    public void Restore(int[] c, int[] m, int[] b, int wins, int games, int runs, float best)
    {
        cards.Clear(); merlin.Clear(); beasts.Clear();
        foreach (var x in c ?? new int[0]) if (x >= 0 && x < CardSpots.Length) cards.Add(x);
        foreach (var x in m ?? new int[0]) if (x >= 0 && x < MerlinSites.Length) merlin.Add(x);
        foreach (var x in b ?? new int[0]) if (x >= 0 && x < BeastSpots.Length) beasts.Add(x);
        snitchWins = wins; snitchGames = games; rorRuns = runs; rorBest = best;
        if (snitch != null) { Destroy(snitch.gameObject); snitch = null; }
        if (room != null) room.Abort(false);
        foreach (var s in stones) s.Abort();
        Respawn();
    }
}

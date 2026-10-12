using System.Collections.Generic;
using UnityEngine;

// Три испытания Хранителей: пламя (жаровни + Страж), небо (сферы на метле на время), стойкость (волны врагов).
public class Trials : MonoBehaviour
{
    public static Trials I;

    public static readonly string[] Ids = { "rackham", "bakar", "niamh" };
    public static readonly string[] Names = { "Испытание пламени", "Испытание неба", "Испытание стойкости" };
    public static readonly string[] Keepers = { "Хранитель Персиваль Рэкхем", "Хранитель Сан Бакар", "Хранительница Ниам Фицджеральд" };
    public static readonly Vector3[] Sites = { new(-80, 0, 40), new(-150, 0, -100), new(-250, 0, -80) };
    public static readonly Color[] Colors = { new(1f, 0.55f, 0.2f), new(0.4f, 0.8f, 1f), new(0.6f, 1f, 0.5f) };

    static readonly Vector3[] OrbSpots =
    {
        new(-120, 10, -60), new(-170, 16, -50), new(-195, 22, -10), new(-160, 14, 25), new(-118, 20, 15), new(-140, 28, -20),
    };

    public readonly bool[] done = new bool[3];
    public int active = -1;

    public readonly List<TrialBrazier> braziers = new();
    public Enemy guardian;
    bool guardianSpawned;

    public Pickup orb;
    public int orbIndex;
    public float timer;

    public int wave;
    public readonly List<Enemy> waveEnemies = new();

    static PlayerController P => GameManager.I.player;

    public int Count
    {
        get { int c = 0; foreach (var d in done) if (d) c++; return c; }
    }

    public bool AllDone => Count == 3;

    void Awake() { I = this; }

    void Start()
    {
        for (int i = 0; i < 5; i++)
        {
            var d = Quaternion.Euler(0, i * 72, 0) * Vector3.forward;
            braziers.Add(TrialBrazier.Spawn(Sites[0] + d * 8f + Vector3.up * 0.4f));
        }
    }

    public void Talk(NPC keeper)
    {
        int idx = System.Array.IndexOf(Ids, keeper.id.Replace("keeper_", ""));
        string who = keeper.displayName;
        if (QuestManager.I.step < QuestManager.Step.Trials)
        {
            Dialogue.Show(null, new Line(who, "Ещё не время, юный волшебник... Мы ждём того, кто несёт древнюю магию."));
            return;
        }
        if (done[idx])
        {
            Dialogue.Show(null, new Line(who, "Ты уже доказал своё право. Ступай к остальным Хранителям."));
            return;
        }
        if (active >= 0 && active != idx)
        {
            Dialogue.Show(null, new Line(who, "Ты уже проходишь другое испытание. Заверши его."));
            return;
        }
        if (active == idx)
        {
            Dialogue.Show(null, new Line(who, "Испытание идёт! Не теряй времени."));
            return;
        }
        switch (idx)
        {
            case 0:
                Dialogue.Show(() => Begin(0),
                    new Line(who, "Я — Персиваль Рэкхем. Древняя магия — это пламя. Неосторожный сгорит в нём."),
                    new Line(who, "Зажги пять жаровен огнём — Инсендио, Конфринго или Бомбардой. А потом встреться с моим Стражем."));
                break;
            case 1:
                Dialogue.Show(() => Begin(1),
                    new Line(who, "Сан Бакар. Я искала свободу в небе — и нашла её."),
                    new Line(who, "Шесть сфер света над озером. Собери их на метле, пока не истекло время. Вперёд!"));
                break;
            case 2:
                Dialogue.Show(() => Begin(2),
                    new Line(who, "Ниам Фицджеральд. Сила — ничто без стойкости."),
                    new Line(who, "Три волны врагов придут к этому кругу. Выстоишь — и я признаю тебя."));
                break;
        }
    }

    void Begin(int idx)
    {
        active = idx;
        SFX.Play("quest", 0.6f);
        HUD.ShowToast(Names[idx] + " началось!", Colors[idx]);
        switch (idx)
        {
            case 0:
                guardianSpawned = false;
                guardian = null;
                foreach (var b in braziers) b.Arm();
                break;
            case 1:
                orbIndex = 0;
                timer = 85f;
                if (!P.onBroom && P.hasBroom) HUD.ShowToast("Садись на метлу — [B]", new Color(0.8f, 0.9f, 1f));
                SpawnOrb();
                break;
            case 2:
                wave = 0;
                SpawnWave();
                break;
        }
    }

    void SpawnOrb()
    {
        orb = Pickup.Spawn(OrbSpots[orbIndex], Colors[1], 1.2f, 3.2f, _ =>
        {
            orbIndex++;
            timer += 4f;
            SFX.Play("ring", 0.7f);
            if (orbIndex >= OrbSpots.Length) Complete(1);
            else SpawnOrb();
        });
    }

    void SpawnWave()
    {
        waveEnemies.Clear();
        var c = Sites[2];
        EnemyKind[] kinds = wave switch
        {
            0 => new[] { EnemyKind.Spider, EnemyKind.Spider, EnemyKind.Spider, EnemyKind.Spider },
            1 => new[] { EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.DarkWizard },
            _ => new[] { EnemyKind.Guardian, EnemyKind.Spider, EnemyKind.Spider },
        };
        for (int i = 0; i < kinds.Length; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / kinds.Length + wave * 40, 0) * Vector3.forward;
            waveEnemies.Add(Enemy.Spawn(kinds[i], c + d * 14f + Vector3.up * 0.5f));
        }
        HUD.ShowToast($"Волна {wave + 1}/3", Colors[2]);
    }

    void Update()
    {
        if (!GameState.Playing || active < 0) return;
        switch (active)
        {
            case 0:
                if (!guardianSpawned)
                {
                    if (braziers.TrueForAll(b => b.lit))
                    {
                        guardianSpawned = true;
                        guardian = Enemy.Spawn(EnemyKind.Guardian, Sites[0] + Vector3.up * 0.5f);
                        GameManager.I.camRig.Shake(0.5f);
                        HUD.ShowToast("Страж Хранителей пробудился!", Colors[0]);
                    }
                }
                else if (guardian == null || guardian.IsDead) Complete(0);
                break;
            case 1:
                timer -= Time.deltaTime;
                if (timer <= 0f) Fail(1);
                break;
            case 2:
                waveEnemies.RemoveAll(e => e == null || e.IsDead);
                if (waveEnemies.Count == 0)
                {
                    wave++;
                    if (wave >= 3) Complete(2); else SpawnWave();
                }
                break;
        }
    }

    public string Status()
    {
        if (active < 0) return $"Пройдено: {Count}/3";
        switch (active)
        {
            case 0:
                if (guardianSpawned) return "Победи Стража Хранителей";
                int lit = 0;
                foreach (var b in braziers) if (b.lit) lit++;
                return $"Жаровни: {lit}/{braziers.Count}";
            case 1: return $"Сферы: {orbIndex}/{OrbSpots.Length} — осталось {Mathf.CeilToInt(timer)} с";
            default: return $"Волна {Mathf.Min(wave + 1, 3)}/3 — врагов: {waveEnemies.Count}";
        }
    }

    public Vector3 MarkerPos()
    {
        var pp = P.transform.position;
        switch (active)
        {
            case 0:
                if (guardianSpawned && guardian != null) return guardian.transform.position;
                foreach (var b in braziers) if (!b.lit) return b.transform.position;
                return Sites[0];
            case 1: return orb != null ? orb.transform.position : Sites[1];
            case 2: return Sites[2];
        }
        Vector3 best = Sites[0];
        float bd = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            if (done[i]) continue;
            float d = (Sites[i] - pp).sqrMagnitude;
            if (d < bd) { bd = d; best = Sites[i]; }
        }
        return best;
    }

    void Complete(int idx)
    {
        done[idx] = true;
        active = -1;
        FX.Burst(Sites[idx] + Vector3.up * 2, Colors[idx], 120, 10f, 0.8f, 1.2f, -0.3f);
        FX.Flash(Sites[idx] + Vector3.up * 3, Colors[idx], 8f, 25f, 0.8f);
        SFX.Play("victory", 0.7f);
        P.inventory.AddGold(150);
        P.AddAncient(100f);
        HUD.ShowToast($"{Names[idx]} пройдено! +150 галлеонов", Colors[idx]);
        Dialogue.Show(() => { if (AllDone) QuestManager.I.OnTrialsComplete(); },
            new Line(Keepers[idx], idx switch
            {
                0 => "Пламя не сожгло тебя. Ты достоин нести его.",
                1 => "Небо приняло тебя. Свобода — твоя.",
                _ => "Ты выстоял. Немногие могут сказать о себе такое.",
            }),
            new Line(Keepers[idx], AllDone ? "Все Хранители признали тебя. Возвращайся к профессору Фигу." : $"Осталось испытаний: {3 - Count}."));
    }

    void Fail(int idx)
    {
        active = -1;
        if (orb != null) Destroy(orb.gameObject);
        orb = null;
        SFX.Play("hurt", 0.6f);
        HUD.ShowToast("Испытание провалено. Поговори с Хранителем, чтобы попробовать снова.", new Color(1f, 0.5f, 0.4f));
    }
}

// Жаровня для испытания пламени: загорается от огненных заклинаний.
public class TrialBrazier : MonoBehaviour, ISpellTarget
{
    public bool lit, armed;
    Targetable target;
    Light glow;
    float hintCd;

    public static TrialBrazier Spawn(Vector3 pos)
    {
        var go = new GameObject("TrialBrazier");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        Prims.Cyl(go.transform, Vector3.zero, 0.6f, 1.2f, MatLib.Solid(new Color(0.3f, 0.28f, 0.27f), 0.2f));
        Prims.Cyl(go.transform, Vector3.up * 1.2f, 0.85f, 0.25f, MatLib.Solid(new Color(0.2f, 0.18f, 0.16f), 0.4f), false);
        var b = go.AddComponent<TrialBrazier>();
        var h = go.AddComponent<Health>();
        h.Init(1);
        b.target = go.AddComponent<Targetable>();
        b.target.health = h;
        b.target.displayName = "Жаровня";
        b.target.aimHeight = 1.4f;
        b.target.enabled = false;
        b.glow = go.AddComponent<Light>();
        b.glow.type = LightType.Point;
        b.glow.color = new Color(1f, 0.55f, 0.2f);
        b.glow.range = 9f;
        b.glow.intensity = 0f;
        return b;
    }

    public void Arm()
    {
        armed = true;
        if (!lit) target.enabled = true;
    }

    void Update() { if (hintCd > 0) hintCd -= Time.deltaTime; }

    public void OnSpellHit(SpellHit h)
    {
        if (!armed || lit) return;
        if (h.type == SpellType.Incendio || h.type == SpellType.Confringo || h.type == SpellType.Bombarda)
        {
            lit = true;
            target.enabled = false;
            glow.intensity = 3f;
            FX.Loop(transform, Vector3.up * 1.4f, new Color(1f, 0.5f, 0.15f), 35, 0.9f, 2.5f);
            FX.Burst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.2f), 40, 6f);
            SFX.Play("fire", 0.8f);
        }
        else if (hintCd <= 0f)
        {
            hintCd = 3f;
            HUD.ShowToast("Нужен огонь: Инсендио [1] или Конфринго!", new Color(1f, 0.6f, 0.3f));
        }
    }
}

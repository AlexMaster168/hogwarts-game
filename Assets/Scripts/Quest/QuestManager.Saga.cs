using System.Collections.Generic;
using UnityEngine;

// Главы VII–X: осколки атласа Хранителей. Порядок шагов сохраняется именами enum.
public partial class QuestManager
{
    public bool SagaActive => step >= Step.Ch7Call && step <= Step.Ch10Return;
    public bool sagaComplete;
    public readonly List<SagaRune> sagaRunes = new();
    readonly List<Enemy> sagaFoes = new();
    public int sagaOrder;
    public bool sagaRevealed;
    public float sagaDeadline;
    public int SagaChapter => SagaActive ? ((int)step - (int)Step.Ch7Call) / 7 : 0;
    public int SagaStage => SagaActive ? ((int)step - (int)Step.Ch7Call) % 7 : 0;
    static readonly string[] SagaTitles = { "Сад пепла", "Сердце часовщика", "Лица в зеркалах", "Последняя страница" };
    static readonly string[] SagaClues = {
        "Королева сжигает сад, чтобы прочесть проявляющиеся в пепле координаты. Вода возвращает саду память.",
        "Часовщик запер время в бронзовом сердце. Разрушь броню, но сохрани механизм: без него атлас рассыплется.",
        "Регент носит лица пропавших учеников. Свет выдаёт оригинал, а путы мешают теням занять его место.",
        "Архивариус хочет переписать историю Хогвартса. Четыре страницы — не оружие, а договор хранить чужие воспоминания." };
    public void ClearSaga()
    {
        foreach (var rune in sagaRunes) if (rune != null) Destroy(rune.gameObject);
        sagaRunes.Clear(); sagaOrder = 0; sagaRevealed = false; sagaDeadline = 0;
        foreach (var foe in sagaFoes) if (foe != null) Destroy(foe.gameObject);
        sagaFoes.Clear();
    }
    void ConfigureSaga(Step s)
    {
        int ch = SagaChapter, phase = SagaStage;
        var at = WorldBuilder.SagaPositions[ch];
        title = $"Глава {ch + 7}. {SagaTitles[ch]}";
        checkpoint = at + Vector3.back * 22;
        Marker(at);
        switch (phase)
        {
            case 0:
                talkTarget = "fig"; objective = "Обсуди с Фигом следующий осколок атласа";
                Marker(Npc("fig").transform.position); checkpoint = WorldBuilder.Courtyard; break;
            case 1:
                objective = "Доберись до места и победи стражу страницы";
                for (int i = 0; i < 3 + Difficulty.Cur.extra; i++)
                    SagaSpawn((EnemyKind)((int)EnemyKind.Ashling + ch), at + new Vector3(-7 + i * 4, 0.5f, 4));
                break;
            case 2: case 5:
                objective = phase == 2 ? "Восстанови три узла в указанном порядке заклинаний" : "Запечатай страницу: пройди цепь узлов в обратном порядке";
                var spells = PuzzleSpells(ch);
                for (int i = 0; i < 3; i++)
                {
                    int n = phase == 5 ? 2 - i : i;
                    var rune = SagaRune.Create(this, at + new Vector3(-8 + i * 8, 0.3f, -5), i, spells[n]);
                    sagaRunes.Add(rune);
                }
                break;
            case 3: objective = "Прочти память страницы у центрального пьедестала [E]"; break;
            case 4:
                objective = ch switch {
                    0 => "Королева пепла: туши щит Агуаменти и уходи из огненных меток",
                    1 => "Бронзовый часовщик: разбивай щит Редукто и избегай кругового удара",
                    2 => "Зеркальный регент: Люмос снимает щит; уничтожай временные копии",
                    _ => "Архивариус гроз: Инкарцеро разрывает проводящий щит; не стой в метках" };
                boss = SagaSpawn((EnemyKind)((int)EnemyKind.AshQueen + ch), at + Vector3.forward * 5 + Vector3.up * 0.5f);
                break;
            case 6:
                talkTarget = "fig"; objective = "Верни страницу профессору Фигу";
                Marker(Npc("fig").transform.position); checkpoint = WorldBuilder.Courtyard; break;
        }
    }
    public static SpellType[] PuzzleSpells(int chapter) => chapter switch
    {
        0 => new[] { SpellType.Aguamenti, SpellType.Lumos, SpellType.Reparo },
        1 => new[] { SpellType.Reducto, SpellType.Reparo, SpellType.Depulso },
        2 => new[] { SpellType.Lumos, SpellType.Rictusempra, SpellType.Incarcerous },
        _ => new[] { SpellType.Aguamenti, SpellType.Glacius, SpellType.Incarcerous }
    };
    Enemy SagaSpawn(EnemyKind kind, Vector3 at)
    {
        var e = Enemy.Spawn(kind, at, false);
        e.Died += _ => enemiesDefeated++;
        sagaFoes.Add(e); return e;
    }
    bool UpdateSaga()
    {
        if (!SagaActive) return false;
        playTime += Time.deltaTime;
        if (SagaStage == 1 || SagaStage == 4)
        {
            int alive = 0;
            foreach (var e in sagaFoes)
            {
                if (e == null || e.IsDead) continue;
                alive++;
                if ((P.transform.position - e.transform.position).sqrMagnitude < 1600) e.aggro = true;
            }
            progress = "Врагов: " + alive;
            if (alive == 0) AdvanceSaga();
        }
        else if (SagaStage == 2 || SagaStage == 5)
        {
            progress = $"Узлы: {sagaOrder}/3. Ошибка порядка сбрасывает цепь";
            if (SagaChapter == 1) progress += ". Ротор Депульсо: 3 попадания";
            if (SagaChapter == 2 && !sagaRevealed) progress += ". Сначала прояви надписи Люмосом";
            if (SagaChapter == 3 && sagaOrder > 0 && sagaOrder < 3)
            {
                progress += $". До разряда: {Mathf.CeilToInt(sagaDeadline - Time.time)} с";
                if (Time.time > sagaDeadline) ResetSagaRunes();
            }
            if (sagaOrder == 3) AdvanceSaga();
        }
        return true;
    }
    public void ResetSagaRunes()
    {
        sagaOrder = 0;
        foreach (var rune in sagaRunes) rune.ResetRune();
        HUD.ShowToast("Цепь сброшена: начни с первого узла", Color.yellow);
    }
    string SagaHint() => SagaStage == 3 && Vector3.Distance(P.transform.position, markerPos) < 4 ? "[E] Память страницы" : null;
    bool InteractSaga() { if (SagaHint() == null) return false; AdvanceSaga(); return true; }
    bool TalkSaga(NPC npc)
    {
        if (step == Step.Free && !sagaComplete && npc.id == "fig") { SetStep(Step.Ch7Call); return true; }
        if (!SagaActive || npc.id != talkTarget) return false;
        AdvanceSaga(); return true;
    }
    void AdvanceSaga()
    {
        int ch = SagaChapter, stage = SagaStage;
        Step next = step == Step.Ch10Return ? Step.Free : (Step)((int)step + 1);
        string text = stage switch {
            0 => $"Атлас открыл путь в место «{WorldBuilder.SagaNames[ch]}». Страница отвечает на заклинания, а не на грубую силу. Я научу тебя нужным чарам.",
            1 => "Стража отступила. Подписи узлов подскажут, какие чары нужны. Порядок начинается с первого узла.",
            2 => "Три узла связались. Прочти память у пьедестала, пока хозяин страницы не явился.",
            3 => SagaClues[ch],
            4 => "Хозяин страницы повержен. Теперь пройди цепь в обратном порядке: сила должна вернуться в атлас.",
            5 => "Страница запечатана. Верни её Фигу — без остальных она лишь обрывок чужой истории.",
            _ => ch == 3 ? "Атлас цел. Мы не будем менять прошлое: сохраним его, чтобы следующие ученики могли выбирать своё будущее. Хогвартс снова твой дом." : "Ещё одна страница дома. На полях проступил следующий адрес. Приходи: нам предстоит разобраться с его хранителем." };
        Dialogue.Show(() => {
            if (stage == 0)
            {
                foreach (var sp in PuzzleSpells(ch)) P.caster.Learn(sp);
                if (ch == 0) P.caster.Learn(SpellType.Aguamenti);
            }
            if (next == Step.Free) sagaComplete = true;
            SetStep(next);
            if (next == Step.Free) GameState.Set(GameMode.Victory);
        }, new Line(stage == 0 || stage == 6 ? Fig : "Атлас Хранителей", text));
    }
}

public class SagaRune : MonoBehaviour, ISpellTarget
{
    public QuestManager quest;
    public SpellType required;
    public int order;
    public bool lit;
    Renderer orb;
    TextMesh label;
    int charges;
    public static SagaRune Create(QuestManager q, Vector3 pos, int order, SpellType spell)
    {
        var go = new GameObject("Узел " + (order + 1)); go.transform.position = pos; go.transform.SetParent(WorldBuilder.Dynamic);
        var r = go.AddComponent<SagaRune>(); r.quest = q; r.required = spell; r.order = order;
        Prims.Cyl(go.transform, Vector3.zero, 0.7f, 1f, MatLib.Solid(Color.gray), false);
        r.orb = Prims.Sphere(go.transform, Vector3.up * 1.7f, 0.9f, MatLib.Glow(new Color(0.3f, 0.65f, 1f)), false).GetComponent<Renderer>();
        var collider = go.AddComponent<SphereCollider>(); collider.center = Vector3.up * 1.7f; collider.radius = 0.8f;
        var hp = go.AddComponent<Health>(); hp.Init(1); hp.invulnerable = true;
        var target = go.AddComponent<Targetable>(); target.health = hp; target.displayName = "Узел " + (order + 1); target.aimHeight = 1.7f;
        var text = new GameObject("Подсказка"); text.transform.SetParent(go.transform, false); text.transform.localPosition = Vector3.up * 3.2f;
        r.label = text.AddComponent<TextMesh>(); r.label.text = $"{order + 1}. {GameManager.I.player.caster.Slot(spell).name}"; r.label.fontSize = 48; r.label.characterSize = 0.065f; r.label.anchor = TextAnchor.MiddleCenter;
        return r;
    }
    void LateUpdate()
    {
        if (label == null || Camera.main == null) return;
        label.transform.rotation = Camera.main.transform.rotation;
        label.text = quest.SagaChapter == 2 && !quest.sagaRevealed ? (order + 1) + ". ??? — Люмос" : $"{order + 1}. {GameManager.I.player.caster.Slot(required).name}" + (quest.SagaChapter == 1 && required == SpellType.Depulso ? $" ({charges}/3)" : "");
    }
    public void OnSpellHit(SpellHit hit)
    {
        if (lit || !quest.SagaActive) return;
        if (quest.SagaChapter == 2 && !quest.sagaRevealed) { HUD.ShowToast("Зеркальные надписи скрыты: примени Люмос", Color.cyan); return; }
        if (hit.type != required || order != quest.sagaOrder)
        {
            quest.ResetSagaRunes(); return;
        }
        if (quest.SagaChapter == 1 && required == SpellType.Depulso && ++charges < 3)
        { transform.Rotate(0, 120, 0); FX.Shockwave(transform.position, Color.yellow, 2); return; }
        if (quest.sagaOrder == 0) quest.sagaDeadline = Time.time + 20;
        lit = true; quest.sagaOrder++; Paint(); SFX.Play("ancient", 0.4f);
    }
    public void ResetRune() { lit = false; charges = 0; Paint(); }
    void Paint() { if (orb != null) orb.sharedMaterial = MatLib.Glow(lit ? Color.green : new Color(0.3f, 0.65f, 1f)); }
}

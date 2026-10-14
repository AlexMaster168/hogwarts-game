using System.Collections.Generic;
using UnityEngine;

// Главы IV–VI: «Колокол безмолвных», «По ту сторону Завесы» и «Камень и лёд».
// Этапы трёх видов: разговор с NPC, улика у метки [E] и схватка на локации.
public partial class QuestManager
{
    public bool HorrorActive => step >= Step.Ch4Call && step <= Step.Ch6Return;
    public bool horrorComplete;
    public static readonly Vector3 Chapel = new(-262, 0.3f, 150);
    public static readonly Vector3 Veil = new(50, 0.3f, 305);
    const string Echo = "Эхо Завесы", Elian = "Элиан", Mrax = "Исольда Мракс";
    readonly List<Enemy> horrorEnemies = new();
    GameObject horrorClue;
    bool horrorBattle;

    // Где проходит этап: часовня (IV), Завеса (V) или одна из локаций главы VI.
    static Vector3 HorrorSite(Step s) => s switch
    {
        <= Step.Ch4Return => Chapel,
        <= Step.Ch5Return => Veil,
        Step.Ch6Manor or Step.Ch6Letter => WorldBuilder.ManorPos,
        Step.Ch6Quarry => WorldBuilder.QuarryPos,
        Step.Ch6Ice or Step.Ch6Map => WorldBuilder.IceCavePos,
        _ => WorldBuilder.PeakPos,
    };

    void ClearHorrorEncounter()
    {
        foreach (var e in horrorEnemies) if (e != null) Destroy(e.gameObject);
        horrorEnemies.Clear();
        if (horrorClue != null) Destroy(horrorClue);
        horrorBattle = false;
    }

    void ConfigureHorror(Step s)
    {
        Vector3 at = HorrorSite(s);
        checkpoint = at + Vector3.back * 24;
        Marker(at);
        var dn = DayNight.I;
        switch (s)
        {
            case Step.Ch4Call:
                title = "Глава 4. Колокол безмолвных"; objective = "Поговори с Гекат о пропавшей экспедиции"; talkTarget = "hecat"; break;
            case Step.Ch4Diary:
                title = "Последняя запись"; objective = "Прочти дневник у западной часовни [E]";
                dn?.SetHour(23f); break;
            case Step.Ch4Hunt:
                title = "Голодные силуэты"; objective = "Отбей нападение могильных гончих";
                HorrorFight(at, EnemyKind.GraveHound, EnemyKind.GraveHound, EnemyKind.GraveHound, EnemyKind.Inferius); break;
            case Step.Ch4Witness:
                title = "Голос выжившей"; objective = "Найди запись исследовательницы на южной стороне часовни [E]"; Marker(at + Vector3.back * 12); break;
            case Step.Ch4Crypt:
                title = "Хор под землёй"; objective = "Уничтожь костяных певчих, питающих колокол";
                HorrorFight(at, EnemyKind.Chorister, EnemyKind.Chorister, EnemyKind.Inferius, EnemyKind.Inferius); break;
            case Step.Ch4Bell:
                title = "Имя на бронзе"; objective = "Сорви погребальную печать с колокола [E]"; Marker(at + new Vector3(0, 0, 9)); break;
            case Step.Ch4Warden:
                title = "Звонарь пустоты"; objective = "Победи Звонаря. Круг под ногами предупреждает об ударе, на 65% придут гончие";
                HorrorBoss(at, EnemyKind.Bellringer); break;
            case Step.Ch4Return:
                title = "Цена тишины"; objective = "Отнеси осколок колокола Фигу"; talkTarget = "fig"; break;
            case Step.Ch5Call:
                title = "Глава 5. По ту сторону Завесы"; objective = "Обсуди с Гекат, как закрыть разлом"; talkTarget = "hecat"; break;
            case Step.Ch5Gate:
                title = "Порог забвения"; objective = "Активируй осколок у северного разлома [E]";
                dn?.SetHour(1.5f); break;
            case Step.Ch5Shadows:
                title = "Те, кто не вернулся"; objective = "Победи плакальщиц и гончих по ту сторону разлома. Патронус им страшен";
                HorrorFight(at, EnemyKind.Mourner, EnemyKind.Mourner, EnemyKind.GraveHound); break;
            case Step.Ch5Memory:
                title = "Чужое воспоминание"; objective = "Прочти память первого Хранителя [E]"; Marker(at + Vector3.left * 12); break;
            case Step.Ch5Sentinel:
                title = "Слепой привратник"; objective = "Победи привратника. Следи за меткой под ногами";
                HorrorBoss(at, EnemyKind.Gatekeeper); break;
            case Step.Ch5Anchor:
                title = "Последний якорь"; objective = "Разорви связь колокола с Завесой [E]"; Marker(at + Vector3.right * 12); break;
            case Step.Ch5Procession:
                title = "Погребальное шествие"; objective = "Останови последнюю стражу Безымянного";
                HorrorFight(at, EnemyKind.Chorister, EnemyKind.Mourner, EnemyKind.GraveHound, EnemyKind.GraveHound); break;
            case Step.Ch5Sovereign:
                title = "Безымянный король"; objective = "Победи хозяина Завесы. На 65% и 30% здоровья он зовёт стражу";
                HorrorBoss(at, EnemyKind.NamelessKing); break;
            case Step.Ch5Return:
                title = "Имена вместо эха"; objective = "Вернись к Фигу и расскажи о судьбе экспедиции"; talkTarget = "fig"; break;
            case Step.Ch6Call:
                title = "Глава 6. Камень и лёд"; objective = "Гекат зовёт: из хранилища Хранителей украли осколок колокола"; talkTarget = "hecat"; break;
            case Step.Ch6Manor:
                title = "Поместье Мракс"; objective = "Ночью проберись в поместье на западе и разгони вампиров";
                dn?.SetHour(22f);
                HorrorFight(at, EnemyKind.Vampire, EnemyKind.Vampire, EnemyKind.Vampire, EnemyKind.DarkWizard); break;
            case Step.Ch6Letter:
                title = "Письмо хозяйки"; objective = "Найди бумаги Исольды Мракс у крыльца поместья [E]"; Marker(at + new Vector3(0, 0, 7)); break;
            case Step.Ch6Quarry:
                title = "Каменоломня"; objective = "Останови каменных големов в каменоломне на юге. Их берут только Бомбарда, Конфринго и Депульсо";
                dn?.SetHour(13f);
                HorrorFight(at, EnemyKind.Golem, EnemyKind.Golem, EnemyKind.Goblin, EnemyKind.Goblin); break;
            case Step.Ch6Ice:
                title = "Ледяная пещера"; objective = "Пробейся через ледяных призраков у пещеры на юго-западе. Огонь им вдвое страшнее";
                dn?.SetHour(9f);
                HorrorFight(at, EnemyKind.IceWraith, EnemyKind.IceWraith, EnemyKind.IceWraith, EnemyKind.Vampire); break;
            case Step.Ch6Map:
                title = "Карта в инее"; objective = "Забери карту Мракс у входа в пещеру [E]"; Marker(at + new Vector3(0, 0, 8)); break;
            case Step.Ch6Mrax:
                title = "Исольда Мракс"; objective = "Победи Мракс на Утёсе ветров. Пока стоят её големы, она под каменным щитом";
                dn?.SetHour(17f);
                HorrorBoss(at, EnemyKind.Mrax); break;
            case Step.Ch6Return:
                title = "Рассвет над Хогвартсом"; objective = "Верни осколок профессору Фигу"; talkTarget = "fig"; break;
        }
        if (talkTarget != null) { Marker(Npc(talkTarget).transform.position); checkpoint = WorldBuilder.Courtyard; }
        else if (!horrorBattle)
        {
            var glow = s >= Step.Ch6Call ? new Color(0.45f, 0.75f, 1f) : new Color(0.55f, 0.15f, 0.8f);
            horrorClue = Prims.Sphere(WorldBuilder.Dynamic, markerPos + Vector3.up, 0.7f, MatLib.Glow(glow));
            horrorClue.name = "Улика";
        }
    }

    Enemy HorrorSpawn(EnemyKind k, Vector3 pos, bool haunted)
    {
        var e = Enemy.Spawn(k, pos, false);
        if (haunted) e.gameObject.AddComponent<HorrorEnemy>().Initialize(e.IsBossKind, k == EnemyKind.NamelessKing, horrorEnemies);
        e.Died += _ => enemiesDefeated++;
        horrorEnemies.Add(e);
        encounter.Add(e);
        return e;
    }

    // Обычная схватка: сложность добавляет лёгких врагов (не големов и не певчих).
    void HorrorFight(Vector3 at, params EnemyKind[] kinds)
    {
        horrorBattle = true; encounter.Clear();
        bool haunted = step < Step.Ch6Call;
        int extra = Difficulty.Cur.extra;
        for (int i = 0; i < kinds.Length + extra; i++)
        {
            var k = kinds[i % kinds.Length];
            if (i >= kinds.Length && (k == EnemyKind.Golem || k == EnemyKind.Chorister)) k = haunted ? EnemyKind.GraveHound : EnemyKind.DarkWizard;
            HorrorSpawn(k, at + new Vector3((i % 3 - 1) * 7, 0, 5 + (i / 3) * 6), haunted);
        }
        encounterTotal = encounter.Count;
    }

    void HorrorBoss(Vector3 at, EnemyKind kind)
    {
        horrorBattle = true; encounter.Clear();
        boss = HorrorSpawn(kind, at + new Vector3(0, 0, 6), kind != EnemyKind.Mrax);
        // Смерть босса уносит его призванных слуг.
        boss.Died += b => { foreach (var e in Enemy.All.ToArray()) if (e != null && e != b && !e.IsDead && (e.transform.position - at).magnitude < 70f) e.Kill(); };
        encounterTotal = 1;
    }

    bool UpdateHorror()
    {
        if (!HorrorActive) return false;
        playTime += Time.deltaTime;
        if (horrorBattle)
        {
            int live = 0;
            foreach (var e in horrorEnemies) if (e != null && !e.IsDead) live++;
            progress = boss != null && !boss.IsDead && encounterTotal == 1 ? $"{boss.targetable.displayName}: {Mathf.CeilToInt(boss.health.Frac * 100)}%" : $"Осталось врагов: {live}";
            if (live == 0) { horrorBattle = false; AdvanceHorror(); }
        }
        return true;
    }

    string HorrorHint() => !horrorBattle && talkTarget == null && Vector3.Distance(P.transform.position, markerPos) < 3.2f ? "[E] Изучить" : null;
    bool InteractHorror() { if (HorrorHint() == null) return false; AdvanceHorror(); return true; }

    bool TalkHorror(NPC npc)
    {
        // Старые завершённые сохранения тоже могут начать продолжение.
        if (step == Step.Free && !horrorComplete && npc.id == "hecat") { SetStep(Step.Ch4Call); return true; }
        if (!HorrorActive || npc.id != talkTarget) return false;
        AdvanceHorror(); return true;
    }

    void AdvanceHorror()
    {
        var next = step == Step.Ch6Return ? Step.Free : (Step)((int)step + 1);
        Line[] lines = step switch
        {
            Step.Ch4Call => new[] { new Line(Hecat, "Экспедиция к западной часовне слышала колокол, которого нет ни на одной карте. Никто не вернулся."),
                                    new Line(Hecat, "У часовни остался их дневник. И что бы ты ни услышал там ночью — не верь голосам знакомых.") },
            Step.Ch4Diary => new[] { new Line("Дневник экспедиции", "Мы нашли имена погибших на внутренней стороне колокола. Ночью одно имя исчезло."),
                                     new Line("Дневник экспедиции", "Утром исчез и человек, помнивший его. Мы слышим лай под землёй…") },
            Step.Ch4Hunt => new[] { new Line(You, "Эти гончие охраняют не могилы, а чужие воспоминания. На южной стене что-то светится.") },
            Step.Ch4Witness => new[] { new Line(Elian, "Меня зовут Элиан. Звонарь украл имена моих товарищей."),
                                       new Line(Elian, "Сначала заставь замолчать певчих в склепе — иначе колокол вернёт их к жизни.") },
            Step.Ch4Crypt => new[] { new Line(You, "Хор умолк. Теперь можно снять печать с колокола, но его хозяин это услышит.") },
            Step.Ch4Bell => new[] { new Line("Звонарь пустоты", "Ты зовёшь это спасением? За каждым возвращённым именем приду я!") },
            Step.Ch4Warden => new[] { new Line(Elian, "В осколке колокола осталась наша память. Отнеси его Фигу — он прочтёт его, не разбив.") },
            Step.Ch4Return => new[] { new Line(Fig, "Звонарь был лишь слугой. За Завесой Безымянный король собирает чужие воспоминания, чтобы обрести тело."),
                                      new Line(Fig, "Поговори с Гекат. У неё есть план, и он мне не нравится.") },
            Step.Ch5Call => new[] { new Line(Hecat, "Осколок откроет северный разлом. Найди память Хранителя, победи привратника и разорви якорь."),
                                    new Line(Hecat, "Против плакальщиц бери Патронус: обычные чары им почти не страшны.") },
            Step.Ch5Gate => new[] { new Line(Echo, "За разломом знакомые голоса зовут из темноты. Ты держишься за собственное имя и делаешь шаг вперёд.") },
            Step.Ch5Shadows => new[] { new Line(You, "Тени расступились. Слева горит чьё-то воспоминание.") },
            Step.Ch5Memory => new[] { new Line("Первый Хранитель", "Я спрятал имена в якоре. Пока он цел, король возвращается."),
                                      new Line("Первый Хранитель", "Привратник слеп, но слышит шаги. Смотри на землю перед его ударом.") },
            Step.Ch5Sentinel => new[] { new Line(You, "Привратник пал. Справа остался последний якорь.") },
            Step.Ch5Anchor => new[] { new Line(Echo, "Имена возвращаются к пленникам. Последняя стража идёт на звук — удержи проход, пока души уходят.") },
            Step.Ch5Procession => new[] { new Line("Безымянный король", "Я помню страх каждого из вас. Но твоё имя всё ещё ускользает. Подойди — я заберу его сам.") },
            Step.Ch5Sovereign => new[] { new Line(Elian, "Король рассыпался эхом. Я снова помню имена товарищей. Их больше не держит Завеса."),
                                         new Line(Elian, "Пора домой. Фиг должен узнать, чем всё закончилось.") },
            Step.Ch5Return => new[] { new Line(Fig, "Мы запишем имена экспедиции в школьную летопись. Ты вернул им память."),
                                      new Line(Fig, "Но Гекат срочно тебя ищет. Похоже, кто-то воспользовался тем, что мы были за Завесой.") },
            Step.Ch6Call => new[] { new Line(Hecat, "Пока ты был за Завесой, кто-то вскрыл хранилище Хранителей и вынес осколок колокола."),
                                    new Line(Hecat, "Следы ведут в старое поместье Мракс на западе. Говорят, там завелись вампиры. Возьми огонь: они его боятся.") },
            Step.Ch6Manor => new[] { new Line(You, "Вампиры охраняли дом для хозяйки. У крыльца лежат бумаги с гербом Мракс.") },
            Step.Ch6Letter => new[] { new Line(Mrax, "«Осколок колокола — ключ к сердцу горы. Каменные големы в южной каменоломне уже просыпаются…"),
                                      new Line(Mrax, "…а ледяные призраки у пещеры удержат дорогу. Встретимся на Утёсе ветров. И. М.»") },
            Step.Ch6Quarry => new[] { new Line(Nat, "Големов будили руны Мракс. Обычные чары их почти не берут — только взрывы."),
                                      new Line(Nat, "Дальше ледяная пещера на юго-западе. Возьми Инсендио и Конфринго.") },
            Step.Ch6Ice => new[] { new Line(You, "Призраки рассыпались инеем. У входа в пещеру кто-то обронил карту.") },
            Step.Ch6Map => new[] { new Line("Карта Мракс", "На карте отмечен Утёс ветров к востоку от Хогсмида. Подпись: «Здесь осколок станет короной»."),
                                   new Line(You, "Она хочет вплавить осколок в скалу и подчинить себе всех големов в горах. Нельзя этого допустить.") },
            Step.Ch6Mrax => new[] { new Line(Mrax, "Камень… треснул… Мой род веками ждал этого трона…"),
                                    new Line(You, "Трон из чужих имён и украденной магии? Пусть ждёт дальше. Осколок возвращается в Хогвартс.") },
            _ => new[] { new Line(Fig, "Осколок снова в хранилище, имена экспедиции записаны, а Утёс ветров затих."),
                         new Line(Fig, "Шесть глав, шесть побед. Ты прошёл путь от новичка до хранителя Хогвартса. Теперь отдыхай по-настоящему.") },
        };
        Dialogue.Show(() =>
        {
            if (next == Step.Free) horrorComplete = true;
            SetStep(next);
            if (next == Step.Free) GameState.Set(GameMode.Victory);
        }, lines);
    }
}

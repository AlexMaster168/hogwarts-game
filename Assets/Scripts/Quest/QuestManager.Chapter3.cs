using UnityEngine;

// Глава 3 «Охота Руквуда»: оборотни у Хогсмида, браконьеры и клетки с волшебными зверями,
// гоблинская шахта, некромант у склепа и крепость Виктора Руквуда.
public partial class QuestManager
{
    const string Rook = "Виктор Руквуд", Poach = "Браконьер", Necro = "Некромант";
    public int cagesOpened;
    public bool Chapter3Active => step >= Step.Ch3Call && step <= Step.Epilogue;

    void ConfigureCh3(Step s)
    {
        checkpoint = WorldBuilder.Courtyard;
        switch (s)
        {
            case Step.Ch3Call:
                title = "Глава 3. Охота Руквуда"; objective = "Профессор Гекат срочно зовёт тебя на тренировочный двор";
                talkTarget = "hecat"; Marker(WorldBuilder.HecatPos);
                break;
            case Step.Werewolves:
                title = "Вой у Хогсмида"; objective = "Ночью оборотни нападают на путников у дороги в Хогсмид. Останови их";
                if (DayNight.I != null) DayNight.I.SetHour(22.5f);
                Marker(WorldBuilder.WerewolfPos); checkpoint = WorldBuilder.WerewolfPos + Vector3.back * 25;
                StartEncounter((EnemyKind.Werewolf, WorldBuilder.WerewolfPos + new Vector3(-6, 0, 4)), (EnemyKind.Werewolf, WorldBuilder.WerewolfPos + new Vector3(6, 0, 4)),
                    (EnemyKind.Werewolf, WorldBuilder.WerewolfPos + new Vector3(0, 0, 9)), (EnemyKind.DarkWizard, WorldBuilder.WerewolfPos + new Vector3(0, 0, 14)));
                break;
            case Step.PoacherTrail:
                title = "Следы браконьеров"; objective = "Иди по следам на юго-запад, к лагерю браконьеров за озером";
                Marker(WorldBuilder.PoacherCampPos + new Vector3(0, 0, 20)); checkpoint = WorldBuilder.Courtyard;
                break;
            case Step.PoacherCamp:
                title = "Лагерь браконьеров"; objective = "Разгони браконьеров Руквуда";
                Marker(WorldBuilder.PoacherCampPos); checkpoint = WorldBuilder.PoacherCampPos + new Vector3(0, 0, 30);
                StartEncounter((EnemyKind.Poacher, WorldBuilder.PoacherCampPos + new Vector3(-8, 0, 4)), (EnemyKind.Poacher, WorldBuilder.PoacherCampPos + new Vector3(8, 0, 4)),
                    (EnemyKind.Poacher, WorldBuilder.PoacherCampPos + new Vector3(0, 0, 10)), (EnemyKind.Berserker, WorldBuilder.PoacherCampPos + new Vector3(0, 0, -4)),
                    (EnemyKind.Troll, WorldBuilder.PoacherCampPos + new Vector3(-10, 0, -8)));
                break;
            case Step.FreeBeasts:
                title = "Клетки"; objective = "Открой клетки и выпусти пойманных зверей [E]";
                cagesOpened = 0;
                foreach (var d in WorldBuilder.CageDoors) if (d != null) d.SetActive(true);
                Marker(WorldBuilder.CageSpots[0] + new Vector3(0, 0, 1.8f)); checkpoint = WorldBuilder.PoacherCampPos + new Vector3(0, 0, 20);
                break;
            case Step.GoblinMine:
                title = "Шахта Ранрока"; objective = "Последователи Ранрока прячут добычу Руквуда в шахте на востоке. Зачисти вход";
                Marker(WorldBuilder.MinePos); checkpoint = WorldBuilder.MinePos + new Vector3(0, 0, -30);
                StartEncounter((EnemyKind.Berserker, WorldBuilder.MinePos + new Vector3(-4, 0, -6)), (EnemyKind.Berserker, WorldBuilder.MinePos + new Vector3(4, 0, -6)),
                    (EnemyKind.Goblin, WorldBuilder.MinePos + new Vector3(-8, 0, -12)), (EnemyKind.Goblin, WorldBuilder.MinePos + new Vector3(8, 0, -12)),
                    (EnemyKind.Goblin, WorldBuilder.MinePos + new Vector3(0, 0, -14)));
                break;
            case Step.Graveyard:
                title = "Некромант у склепа"; objective = "Кто-то поднимает мертвецов у старого склепа. Победи некроманта";
                Marker(WorldBuilder.GraveyardPos); checkpoint = WorldBuilder.Courtyard;
                StartEncounter((EnemyKind.Necromancer, WorldBuilder.GraveyardPos + new Vector3(0, 0, 2)), (EnemyKind.Inferius, WorldBuilder.GraveyardPos + new Vector3(-5, 0, -3)),
                    (EnemyKind.Inferius, WorldBuilder.GraveyardPos + new Vector3(5, 0, -3)));
                break;
            case Step.FortressMarch:
                title = "Крепость Руквуда"; objective = "Иди к крепости Руквуда на северо-востоке, за полем для квиддича";
                if (DayNight.I != null) DayNight.I.SetHour(19.5f);
                Marker(WorldBuilder.FortressPos + new Vector3(0, 0, -24)); checkpoint = WorldBuilder.FortressPos + new Vector3(0, 0, -40);
                break;
            case Step.Rookwood:
                title = "Виктор Руквуд"; objective = "Победи Руквуда! Он аппарирует, зовёт браконьеров и оборотней";
                Marker(WorldBuilder.FortressPos); checkpoint = WorldBuilder.FortressPos + new Vector3(0, 0, -30);
                encounter.Clear();
                boss = Enemy.Spawn(EnemyKind.Rookwood, WorldBuilder.FortressPos + new Vector3(0, 0.3f, 6));
                boss.Died += _ =>
                {
                    enemiesDefeated++;
                    foreach (var e in Enemy.All.ToArray()) if (e != null && !e.IsDead) e.Kill();
                    Dialogue.Show(() => SetStep(Step.Epilogue),
                        new Line(Rook, "Ты… школьник… Мои покупатели заплатили бы за тебя больше, чем за всех драконов…"),
                        new Line(You, "Зверям нужна свобода, а не клетки. И Хогсмиду — покой."),
                        new Line(Hecat, "Отличная работа. Министерство уже в пути. Возвращайся к Фигу."));
                };
                break;
            case Step.Epilogue:
                title = "Тишина над Хогвартсом"; objective = "Вернись к профессору Фигу во дворе";
                talkTarget = "fig"; Marker(Npc("fig").transform.position);
                break;
        }
    }

    bool UpdateCh3()
    {
        if (!Chapter3Active) return false;
        var pp = P.transform.position;
        switch (step)
        {
            case Step.Werewolves:
            case Step.PoacherCamp:
            case Step.GoblinMine:
            case Step.Graveyard:
                progress = $"Осталось врагов: {AliveCount()}";
                if (AliveCount() == 0)
                {
                    var next = step == Step.Werewolves ? Step.PoacherTrail : step == Step.PoacherCamp ? Step.FreeBeasts : step == Step.GoblinMine ? Step.Graveyard : Step.FortressMarch;
                    Line[] lines = step switch
                    {
                        Step.Werewolves => new[] { new Line(Nat, "Это не обычные оборотни: на них ошейники с клеймом Руквуда!"),
                                                   new Line(Nat, "Следы его людей ведут на юго-запад, за озеро. Там его лагерь.") },
                        Step.PoacherCamp => new[] { new Line(You, "В клетках звери… нюхлеры, лунные тельцы, даже детёныш гиппогрифа."),
                                                    new Line(You, "Нужно выпустить всех, пока браконьеры не вернулись.") },
                        Step.GoblinMine => new[] { new Line("Гоблин-пленник", "Руквуд платил нам золотом за молчание… Но его союзник — некромант у склепа."),
                                                   new Line("Гоблин-пленник", "Он поднимает мёртвых, чтобы охранять крепость Руквуда.") },
                        _ => new[] { new Line(Necro, "Мертвецы… не… остановятся…"),
                                     new Line(You, "Теперь у Руквуда нет ни армии, ни охраны. Пора в его крепость.") },
                    };
                    Dialogue.Show(() => SetStep(next), lines);
                }
                break;
            case Step.PoacherTrail:
                if ((pp - WorldBuilder.PoacherCampPos).magnitude < 42f)
                    Dialogue.Show(() => SetStep(Step.PoacherCamp),
                        new Line(Poach, "Эй! Это же тот самый студент, за которого Руквуд обещал награду!"),
                        new Line(You, "Отпустите зверей — и никто не пострадает."));
                break;
            case Step.FreeBeasts:
                progress = $"Клетки: {cagesOpened}/{WorldBuilder.CageSpots.Length}";
                break;
            case Step.FortressMarch:
                if ((pp - WorldBuilder.FortressPos).magnitude < 36f)
                    Dialogue.Show(() => SetStep(Step.Rookwood),
                        new Line(Rook, "А вот и защитник зверушек. Знаешь, сколько стоит шкура дракона на чёрном рынке?"),
                        new Line(Rook, "Ты сорвал мне сделку века. Теперь твоя палочка украсит мою коллекцию!"));
                break;
            case Step.Rookwood:
                if (boss != null && !boss.IsDead) progress = $"Руквуд: {Mathf.CeilToInt(boss.health.Frac * 100)}%";
                break;
        }
        return true;
    }

    int NearestCage()
    {
        if (step != Step.FreeBeasts) return -1;
        var pp = P.transform.position;
        for (int i = 0; i < WorldBuilder.CageSpots.Length; i++)
            if (WorldBuilder.CageDoors[i] != null && WorldBuilder.CageDoors[i].activeSelf && (pp - WorldBuilder.CageSpots[i]).magnitude < 3.2f) return i;
        return -1;
    }

    string HintCh3() => NearestCage() >= 0 ? "[E] Открыть клетку" : null;

    bool InteractCh3()
    {
        int i = NearestCage();
        if (i < 0) return false;
        WorldBuilder.CageDoors[i].SetActive(false);
        cagesOpened++;
        var at = WorldBuilder.CageSpots[i] + Vector3.up;
        FX.Burst(at, new Color(0.6f, 0.9f, 1f), 50, 6f, 0.5f, 0.8f, -1f);
        SFX.Play("pickup", 0.7f);
        P.inventory.AddGold(60);
        HUD.ShowToast("Зверь на свободе!", new Color(0.6f, 0.9f, 1f));
        if (cagesOpened >= WorldBuilder.CageSpots.Length)
            Dialogue.Show(() => SetStep(Step.GoblinMine),
                new Line(Nat, "Ты их выпустил! Смотри — у браконьеров карта: часть добычи они прячут в гоблинской шахте на востоке."));
        else
        {
            for (int k = 0; k < WorldBuilder.CageSpots.Length; k++)
                if (WorldBuilder.CageDoors[k].activeSelf) { Marker(WorldBuilder.CageSpots[k] + new Vector3(0, 0, 1.8f)); break; }
        }
        return true;
    }

    bool TalkCh3(NPC npc)
    {
        if (!Chapter3Active || npc.id != talkTarget) return false;
        if (step == Step.Ch3Call)
            Dialogue.Show(() => SetStep(Step.Werewolves),
                new Line(Hecat, "Хорошо, что ты пришёл. По ночам у дороги в Хогсмид пропадают люди и звери."),
                new Line(Hecat, "Говорят про оборотней и про браконьера Виктора Руквуда. Он торгует волшебными тварями."),
                new Line(Hecat, "Натсай ждёт у Хогсмида. Будь осторожен: оборотни быстры, перекат спасёт тебе жизнь."));
        else if (step == Step.Epilogue)
            Dialogue.Show(() => SetStep(Step.Ch4Call),
                new Line(Fig, "Руквуд в Азкабане, звери на свободе, а мёртвые снова спят."),
                new Line(Fig, "Ты прошёл путь от новичка до защитника Хогвартса. Я горжусь тобой."),
                new Line(Fig, "Но из экспедиции к старой часовне никто не вернулся. Гекат нашла в бумагах Руквуда имя: Звонарь."));
        return true;
    }
}

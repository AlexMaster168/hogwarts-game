using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Автопрогон всего сюжета: запускается с аргументом -hmSmoke (опционально -hmShots <папка> для скриншотов).
// Ходит по шагам через те же API, что и игрок: разговоры, реальные заклинания по врагам, пролёт колец.
public partial class SmokeTest : MonoBehaviour
{
    public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), "-hmSmoke") >= 0;

    int errors;
    bool sawCharm;
    string shotDir;
    readonly List<string> notes = new();
    GameManager gm;
    QuestManager q;
    PlayerController p;

    static string Arg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
    }

    void Awake()
    {
        Application.logMessageReceived += OnLog;
        shotDir = Arg("-hmShots");
        if (shotDir != null) Directory.CreateDirectory(shotDir);
    }

    void OnDestroy() { Application.logMessageReceived -= OnLog; }

    void OnLog(string msg, string stack, LogType t)
    {
        if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert)
        {
            errors++;
            if (notes.Count < 50) notes.Add("ERR: " + msg + " | " + stack.Split('\n')[0]);
        }
    }

    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    IEnumerator Shot(string name)
    {
        if (shotDir == null) yield break;
        yield return Frames(25);
        yield return Grab(name);
    }

    // Кадр забираем сами в конце кадра: CaptureScreenshot молчит, если окно не на экране (заблокирован ПК, свёрнуто).
    IEnumerator Grab(string name)
    {
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex != null)
        {
            File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
        yield return Frames(2);
    }

    void Aim(Vector3 world)
    {
        var from = p.transform.position + Vector3.up * 1.6f;
        var d = world - from;
        var flat = new Vector3(d.x, 0, d.z);
        gm.camRig.yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        gm.camRig.pitch = -Mathf.Atan2(d.y, Mathf.Max(0.1f, flat.magnitude)) * Mathf.Rad2Deg + 4f;
    }

    void Place(Vector3 pos, Vector3 lookAt)
    {
        p.Teleport(pos);
        Aim(lookAt);
    }

    IEnumerator Start()
    {
        gm = GameManager.I;
        q = QuestManager.I;
        p = gm.player;
        yield return Frames(30);
        yield return Shot("01_title");
        HUD.I.StartGame();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmBossPortraits") >= 0)
        {
            yield return BossPortraits();
            Debug.Log($"HM_BOSS_PORTRAITS_RESULT errors={errors} bosses=4 views=8");
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmSagaOnly") >= 0)
        {
            yield return SagaTest();
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Debug.Log($"HM_SAGA_RESULT errors={errors} complete={q.sagaComplete} step={q.step}");
            Application.Quit(errors == 0 && q.sagaComplete ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmEchoOnly") >= 0)
        {
            q.SetStep(QuestManager.Step.Free, false);
            yield return EchoTest();
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Debug.Log($"HM_ECHO_RESULT errors={errors}");
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmMapsOnly") >= 0)
        {
            GameState.Set(GameMode.Play);
            DayNight.I.raining = false;
            DayNight.I.SetHour(11f);
            p.Teleport(WorldBuilder.Courtyard + Vector3.up * 0.3f);
            CastleMap.I.Open();
            int[] floors = { -1, 0, 1, 2, 3, 4 };
            string[] names = { "ui-map-basement", "ui-map-ground", "ui-map-upper", "ui-map-tower", "ui-map-outskirts", "ui-map-hogsmeade" };
            for (int i = 0; i < floors.Length; i++)
            {
                CastleMap.I.level = floors[i];
                yield return Shot(names[i]);
            }
            CastleMap.I.level = 3;
            yield return Shot("ui-map-vaults");
            CastleMap.I.SelectPoint(new Vector3(150, 0, 40));
            yield return Shot("ui-map-free-point");
            yield return Shot("ui-map-travel");
            CastleMap.I.selectedPoint = null;
            CastleMap.I.selected = -1;
            CastleMap.I.target = null;
            q.SetStep(QuestManager.Step.MeetFig, false);
            GameState.Set(GameMode.Map);
            yield return Shot("ui-map-quest-target");
            CastleMap.I.Close();
            Debug.Log($"HM_MAPS_RESULT errors={errors} views=10");
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmShowcase") >= 0)
        {
            yield return Showcase();
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmJitter") >= 0) { yield return JitterTest(); yield break; }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmPatchOnly") >= 0)
        {
            q.SetStep(QuestManager.Step.Free, false);
            for (int i = 0; i < 3; i++) Trials.I.done[i] = true;
            foreach (var task in SideQuests.I.all) { task.have = task.need; task.state = SQState.Done; }
            yield return PatchTest();
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmUpgradesOnly") >= 0)
        {
            q.SetStep(QuestManager.Step.Free, false);
            for (int i = 0; i < 3; i++) Trials.I.done[i] = true;
            foreach (var task in SideQuests.I.all) { task.have = task.need; task.state = SQState.Done; }
            yield return UpgradesTest();
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmFeaturesOnly") >= 0)
        {
            q.SetStep(QuestManager.Step.Free,false);
            for(int i=0;i<3;i++)Trials.I.done[i]=true;
            foreach(var task in SideQuests.I.all){task.have=task.need;task.state=SQState.Done;}
            yield return FeaturesTest();
            Application.Quit(errors==0?0:1);yield break;
        }
        yield return LocationsTest();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-hmLocationsOnly") >= 0)
        {
            foreach (var note in notes) Debug.Log("HM_SMOKE_NOTE " + note);
            Application.Quit(errors == 0 ? 0 : 1);
            yield break;
        }
        Place(WorldBuilder.Spawn, new Vector3(0, 8, 0));
        yield return Shot("02_arrival");

        float deadline = Time.realtimeSinceStartup + 1500f;
        while (GameState.Mode != GameMode.Victory && Time.realtimeSinceStartup < deadline)
        {
            if (GameState.Mode == GameMode.Dead)
            {
                notes.Add("player died at " + q.step);
                q.Respawn();
                yield return Frames(2);
            }
            if (GameState.Mode == GameMode.Dialogue)
            {
                yield return Frames(2);
                if (Dialogue.Choices != null) Dialogue.Pick(0); else Dialogue.Advance();
                continue;
            }
            if (GameState.Mode != GameMode.Play) GameState.Set(GameMode.Play);
            p.health.current = p.health.max;
            yield return DoStep();
            yield return Frames(3);
        }

        yield return SideQuestsTest();
        int sideDone = SideQuests.I.all.FindAll(s => s.state == SQState.Done).Count;
        CheckLocation(p.alohomora, "Caretaker Moon taught Alohomora");
        yield return FeaturesTest();
        yield return UpgradesTest();
        yield return PatchTest();

        // Проверим экран смерти и паузы.
        GameState.Set(GameMode.Play);
        p.health.TakeDamage(9999);
        yield return Shot("13_dead");
        bool deadOk = GameState.Mode == GameMode.Dead;
        q.Respawn();
        yield return Frames(3);
        bool respawnOk = GameState.Mode == GameMode.Play && !p.health.Dead;

        foreach (var n in notes) Debug.Log("HM_SMOKE_NOTE " + n);
        Debug.Log($"HM_SMOKE_RESULT errors={errors} step={q.step} house={q.house} pages={q.pages} defeated={q.enemiesDefeated} " +
                  $"ancient={p.ancientUnlocked} broom={p.hasBroom} deadScreen={deadOk} respawn={respawnOk} trials={Trials.I.Count}/3 " +
                  $"side={sideDone}/{SideQuests.I.all.Count} time={Time.realtimeSinceStartup:0} fps={Time.frameCount / Time.realtimeSinceStartup:0}");
        yield return Frames(5);
        bool ok = errors == 0 && q.step == QuestManager.Step.Free && deadOk && respawnOk && Trials.I.AllDone && sideDone == SideQuests.I.all.Count;
        Application.Quit(ok ? 0 : 1);
    }

    IEnumerator RunDialogue()
    {
        for (int guard = 0; guard < 40 && GameState.Mode == GameMode.Dialogue; guard++)
        {
            yield return Frames(2);
            if (Dialogue.Choices != null) Dialogue.Pick(0); else Dialogue.Advance();
        }
        yield return Frames(2);
    }

    // Принимаем, выполняем и сдаём все поручения через настоящие механики.
    IEnumerator SideQuestsTest()
    {
        GameState.Set(GameMode.Play);
        var sq = SideQuests.I;
        bool shot = false;
        foreach (var s in sq.all)
        {
            if (s.state == SQState.Locked) { notes.Add("side locked: " + s.id); continue; }
            if (s.state == SQState.Available)
            {
                yield return Talk(s.giver, new Vector3(0, 0, -2.5f));
                yield return RunDialogue();
            }
            if (s.state != SQState.Active && s.state != SQState.Ready) { notes.Add($"side not accepted: {s.id} ({s.state})"); continue; }
            if (!shot)
            {
                shot = true;
                yield return Shot("14_side_quests");
            }
            switch (s.id)
            {
                case "hecat_duel":
                case "garlick_spiders":
                    for (int i = 0; i < s.need && s.state == SQState.Active; i++)
                    {
                        var e = Enemy.Spawn(s.id == "hecat_duel" ? EnemyKind.DarkWizard : EnemyKind.Spider, p.transform.position + p.transform.forward * 12f, false);
                        yield return Frames(2);
                        e.Kill();
                        yield return Frames(2);
                    }
                    break;
                case "sharp_herbs":
                    foreach (var h in sq.herbs.ToArray())
                    {
                        if (h == null) continue;
                        p.Teleport(h.transform.position - Vector3.up);
                        yield return Frames(4);
                    }
                    break;
                case "moon_alohomora":
                    foreach (var h in sq.statues.ToArray())
                    {
                        if (h == null) continue;
                        p.Teleport(h.transform.position - Vector3.up * 0.6f);
                        yield return Frames(4);
                    }
                    break;
                case "ronen_pages":
                    foreach (var pg in FieldGuidePage.All.ToArray())
                    {
                        if (s.state != SQState.Active) break;
                        pg.Collect();
                        yield return Frames(2);
                    }
                    break;
                case "kogawa_race":
                    if (!p.onBroom) p.ToggleBroom();
                    for (int guard = 0; guard < 10 && sq.raceIdx < sq.race.Count; guard++)
                    {
                        p.Teleport(sq.race[sq.raceIdx].transform.position - Vector3.up);
                        yield return Frames(4);
                    }
                    if (p.onBroom) p.ToggleBroom();
                    break;
                case "fig_chests":
                    foreach (var c in Chest.All.ToArray())
                    {
                        if (s.state != SQState.Active) break;
                        if (!c.opened) c.Open(p);
                        yield return Frames(1);
                    }
                    break;
                case "seb_crypt":
                    for (int pass = 0; pass < 25 && sq.crypt.Exists(e => e != null && !e.IsDead); pass++)
                    {
                        foreach (var e in sq.crypt.ToArray())
                            if (e != null && !e.IsDead) yield return FightTarget(e.targetable, 15f, null);
                        yield return new WaitForSecondsRealtime(1f);
                    }
                    break;
            }
            yield return Frames(5);
            if (GameState.Mode != GameMode.Play) GameState.Set(GameMode.Play);
            if (s.state != SQState.Ready)
            {
                notes.Add($"side not ready: {s.id} {s.have}/{s.need} crypt={sq.crypt.Count} alive={sq.crypt.FindAll(e => e != null && !e.IsDead).Count} mode={GameState.Mode}");
                continue;
            }
            yield return Talk(s.giver, new Vector3(0, 0, -2.5f));
            yield return RunDialogue();
            if (s.state != SQState.Done) notes.Add("side not done: " + s.id);
        }
    }

    IEnumerator DoTrials()
    {
        var t = Trials.I;
        if (t.active < 0)
        {
            int idx = System.Array.FindIndex(t.done, d => !d);
            if (idx < 0) yield break;
            if (p.onBroom) p.ToggleBroom();
            yield return Talk("keeper_" + Trials.Ids[idx], new Vector3(0, 0, -3));
            yield break;
        }
        switch (t.active)
        {
            case 0:
                if (p.onBroom) p.ToggleBroom();
                int lit = 0;
                foreach (var b in t.braziers)
                {
                    if (b.lit) { lit++; continue; }
                    var dir = (b.transform.position - Trials.Sites[0]);
                    dir.y = 0;
                    Place(b.transform.position - dir.normalized * 4.5f + Vector3.up * 0.4f, b.transform.position + Vector3.up * 1.4f);
                    yield return Frames(4);
                    p.caster.DebugCast(SpellType.Incendio);
                    yield return new WaitForSecondsRealtime(0.3f);
                    if (++lit == 3) yield return Shot("11a_trial_fire");
                    if (GameState.Mode != GameMode.Play) yield break;
                }
                yield return Frames(3);
                if (t.guardian != null && !t.guardian.IsDead) yield return FightTarget(t.guardian.targetable, 40f, "11b_trial_guardian");
                break;
            case 1:
                if (!p.onBroom) p.ToggleBroom();
                if (t.orb != null)
                {
                    if (t.orbIndex == 2)
                    {
                        Place(t.orb.transform.position + new Vector3(0, 2, -14), t.orb.transform.position);
                        yield return Shot("11c_trial_sky");
                    }
                    p.Teleport(t.orb.transform.position - Vector3.up);
                    yield return Frames(4);
                }
                break;
            case 2:
                if (p.onBroom) p.ToggleBroom();
                foreach (var e in t.waveEnemies.ToArray())
                {
                    if (e == null || e.IsDead) continue;
                    yield return FightTarget(e.targetable, 25f, t.wave == 2 ? "11d_trial_waves" : null);
                    if (GameState.Mode != GameMode.Play) yield break;
                }
                break;
        }
    }

    IEnumerator Talk(string id, Vector3 offset)
    {
        var npc = q.Npc(id);
        Place(npc.transform.position + offset, npc.transform.position + Vector3.up * 1.5f);
        yield return Frames(3);
        if (p.nearbyNpc != npc) notes.Add($"nearbyNpc mismatch for {id}");
        q.Talk(npc);
    }

    IEnumerator DoStep()
    {
        if (q.SagaActive) { yield return SagaStep(); yield break; }
        if (q.ExtensionActive) { yield return ExtensionSteps(); yield break; }
        if (q.HorrorActive) { yield return HorrorSteps(); yield break; }
        if (q.Chapter3Active) { yield return Chapter3Steps(); yield break; }
        switch (q.step)
        {
            case QuestManager.Step.MeetFig:
                yield return Talk("fig", new Vector3(0, 0, -3));
                yield return Shot("03_dialogue");
                break;
            case QuestManager.Step.Sorting:
                Place(WorldBuilder.Courtyard, new Vector3(0, 6, 40));
                yield return Shot("04_courtyard");
                Place(new Vector3(0, 0, 36), new Vector3(0, 4, 80));
                yield return Shot("05_great_hall");
                Place(new Vector3(-16, 0.1f, 43), new Vector3(-34, 2, 41));
                yield return Shot("05b_library");
                Place(new Vector3(-17, 0.1f, 60), new Vector3(-28, 2, 72));
                yield return Shot("05c_classroom");
                Place(new Vector3(20, 0.1f, 39), new Vector3(36, 1.5f, 43));
                yield return Shot("05d_common_room");
                Place(new Vector3(18, 0.1f, 60), new Vector3(34, 1, 70));
                yield return Shot("05e_dormitory");
                Place(new Vector3(0, 0.1f, 87), new Vector3(0, 2, 101));
                yield return Shot("05f_trophy_room");
                Place(new Vector3(-60, 30, 20), new Vector3(0, 5, 60));
                yield return Shot("05g_wings_outside");
                yield return Talk("hat", new Vector3(0, 0, -3));
                break;
            case QuestManager.Step.MeetHecat:
                yield return Talk("hecat", new Vector3(-3, 0, 0));
                break;
            case QuestManager.Step.Dummies:
            {
                int k = 0;
                foreach (var d in q.dummies.ToArray())
                {
                    if (d == null) continue;
                    yield return FightTarget(d.GetComponent<Targetable>(), 12f, k++ == 1 ? "06_training" : null);
                }
                break;
            }
            case QuestManager.Step.MeetSebastian:
                yield return Talk("sebastian", new Vector3(0, 0, -3));
                break;
            case QuestManager.Step.Duel:
                if (q.encounter.Count > 0 && q.encounter[0] != null)
                    yield return FightTarget(q.encounter[0].targetable, 25f, "07_duel");
                break;
            case QuestManager.Step.MeetKogawa:
                yield return Talk("kogawa", new Vector3(0, 0, -3));
                break;
            case QuestManager.Step.Rings:
                if (!p.onBroom) p.ToggleBroom();
                Place(new Vector3(75, 40, -70), new Vector3(0, 20, 40));
                yield return Shot("08_castle_aerial");
                for (int i = q.ringIndex; i < q.rings.Count; i++)
                {
                    var r = q.rings[i].transform.position;
                    if (i == 2)
                    {
                        Place(r - Vector3.up - (q.rings[i].transform.up) * 14f, r);
                        yield return Shot("09_broom_rings");
                    }
                    p.Teleport(r - Vector3.up);
                    yield return Frames(3);
                    if (GameState.Mode != GameMode.Play) yield break;
                }
                break;
            case QuestManager.Step.MeetNatsai:
                if (p.onBroom) p.ToggleBroom();
                yield return Shopping();
                yield return Talk("natsai", new Vector3(-3, 0, 0));
                break;
            case QuestManager.Step.Ambush:
            case QuestManager.Step.ForestFight:
            case QuestManager.Step.Boss:
            {
                string shot = q.step == QuestManager.Step.Ambush ? "10_hogsmeade_fight" : q.step == QuestManager.Step.ForestFight ? "11_forest" : "12_boss";
                if (q.step == QuestManager.Step.Boss && q.boss != null)
                {
                    yield return FightTarget(q.boss.targetable, 60f, shot);
                    break;
                }
                foreach (var e in q.encounter.ToArray())
                {
                    if (e == null || e.IsDead) continue;
                    yield return FightTarget(e.targetable, 20f, shot);
                    shot = null;
                    if (GameState.Mode != GameMode.Play) yield break;
                }
                break;
            }
            case QuestManager.Step.GoForest:
                Place(WorldBuilder.ForestClearing + new Vector3(0, 0, -35), WorldBuilder.ForestClearing);
                yield return Frames(5);
                break;
            case QuestManager.Step.TakeRelic:
                Place(q.relic.transform.position - Vector3.up * 1.2f + Vector3.back, q.relic.transform.position);
                yield return Frames(5);
                break;
            case QuestManager.Step.FigReveal:
            case QuestManager.Step.FigVerdict:
                if (p.onBroom) p.ToggleBroom();
                yield return Talk("fig", new Vector3(0, 0, -3));
                break;
            case QuestManager.Step.Trials:
                yield return DoTrials();
                break;
            case QuestManager.Step.GoRuins:
                Place(WorldBuilder.RuinsCenter + new Vector3(0, 0.6f, -40), WorldBuilder.RuinsCenter);
                yield return Frames(5);
                break;
        }
    }

    // Сундук, монеты, все лавки, инвентарь.
    IEnumerator Shopping()
    {
        var inv = p.inventory;
        int before = inv.galleons;
        var chest = Chest.All.Find(c => !c.opened && (c.transform.position - WorldBuilder.HogsmeadeCenter).magnitude < 60f);
        if (chest != null)
        {
            Place(chest.transform.position + chest.transform.forward * 2f, chest.transform.position);
            yield return Frames(3);
            if (p.nearbyChest != chest) notes.Add("nearbyChest mismatch");
            chest.Open(p);
            yield return new WaitForSecondsRealtime(2.5f);
            if (inv.galleons <= before) notes.Add("chest coins not collected");
        }
        inv.galleons += 5000;
        bool first = true;
        foreach (var kv in ItemDB.Shops)
        {
            var npc = q.Npc(kv.Key);
            Place(npc.transform.position + npc.transform.forward * 2.5f, npc.transform.position + Vector3.up * 1.5f);
            yield return Frames(3);
            q.Talk(npc);
            while (GameState.Mode == GameMode.Dialogue) { yield return Frames(2); Dialogue.Advance(); }
            yield return Frames(2);
            if (GameState.Mode != GameMode.Shop) notes.Add("shop did not open: " + kv.Key);
            if (first) { yield return Shot("10a_shop"); first = false; }
            foreach (var id in kv.Value.items)
            {
                var d = ItemDB.Get(id);
                if (!inv.Buy(d)) notes.Add("cannot buy " + id + ": " + inv.CantBuyReason(d));
            }
            if (kv.Key == "gladrags") yield return Shot("10b_shop_bought");
            GameState.Set(GameMode.Play);
            yield return Frames(2);
        }
        ShopUI.OpenInventory();
        yield return Shot("10c_inventory");
        GameState.Set(GameMode.Play);
        p.caster.page = 1;
        yield return Shot("10d_spellset2");
        notes.Add($"learned={p.caster.learned.Count} pages={p.caster.Pages} dmg={inv.DamageMult:0.00} def={inv.DefenseMult:0.00} hp={p.health.max}");
    }

    // Бой с целью настоящими заклинаниями. Если за timeout не справились — добиваем и пишем заметку.
    IEnumerator FightTarget(Targetable t, float timeout, string shot)
    {
        if (t == null) yield break;
        if (p.onBroom) p.ToggleBroom();
        SpellType[] seq = { SpellType.Basic, SpellType.Imperio, SpellType.Crucio, SpellType.Glacius, SpellType.Expelliarmus, SpellType.Accio, SpellType.Diffindo,
            SpellType.Bombarda, SpellType.AvadaKedavra, SpellType.Incendio, SpellType.Levioso, SpellType.Depulso, SpellType.Confringo, SpellType.Basic };
        var en = t.GetComponent<Enemy>();
        float end = Time.realtimeSinceStartup + timeout;
        int i = 0;
        bool shotDone = shot == null;
        while (t != null && t.Alive && Time.realtimeSinceStartup < end)
        {
            if (GameState.Mode != GameMode.Play) yield break;
            bool charmed = en != null && en.Charmed;
            if (charmed && !sawCharm) { sawCharm = true; yield return Shot("10e_imperio"); }
            // Зачарованного добиваем сами (правило игры), Империо повторно не кидаем.
            if (charmed && seq[i % seq.Length] == SpellType.Imperio) i++;
            p.health.current = p.health.max;
            var to = t.transform.position - p.transform.position;
            to.y = 0;
            float want = t.isBoss ? 13f : 7f;
            if (to.magnitude > want + 6f || to.magnitude < want - 3f)
            {
                var dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward;
                p.Teleport(t.transform.position - dir * want + Vector3.up * 0.3f);
            }
            Aim(t.AimPoint);
            yield return Frames(2);
            if (!shotDone && i == 4)
            {
                shotDone = true;
                yield return Shot(shot);
                if (t == null || !t.Alive) break;
            }
            var spell = seq[i % seq.Length];
            if (p.ancientUnlocked && t.isBoss && i % 10 == 9) { p.ancient = 100f; spell = SpellType.Ancient; }
            p.caster.DebugCast(spell);
            i++;
            yield return new WaitForSecondsRealtime(0.4f);
        }
        if (t != null && t.Alive)
        {
            notes.Add($"fallback kill: {t.displayName} hp={t.health.current:0}");
            var e = t.GetComponent<Enemy>();
            if (e != null) e.Kill(); else t.health.TakeDamage(t.health.current + 1);
        }
    }
}

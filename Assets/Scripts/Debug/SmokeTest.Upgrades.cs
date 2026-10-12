using System.Collections;
using System.Linq;
using UnityEngine;

// Проверка всего, что добавлено поверх базы: атмосфера, бой и враги, второй этаж восточного крыла,
// Большая лестница, активности, журнал и сохранение их прогресса.
public partial class SmokeTest
{
    IEnumerator UpgradesTest()
    {
        GameState.Set(GameMode.Play);
        if (p.onBroom) p.ToggleBroom();
        foreach (var s in p.caster.catalog) if (!s.learned && !s.special) p.caster.Learn(s.type);
        p.caster.LearnProtego();
        var dn = DayNight.I;
        var act = Activities.I;
        HUD.Clear();

        // ---- День и ночь ----
        dn.SetHour(12f);
        CheckLocation(dn.Daylight > 0.9f && !dn.IsNight, "Noon daylight");
        dn.SetHour(0.5f);
        CheckLocation(dn.Daylight < 0.05f && dn.IsNight && RenderSettings.sun.intensity < 0.05f, "Midnight darkness");
        CheckLocation(PostFX.Profile != null, "Post-processing profile");

        // ---- Уровни ----
        int lvl = Progression.I.level;
        Progression.I.Gain(Progression.I.XpToNext - Progression.I.xp);
        CheckLocation(Progression.I.level == lvl + 1 && Mathf.Approximately(p.health.max, Progression.BaseHp + p.inventory.HpBonus), "Level up raises HP");

        // ---- Омут памяти учит Патронусу ----
        p.Teleport(WorldBuilder.PensievePos + new Vector3(1.3f, 0.15f, 0));
        Physics.SyncTransforms();
        yield return Frames(2);
        CheckLocation(CastleLocations.Current(p.transform.position) == "Кабинет директора", "Headmaster office on map: " + CastleLocations.Current(p.transform.position));
        CheckLocation(Activities.TryInteract(p), "Pensieve interaction");
        yield return RunDialogue();
        CheckLocation(p.caster.Knows(SpellType.Patronus), "Patronus learned from Pensieve");

        // ---- Ночные враги и новые заклинания ----
        dn.SetHour(23f);
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return null;
        NightSpawns.I.night.Clear();
        NightSpawns.I.SpawnNight(dn);
        CheckLocation(NightSpawns.I.night.Count == 7, "Night spawns: " + NightSpawns.I.night.Count);
        foreach (var e in NightSpawns.I.night.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return null;
        var arena = new Vector3(90, 0.2f, -100);
        p.Teleport(arena);
        var dem = Enemy.Spawn(EnemyKind.Dementor, arena + new Vector3(0, 0.5f, 8), false);
        yield return Frames(3);
        float hp0 = dem.health.current;
        dem.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = 40f, point = dem.targetable.AimPoint, dir = Vector3.forward, source = p.gameObject });
        CheckLocation(hp0 - dem.health.current <= 20.1f, "Dementor resists ordinary spells");
        hp0 = dem.health.current;
        Aim(dem.targetable.AimPoint);
        yield return Frames(3);
        p.caster.DebugCast(SpellType.Patronus);
        yield return new WaitForSecondsRealtime(1.2f);
        CheckLocation(dem == null || dem.IsDead || hp0 - dem.health.current >= 100f, "Patronus damages dementor");
        if (dem != null) Destroy(dem.gameObject);
        var inf = Enemy.Spawn(EnemyKind.Inferius, arena + new Vector3(0, 0.5f, 8), false);
        yield return Frames(2);
        hp0 = inf.health.current;
        inf.OnSpellHit(new SpellHit { type = SpellType.Confringo, damage = 20f, point = inf.targetable.AimPoint, dir = Vector3.forward, source = p.gameObject });
        CheckLocation(hp0 - inf.health.current >= 39f, "Inferius weak to fire");
        Destroy(inf.gameObject);
        var gob = Enemy.Spawn(EnemyKind.Goblin, arena + new Vector3(0, 0.5f, 7), false);
        yield return Frames(3);
        Aim(gob.targetable.AimPoint);
        yield return Frames(2);
        p.caster.DebugCast(SpellType.Arresto);
        CheckLocation(gob.slowT > 0f, "Arresto Momentum slows");
        gob.OnSpellHit(new SpellHit { type = SpellType.Diffindo, damage = 9999f, point = gob.targetable.AimPoint, dir = Vector3.forward, source = p.gameObject });
        yield return new WaitForSecondsRealtime(0.4f);
        CheckLocation(Mathf.Approximately(Time.timeScale, 1f), "Hit-stop restores time scale");
        dn.SetHour(10f);
        yield return new WaitForSecondsRealtime(1.5f);

        // ---- Лестница на 2 этаж восточного крыла ----
        p.enabled = false;
        p.Teleport(new Vector3(15.4f, 0.25f, 32.4f));
        yield return WalkTo(new Vector3(35.4f, 6.3f, 32.4f), 20);
        CheckLocation(p.transform.position.y > 5.8f, "East wing stairs ascent");
        yield return WalkTo(new Vector3(35.4f, 6.3f, 38f), 5);
        yield return WalkTo(new Vector3(25f, 6.3f, 40f), 6);
        yield return WalkTo(new Vector3(25f, 6.3f, 50f), 6);
        yield return WalkTo(new Vector3(29f, 6.3f, 52f), 4);
        yield return WalkTo(new Vector3(29f, 6.3f, 54.2f), 4);
        yield return WalkTo(new Vector3(29f, 6.3f, 60f), 5);
        CheckLocation(CastleLocations.Current(p.transform.position) == "Выручай-комната", "Room of Requirement reachable");

        // ---- Большая лестница: подъём по трём пролётам ----
        foreach (var s in WorldBuilder.Stairs) s.forceConnected = true;
        yield return Frames(2);
        p.Teleport(new Vector3(0, 0.25f, 150.5f));
        yield return WalkTo(new Vector3(-1.2f, 0.25f, 163f), 8);
        yield return WalkTo(new Vector3(10.6f, 7.2f, 163f), 12);
        yield return WalkTo(new Vector3(12.8f, 7.2f, 163f), 3);
        CheckLocation(p.transform.position.y > 6.6f, "Grand stair flight 1 (y=" + p.transform.position.y.ToString("0.0") + ")");
        yield return WalkTo(new Vector3(12.8f, 7.2f, 150f), 6);
        yield return WalkTo(new Vector3(-13.2f, 7.2f, 150f), 12);
        yield return WalkTo(new Vector3(-13.6f, 7.2f, 158.5f), 6);
        yield return WalkTo(new Vector3(10.6f, 14.2f, 158.5f), 14);
        yield return WalkTo(new Vector3(13.4f, 14.2f, 158.5f), 3);
        CheckLocation(p.transform.position.y > 13.6f, "Grand stair flight 2 (y=" + p.transform.position.y.ToString("0.0") + ")");
        yield return WalkTo(new Vector3(13.4f, 14.2f, 167.5f), 5);
        yield return WalkTo(new Vector3(-10.6f, 21.2f, 167.5f), 14);
        yield return WalkTo(new Vector3(-12.8f, 21.2f, 167.5f), 3);
        CheckLocation(p.transform.position.y > 20.6f, "Grand stair flight 3 (y=" + p.transform.position.y.ToString("0.0") + ")");
        CheckLocation(CastleLocations.Current(p.transform.position) == "Вершина лестницы", "Staircase top on map");
        p.enabled = true;
        // Пролёт действительно уезжает в тупик и возвращается.
        p.Teleport(new Vector3(0, 0.25f, 150.5f));
        var st = WorldBuilder.Stairs[0];
        st.forceConnected = false;
        st.Kick();
        yield return new WaitForSecondsRealtime(3.6f);
        CheckLocation(!st.Connected, "Moving stair swings away");
        st.Kick();
        yield return new WaitForSecondsRealtime(3.6f);
        CheckLocation(st.Connected, "Moving stair returns");
        foreach (var s in WorldBuilder.Stairs) s.forceConnected = false;

        // ---- Карточки волшебников ----
        foreach (var c in act.cardObjs.ToArray())
        {
            if (c == null) continue;
            p.Teleport(c.transform.position - Vector3.up * 1.2f);
            yield return Frames(3);
        }
        CheckLocation(act.cards.Count == Activities.CardNames.Length, "Wizard cards " + act.cards.Count);

        // ---- Испытания Мерлина ----
        foreach (var stone in act.stones)
        {
            if (stone.done) continue;
            p.Teleport(stone.transform.position + new Vector3(0, 0.3f, -2.2f));
            Physics.SyncTransforms();
            yield return Frames(2);
            CheckLocation(Activities.TryInteract(p) && stone.running, "Merlin start " + stone.index);
            yield return Frames(2);
            foreach (var t in stone.targets.ToArray())
            {
                if (stone.Orbs) t.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = 10, point = t.transform.position, source = p.gameObject });
                else
                {
                    // Жаровни зажигаем настоящим Инсендио.
                    var dir = (t.transform.position - stone.transform.position).normalized;
                    var tangent = Vector3.Cross(Vector3.up, dir);
                    p.Teleport(t.transform.position + tangent * 3.2f + Vector3.up * 0.3f);
                    Aim(t.target.AimPoint);
                    yield return Frames(3);
                    p.caster.target = t.target;
                    p.caster.DebugCast(SpellType.Incendio);
                    yield return Frames(2);
                }
            }
            yield return Frames(3);
            CheckLocation(stone.done, $"Merlin trial {stone.index}: hit {stone.targets.FindAll(t => t.hit).Count}/{stone.targets.Count} at {p.transform.position} target={(p.caster.target != null ? p.caster.target.displayName : "-")}");
        }

        // ---- Звери ----
        foreach (var b in act.beastObjs.ToArray())
        {
            if (b == null) continue;
            p.Teleport(b.transform.position + new Vector3(1.2f, 0.3f, 0));
            Physics.SyncTransforms();
            yield return Frames(1);
            Activities.TryInteract(p);
        }
        CheckLocation(act.beasts.Count == Activities.BeastSpots.Length, "Beasts caught " + act.beasts.Count);

        // ---- Снитч ----
        p.hasBroom = true;
        p.Teleport(Activities.SnitchStand + new Vector3(1.5f, 0.3f, 0));
        Physics.SyncTransforms();
        yield return Frames(2);
        CheckLocation(Activities.TryInteract(p) && act.snitch != null, "Snitch game start");
        yield return Frames(30);
        if (act.snitch != null)
        {
            p.Teleport(act.snitch.snitch.position - Vector3.up * 1f);
            yield return Frames(2);
        }
        CheckLocation(act.snitchWins == 1, "Snitch caught");
        if (p.onBroom) p.ToggleBroom();

        // ---- Выручай-комната ----
        p.Teleport(WorldBuilder.RoomOfRequirementPos + new Vector3(1.6f, 0.2f, -1f));
        Physics.SyncTransforms();
        yield return Frames(2);
        CheckLocation(Activities.TryInteract(p) && act.room != null && act.room.Active, "Room challenge start");
        for (int guard = 0; guard < 40 && act.room != null && act.room.Active; guard++)
        {
            p.health.current = p.health.max;
            foreach (var e in act.room.alive.ToArray()) if (e != null && !e.IsDead) e.health.TakeDamage(99999);
            yield return new WaitForSecondsRealtime(0.6f);
        }
        CheckLocation(act.rorRuns == 1, "Room of Requirement completed");
        yield return new WaitForSecondsRealtime(2f);

        // ---- Журнал ----
        act.OpenJournal();
        CheckLocation(GameState.Mode == GameMode.Journal && Time.timeScale == 0f, "Journal pauses");
        yield return Shot("ui_journal");
        act.CloseJournal();
        CheckLocation(GameState.Playing && Time.timeScale == 1f, "Journal closes");

        // ---- Сохранение новых систем ----
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return null;
        p.Teleport(WorldBuilder.Spawn);
        dn.SetHour(15.5f);
        SaveGame.TestPath = System.IO.Path.Combine(Application.dataPath, "..", "Verification", "test-upgrades.json");
        int level = Progression.I.level, wins = act.snitchWins;
        CheckLocation(SaveGame.Save(false), "Upgrades save write");
        act.Restore(null, null, null, 0, 0, 0, 0);
        Progression.I.Restore(1, 0);
        dn.SetHour(3f);
        CheckLocation(SaveGame.Load(), "Upgrades load accepted");
        yield return Frames(5);
        CheckLocation(act.cards.Count == Activities.CardNames.Length && act.merlin.Count == Activities.MerlinSites.Length
            && act.beasts.Count == Activities.BeastSpots.Length && act.snitchWins == wins && act.rorRuns == 1, "Activities restored");
        CheckLocation(Progression.I.level == level && Mathf.Abs(dn.hour - 15.5f) < 0.1f, "Level and time restored");
        CheckLocation(act.cardObjs.Count == 0 && act.beastObjs.Count == 0, "Collected items stay collected");
        SaveGame.TestPath = null;
        p.Teleport(WorldBuilder.Spawn);
        Debug.Log($"HM_UPGRADE_RESULT errors={errors} level={Progression.I.level} cards={act.cards.Count} merlin={act.merlin.Count} beasts={act.beasts.Count} snitch={act.snitchWins} room={act.rorRuns}");
    }
}

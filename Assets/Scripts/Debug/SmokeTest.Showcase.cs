using System.Collections;
using UnityEngine;

// Тур по возможностям игры для README: -hmSmoke -hmShowcase -hmShots <папка>.
// Ставит сцены (время суток, погода, бой, интерфейсы) и снимает кадры с понятными именами.
public partial class SmokeTest
{
    IEnumerator Showcase()
    {
        var dn = DayNight.I;
        GameState.Set(GameMode.Play);

        // ---- Атмосфера ----
        dn.raining = false;
        dn.SetHour(10f);
        Place(new Vector3(0, 0.2f, -48), new Vector3(0, 12, 20));
        yield return Shot("atmo_day_bridge");
        dn.SetHour(17.3f);
        Place(new Vector3(-30, 0.2f, -70), new Vector3(0, 15, 10));
        yield return Shot("atmo_sunset");
        dn.SetHour(23f);
        Place(new Vector3(0, 0.2f, -58), new Vector3(0, 6, -20));
        yield return Shot("atmo_night_bridge");
        Place(new Vector3(-6, 0.2f, -18), new Vector3(10, 4, 10));
        yield return Shot("atmo_night_courtyard");
        Place(WorldBuilder.ForestCenter + new Vector3(10, 0.2f, -30), WorldBuilder.ForestCenter + Vector3.up * 3);
        yield return new WaitForSecondsRealtime(3f);
        yield return Shot("atmo_fireflies");
        dn.SetHour(13f);
        dn.raining = true;
        Place(WorldBuilder.HogsmeadeCenter + new Vector3(-20, 0.2f, 0), WorldBuilder.HogsmeadeCenter + new Vector3(10, 3, 0));
        yield return new WaitForSecondsRealtime(5f);
        yield return Shot("atmo_rain_hogsmeade");
        dn.raining = false;
        dn.SetHour(11f);
        Place(new Vector3(0, 0.2f, 36), new Vector3(0, 6, 80));
        yield return Shot("castle_great_hall");
        Place(new Vector3(-21, 0.2f, 33), new Vector3(-33, 2, 46));
        yield return Shot("castle_library");

        yield return ShowcaseCombat();
        yield return ShowcaseCastle();
        yield return ShowcaseActivities();
        Debug.Log("HM_SHOWCASE_RESULT errors=" + errors);
    }

    IEnumerator ShowcaseCastle()
    {
        var dn = DayNight.I;
        dn.raining = false;
        dn.SetHour(11f);
        HUD.Clear();
        p.hasBroom = true;
        p.Teleport(new Vector3(70, 45, -60));
        if (!p.onBroom) p.ToggleBroom();
        yield return Frames(5);
        Aim(new Vector3(0, 8, 90));
        yield return Shot("castle_aerial");
        p.ToggleBroom();
        Place(new Vector3(18, 0.2f, 31.5f), new Vector3(34, 5, 33));
        yield return Shot("castle_east_stairs");
        Place(new Vector3(24f, 6.3f, 36f), new Vector3(32, 7, 47));
        yield return Shot("castle_headmaster_office");
        Place(WorldBuilder.PensievePos + new Vector3(2.4f, 0.15f, -2.2f), WorldBuilder.PensievePos + Vector3.up);
        yield return Shot("castle_pensieve");
        Place(new Vector3(26, 6.3f, 56.5f), new Vector3(26, 7, 72));
        yield return Shot("castle_room_of_requirement");
        Place(new Vector3(13f, 7.25f, 151f), new Vector3(-4, 11, 168));
        yield return Shot("castle_grand_staircase");
        Place(new Vector3(-13, 21.3f, 175f), new Vector3(4, 9, 160));
        yield return Shot("castle_grand_staircase_top");
        Place(new Vector3(-23, 6.3f, 60), new Vector3(-35, 8, 64));
        yield return Shot("castle_library_gallery");
        Place(new Vector3(-26, 24.3f, 91), new Vector3(0, 4, 50));
        yield return Shot("castle_observatory");
        Place(new Vector3(0, -6.7f, 38), new Vector3(0, -5, 62));
        yield return Shot("castle_kitchen");
        Place(new Vector3(18, 0.2f, 39), new Vector3(36, 1.5f, 43));
        yield return Shot("castle_common_room");
        Place(new Vector3(18, 0.2f, 60), new Vector3(34, 1, 70));
        yield return Shot("castle_dormitory");
        Place(new Vector3(0, 0.2f, 87), new Vector3(0, 2, 101));
        yield return Shot("castle_trophy_room");
        Place(new Vector3(-17, 0.2f, 60), new Vector3(-28, 2, 72));
        yield return Shot("castle_dada_classroom");
        Place(new Vector3(14, 0.2f, 119), new Vector3(14, 1, 127));
        yield return Shot("castle_potions_classroom");
        Place(new Vector3(9, 0.2f, 131), new Vector3(20, 1, 142));
        yield return Shot("castle_greenhouse");
        Place(new Vector3(-14, 0.2f, 115), new Vector3(-14, 1, 125));
        yield return Shot("castle_infirmary");
        CastleMap.I.Open();
        CastleMap.I.level = 0;
        yield return Shot("ui_map_ground");
        CastleMap.I.level = 1;
        yield return Shot("ui_map_upper");
        CastleMap.I.level = 3;
        yield return Shot("ui_map_outskirts");
        CastleMap.I.Close();
    }

    IEnumerator ShowcaseActivities()
    {
        var dn = DayNight.I;
        var act = Activities.I;
        dn.SetHour(11f);
        HUD.Clear();
        var card = act.cardObjs.Find(c => c != null && c.id == 0);
        if (card != null)
        {
            Place(card.transform.position + new Vector3(3.5f, -0.8f, -1.5f), card.transform.position);
            yield return Shot("activity_wizard_card");
        }
        var orbs = act.stones[0];
        Place(orbs.transform.position + new Vector3(0, 0.3f, -11), orbs.transform.position + Vector3.up * 2.5f);
        orbs.Begin();
        yield return Shot("activity_merlin_orbs");
        orbs.Abort();
        var braz = act.stones[1];
        Place(braz.transform.position + new Vector3(0, 0.3f, -11), braz.transform.position + Vector3.up);
        braz.Begin();
        yield return Frames(5);
        braz.targets[0].OnSpellHit(new SpellHit { type = SpellType.Incendio, source = p.gameObject });
        yield return Shot("activity_merlin_braziers");
        braz.Abort();
        HUD.Clear();
        foreach (int species in new[] { 0, 1, 2 })
        {
            var b = act.beastObjs.Find(x => x != null && x.species == species);
            if (b == null) continue;
            if (species == 2) dn.SetHour(16.5f);
            Place(b.transform.position + new Vector3(2.6f, 0.3f, -2.6f), b.transform.position + Vector3.up * 0.5f);
            yield return Shot("activity_beast_" + new[] { "niffler", "puffskein", "mooncalf" }[species]);
        }
        dn.SetHour(12f);
        HUD.Clear();
        p.hasBroom = true;
        Place(Activities.SnitchStand + new Vector3(1.5f, 0.3f, 0), Activities.SnitchStand + Vector3.up);
        act.StartSnitch();
        yield return new WaitForSecondsRealtime(1.5f);
        if (act.snitch != null)
        {
            var s = act.snitch.snitch.position;
            p.Teleport(s + new Vector3(-5, -1.5f, -7));
            Aim(s);
            yield return ShotNow("activity_snitch", 3);
            act.snitch.ForceCatch();
        }
        yield return new WaitForSecondsRealtime(1f);
        if (p.onBroom) p.ToggleBroom();
        HUD.Clear();
        Place(WorldBuilder.RoomOfRequirementPos + new Vector3(0, 0.2f, -8.5f), WorldBuilder.RoomOfRequirementPos + Vector3.up);
        act.StartRoom();
        yield return new WaitForSecondsRealtime(1.2f);
        yield return ShotNow("activity_room_challenge", 3);
        act.room.Abort(false);
        HUD.Clear();
        act.OpenJournal();
        yield return Shot("ui_journal");
        act.CloseJournal();
    }

    IEnumerator ShotNow(string name, int frames = 4)
    {
        if (shotDir == null) yield break;
        yield return Frames(frames);
        yield return Grab(name);
    }

    void ClearEnemies()
    {
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
    }

    IEnumerator ShowcaseCombat()
    {
        var dn = DayNight.I;
        foreach (var s in p.caster.catalog) if (!s.learned) p.caster.Learn(s.type);
        p.caster.LearnProtego();
        dn.raining = false;
        dn.SetHour(12f);
        var arena = new Vector3(90, 0.2f, -100);
        ClearEnemies();
        var gobs = new System.Collections.Generic.List<Enemy>();
        for (int i = 0; i < 3; i++) gobs.Add(Enemy.Spawn(EnemyKind.Goblin, arena + new Vector3(-3 + i * 3, 0.3f, 11), false));
        Place(arena, arena + new Vector3(0, 1, 11));
        yield return Frames(20);
        HUD.Clear();
        p.caster.DebugCast(SpellType.Arresto);
        yield return ShotNow("combat_arresto", 12);
        for (int i = 0; i < 3; i++) { p.caster.basicCd = 0; p.caster.DebugCast(SpellType.Basic); yield return new WaitForSecondsRealtime(0.3f); }
        yield return ShotNow("combat_combo", 6);
        foreach (var g in gobs) if (g != null && !g.IsDead) g.health.TakeDamage(9999);
        Progression.I.Gain(Progression.I.XpToNext - Progression.I.xp, p.transform.position + Vector3.up * 2.4f);
        yield return ShotNow("combat_levelup", 20);
        yield return new WaitForSecondsRealtime(2f);

        // Ночь: дементоры у озера и Патронус.
        dn.SetHour(18.3f);
        var shore = NightSpawns.DementorShore;
        var dems = new System.Collections.Generic.List<Enemy>();
        for (int i = 0; i < 3; i++) dems.Add(Enemy.Spawn(EnemyKind.Dementor, shore + new Vector3(-4 + i * 4, 0.5f, 0), false));
        Place(shore + new Vector3(0, 0.2f, -14), shore + Vector3.up * 2);
        HUD.Clear();
        yield return Shot("enemy_dementors");
        p.caster.DebugCast(SpellType.Patronus);
        yield return ShotNow("spell_patronus", 12);
        yield return new WaitForSecondsRealtime(1.5f);
        ClearEnemies();

        // Инферналы у склепа боятся огня.
        dn.SetHour(18.6f);
        HUD.Clear();
        var yard = NightSpawns.InferiYard;
        for (int i = 0; i < 4; i++) Enemy.Spawn(EnemyKind.Inferius, yard + new Vector3(-6 + i * 4, 0.5f, 4), false);
        Place(yard + new Vector3(0, 0.2f, -8), yard + new Vector3(0, 1, 4));
        yield return Shot("enemy_inferi");
        p.caster.DebugCast(SpellType.Incendio);
        yield return ShotNow("spell_incendio_inferi", 8);
        yield return new WaitForSecondsRealtime(1f);
        ClearEnemies();
        dn.SetHour(12f);
    }
}

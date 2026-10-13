using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Выбор героя, голос заклинаний, посадка на метлу, фокус заданий, точки перемещения, логово тёмных магов, карта Хогсмида.
public partial class SmokeTest
{
    // Кадр с отдельной камеры (вид сбоку на героя и т. п.), чтобы не зависеть от камеры за спиной.
    IEnumerator ShotFrom(string name, Vector3 from, Vector3 look)
    {
        if (shotDir == null) yield break;
        yield return new WaitForEndOfFrame();
        var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
        var cam = new GameObject("ShotCam").AddComponent<Camera>();
        cam.CopyFrom(Camera.main);
        cam.targetTexture = rt;
        cam.fieldOfView = 40f;
        cam.transform.position = from;
        cam.transform.LookAt(look);
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(shotDir, name + ".png"), tex.EncodeToPNG());
        Destroy(cam.gameObject); Destroy(rt); Destroy(tex);
    }

    IEnumerator PatchTest()
    {
        GameState.Set(GameMode.Play);
        if (p.onBroom) p.ToggleBroom();
        foreach (var s in p.caster.catalog) if (!s.learned) p.caster.Learn(s.type);
        p.caster.LearnProtego();
        DayNight.I.raining = false;
        DayNight.I.SetHour(11f);
        HUD.Clear();

        // ---- Мальчик / девочка ----
        p.SetGender(true);
        yield return Frames(3);
        CheckLocation(p.female && Gender.Female && p.visual != null && p.visual.name.Contains("witch"), "Girl uses witch model");
        CheckLocation(p.parts.wandTip != null && p.visual.hand != null && p.parts.wandTip.IsChildOf(p.visual.hand), "Wand moved to girl's hand");
        CheckLocation(Gender.Fix("Ты сделал это!") == "Ты сделала это!" && Gender.Fix("Новенький? Отлично") == "Новенькая? Отлично", "Female dialogue forms");
        Dialogue.Show(null, new Line("Фиг", "Ты справился с реликвией."));
        CheckLocation(Dialogue.Current.text == "Ты справилась с реликвией.", "Dialogue shows female form");
        Dialogue.Advance(); yield return null; Dialogue.Advance(); GameState.Set(GameMode.Play);

        // ---- Голос заклинаний ----
        foreach (var g in new[] { "m", "f" })
            foreach (var sp in new[] { "Incendio", "Levioso", "Depulso", "Confringo", "Expelliarmus", "Accio", "Diffindo", "Glacius", "Bombarda",
                         "Imperio", "Crucio", "AvadaKedavra", "Stupefy", "Arresto", "Patronus", "Protego" })
                CheckLocation(Resources.Load<AudioClip>($"Audio/voice_{g}_{sp}") != null, $"Voice clip {g} {sp}");
        Place(new Vector3(60, 0.2f, -100), new Vector3(60, 1, -80));
        yield return Frames(5);
        p.caster.DebugCast(SpellType.Incendio);
        CheckLocation(SFX.LastVoice == "voice_f_Incendio", "Girl says Incendio: " + SFX.LastVoice);
        yield return ShotFrom("hero_girl", p.transform.position + p.transform.forward * 3.2f + Vector3.up * 1.3f, p.transform.position + Vector3.up * 1f);
        p.SetGender(false);
        yield return Frames(3);
        p.caster.DebugCast(SpellType.Confringo);
        CheckLocation(SFX.LastVoice == "voice_m_Confringo" && p.visual.name.Contains("rpg_wizard"), "Boy says Confringo: " + SFX.LastVoice);
        yield return ShotFrom("hero_boy", p.transform.position + p.transform.forward * 3.2f + Vector3.up * 1.3f, p.transform.position + Vector3.up * 1f);
        yield return new WaitForSecondsRealtime(1.2f);

        // ---- Посадка на метлу ----
        foreach (bool girl in new[] { false, true })
        {
            p.SetGender(girl);
            p.hasBroom = true;
            p.Teleport(new Vector3(60, 25, -100));
            if (!p.onBroom) p.ToggleBroom();
            yield return Frames(30);
            var v = p.visual;
            var seat = v.Seat;
            var stick = p.broom.transform.TransformPoint(new Vector3(0, 0.8f, 0.1f));
            var model = p.model;
            var thigh = (FindBone(v.transform, "LowerLeg.L").position - FindBone(v.transform, "UpperLeg.L").position).normalized;
            var thighR = (FindBone(v.transform, "LowerLeg.R").position - FindBone(v.transform, "UpperLeg.R").position).normalized;
            // Древко идёт вдоль forward метлы: меряем расстояние от точки посадки до его оси и что оно ниже посадки.
            float axis = seat.HasValue ? Vector3.Cross(seat.Value - stick, p.broom.transform.forward).magnitude : -1;
            float below = seat.HasValue ? Vector3.Dot(seat.Value - stick, p.broom.transform.up) : -1;
            CheckLocation(v.riding && axis < 0.1f && below > 0.02f, $"Broom under seat ({(girl ? "girl" : "boy")}): axis={axis:0.00} below={below:0.00}");
            CheckLocation(Vector3.Dot(thigh, model.forward) > 0.5f && Vector3.Dot(thighR, model.forward) > 0.5f, $"Thighs forward ({(girl ? "girl" : "boy")})");
            CheckLocation(Vector3.Dot(thigh, model.right) < -0.1f && Vector3.Dot(thighR, model.right) > 0.1f, $"Legs astride ({(girl ? "girl" : "boy")})");
            var pos = p.transform.position;
            yield return ShotFrom(girl ? "broom_girl_side" : "broom_boy_side", pos + p.transform.right * 3.6f + Vector3.up * 1.4f + p.transform.forward * 0.6f, pos + Vector3.up * 0.9f);
            yield return ShotFrom(girl ? "broom_girl_front" : "broom_boy_front", pos + p.transform.forward * 3.4f + p.transform.right * 1.4f + Vector3.up * 1.8f, pos + Vector3.up * 0.9f);
            p.ToggleBroom();
            yield return Frames(5);
            CheckLocation(!p.visual.riding, "Riding pose released");
        }
        p.SetGender(false);

        // ---- Фокус заданий: побочки не перехватывают сюжет ----
        var sq = SideQuests.I;
        var saved = sq.all.Select(x => (x.state, x.have)).ToArray();
        var a = sq.all.First(x => x.id == "hecat_duel");
        var b = sq.all.First(x => x.id == "fig_chests");
        sq.tracked = null;
        a.state = SQState.Active; b.state = SQState.Active;
        yield return Frames(2);
        CheckLocation(sq.tracked == null, "New side quest does not steal focus");
        sq.CycleFocus(); CheckLocation(sq.tracked == a, "Z -> first side quest");
        CheckLocation(sq.TrackedMarker().HasValue, "Duel quest has a marker");
        sq.CycleFocus(); CheckLocation(sq.tracked == b, "Z -> second side quest");
        CheckLocation(sq.TrackedMarker().HasValue, "Chest quest has a marker");
        var qtp = CastleMap.QuestTarget(out var qtTitle, out _);
        CheckLocation(qtp.HasValue && qtTitle == b.title, "Map quest target follows focused quest");
        CastleMap.I.Open(); CastleMap.I.level = CastleMap.TabOf(qtp.Value);
        yield return Shot("ui_map_quest_target");
        CastleMap.I.Close();
        yield return Shot("quest_focus_side");
        sq.CycleFocus(); CheckLocation(sq.tracked == null, "Z -> back to story");
        sq.Focus(b);
        b.state = SQState.Done;
        yield return Frames(2);
        CheckLocation(sq.tracked == null, "Finished side quest returns focus to story");
        Activities.I.OpenJournal();
        yield return Shot("ui_journal_quests");
        Activities.I.CloseJournal();
        for (int i = 0; i < saved.Length; i++) { sq.all[i].state = saved[i].state; sq.all[i].have = saved[i].have; }
        sq.tracked = null;

        // ---- Точки перемещения ----
        var rs = RoomServices.I;
        // Без посещения: любая комната замка и любая точка окрестностей доступны сразу.
        rs.visited.Clear();
        int kitchen = CastleLocations.All.FindIndex(l => l.name == "Кухня");
        p.inventory.galleons = 100;
        CheckLocation(kitchen >= 0 && rs.Travel(kitchen), "Travel to unvisited kitchen");
        yield return Frames(10);
        CheckLocation(CastleLocations.Current(p.transform.position) == "Кухня" && p.inventory.galleons == 75, "Landed in kitchen: " + CastleLocations.Current(p.transform.position));
        foreach (var spot in new[] { new Vector3(120, 0, -40), new Vector3(-250, 0, 150), new Vector3(60, 0, 300), WorldBuilder.LakeCenter, new Vector3(500, 0, 500) })
        {
            p.inventory.galleons = 100;
            var want = RoomServices.SafeOutdoor(spot);
            bool ok = rs.TravelToPoint(spot);
            yield return Frames(20);
            var pos = p.transform.position;
            CheckLocation(ok && new Vector2(pos.x - want.x, pos.z - want.z).magnitude < 15f && pos.y > -0.5f && pos.y < 4f && p.cc.isGrounded, $"Free point {spot} -> {pos}");
            CheckLocation((new Vector3(pos.x, 0, pos.z) - WorldBuilder.LakeCenter).magnitude > 72f, "Not dropped into the lake");
        }
        CastleMap.I.Open(); CastleMap.I.level = 3; CastleMap.I.SelectPoint(new Vector3(150, 0, 40));
        yield return Shot("ui_map_free_point");
        CastleMap.I.Close();
        for (int i = 0; i < RoomServices.Waypoints.Length; i++)
        {
            var w = RoomServices.Waypoints[i];
            p.inventory.galleons = 100;
            bool ok = rs.TravelTo(i);
            yield return Frames(20);
            var pos = p.transform.position;
            var flat = new Vector2(pos.x - w.pos.x, pos.z - w.pos.z).magnitude;
            CheckLocation(ok && p.inventory.galleons == 75, "Travel to " + w.name);
            CheckLocation(flat < 15f && pos.y > -0.5f && pos.y < w.pos.y + 4f && p.cc.isGrounded, $"Landed at {w.name}: {pos} grounded={p.cc.isGrounded}");
            CheckLocation(!Physics.CheckCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore) ||
                Physics.OverlapCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore).All(c => c is CharacterController), "Not stuck at " + w.name);
        }

        // ---- Таблички комнат читаются с обеих сторон ----
        var signs = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Sign: ")).ToList();
        CheckLocation(signs.Count >= 5 && signs.All(t => t.Find("Front") != null && t.Find("Back") != null && t.Find("Board") != null), "Signs are two-sided: " + signs.Count);
        var sg = signs.FirstOrDefault(t => t.name.Contains("Лазарет")) ?? signs.FirstOrDefault();
        if (sg != null)
        {
            yield return ShotFrom("sign_front", sg.position - sg.forward * 3.5f + Vector3.down * 0.6f, sg.position);
            yield return ShotFrom("sign_back", sg.position + sg.forward * 3.5f + Vector3.down * 0.6f, sg.position);
        }

        // ---- Библиотека: список заклинаний и понятный отказ ----
        var glac = p.caster.Slot(SpellType.Glacius);
        glac.learned = false; p.caster.learned.Remove(glac);
        p.inventory.galleons = 100;
        CheckLocation(!rs.Study(SpellType.Glacius) && !p.caster.Knows(SpellType.Glacius) && p.inventory.galleons == 100, "Library refuses without gold");
        CheckLocation(!rs.Study(SpellType.AvadaKedavra) && !rs.Study(SpellType.Patronus), "Library does not teach unforgivables or Patronus");
        p.inventory.galleons = 500;
        Place(new Vector3(-20, 0.2f, 43), new Vector3(-30, 1.5f, 43));
        yield return Frames(3);
        CheckLocation(RoomServices.TryUse(p) && GameState.Mode == GameMode.Service, "Library opens with E");
        yield return Shot("ui_library");
        CheckLocation(rs.Study(SpellType.Glacius) && p.caster.Knows(SpellType.Glacius) && p.inventory.galleons == 300, "Library teaches chosen spell");
        GameState.Set(GameMode.Play);
        HUD.Clear();

        // ---- Далёкие враги и сюжетный этап не блокируют перемещение ----
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(2);
        var stepWas = q.step;
        q.SetStep(QuestManager.Step.Rift, false);
        p.Teleport(WorldBuilder.Courtyard + Vector3.up * 0.3f);
        yield return Frames(3);
        CheckLocation(!RoomServices.Threatened, "Rift step far from the ruins does not block: " + RoomServices.ThreatReason);
        var far = Enemy.Spawn(EnemyKind.DarkWizard, p.transform.position + new Vector3(60, 0.5f, 0), true);
        yield return Frames(3);
        CheckLocation(!RoomServices.Threatened, "Aggro enemy 60 m away does not block: " + RoomServices.ThreatReason);
        far.transform.position = p.transform.position + new Vector3(100, 0.5f, 0);
        yield return Frames(3);
        CheckLocation(!far.aggro, "Enemy beyond 70 m calms down");
        var close = Enemy.Spawn(EnemyKind.DarkWizard, p.transform.position + new Vector3(10, 0.5f, 0), true);
        yield return Frames(2);
        CheckLocation(RoomServices.Threatened && RoomServices.ThreatReason.Contains("м)"), "Close enemy blocks with reason: " + RoomServices.ThreatReason);
        p.inventory.galleons = 100;
        CheckLocation(!rs.TravelTo(0), "Travel refused next to an enemy");
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(2);
        CheckLocation(rs.TravelTo(0), "Travel allowed once enemies are gone");
        q.SetStep(stepWas, false);
        yield return Frames(5);

        // ---- Конфринго поджигает издалека, Диффиндо режет ----
        {
            DayNight.I.SetHour(12f);
            var arena = new Vector3(90, 0.2f, -100);
            Place(arena, arena + new Vector3(0, 1, 20));
            yield return Frames(3);
            var torch = Enemy.Spawn(EnemyKind.DarkWizard, arena + new Vector3(0, 0.5f, 22), false);
            torch.health.Init(5000);
            yield return Frames(3);
            Aim(torch.targetable.AimPoint);
            p.caster.target = torch.targetable;
            p.caster.DebugCast(SpellType.Confringo);
            for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 3f && torch.burnT <= 0f;) yield return null;
            CheckLocation(torch.burnT > 2f, "Confringo sets a far enemy on fire (22 m): burn=" + torch.burnT.ToString("0.0"));
            yield return new WaitForSecondsRealtime(0.4f);
            yield return ShotFrom("spell_confringo_burn", torch.transform.position + new Vector3(3f, 1.6f, -4f), torch.transform.position + Vector3.up);
            Destroy(torch.gameObject);

            var victim = Enemy.Spawn(EnemyKind.DarkWizard, arena + new Vector3(0, 0.5f, 8), false);
            yield return Frames(3);
            victim.health.Init(400);
            Aim(victim.targetable.AimPoint);
            p.caster.target = victim.targetable;
            p.caster.DebugCast(SpellType.Diffindo);
            for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 2f && victim.cutT <= 0f;) yield return null;
            CheckLocation(victim.cutT > 0f && !victim.IsDead, "Diffindo cuts and bleeds");
            yield return ShotNow("spell_diffindo_slash", 1);
            victim.health.Init(20);
            victim.cutT = 0f;
            yield return new WaitForSecondsRealtime(0.3f);
            p.caster.DebugCast(SpellType.Diffindo);
            for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 2f && !victim.IsDead;) yield return null;
            CheckLocation(victim.IsDead && victim.sliced, "Finishing Diffindo slices the enemy in two");
            var halves = FindObjectsByType<SliceHalf>(FindObjectsSortMode.None);
            CheckLocation(halves.Length >= 2, "Two halves exist: " + halves.Length);
            var vp = arena + new Vector3(0, 0, 8);
            yield return ShotFrom("spell_diffindo_sliced", vp + new Vector3(3.2f, 1.4f, -3.2f), vp + Vector3.up * 0.9f);
            yield return new WaitForSecondsRealtime(1.1f);
            var hs = FindObjectsByType<SliceHalf>(FindObjectsSortMode.None);
            var mid = hs.Length > 0 ? hs.Aggregate(Vector3.zero, (acc, x) => acc + x.transform.position) / hs.Length : vp;
            yield return ShotFrom("spell_diffindo_halves", mid + new Vector3(2.4f, 1.3f, -2.4f), mid + Vector3.up * 0.2f);
        }

        // ---- Логово тёмных магов ----
        var morven = q.Npc("darkcamp");
        var izolda = q.Npc("darkgear");
        CheckLocation(morven != null && izolda != null, "Dark camp merchants exist");
        foreach (var id in ItemDB.Shops["darkcamp"].items.Concat(ItemDB.Shops["darkgear"].items))
            CheckLocation(ItemDB.Get(id) != null, "Dark item " + id);
        CheckLocation(ItemDB.Shops["darkcamp"].items.All(id => ItemDB.Get(id).forbidden), "Dark camp sells unforgivables");
        p.Teleport(RoomServices.Ground(RoomServices.Waypoints[2].pos));
        yield return Frames(5);
        yield return WalkTo(morven.transform.position + new Vector3(0, 0, -1.6f), 20f);
        Aim(morven.transform.position + Vector3.up * 1.4f);
        yield return Frames(5);
        CheckLocation(p.nearbyNpc == morven, "Can reach Morven: nearby=" + (p.nearbyNpc != null ? p.nearbyNpc.id : "none"));
        DayNight.I.SetHour(17.6f);
        Place(WorldBuilder.DarkCampPos + new Vector3(2, 0.2f, -13), WorldBuilder.DarkCampPos + new Vector3(0, 1.5f, 0));
        yield return Shot("dark_camp");
        DayNight.I.SetHour(11f);
        q.Talk(morven);
        int guard = 0;
        while (GameState.Mode == GameMode.Dialogue && guard++ < 10) { yield return Frames(2); Dialogue.Advance(); }
        yield return Frames(3);
        CheckLocation(GameState.Mode == GameMode.Shop, "Morven opens shop");
        p.inventory.galleons = 2000;
        p.caster.Slot(SpellType.Crucio).learned = false;
        p.caster.learned.RemoveAll(s => s.type == SpellType.Crucio);
        bool bought = p.inventory.Buy(ItemDB.Get("book_crucio"));
        CheckLocation(bought && p.caster.Knows(SpellType.Crucio), "Bought Crucio from dark camp");
        yield return Shot("shop_dark_camp");
        GameState.Set(GameMode.Play);

        // ---- Карта: Хогсмид и окрестности ----
        var map = CastleMap.I;
        p.Teleport(RoomServices.Ground(RoomServices.Waypoints[0].pos));
        yield return Frames(3);
        map.Open();
        CheckLocation(map.level == 4, "Map opens on Hogsmeade tab in village");
        CheckLocation(WorldBuilder.VillageHouses.Count >= 14, "Village houses recorded: " + WorldBuilder.VillageHouses.Count);
        map.SelectWaypoint(1);
        yield return Shot("ui_map_hogsmeade");
        map.level = 3;
        map.SelectWaypoint(2);
        yield return Shot("ui_map_outskirts_travel");
        p.inventory.galleons = 100;
        CheckLocation(rs.TravelTo(2) && GameState.Mode == GameMode.Play, "Travel from map closes it");
        yield return Frames(5);

        // ---- Титульный экран с выбором ----
        p.Teleport(WorldBuilder.Spawn);
        p.transform.rotation = Quaternion.identity;
        DayNight.I.SetHour(11f);
        p.SetGender(true);
        GameState.Set(GameMode.Title);
        yield return Shot("ui_title_girl");
        HUD.PickGender(false);
        yield return Shot("ui_title_boy");
        GameState.Set(GameMode.Play);

        // ---- Сохранение пола ----
        p.SetGender(true);
        SaveGame.TestPath = Path.Combine(Application.dataPath, "..", "Verification", "test-patch.json");
        CheckLocation(SaveGame.Save(false), "Save with girl");
        p.SetGender(false);
        CheckLocation(SaveGame.Load(), "Load accepted");
        yield return Frames(5);
        SaveGame.TestPath = null;
        CheckLocation(p.female && p.visual.name.Contains("witch"), "Load restores girl");
        p.SetGender(false);
        yield return DifficultyTest();
        Debug.Log($"HM_PATCH_RESULT errors={errors}");
    }

    static Transform FindBone(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t) { var f = FindBone(c, n); if (f != null) return f; }
        return null;
    }
}

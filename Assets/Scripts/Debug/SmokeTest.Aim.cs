using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public partial class SmokeTest
{
    IEnumerator SideMapTest()
    {
        SaveGame.TestPath = System.IO.Path.Combine(Application.dataPath, "../Build/Verification/side-map-save.json");
        GameState.Set(GameMode.Play); p.health.invulnerable = true;
        DayNight.I.SetHour(13); DayNight.I.raining = false;
        p.Teleport(WorldBuilder.Courtyard + Vector3.up * .3f);
        var sq = SideQuests.I;
        foreach (var quest in sq.all)
        {
            quest.state = SQState.Locked;
            CheckLocation(!sq.Marker(quest).HasValue, "Locked quest hidden: " + quest.id);
            quest.state = SQState.Available;
            CheckLocation(sq.Marker(quest).HasValue && Vector3.Distance(sq.Marker(quest).Value, q.Npc(quest.giver).transform.position) < .01f, "Offer at giver: " + quest.id);
        }
        CastleMap.I.Open(); CastleMap.I.level = 0; HUD.Clear();
        yield return Shot("map-side-offers"); CastleMap.I.Close();
        foreach (var quest in sq.all)
        {
            CheckLocation(sq.TryTalk(q.Npc(quest.giver)), "Accept dialogue: " + quest.id);
            yield return RunDialogue();
            CheckLocation(sq.Marker(quest).HasValue, "Objective marker: " + quest.id);
            CastleMap.I.SelectSideQuest(quest);
            var target = CastleMap.QuestTarget(out var title, out _);
            CheckLocation(sq.tracked == quest && title == quest.title && target.HasValue && Vector3.Distance(target.Value, sq.Marker(quest).Value) < .01f, "Track from map: " + quest.id);
            CheckLocation(CastleMap.I.level == CastleMap.TabOf(target.Value), "Correct map tab: " + quest.id);
        }
        var moon = sq.all.Find(quest => quest.id == "moon_alohomora");
        CastleMap.I.SelectSideQuest(moon); CastleMap.I.Open(); CastleMap.I.level = 3; HUD.Clear();
        yield return Shot("map-side-objectives"); CastleMap.I.Close();
        var nearest = sq.Marker(moon);
        foreach (var statue in sq.statues.ToArray()) statue.Collect();
        yield return Frames(3);
        CheckLocation(nearest.HasValue && moon.state == SQState.Ready && sq.statues.Count == 0, "Collecting statues completes objective");
        CheckLocation(Vector3.Distance(sq.Marker(moon).Value, q.Npc(moon.giver).transform.position) < .01f, "Ready marker returns to Moon");
        CastleMap.I.SelectSideQuest(moon); CastleMap.I.Open(); CastleMap.I.level = CastleMap.TabOf(sq.Marker(moon).Value); HUD.Clear();
        yield return Shot("map-side-return"); CastleMap.I.Close();
        sq.TryTalk(q.Npc(moon.giver)); yield return RunDialogue();
        CheckLocation(moon.state == SQState.Done && !sq.Marker(moon).HasValue, "Completed quest marker disappears");
    }

    IEnumerator WaitShot(SpellSlot slot)
    {
        float end = Time.realtimeSinceStartup + 5;
        while (slot.pending && Time.realtimeSinceStartup < end) yield return null;
        yield return Frames(2);
        CheckLocation(!slot.pending, "Projectile resolved: " + slot.name);
    }
    void PointCamera(Vector3 aim)
    {
        Camera.main.transform.position = p.transform.position + new Vector3(0.6f, 2, -4);
        Camera.main.transform.LookAt(aim);
    }
    IEnumerator AimHitTest()
    {
        SaveGame.TestPath = System.IO.Path.Combine(Application.dataPath, "../Build/Verification/aim-save.json");
        GameState.Set(GameMode.Play); p.health.invulnerable = true;
        DayNight.I.SetHour(13); DayNight.I.raining = false;
        foreach (var enemy in Enemy.All.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
        foreach (var dummy in FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None)) Destroy(dummy.gameObject);
        yield return Frames(3);
        foreach (var slot in p.caster.catalog) p.caster.Learn(slot.type);
        gm.camRig.enabled = false;
        p.Teleport(WorldBuilder.TrainingCenter + Vector3.up * 0.3f);
        yield return Frames(10);
        // Все типы врагов: реальный снаряд, реальное столкновение, перезарядка только после него.
        int enemies = 0;
        var spell = p.caster.Slot(SpellType.Expelliarmus);
        foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
        {
            var enemy = Enemy.Spawn(kind, p.transform.position + Vector3.forward * 10, false);
            enemy.enabled = false;
            yield return Frames(2);
            PointCamera(enemy.targetable.AimPoint);
            Physics.SyncTransforms();
            float hp = enemy.health.current;
            spell.cd = 0;
            p.caster.TryCast(spell);
            CheckLocation(spell.pending && spell.cd == 0, "No cooldown before impact: " + kind);
            int projectiles = FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length;
            p.caster.TryCast(spell);
            CheckLocation(FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length == projectiles, "No duplicate while pending: " + kind);
            yield return WaitShot(spell);
            CheckLocation(enemy.health.current < hp && spell.cd > 0, "Hit and cooldown: " + kind);
            Destroy(enemy.gameObject); yield return Frames(2); enemies++;
        }
        // Стена между палочкой и противником не считается попаданием.
        var behind = Enemy.Spawn(EnemyKind.DarkWizard, p.transform.position + Vector3.forward * 12, false); behind.enabled = false;
        var wall = Prims.Box(WorldBuilder.Dynamic, p.transform.position + new Vector3(0, 2, 4), new Vector3(6, 4, 0.5f), MatLib.Solid(Color.gray));
        yield return Frames(2); PointCamera(behind.targetable.AimPoint); Physics.SyncTransforms();
        spell.cd = 0; float hpBehind = behind.health.current;
        p.caster.TryCast(spell); yield return WaitShot(spell);
        CheckLocation(spell.cd == 0 && behind.health.current == hpBehind, "Wall miss costs no cooldown");
        Destroy(wall); Destroy(behind.gameObject); yield return Frames(3);
        // Жаровня: в прицеле чаша, а не только основание.
        var brazier = TrialBrazier.Spawn(p.transform.position + Vector3.forward * 12); brazier.Arm();
        yield return Frames(3); PointCamera(brazier.transform.position + Vector3.up * 1.4f); Physics.SyncTransforms();
        var fire = p.caster.Slot(SpellType.Confringo); fire.cd = 0;
        p.caster.WandImpact(out bool valid, out _); CheckLocation(valid, "Reticle sees brazier bowl");
        HUD.Clear(); yield return Shot("aim-brazier-before");
        p.caster.TryCast(fire); yield return WaitShot(fire);
        CheckLocation(brazier.lit && fire.cd > 0, "Confringo ignites aimed brazier");
        HUD.Clear(); yield return Shot("aim-brazier-lit");
        Destroy(brazier.gameObject); yield return Frames(3);
        PointCamera(p.transform.position + new Vector3(0, 100, 100));
        fire.cd = 0; p.caster.TryCast(fire); yield return WaitShot(fire);
        CheckLocation(fire.cd == 0, "Sky miss costs no cooldown");
        p.ancientUnlocked = true; p.ancient = 100; p.caster.CastAncient(); CheckLocation(p.ancient == 100, "Ancient magic miss preserves meter");
        var patronus = p.caster.Slot(SpellType.Patronus); patronus.cd = 0; p.caster.TryCast(patronus); yield return WaitShot(patronus);
        CheckLocation(patronus.cd == 0, "Patronus miss costs no cooldown");
        // Стыки стоек и перемычки не перекрываются.
        int frames = 0;
        foreach (Transform frame in WorldBuilder.Root.GetComponentsInChildren<Transform>())
        {
            if (frame.name != "DoorFrame") continue;
            var lintel = frame.Find("DoorLintel").GetComponent<Renderer>().bounds;
            foreach (Transform post in frame) if (post.name == "DoorPost")
                CheckLocation(post.GetComponent<Renderer>().bounds.max.y <= lintel.min.y + 0.001f, "Door frame surfaces do not overlap");
            frames++;
        }
        CheckLocation(frames == 5, "All five hall door frames verified");
        // Скины обоих героев: палочка и бонусы остаются на месте.
        p.caster.enabled = false;
        p.enabled = false;
        foreach (var projectile in FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
        yield return new WaitForSecondsRealtime(2f);
        float bonus = p.inventory.DamageMult;
        foreach (bool female in new[] { false, true })
        foreach (int skin in Enumerable.Range(0, PlayerController.SkinNames.Length))
        {
            p.SetGender(female); p.SetSkin(skin); p.visual.SetSpeed(0); yield return Frames(12);
            CheckLocation(p.visual != null && p.parts.wandTip.IsChildOf(p.visual.hand), "Skin wand attachment " + female + "/" + skin);
            CheckLocation(Mathf.Approximately(p.inventory.DamageMult, bonus), "Skin keeps equipment bonuses");
            yield return ShotFrom($"skin-{(female ? "girl" : "boy")}-{skin}", p.transform.position + new Vector3(2, 1.5f, 3), p.transform.position + Vector3.up);
        }
        p.SetSkin(1); SaveGame.Save(false); p.SetSkin(0); SaveGame.Load(); yield return Frames(8);
        CheckLocation(p.skin == 1, "Skin restored from save");
        p.enabled = true; p.caster.enabled = true;
        gm.camRig.enabled = true;
        Debug.Log($"HM_AIM_COVERAGE enemies={enemies} frames={frames} skins={PlayerController.SkinNames.Length * 2}");
    }
}

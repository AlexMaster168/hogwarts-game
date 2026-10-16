using System.Collections;
using System.Linq;
using UnityEngine;

public partial class SmokeTest
{
    IEnumerator SagaStep()
    {
        if (q.talkTarget != null) { q.Talk(q.Npc(q.talkTarget)); yield return RunDialogue(); yield break; }
        int phase = q.SagaStage;
        int ch = q.SagaChapter;
        var at = WorldBuilder.SagaPositions[ch];
        if (phase == 2 || phase == 5)
        {
            var runes = q.sagaRunes.ToArray();
            if (ch == 2) { p.Teleport(at + Vector3.back * 5); SagaMagic.Cast(p, SpellType.Lumos, at); }
            foreach (var rune in runes)
            {
                Place(rune.transform.position + Vector3.back * 4, rune.transform.position + Vector3.up * 1.7f);
                if (rune.required == SpellType.Lumos || rune.required == SpellType.Reparo) SagaMagic.Cast(p, rune.required, rune.transform.position);
                else for (int i = 0; i < 3 && !rune.lit; i++) rune.OnSpellHit(new SpellHit { type = rune.required, source = p.gameObject });
                yield return Frames(2);
            }
        }
        else if (phase == 3) { p.Teleport(q.markerPos); q.InteractExtension(); yield return RunDialogue(); }
        else
        {
            Place(at + new Vector3(0, 0.5f, -10), at + Vector3.up * 2);
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || e.IsDead || (e.transform.position - at).sqrMagnitude > 1600) continue;
                // Урон проходит через обработчик заклинаний, включая ограничение Древней магии на боссах.
                for (int i = 0; i < 15 && !e.IsDead; i++) e.OnSpellHit(new SpellHit { type = SpellType.Ancient, damage = 5000, source = p.gameObject, dir = Vector3.forward });
            }
        }
        yield return Frames(3);
    }

    IEnumerator SagaTest()
    {
        SaveGame.TestPath = System.IO.Path.Combine(Application.dataPath, "../Verification/saga-save.json");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SaveGame.TestPath));
        p.health.invulnerable = true;
        DayNight.I.SetHour(13); DayNight.I.raining = false;
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(3);
        yield return HallWalkTest();
        var inv = p.inventory;
        inv.galleons = 1000000; p.hasBroom = true;
        CheckLocation(ItemDB.All.Select(d => d.id).Distinct().Count() == ItemDB.All.Count, "Unique stock IDs");
        foreach (var shop in ItemDB.Shops.Values)
            foreach (var id in shop.items) CheckLocation(ItemDB.Get(id) != null, "Stock exists " + id);
        CheckLocation(inv.Buy(ItemDB.Get("bottomless_satchel")), "Bottomless satchel can be bought");
        foreach (var id in inv.potions.Keys.ToArray()) inv.potions[id] = 0;
        p.health.current = 10;
        inv.UsePotion("wiggenweld");
        CheckLocation(inv.InfinitePotions && p.health.current == 80 && inv.potions["wiggenweld"] == 0, "Infinite potion works from zero and does not consume");
        inv.Unequip(ItemKind.Artifact);
        yield return new WaitForSecondsRealtime(1.1f);
        float hp = p.health.current; inv.UsePotion("wiggenweld");
        CheckLocation(!inv.InfinitePotions && p.health.current == hp, "Removing satchel restores finite supply");
        inv.Equip(ItemDB.Get("bottomless_satchel"));
        int beforeGear = inv.galleons;
        CheckLocation(inv.Buy(ItemDB.Get("robe_keeper")) && inv.galleons == beforeGear - 12000, "Late robe spends correct gold");
        CheckLocation(inv.HpBonus >= 120 && inv.DamageMult > 1.3f, "Late robe bonuses applied");
        CheckLocation(inv.Buy(ItemDB.Get("broom_keeper")) && inv.BroomMult > 2.7f, "Late broom bonus applied");
        SaveGame.Save(true);
        inv.Unequip(ItemKind.Artifact); SaveGame.Load(); yield return Frames(5);
        CheckLocation(inv.InfinitePotions, "Satchel restored from save");
        HUD.Clear(); ShopUI.Open("pippin", true); yield return Shot("shop-bottomless-satchel"); GameState.Set(GameMode.Play);
        ShopUI.Open("gladrags", true); yield return Shot("shop-late-equipment"); GameState.Set(GameMode.Play);
        q.SetStep(QuestManager.Step.Ch7Call, false);
        int stages = 0;
        while (q.SagaActive && stages++ < 45)
        {
            var before = q.step;
            int ch = q.SagaChapter, phase = q.SagaStage;
            var at = WorldBuilder.SagaPositions[ch];
            if (phase == 2)
            {
                Place(at + new Vector3(0, 0.5f, -13), at + Vector3.up * 2);
                HUD.Clear();
                yield return Shot("chapter-" + (ch + 7) + "-puzzle");
                q.sagaRunes[2].OnSpellHit(new SpellHit { type = q.sagaRunes[2].required, source = p.gameObject });
                CheckLocation(q.sagaOrder == 0, "Wrong order resets chapter " + (ch + 7));
            }
            if (phase == 4)
            {
                Place(at + new Vector3(3, 0.5f, -9), q.boss.targetable.AimPoint);
                yield return Frames(3);
                q.boss.health.current = q.boss.health.max * 0.6f;
                q.boss.aggro = true;
                yield return Frames(3);
                CheckLocation(q.boss.IsBossKind && q.boss.targetable.isBoss, "Boss identity " + ch);
                HUD.Clear();
                yield return Shot("chapter-" + (ch + 7) + "-boss");
                // Слом щита правильным заклинанием не должен наносить лишь 15% урона.
                var breaker = ch == 0 ? SpellType.Aguamenti : ch == 1 ? SpellType.Reducto : ch == 2 ? SpellType.Lumos : SpellType.Incarcerous;
                if (breaker == SpellType.Lumos) SagaMagic.Cast(p, breaker, q.boss.transform.position);
                else q.boss.OnSpellHit(new SpellHit { type = breaker, damage = 10, source = p.gameObject });
            }
            yield return SagaStep(); yield return RunDialogue();
            CheckLocation(q.step != before, "Chapter advances from " + before);
        }
        CheckLocation(q.sagaComplete && q.step == QuestManager.Step.Free, "All four chapters complete");
        GameState.Set(GameMode.Play);
        foreach (var sp in new[] { SpellType.Lumos, SpellType.Reparo, SpellType.Aguamenti, SpellType.Reducto, SpellType.Incarcerous, SpellType.Rictusempra })
            CheckLocation(p.caster.Knows(sp), "Learned " + sp);
        SaveGame.Save(true); q.sagaComplete = false; SaveGame.Load(); yield return Frames(3);
        CheckLocation(q.sagaComplete, "Saga completion persists");
        Debug.Log("HM_SAGA_STAGES " + stages);
    }

    IEnumerator HallWalkTest()
    {
        GameState.Set(GameMode.Play);
        foreach (float z in new[] { 43f, 67f })
        foreach (int sign in new[] { -1, 1 })
        {
            Place(new Vector3(sign * 12f, 0.5f, z), new Vector3(sign * 22, 2, z));
            yield return Frames(45);
            float min = float.MaxValue, max = float.MinValue, camStep = 0;
            var previousCam = Camera.main.transform.position;
            for (int i = 0; i < 90; i++)
            {
                p.cc.Move(Vector3.right * sign * 0.075f);
                yield return null;
                min = Mathf.Min(min, p.transform.position.y); max = Mathf.Max(max, p.transform.position.y);
                camStep = Mathf.Max(camStep, Vector3.Distance(previousCam, Camera.main.transform.position)); previousCam = Camera.main.transform.position;
            }
            CheckLocation(sign * p.transform.position.x > 16 && max - min < 0.3f && camStep < 0.7f, $"Hall doorway {sign}/{z}: x={p.transform.position.x:0.00} dy={max-min:0.000} camera={camStep:0.000}");
            Debug.Log($"HM_DOORWAY side={sign} z={z} dy={max-min:0.000} cameraStep={camStep:0.000}");
        }
        Place(new Vector3(0, 0.5f, 30), new Vector3(0, 8, 75));
        yield return Frames(40);
        float start = Time.realtimeSinceStartup;
        yield return Frames(180);
        Debug.Log($"HM_HALL_FPS {180f / (Time.realtimeSinceStartup - start):0.0}");
        yield return Shot("great-hall-current");
        Place(new Vector3(-19, 0.5f, 43), new Vector3(-33, 2, 43));
        yield return Shot("library-doorway-current");
    }
}

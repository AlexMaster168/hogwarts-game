using System.Collections;
using System.Linq;
using UnityEngine;

// Глава 3 «Охота Руквуда» в автопрогоне + проверки сложности и скорости Древней магии.
public partial class SmokeTest
{
    IEnumerator Chapter3Steps()
    {
        if (p.onBroom) p.ToggleBroom();
        switch (q.step)
        {
            case QuestManager.Step.Ch3Call:
                yield return Talk("hecat", Vector3.back * 3);
                yield return Shot("story_ch3_call");
                break;
            case QuestManager.Step.Epilogue:
                yield return Talk("fig", Vector3.back * 3);
                break;
            case QuestManager.Step.PoacherTrail:
                Place(WorldBuilder.PoacherCampPos + new Vector3(0, 0.3f, 36), WorldBuilder.PoacherCampPos);
                yield return Frames(5);
                break;
            case QuestManager.Step.FortressMarch:
                Place(WorldBuilder.FortressPos + new Vector3(0, 0.3f, -32), WorldBuilder.FortressPos + Vector3.up * 3);
                yield return Shot("story_fortress");
                yield return Frames(5);
                break;
            case QuestManager.Step.FreeBeasts:
                for (int i = 0; i < WorldBuilder.CageSpots.Length && q.step == QuestManager.Step.FreeBeasts; i++)
                {
                    var c = WorldBuilder.CageSpots[i];
                    Place(c + new Vector3(0.6f, 0.3f, 2.4f), c + Vector3.up * 0.8f);
                    yield return Frames(3);
                    if (i == 0) yield return Shot("story_cages");
                    CheckLocation(q.InteractionHint() != null, "Cage hint " + i);
                    CheckLocation(q.InteractExtension(), "Open cage " + i);
                    yield return Frames(3);
                }
                break;
            case QuestManager.Step.Rookwood:
                if (q.boss != null) yield return FightTarget(q.boss.targetable, 120, "story_rookwood");
                break;
            default:
            {
                string shot = q.step switch
                {
                    QuestManager.Step.Werewolves => "story_werewolves",
                    QuestManager.Step.PoacherCamp => "story_poachers",
                    QuestManager.Step.GoblinMine => "story_mine",
                    QuestManager.Step.Graveyard => "story_necromancer",
                    _ => null,
                };
                bool first = true;
                foreach (var e in q.encounter.ToArray())
                    if (e != null && !e.IsDead)
                    {
                        yield return FightTarget(e.targetable, 30, first ? shot : null);
                        first = false;
                    }
                break;
            }
        }
    }

    // Сложность и Древняя магия: отдельный блок, вызывается из PatchTest.
    IEnumerator DifficultyTest()
    {
        GameState.Set(GameMode.Play);
        var arena = new Vector3(90, 0.2f, -100);
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(2);
        p.Teleport(arena);
        float[] hpBy = new float[4], hurtBy = new float[4];
        for (int lv = 0; lv < 4; lv++)
        {
            Difficulty.Set(lv);
            var w = Enemy.Spawn(EnemyKind.Werewolf, arena + new Vector3(0, 0.5f, 30), false);
            yield return Frames(2);
            hpBy[lv] = w.health.max;
            float before = p.health.current = p.health.max;
            p.ReceiveMelee(20f, true, null, Vector3.zero);
            hurtBy[lv] = before - p.health.current;
            Destroy(w.gameObject);
            yield return Frames(1);
        }
        CheckLocation(hpBy[0] < hpBy[1] && hpBy[1] < hpBy[2] && hpBy[2] < hpBy[3] && hpBy[3] >= hpBy[1] * 2.9f, $"Enemy HP by difficulty {string.Join("/", hpBy.Select(x => x.ToString("0")))}");
        CheckLocation(hurtBy[0] < hurtBy[1] && hurtBy[3] >= hurtBy[1] * 2.4f, $"Damage to hero by difficulty {string.Join("/", hurtBy.Select(x => x.ToString("0.0")))}");
        // Смена сложности на лету пересчитывает живых врагов.
        Difficulty.Set(1);
        var live = Enemy.Spawn(EnemyKind.Poacher, arena + new Vector3(0, 0.5f, 30), false);
        yield return Frames(2);
        float n = live.health.max;
        Difficulty.Set(3);
        CheckLocation(Mathf.Abs(live.health.max - n * 3f) < 1f, "Live enemy rescaled on difficulty change");
        Destroy(live.gameObject);
        Difficulty.Set(1);
        // Кошмар добавляет врагов в сюжетные схватки.
        var stepWas = q.step;
        Difficulty.Set(3);
        q.SetStep(QuestManager.Step.Werewolves, false);
        CheckLocation(q.encounter.Count == 6, "Nightmare adds 2 enemies to an encounter: " + q.encounter.Count);
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(2);
        q.SetStep(stepWas, false);
        Difficulty.Set(1);
        DayNight.I.SetHour(12f);
        p.health.current = p.health.max;
        HUD.Clear();
        GameState.Set(GameMode.Paused);
        yield return Shot("ui_pause_difficulty");
        GameState.Set(GameMode.Play);

        // Древняя магия: десяток обычных попаданий больше не заполняет шкалу.
        p.ancientUnlocked = true;
        p.ancient = 0f;
        var dummy = Enemy.Spawn(EnemyKind.Troll, arena + new Vector3(0, 0.5f, 12), false);
        yield return Frames(2);
        dummy.health.Init(99999);
        for (int i = 0; i < 10; i++)
            dummy.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = 25f, point = dummy.targetable.AimPoint, dir = Vector3.forward, source = p.gameObject });
        CheckLocation(p.ancient > 5f && p.ancient < 35f, "Ancient magic after 10 hits: " + p.ancient.ToString("0"));
        Destroy(dummy.gameObject);
        p.ancient = 0f;

        // Галереи новых врагов для README.
        yield return Gallery("enemies_chapter3", arena, 2.8f, 10f, EnemyKind.Werewolf, EnemyKind.Poacher, EnemyKind.Berserker, EnemyKind.Necromancer, EnemyKind.Rookwood);
        yield return Gallery("enemies_veil", arena, 3.6f, 15f, EnemyKind.GraveHound, EnemyKind.Chorister, EnemyKind.Mourner, EnemyKind.Bellringer, EnemyKind.NamelessKing, EnemyKind.Gatekeeper);
        yield return Gallery("enemies_chapter6", arena, 3.8f, 12f, EnemyKind.Vampire, EnemyKind.IceWraith, EnemyKind.Golem, EnemyKind.Mrax);
        DayNight.I.SetHour(12f);
        yield return Frames(2);
    }

    IEnumerator Gallery(string name, Vector3 arena, float step, float dist, params EnemyKind[] kinds)
    {
        DayNight.I.SetHour(11f);
        foreach (var c in FindObjectsByType<Coin>(FindObjectsSortMode.None)) Destroy(c.gameObject);
        p.Teleport(arena + new Vector3(0, 0, -30));
        float x0 = -step * (kinds.Length - 1) / 2f;
        for (int i = 0; i < kinds.Length; i++)
        {
            var e = Enemy.Spawn(kinds[i], arena + new Vector3(x0 + i * step, 0.5f, dist), false);
            bool flying = kinds[i] is EnemyKind.Mourner or EnemyKind.IceWraith;
            CheckLocation(e != null && (e.visual != null || flying), "New enemy has a model: " + kinds[i]);
            e.Stun(60f);
            e.transform.rotation = Quaternion.LookRotation(Vector3.back);
        }
        yield return Frames(10);
        yield return ShotFrom(name, arena + new Vector3(0, 2f + dist * 0.05f, 0f), arena + new Vector3(0, 1.4f + dist * 0.04f, dist));
        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        yield return Frames(2);
    }
}

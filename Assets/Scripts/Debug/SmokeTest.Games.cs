using System.Collections;
using UnityEngine;

// Квиддичный матч и Акцио-тир: старт со стойки, гол, отбор, очки за шары и награды.
public partial class SmokeTest
{
    IEnumerator GamesTest()
    {
        GameState.Set(GameMode.Play);
        var act = Activities.I;
        DayNight.I.SetHour(14f);

        // ---- Квиддич ----
        if (!p.hasBroom) p.hasBroom = true;
        if (p.onBroom) p.ToggleBroom();
        p.Teleport(Activities.MatchStand + new Vector3(1.2f, 0.2f, -1f));
        yield return Frames(3);
        CheckLocation(Activities.Hint(p) != null && Activities.Hint(p).Contains("Квиддичный матч"), "Quidditch stand hint");
        int gold0 = p.inventory.galleons, games0 = act.matchGames;
        CheckLocation(Activities.TryInteract(p) && act.match != null && act.matchGames == games0 + 1, "Quidditch match starts");
        var m = act.match;
        CheckLocation(m != null && m.flyers.Count == 8 && p.onBroom, "8 players on brooms");
        // Даём ИИ поиграть: мяч должен перейти к кому-то из игроков.
        float until = Time.realtimeSinceStartup + 8f;
        while (m != null && m.holder == null && Time.realtimeSinceStartup < until) yield return Frames(5);
        CheckLocation(m != null && m.holder != null, "Someone picks up the quaffle");
        p.Teleport(QuidditchMatch.C + new Vector3(4, 12, 6));
        yield return new WaitForSecondsRealtime(1.5f);
        yield return ShotFrom("quidditch_match", QuidditchMatch.C + new Vector3(-6, 22, -30), QuidditchMatch.C + new Vector3(4, 10, 2));
        // Герой берёт квоффл и бросает в кольцо, вратарь соперника оглушён бладжером.
        int us0 = m.score[0];
        m.Keeper(1).stun = 5f;
        var hoop = QuidditchMatch.Hoop(1, 0);
        p.Teleport(hoop + new Vector3(-10f, -1f, 0f));
        yield return Frames(2);
        m.Grab(m.player);
        CheckLocation(m.holder == m.player, "Player holds the quaffle");
        Place(hoop + new Vector3(-10f, -1f, 0f), hoop);
        yield return Frames(2);
        yield return Shot("quidditch_throw");
        m.Throw(m.player, (hoop - (p.transform.position + Vector3.up)).normalized);
        until = Time.realtimeSinceStartup + 3f;
        while (m.score[0] == us0 && Time.realtimeSinceStartup < until) yield return Frames(2);
        CheckLocation(m.score[0] == us0 + 10, "Player scores a goal: " + m.score[0]);
        // Отбор: соперник рядом с героем рано или поздно забирает мяч.
        m.Grab(m.player);
        var rival = m.flyers.Find(f => f.team == 1 && !f.keeper);
        until = Time.realtimeSinceStartup + 10f;
        while (m.holder == m.player && Time.realtimeSinceStartup < until)
        {
            rival.t.position = p.transform.position + Vector3.up + p.transform.right * 1.2f;
            yield return Frames(2);
        }
        CheckLocation(m.holder != m.player, "Rival chaser tackles the quaffle");
        m.score[1] = 0;
        m.Finish();
        yield return Frames(3);
        CheckLocation(act.match == null && act.matchWins >= 1 && p.inventory.galleons > gold0, "Quidditch win rewarded");
        if (p.onBroom) p.ToggleBroom();

        // ---- Акцио-тир ----
        p.Teleport(AccioRange.Stand + new Vector3(1.2f, 0.2f, -1f));
        yield return Frames(3);
        bool knew = p.caster.Knows(SpellType.Accio);
        var slot = p.caster.Slot(SpellType.Accio);
        slot.learned = false;
        p.caster.learned.Remove(slot);
        CheckLocation(Activities.Hint(p).Contains("выучите"), "Accio range needs Accio");
        CheckLocation(Activities.TryInteract(p) && act.accio == null, "Accio range locked without spell");
        p.caster.Learn(SpellType.Accio);
        CheckLocation(Activities.TryInteract(p) && act.accio != null, "Accio range starts");
        var g = act.accio;
        yield return Frames(5);
        CheckLocation(g.orbs.Count >= 4, "Orbs spawned: " + g.orbs.Count);
        // Настоящее Акцио: целимся в синий шар и кастуем быстрым ЛКМ-Акцио.
        foreach (var o in g.orbs) if (o != null && o.kind != 0) Destroy(o.gameObject);
        g.orbs.RemoveAll(o => o == null || o.kind != 0);
        var blue = AccioOrb.Spawn(g, 0, p.transform.position + p.transform.forward * 9f + Vector3.up * 2f);
        g.orbs.Add(blue);
        yield return Frames(2);
        Aim(blue.transform.position);
        yield return Frames(2);
        p.caster.CastAccioQuick();
        until = Time.realtimeSinceStartup + 3f;
        while (g.pulled + g.cursed == 0 && Time.realtimeSinceStartup < until) yield return Frames(2);
        CheckLocation(g.pulled >= 1 && g.score >= 10, "Accio spell pulls a blue orb: " + g.score);
        yield return Shot("accio_range");
        int s0 = g.score;
        var gold = g.orbs.Find(o => o != null && o.kind == 1);
        if (gold == null) { gold = AccioOrb.Spawn(g, 1, AccioRange.Stand + new Vector3(10, 2, 0)); g.orbs.Add(gold); }
        gold.ForcePull();
        yield return new WaitForSecondsRealtime(0.6f);
        CheckLocation(g.score == s0 + 25, "Golden orb gives 25: " + (g.score - s0));
        var red = AccioOrb.Spawn(g, 2, AccioRange.Stand + new Vector3(10, 2, 0));
        g.orbs.Add(red);
        s0 = g.score;
        red.ForcePull();
        yield return new WaitForSecondsRealtime(0.6f);
        CheckLocation(g.score == Mathf.Max(0, s0 - 15), "Cursed orb takes 15");
        int best0 = act.accioBest, final = g.score;
        g.timeLeft = 0.01f;
        yield return Frames(5);
        CheckLocation(act.accio == null && act.accioRuns >= 1 && act.accioBest == Mathf.Max(best0, final), "Accio range finished, best " + act.accioBest);
        var sv = SaveGame.Capture();
        CheckLocation(sv.matchWins == act.matchWins && sv.accioBest == act.accioBest, "Save keeps match wins and Accio record");
        if (!knew) { slot.learned = false; p.caster.learned.Remove(slot); }
        yield return Frames(2);
    }
}

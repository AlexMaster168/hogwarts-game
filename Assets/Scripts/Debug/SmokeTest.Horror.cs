using System.Collections;
using UnityEngine;

// Главы IV–VI в автопрогоне: разговоры, улики [E] и схватки со скриншотами для README.
public partial class SmokeTest
{
    IEnumerator HorrorSteps()
    {
        if (p.onBroom) p.ToggleBroom();
        var phase = q.step;
        string shot = phase switch
        {
            QuestManager.Step.Ch4Call => "story_ch4_call",
            QuestManager.Step.Ch4Hunt => "story_ch4_hounds",
            QuestManager.Step.Ch4Crypt => "story_ch4_choir",
            QuestManager.Step.Ch4Warden => "story_ch4_bellringer",
            QuestManager.Step.Ch5Gate => "story_ch5_gate",
            QuestManager.Step.Ch5Shadows => "story_ch5_mourners",
            QuestManager.Step.Ch5Sentinel => "story_ch5_gatekeeper",
            QuestManager.Step.Ch5Sovereign => "story_ch5_king",
            QuestManager.Step.Ch6Call => "story_ch6_call",
            QuestManager.Step.Ch6Manor => "story_ch6_manor",
            QuestManager.Step.Ch6Quarry => "story_ch6_golems",
            QuestManager.Step.Ch6Ice => "story_ch6_ice",
            QuestManager.Step.Ch6Map => "story_ch6_map",
            QuestManager.Step.Ch6Mrax => "story_ch6_mrax",
            _ => null,
        };
        if (q.talkTarget != null)
        {
            yield return Talk(q.talkTarget, Vector3.back * 3);
            if (shot != null) yield return Shot(shot);
            yield break;
        }
        if (!q.encounter.Exists(e => e != null && !e.IsDead))
        {
            Place(q.markerPos + Vector3.back * 2, q.markerPos + Vector3.up);
            yield return Frames(3);
            if (shot != null) yield return Shot(shot);
            CheckLocation(q.InteractionHint() != null, "Clue hint at " + phase);
            CheckLocation(q.InteractExtension(), "Clue interaction at " + phase);
            yield break;
        }
        // Схватка: сначала исходные враги, затем призванные подкрепления.
        float until = Time.realtimeSinceStartup + 160;
        bool first = true;
        while (q.step == phase && GameState.Playing && Time.realtimeSinceStartup < until)
        {
            Enemy target = null;
            float best = 200f;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.IsDead) continue;
                // Слуг босса бьём первыми: пока живы големы, Мракс под щитом.
                float d = Vector3.Distance(e.transform.position, q.markerPos) + (e.IsBossKind && !first ? 60f : 0f);
                if (d < best) { best = d; target = e; }
            }
            if (target == null) { yield return Frames(3); break; }
            yield return FightTarget(target.targetable, 45, first ? shot : null);
            first = false;
        }
    }
}

using System.Collections;
using System.IO;
using UnityEngine;

// Диагностика мерцания локаций: серия подряд идущих кадров с неподвижной камеры во время боя.
public partial class SmokeTest
{
    IEnumerator JitterTest()
    {
        GameState.Set(GameMode.Play);
        p.health.invulnerable = true;
        yield return StandTest();
        foreach (var n in notes) Debug.Log("HM_STAND " + n);
        foreach (var site in new[] { (QuestManager.Step.Ch4Hunt, QuestManager.Chapel, "chapel"), (QuestManager.Step.Ch5Shadows, QuestManager.Veil, "veil") })
        {
            q.SetStep(site.Item1, false);
            DayNight.I.SetHour(23f);
            var at = site.Item2;
            p.Teleport(at + new Vector3(0, 0.3f, -14));
            yield return Frames(30);
            var cam = new GameObject("JitterCam").AddComponent<Camera>();
            cam.CopyFrom(Camera.main);
            cam.transform.position = at + new Vector3(-6, 5, -22);
            cam.transform.LookAt(at + new Vector3(0, 1, 6));
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.enabled = false;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForEndOfFrame();
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(shotDir, $"jit_{site.Item3}_{i:00}.png"), tex.EncodeToPNG());
                // Бой продолжается: герой стреляет базовыми в ближайшего врага.
                if (i % 3 == 0) p.caster.DebugCast(SpellType.Basic);
            }
            Destroy(cam.gameObject); Destroy(rt); Destroy(tex);
        }
        // Бой с привратником: реальный экран вместе с HUD.
        q.SetStep(QuestManager.Step.Ch5Sentinel, false);
        DayNight.I.SetHour(1.5f);
        p.Teleport(QuestManager.Veil + new Vector3(0, 0.3f, -8));
        yield return Frames(10);
        for (int i = 0; i < 6; i++)
        {
            var b = q.boss;
            if (b != null) { b.aggro = true; Aim(b.targetable.AimPoint); p.caster.target = b.targetable; p.caster.DebugCast(SpellType.Basic); }
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Grab($"gate_{i}");
        }
        // Покадровый журнал камеры в реальном бою у часовни.
        q.SetStep(QuestManager.Step.Ch4Hunt, false);
        p.Teleport(QuestManager.Chapel + new Vector3(0, 0.3f, -6));
        yield return Frames(10);
        var rig = Camera.main.GetComponent<CameraRig>();
        var shakeF = typeof(CameraRig).GetField("shake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var sb = new System.Text.StringBuilder();
        float prevD = 0f; int jumps = 0, shakes = 0;
        for (int i = 0; i < 600; i++)
        {
            yield return null;
            var pivot = p.transform.position + Vector3.up * 1.6f;
            float d = Vector3.Distance(Camera.main.transform.position, pivot);
            float sh = (float)shakeF.GetValue(rig);
            if (i > 0 && Mathf.Abs(d - prevD) > 0.6f)
            {
                jumps++;
                var hits = Physics.SphereCastAll(pivot, 0.25f, (Camera.main.transform.position - pivot).normalized, 6f, ~0, QueryTriggerInteraction.Ignore);
                var names = new System.Collections.Generic.List<string>();
                foreach (var h in hits) if (!(h.collider is CharacterController) && !h.collider.transform.IsChildOf(p.transform)) names.Add(h.collider.name + "@" + h.distance.ToString("0.0") + "<" + (h.collider.transform.parent != null ? h.collider.transform.parent.name : "-"));
                sb.AppendLine($"jump f={i} {prevD:0.00}->{d:0.00} hits={string.Join(",", names)}");
            }
            if (sh > 0.05f) shakes++;
            prevD = d;
            if (i % 2 == 0)
            {
                Enemy tgt = null;
                foreach (var e in Enemy.All) if (e != null && !e.IsDead) { tgt = e; break; }
                if (tgt == null) { q.SetStep(QuestManager.Step.Ch4Hunt, false); continue; }
                Aim(tgt.targetable.AimPoint);
                p.caster.target = tgt.targetable;
                if (i % 10 == 0) p.caster.DebugCast(SpellType.Basic);
            }
        }
        Debug.Log($"HM_JITTER_CAM jumps={jumps} shakeFrames={shakes} \n{sb}");
        Debug.Log("HM_JITTER_DONE");
        Application.Quit(0);
    }
}

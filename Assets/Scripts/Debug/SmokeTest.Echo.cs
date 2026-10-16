using System.Collections;
using UnityEngine;

// Пожиратель эха: свой вид, зеркало эха, шкала босса только рядом. Плюс поиск плит, совпадающих с травой.
public partial class SmokeTest
{
    IEnumerator EchoTest()
    {
        GameState.Set(GameMode.Play);
        DayNight.I.SetHour(13f);
        // Плиты локаций не должны лежать в одной плоскости с травой (y = 0): это мерцает при движении камеры.
        int coplanar = 0;
        foreach (var r in WorldBuilder.Root.GetComponentsInChildren<MeshRenderer>())
        {
            var b = r.bounds;
            if (b.size.x < 6f || b.size.z < 6f || b.size.y > 0.5f) continue;
            if (r.name.Contains("Plane")) continue;
            if (Mathf.Abs(b.max.y) < 0.01f) { coplanar++; Debug.Log("HM_COPLANAR " + r.transform.parent?.name + "/" + r.name + " " + b); }
        }
        CheckLocation(coplanar == 0, "No location floors flush with the grass: " + coplanar);
        yield return StandTest();
        var at = QuestManager.RiftPos + new Vector3(0, 0.3f, 0);
        p.Teleport(at + new Vector3(0, 0, -14));
        yield return Frames(3);
        var e = Enemy.Spawn(EnemyKind.EchoDevourer, at, false);
        yield return Frames(3);
        CheckLocation(e.targetable.displayName == "Пожиратель эха" && e.IsBossKind && e.kind != EnemyKind.Boss, "Echo Devourer is its own boss");
        CheckLocation(HUD.BossBarVisible(e.targetable), "Boss bar visible nearby");
        p.Teleport(WorldBuilder.Courtyard + Vector3.up * 0.3f);
        yield return Frames(3);
        CheckLocation(!HUD.BossBarVisible(e.targetable), "Boss bar hidden from Hogwarts");
        p.Teleport(at + new Vector3(0, 0, -12));
        yield return Frames(3);
        // Зеркало: удар героя возвращается, сам босс получает четверть.
        e.mirrorT = 3f;
        float hp0 = p.health.current = p.health.max, bhp0 = e.health.current;
        e.OnSpellHit(new SpellHit { type = SpellType.Basic, damage = 40f, point = e.targetable.AimPoint, dir = Vector3.forward, source = p.gameObject });
        CheckLocation(p.health.current < hp0 && bhp0 - e.health.current <= 40f * 0.25f * 2.01f, $"Mirror reflects: hero {hp0 - p.health.current:0}, boss {bhp0 - e.health.current:0}");
        e.aggro = true;
        e.health.current = e.health.max * 0.5f;
        e.mirrorT = 0f;
        yield return Frames(20);
        Place(at + new Vector3(4, 0, -11), e.targetable.AimPoint);
        yield return Frames(4);
        yield return ShotFrom("boss_echo", at + new Vector3(-7, 4.5f, -13), at + Vector3.up * 3f);
        e.mirrorT = 3f;
        yield return Frames(3);
        yield return ShotFrom("boss_echo_mirror", at + new Vector3(6, 4f, -12), at + Vector3.up * 3f);
        Destroy(e.gameObject);
        yield return Frames(2);
    }

    // Герой спокойно стоит на каждой локации: высота не дрожит, «на земле» не мигает, камера не прыгает.
    IEnumerator StandTest()
    {
        var sites = new[] { QuestManager.Chapel, QuestManager.Veil, WorldBuilder.ManorPos, WorldBuilder.QuarryPos, WorldBuilder.IceCavePos, WorldBuilder.PeakPos,
            WorldBuilder.PoacherCampPos, WorldBuilder.FortressPos, WorldBuilder.GraveyardPos, QuestManager.RiftPos };
        foreach (var s in sites)
        {
            var spot = new Vector3(s.x + 3f, 0.5f, s.z - 10f);
            p.Teleport(spot);
            GameManager.I.camRig.SnapBehind();
            yield return Frames(60);
            float minY = float.MaxValue, maxY = float.MinValue, maxStep = 0f, prevY = p.transform.position.y;
            int flips = 0; bool prevG = p.cc.isGrounded;
            var camPrev = Camera.main.transform.position; float camStep = 0f;
            for (int i = 0; i < 90; i++)
            {
                yield return null;
                float y = p.transform.position.y;
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                maxStep = Mathf.Max(maxStep, Mathf.Abs(y - prevY)); prevY = y;
                if (p.cc.isGrounded != prevG) { flips++; prevG = p.cc.isGrounded; }
                camStep = Mathf.Max(camStep, (Camera.main.transform.position - camPrev).magnitude); camPrev = Camera.main.transform.position;
            }
            CheckLocation(maxY - minY < 0.02f && flips <= 1 && camStep < 0.05f, $"Hero stands still at {s}: dy={maxY - minY:0.000} flips={flips} cam={camStep:0.000}");
        }

    }
}

using System.Collections.Generic;
using UnityEngine;

// Ночные встречи: дементоры у озера и инферналы у склепа. Появляются каждую ночь после засады в Хогсмиде
// и рассеиваются с рассветом.
public class NightSpawns : MonoBehaviour
{
    public static NightSpawns I;
    public static readonly Vector3 DementorShore = WorldBuilder.LakeCenter + new Vector3(78, 0, 8);
    public static Vector3 InferiYard => SideQuests.CryptPos + new Vector3(0, 0, -20);

    public readonly List<Enemy> night = new();
    int spawnedDay = -1;
    float check;

    void Awake() { I = this; }

    public bool Unlocked => QuestManager.I != null && QuestManager.I.step >= QuestManager.Step.Ambush;

    void Update()
    {
        if (!GameState.Playing || DayNight.I == null) return;
        check -= Time.deltaTime;
        if (check > 0f) return;
        check = 2f;
        var dn = DayNight.I;
        night.RemoveAll(e => e == null || e.IsDead);
        if (dn.IsNight && Unlocked && spawnedDay != NightIndex(dn)) SpawnNight(dn);
        else if (!dn.IsNight && night.Count > 0) Dawn();
    }

    // Ночь «принадлежит» дню, в котором началась (после полуночи — ещё вчерашняя).
    static int NightIndex(DayNight dn) => dn.hour < 12f ? dn.day - 1 : dn.day;

    public void SpawnNight(DayNight dn)
    {
        spawnedDay = NightIndex(dn);
        for (int i = 0; i < 3; i++)
        {
            var off = Quaternion.Euler(0, i * 120f, 0) * Vector3.forward * 6f;
            night.Add(Enemy.Spawn(EnemyKind.Dementor, DementorShore + off + Vector3.up * 0.5f, false));
        }
        for (int i = 0; i < 4; i++)
            night.Add(Enemy.Spawn(EnemyKind.Inferius, InferiYard + new Vector3(-6f + i * 4f, 0.5f, (i % 2) * 3f), false));
        HUD.ShowToast("Ночью у озера кружат дементоры, у склепа бродят инферналы", new Color(0.65f, 0.75f, 1f));
    }

    void Dawn()
    {
        foreach (var e in night)
            if (e != null && !e.IsDead && !e.aggro)
            {
                FX.Burst(e.transform.position + Vector3.up, new Color(0.6f, 0.7f, 0.9f), 30, 4f, 0.5f, 0.8f, -0.4f);
                Destroy(e.gameObject);
            }
        night.RemoveAll(e => e == null || e.IsDead || !e.aggro);
    }
}

using System.Collections;
using UnityEngine;

// Запертые хранилища и Алохомора: без заклинания дверь не открыть, с ним — сундук с богатой наградой.
public partial class SmokeTest
{
    IEnumerator VaultTest()
    {
        GameState.Set(GameMode.Play);
        DayNight.I.SetHour(16f);
        CheckLocation(LockedVault.All.Count == LockedVault.Spots.Length, "Vaults built: " + LockedVault.All.Count);
        CheckLocation(q.Npc("moon") != null, "Caretaker Moon present");
        var moon = q.Npc("moon");
        Place(moon.transform.position + new Vector3(1.2f, 0, -2.6f), moon.transform.position + Vector3.up * 1.4f);
        yield return Frames(4);
        yield return Shot("npc_moon");
        bool had = p.alohomora;
        p.alohomora = false;
        var v = LockedVault.All[0];
        var front = v.DoorPos + v.transform.forward * 1.2f;
        Place(front, v.DoorPos + Vector3.up * 1.2f);
        yield return Frames(4);
        CheckLocation(LockedVault.Nearby(p.transform.position) == v, "Vault door detected nearby");
        yield return Shot("vault_locked");
        v.Use(p);
        yield return Frames(2);
        CheckLocation(!v.open, "Vault stays locked without Alohomora");
        // Закрытая дверь не пускает внутрь: луч к сундуку упирается в дверь.
        var from = v.DoorPos + Vector3.up * 1f;
        var to = v.chest.transform.position + Vector3.up * 0.4f;
        bool blocked = Physics.Raycast(from, to - from, out var hit, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(v.chest.transform);
        CheckLocation(blocked, "Closed door blocks the doorway");

        p.alohomora = true;
        v.Use(p);
        CheckLocation(v.open, "Alohomora opens the vault");
        yield return new WaitForSecondsRealtime(1f);
        blocked = Physics.Raycast(from, to - from, out hit, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(v.chest.transform);
        CheckLocation(!blocked, "Open doorway reaches the chest" + (blocked ? " (hit " + hit.collider.name + ")" : ""));
        Place(v.DoorPos + v.transform.forward * 3.2f + Vector3.up * 0.3f, v.chest.transform.position + Vector3.up * 0.6f);
        yield return Frames(4);
        yield return Shot("vault_open");
        int gold = p.inventory.galleons;
        v.chest.Open(p);
        yield return new WaitForSecondsRealtime(2.5f);
        CheckLocation(v.chest.opened, "Vault chest opened");
        var sv = SaveGame.Capture();
        CheckLocation(sv.alohomora && sv.vaults != null && sv.vaults[0] && !sv.vaults[1], "Save keeps Alohomora and vault state");
        // Карта показывает хранилища.
        CastleMap.I.Open(); CastleMap.I.level = 3;
        yield return Frames(3);
        yield return Shot("ui_map_vaults");
        CastleMap.I.Close();
        p.alohomora = had;
        yield return Frames(2);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SmokeTest
{
    IEnumerator RoomsVisibleTest()
    {
        SaveGame.TestPath = System.IO.Path.Combine(Application.dataPath, "../Build/Verification/rooms-visible-save.json");
        GameState.Set(GameMode.Play);
        DayNight.I.SetHour(13f); DayNight.I.raining = false;
        var renderers = new List<MeshRenderer>();
        renderers.AddRange(WorldBuilder.Root.GetComponentsInChildren<MeshRenderer>());
        renderers.AddRange(GameObject.Find("CastleKit").GetComponentsInChildren<MeshRenderer>());
        CheckLocation(renderers.Count > 100, "Static castle renderers collected");
        foreach (var room in CastleLocations.All)
        {
            p.Teleport(room.spawn);
            gm.camRig.SnapBehind();
            // Проверяем уже первый отрисованный кадр, не ожидая таймера прорисовки.
            for (int angle = 0; angle < 8; angle++)
            {
                gm.camRig.yaw = angle * 45f;
                yield return new WaitForEndOfFrame();
                int missing = 0;
                foreach (var renderer in renderers) if (renderer != null && !renderer.enabled) missing++;
                CheckLocation(missing == 0, $"Visible on entry/turn: {room.name}, angle={angle * 45}, hidden={missing}");
            }
        }
        yield return HallWalkTest();
        foreach (var room in CastleLocations.All)
        {
            if (room.name != "Библиотека" && room.name != "Гостиная" && room.name != "Кухня") continue;
            Place(room.spawn, room.spawn + new Vector3(8, 1.5f, 8));
            HUD.Clear();
            yield return Shot("visible-room-" + CastleLocations.All.IndexOf(room));
        }
        Debug.Log("HM_ROOMS_RENDERERS " + renderers.Count);
    }
}

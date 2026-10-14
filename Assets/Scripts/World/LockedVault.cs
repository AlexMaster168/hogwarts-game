using System.Collections.Generic;
using UnityEngine;

// Запертые хранилища: каменная кладовая, дверь с золотым замком и богатый сундук внутри.
// Дверь открывает только Алохомора — её учит смотритель Мун за три лунные статуэтки.
public class LockedVault : MonoBehaviour
{
    public static readonly List<LockedVault> All = new();
    public static readonly Vector3[] Spots =
    {
        new(-30, 0, -92), new(96, 0, -38), new(150, 0, -108), new(226, 0, -118),
        new(-76, 0, 82), new(182, 0, 190), new(-182, 0, -132), new(22, 0, 214),
    };
    public bool open;
    public Chest chest;
    Transform door;
    GameObject seal;
    float openT;

    public static int OpenedCount => All.FindAll(v => v.open).Count;
    public Vector3 DoorPos => transform.TransformPoint(new Vector3(0, 0, 2.6f));

    public static void BuildAll()
    {
        All.Clear();
        foreach (var p in Spots) Create(p);
    }

    static void Create(Vector3 p)
    {
        var go = new GameObject("Запертое хранилище");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        var face = -new Vector3(p.x, 0, p.z).normalized;
        go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(face));
        var v = go.AddComponent<LockedVault>();
        var t = go.transform;
        var stone = MatLib.Solid(new Color(0.48f, 0.47f, 0.5f), 0.1f, 0.35f, 0.6f);
        var trim = MatLib.Solid(new Color(0.3f, 0.29f, 0.32f), 0.1f);
        var gold = MatLib.Solid(new Color(0.95f, 0.75f, 0.25f), 0.8f, 1f, 0.2f);
        Prims.Box(t, new Vector3(0, 0.05f, 0), new Vector3(4f, 0.1f, 4f), trim);
        Prims.Box(t, new Vector3(0, 1.6f, -1.8f), new Vector3(4f, 3.2f, 0.4f), stone);
        Prims.Box(t, new Vector3(-1.8f, 1.6f, 0), new Vector3(0.4f, 3.2f, 4f), stone);
        Prims.Box(t, new Vector3(1.8f, 1.6f, 0), new Vector3(0.4f, 3.2f, 4f), stone);
        Prims.Box(t, new Vector3(-1.3f, 1.6f, 1.8f), new Vector3(1.4f, 3.2f, 0.4f), stone);
        Prims.Box(t, new Vector3(1.3f, 1.6f, 1.8f), new Vector3(1.4f, 3.2f, 0.4f), stone);
        Prims.Box(t, new Vector3(0, 2.85f, 1.8f), new Vector3(1.2f, 0.7f, 0.4f), stone);
        Prims.Box(t, new Vector3(0, 3.35f, 0), new Vector3(4.6f, 0.4f, 4.6f), trim);
        Prims.Cone(t, new Vector3(0, 3.55f, 0), 2.6f, 1.6f, MatLib.Solid(new Color(0.25f, 0.2f, 0.3f), 0.2f));
        // Дверь на петле слева: в закрытом виде перекрывает проём 1.2 × 2.5 м.
        v.door = Prims.Group(t, "Дверь");
        v.door.localPosition = new Vector3(-0.6f, 0, 1.85f);
        Prims.Box(v.door, new Vector3(0.6f, 1.25f, 0), new Vector3(1.2f, 2.5f, 0.14f), MatLib.Solid(new Color(0.32f, 0.18f, 0.08f), 0.2f, 0.8f, 0.5f));
        for (int i = 0; i < 3; i++) Prims.Box(v.door, new Vector3(0.6f, 0.4f + i * 0.85f, 0.08f), new Vector3(1.22f, 0.08f, 0.04f), trim, false);
        Prims.Box(v.door, new Vector3(1f, 1.2f, 0.1f), new Vector3(0.24f, 0.32f, 0.06f), gold, false);
        // Печать над дверью горит, пока хранилище закрыто.
        v.seal = Prims.Sphere(t, new Vector3(0, 2.85f, 2.05f), 0.36f, MatLib.Glow(new Color(1f, 0.6f, 0.15f), 3f));
        v.chest = Chest.Spawn(t.TransformPoint(new Vector3(0, 0.1f, -0.7f)), go.transform.eulerAngles.y);
        v.chest.rich = true;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static LockedVault Nearby(Vector3 p) => All.Find(v => !v.open && (v.DoorPos - p).sqrMagnitude < 2.6f * 2.6f);

    public void Use(PlayerController p)
    {
        if (open) return;
        if (!p.alohomora)
        {
            HUD.ShowToast("Заперто. Нужна Алохомора — спроси смотрителя Муна во дворе замка", new Color(1f, 0.75f, 0.4f));
            SFX.Play("click", 0.5f);
            return;
        }
        SFX.Voice("Alohomora");
        Unlock(true);
        Progression.I.Gain(40);
        HUD.ShowToast($"Алохомора! Хранилище открыто ({OpenedCount}/{All.Count})", new Color(1f, 0.85f, 0.4f));
    }

    public void Unlock(bool fx)
    {
        open = true;
        if (seal != null) seal.SetActive(false);
        if (!fx) { openT = 1f; door.localRotation = Quaternion.Euler(0, -105f, 0); return; }
        var at = transform.TransformPoint(new Vector3(0.4f, 1.2f, 2f));
        FX.Burst(at, new Color(1f, 0.85f, 0.4f), 50, 5f, 0.4f, 0.7f, -0.5f);
        FX.Flash(at, new Color(1f, 0.8f, 0.4f), 3f, 8f);
        SFX.Play("chest", 0.6f);
    }

    void Update()
    {
        if (!open || openT >= 1f) return;
        openT = Mathf.Min(1f, openT + Time.deltaTime * 1.6f);
        door.localRotation = Quaternion.Euler(0, -105f * Mathf.SmoothStep(0, 1, openT), 0);
    }
}

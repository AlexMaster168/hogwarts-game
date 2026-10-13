using System.Collections.Generic;
using UnityEngine;

// Локации третьей главы «Охота Руквуда»: лагерь браконьеров с клетками, гоблинская шахта,
// кладбище некроманта у склепа и крепость Виктора Руквуда.
public static partial class WorldBuilder
{
    public static readonly Vector3 WerewolfPos = new(100, 0, -112);
    public static readonly Vector3 PoacherCampPos = new(-200, 0, -180);
    public static readonly Vector3 MinePos = new(262, 0, -20);
    public static readonly Vector3 GraveyardPos = new(-58, 0, 122);
    public static readonly Vector3 FortressPos = new(250, 0, 232);
    public static readonly Vector3[] CageSpots = { new(-208, 0, -186), new(-192, 0, -188), new(-200, 0, -170) };
    public static readonly List<GameObject> CageDoors = new();

    static bool BlockedCh3(Vector3 p)
        => (p - PoacherCampPos).magnitude < 26 || (p - MinePos).magnitude < 24 || (p - GraveyardPos).magnitude < 14
           || (p - FortressPos).magnitude < 34 || (p - WerewolfPos).magnitude < 18;

    static void Chapter3()
    {
        PoacherCamp();
        GoblinMine();
        Graveyard();
        Fortress();
    }

    static void PoacherCamp()
    {
        var g = Prims.Group(Root, "PoacherCamp");
        var c = PoacherCampPos;
        var canvas = MatLib.Solid(new Color(0.45f, 0.38f, 0.26f), 0.05f);
        Prims.Box(g, c + Vector3.up * 0.03f, new Vector3(30, 0.06f, 26), dirt, false);
        Prims.Roof(g, c + new Vector3(-9, 0, 6), 5f, 3f, 6f, canvas, 90);
        Prims.Roof(g, c + new Vector3(9, 0, 7), 5f, 3f, 6f, canvas, 90);
        Prims.Cyl(g, c + new Vector3(0, 0, 2), 0.9f, 0.25f, stoneDark);
        Prims.Sphere(g, c + new Vector3(0, 0.6f, 2), 0.9f, flame);
        var l = new GameObject("CampFire").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = c + new Vector3(0, 1.5f, 2);
        l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.3f); l.range = 14; l.intensity = 1.6f;
        // Клетки с пойманными зверями: прутья, крыша и дверца, которую открывает игрок.
        CageDoors.Clear();
        var bars = MatLib.Solid(new Color(0.25f, 0.24f, 0.22f), 0.5f);
        var critter = MatLib.Glow(new Color(0.6f, 0.9f, 1f), 1.6f);
        foreach (var p in CageSpots)
        {
            Prims.Box(g, p + new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.1f, 2.2f), woodDark);
            Prims.Box(g, p + new Vector3(0, 2.05f, 0), new Vector3(2.2f, 0.1f, 2.2f), woodDark, false);
            for (int i = 0; i < 6; i++)
            {
                float x = -1f + i * 0.4f;
                Prims.Box(g, p + new Vector3(x, 1f, -1.05f), new Vector3(0.06f, 2f, 0.06f), bars, false);
                Prims.Box(g, p + new Vector3(-1.05f, 1f, x), new Vector3(0.06f, 2f, 0.06f), bars, false);
                Prims.Box(g, p + new Vector3(1.05f, 1f, x), new Vector3(0.06f, 2f, 0.06f), bars, false);
            }
            // Дверца (передняя стенка) смотрит на костёр — её и прячем, когда клетку открывают.
            var door = Prims.Group(g, "CageDoor");
            for (int i = 0; i < 6; i++)
                Prims.Box(door, p + new Vector3(-1f + i * 0.4f, 1f, 1.05f), new Vector3(0.06f, 2f, 0.06f), bars, false);
            Prims.Box(door, p + new Vector3(0, 1f, 1.05f), new Vector3(2.1f, 0.08f, 0.08f), gold, false);
            CageDoors.Add(door.gameObject);
            Prims.Sphere(g, p + new Vector3(0, 0.45f, 0), 0.7f, critter);
        }
    }

    static void GoblinMine()
    {
        var g = Prims.Group(Root, "GoblinMine");
        var c = MinePos;
        Prims.Make(PrimitiveType.Sphere, g, c + new Vector3(0, 0, 10), new Vector3(26, 14, 16), rock, true);
        Prims.Box(g, c + new Vector3(0, 2f, 2.6f), new Vector3(5f, 4f, 1f), MatLib.Solid(new Color(0.02f, 0.02f, 0.02f), 0f), false);
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Box(g, c + new Vector3(s * 2.8f, 2.2f, 2.2f), new Vector3(0.5f, 4.4f, 0.5f), woodDark);
            var lamp = c + new Vector3(s * 3.4f, 3f, 1.8f);
            Prims.Sphere(g, lamp, 0.4f, flame);
        }
        Prims.Box(g, c + new Vector3(0, 4.5f, 2.2f), new Vector3(6.4f, 0.5f, 0.6f), woodDark, false);
        Prims.Box(g, c + new Vector3(5f, 0.6f, -2f), new Vector3(1.6f, 1.1f, 2.4f), woodDark);
        for (int i = 0; i < 5; i++)
            Prims.Sphere(g, c + new Vector3(5f + Random.Range(-0.5f, 0.5f), 1.3f, -2f + Random.Range(-0.8f, 0.8f)), 0.45f, MatLib.Glow(new Color(0.4f, 0.8f, 1f), 1.4f));
        Prims.Box(g, c + new Vector3(0, 0.05f, -6), new Vector3(1.2f, 0.1f, 14), woodDark, false);
        var l = new GameObject("MineLight").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = c + new Vector3(0, 3f, 0);
        l.type = LightType.Point; l.color = new Color(1f, 0.7f, 0.35f); l.range = 14; l.intensity = 1.4f;
    }

    static void Graveyard()
    {
        var g = Prims.Group(Root, "Graveyard");
        var c = GraveyardPos;
        var grave = MatLib.Solid(new Color(0.42f, 0.44f, 0.45f), 0.1f);
        for (int x = -2; x <= 2; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                var p = c + new Vector3(x * 3.2f, 0, z * 3.4f);
                Prims.Box(g, p + Vector3.up * 0.55f, new Vector3(0.8f, 1.1f, 0.2f), grave, false, Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-10f, 10f), 0));
            }
        Prims.Cyl(g, c, 1.2f, 0.3f, stoneDark);
        Prims.Sphere(g, c + Vector3.up * 0.8f, 0.8f, MatLib.Glow(new Color(0.3f, 1f, 0.45f), 2f));
        FX.Loop(g, c + Vector3.up * 0.3f, new Color(0.3f, 1f, 0.45f, 0.35f), 16, 3f, 0.4f);
    }

    static void Fortress()
    {
        var g = Prims.Group(Root, "RookwoodFortress");
        var c = FortressPos;
        var wall = MatLib.Solid(new Color(0.32f, 0.3f, 0.3f), 0.1f, 0.25f, 0.6f);
        var banner = MatLib.Solid(new Color(0.55f, 0.05f, 0.08f), 0.2f);
        const float half = 15f, h = 6f;
        Prims.Box(g, c + Vector3.up * 0.04f, new Vector3(30, 0.08f, 30), MatLib.Solid(new Color(0.35f, 0.33f, 0.3f), 0.05f), false);
        Prims.Box(g, c + new Vector3(0, h / 2, half), new Vector3(30, h, 1.2f), wall);
        Prims.Box(g, c + new Vector3(-half, h / 2, 0), new Vector3(1.2f, h, 30), wall);
        Prims.Box(g, c + new Vector3(half, h / 2, 0), new Vector3(1.2f, h, 30), wall);
        // Южная стена с воротами шириной 6 м.
        Prims.Box(g, c + new Vector3(-9, h / 2, -half), new Vector3(12, h, 1.2f), wall);
        Prims.Box(g, c + new Vector3(9, h / 2, -half), new Vector3(12, h, 1.2f), wall);
        Prims.Box(g, c + new Vector3(0, h - 0.8f, -half), new Vector3(6, 1.6f, 1.2f), wall, false);
        foreach (var t in new[] { new Vector3(-half, 0, -half), new Vector3(half, 0, -half), new Vector3(-half, 0, half), new Vector3(half, 0, half) })
        {
            Prims.Cyl(g, c + t, 2.2f, 9f, wall);
            Prims.Cone(g, c + t + Vector3.up * 9f, 2.6f, 3.5f, MatLib.Solid(new Color(0.3f, 0.05f, 0.06f), 0.2f));
        }
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Box(g, c + new Vector3(s * 4.2f, 3.2f, -half - 0.7f), new Vector3(1.6f, 3.6f, 0.08f), banner, false);
            var br = c + new Vector3(s * 6f, 0, -6f);
            Prims.Cyl(g, br, 0.5f, 1.2f, stoneDark);
            Prims.Sphere(g, br + Vector3.up * 1.5f, 0.7f, flame);
        }
        Prims.Box(g, c + new Vector3(0, 0.5f, 10), new Vector3(6, 1f, 4), stoneDark);
        Prims.Box(g, c + new Vector3(0, 2.2f, 11.5f), new Vector3(2.4f, 2.6f, 0.6f), banner, false);
        var l = new GameObject("FortressLight").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = c + new Vector3(0, 5f, 0);
        l.type = LightType.Point; l.color = new Color(1f, 0.35f, 0.3f); l.range = 26; l.intensity = 1.5f;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Строит весь мир из примитивов: замок, мост, двор, арену, поле для полётов, Хогсмид, озеро, лес, руины.
public static partial class WorldBuilder
{
    // Ключевые точки сюжета.
    public static readonly Vector3 Spawn = new(0, 0, -118);
    public static readonly Vector3 FigPos = new(2.5f, 0, -106);
    public static readonly Vector3 Courtyard = new(0, 0, -15);
    public static readonly Vector3 HatPos = new(0, 0, 76);
    public static readonly Vector3 HecatPos = new(36, 0, -5);
    public static readonly Vector3 TrainingCenter = new(54, 0, 0);
    public static readonly Vector3 DuelCenter = new(52, 0, 48);
    public static readonly Vector3 KogawaPos = new(112, 0, 92);
    public static readonly Vector3 PitchCenter = new(135, 0, 120);
    public static readonly Vector3 HogsmeadeCenter = new(170, 0, -150);
    public static readonly Vector3 NatsaiPos = new(160, 0, -147);
    public static readonly Vector3 ForestCenter = new(-130, 0, 170);
    public static readonly Vector3 ForestClearing = new(-140, 0, 190);
    public static readonly Vector3 RuinsCenter = new(0, 0, 270);
    public static readonly Vector3 LakeCenter = new(-150, 0, -20);

    public static readonly Vector3[] Rings =
    {
        new(130, 18, 150), new(165, 28, 190), new(130, 40, 235), new(75, 45, 220),
        new(55, 35, 160), new(85, 25, 115), new(120, 20, 70), new(150, 15, 100),
    };

    public static readonly Vector3[] DummySpots =
    {
        new(48, 0, -10), new(57, 0, -14), new(65, 0, -7), new(66, 0, 7), new(57, 0, 13), new(48, 0, 9),
    };

    public static readonly Vector3[] PageSpots =
    {
        new(-20, 1.5f, 18), new(0, 1.5f, 60), new(62, 1.5f, 18), new(52, 1.5f, 57), new(170, 1.5f, -150),
        new(-85, 1.5f, -30), new(-150, 1.5f, 160), new(0, 1.8f, 262), new(-36, 66, 58), new(34, 52, 60),
    };

    public static Transform Root, Dynamic;

    static Material grass, grassDark, dirt, stone, stoneDark, floor, roof, wood, woodDark, water, sand, leaf, pine, rock, gold, windowGlow, candle, flame, straw;

    public static void Build()
    {
        Root = new GameObject("World").transform;
        Dynamic = new GameObject("WorldDynamic").transform;
        var state = Random.state;
        Random.InitState(1337);
        InitMaterials();
        kitRoot = new GameObject("CastleKit").transform;
        Ground();
        Castle();
        Interiors();
        Bridge();
        TrainingYard();
        DuelArena();
        Pitch();
        Hogsmeade();
        Lake();
        Forest();
        Ruins();
        Scenery();
        Paths();
        foreach (var p in PageSpots) FieldGuidePage.Spawn(p);
        Chests();
        Camps();
        TrialSites();
        Crypt();
        ShadyTent();
        Random.state = state;
        StaticBatchingUtility.Combine(Root.gameObject);
    }

    static void InitMaterials()
    {
        grass = MatLib.Solid(new Color(0.3f, 0.48f, 0.2f), 0.05f, 0.08f, 0.5f);
        grassDark = MatLib.Solid(new Color(0.16f, 0.27f, 0.13f), 0.05f, 0.1f, 0.5f);
        dirt = MatLib.Solid(new Color(0.48f, 0.4f, 0.28f), 0.05f, 0.2f, 0.5f);
        stone = MatLib.Solid(new Color(0.66f, 0.63f, 0.58f), 0.1f, 0.35f, 0.5f);
        stoneDark = MatLib.Solid(new Color(0.42f, 0.41f, 0.4f), 0.1f, 0.35f, 0.55f);
        floor = MatLib.Solid(new Color(0.55f, 0.52f, 0.48f), 0.25f, 0.5f, 0.5f);
        roof = MatLib.Solid(new Color(0.2f, 0.25f, 0.34f), 0.3f, 0.4f, 0.4f);
        wood = MatLib.Solid(new Color(0.45f, 0.3f, 0.17f), 0.2f, 0.6f, 0.5f);
        woodDark = MatLib.Solid(new Color(0.26f, 0.17f, 0.1f), 0.2f, 0.6f, 0.5f);
        water = MatLib.Fade(new Color(0.12f, 0.3f, 0.42f, 0.78f), 0.5f, 0.95f);
        sand = MatLib.Solid(new Color(0.62f, 0.56f, 0.42f), 0.05f, 0.15f, 0.4f);
        leaf = MatLib.Solid(new Color(0.24f, 0.42f, 0.16f), 0.05f, 0.5f, 0.6f);
        pine = MatLib.Solid(new Color(0.1f, 0.24f, 0.13f), 0.05f, 0.5f, 0.5f);
        rock = MatLib.Solid(new Color(0.48f, 0.47f, 0.45f), 0.1f, 0.15f, 0.6f);
        gold = MatLib.Solid(new Color(0.85f, 0.65f, 0.2f), 0.7f, 1f, 0.2f);
        windowGlow = MatLib.Glow(new Color(1f, 0.68f, 0.3f), 0.9f);
        candle = MatLib.Solid(new Color(0.95f, 0.92f, 0.85f), 0.3f);
        flame = MatLib.Glow(new Color(1f, 0.7f, 0.3f), 4f);
        straw = MatLib.Solid(new Color(0.75f, 0.62f, 0.35f), 0.05f, 1f, 0.5f);
    }

    static void Ground()
    {
        Prims.Make(PrimitiveType.Plane, Root, Vector3.zero, new Vector3(100, 1, 100), grass);
    }

    // ---------------- ЗАМОК ----------------
    static void Castle()
    {
        var c = Prims.Group(Root, "Castle");
        Prims.Box(c, new Vector3(0, 0.05f, 0), new Vector3(50, 0.1f, 50), floor);

        // Стены двора с тремя воротами.
        float h = 9f, t = 2f;
        Prims.Wall(c, new Vector3(-25, 0, -25), new Vector3(-4, 0, -25), h, t, stone, stone);
        Prims.Wall(c, new Vector3(4, 0, -25), new Vector3(25, 0, -25), h, t, stone, stone);
        Prims.Box(c, new Vector3(0, 7.5f, -25), new Vector3(8, 3, 2.4f), stoneDark);
        Prims.Wall(c, new Vector3(25, 0, -25), new Vector3(25, 0, -4), h, t, stone, stone);
        Prims.Wall(c, new Vector3(25, 0, 4), new Vector3(25, 0, 25), h, t, stone, stone);
        Prims.Box(c, new Vector3(25, 7.5f, 0), new Vector3(2.4f, 3, 8), stoneDark);
        Prims.Wall(c, new Vector3(-25, 0, -25), new Vector3(-25, 0, -4), h, t, stone, stone);
        Prims.Wall(c, new Vector3(-25, 0, 4), new Vector3(-25, 0, 25), h, t, stone, stone);
        Prims.Box(c, new Vector3(-25, 7.5f, 0), new Vector3(2.4f, 3, 8), stoneDark);
        Prims.Wall(c, new Vector3(-25, 0, 25), new Vector3(-14, 0, 25), h, t, stone, stone);
        Prims.Wall(c, new Vector3(14, 0, 25), new Vector3(25, 0, 25), h, t, stone, stone);

        foreach (var p in new[] { new Vector3(-25, 0, -25), new Vector3(25, 0, -25), new Vector3(-25, 0, 25), new Vector3(25, 0, 25) })
            Tower(c, p, 4f, 20f, 9f);
        Tower(c, new Vector3(-6.5f, 0, -25), 2.2f, 13f, 6f);
        Tower(c, new Vector3(6.5f, 0, -25), 2.2f, 13f, 6f);

        // Знамёна факультетов на внутренних стенах.
        for (int i = 0; i < 4; i++)
        {
            var m = MatLib.Solid(Houses.Main[i], 0.1f);
            var tr = MatLib.Solid(Houses.Second[i], 0.3f);
            Vector3 pos = i switch
            {
                0 => new Vector3(-14, 5.5f, -23.9f),
                1 => new Vector3(14, 5.5f, -23.9f),
                2 => new Vector3(-23.9f, 5.5f, 14),
                _ => new Vector3(23.9f, 5.5f, 14),
            };
            var rot = i < 2 ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
            Prims.Box(c, pos, new Vector3(3, 6, 0.1f), m, false, rot);
            Prims.Box(c, pos + Vector3.up * 0.5f + rot * new Vector3(0, 0, 0.06f), new Vector3(1.2f, 1.2f, 0.05f), tr, false, rot);
        }

        // Фонтан.
        Prims.Cyl(c, new Vector3(0, 0, 0), 4f, 0.9f, stoneDark);
        Prims.Cyl(c, new Vector3(0, 0.82f, 0), 3.7f, 0.05f, water, false);
        Prims.Cyl(c, new Vector3(0, 0, 0), 0.5f, 3f, stone);
        Prims.Cyl(c, new Vector3(0, 3f, 0), 1.4f, 0.3f, stone);
        FX.Loop(c, new Vector3(0, 3.3f, 0), new Color(0.4f, 0.7f, 1f, 0.6f), 30, 0.3f, 3f);

        foreach (var p in new[] { new Vector3(-15, 0, -15), new Vector3(15, 0, -15), new Vector3(-15, 0, 12), new Vector3(15, 0, 12) })
            Tree(c, p, 0.9f);

        GreatHall(c);

        // Крылья и высокие башни.
        Tower(c, new Vector3(-44, 0, 54), 6f, 46f, 18f);
        Tower(c, new Vector3(30, 0, 84), 4.5f, 36f, 14f);
        Tower(c, new Vector3(-14, 0, 85), 3f, 24f, 10f);
        Tower(c, new Vector3(14, 0, 85), 3f, 24f, 10f);
    }

    static void Tower(Transform p, Vector3 basePos, float r, float h, float roofH)
    {
        Prims.Cyl(p, basePos, r, h, stone);
        Prims.Cyl(p, basePos + Vector3.up * h, r + 0.5f, 1f, stoneDark);
        Prims.Cone(p, basePos + Vector3.up * (h + 1f), r + 1f, roofH, roof);
        TowerFlag(basePos + Vector3.up * (h + 1f + roofH - 0.3f), 2.5f + r * 0.4f);
        // Светящиеся окна по кругу.
        for (float y = 6; y < h - 2; y += 7)
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90 + y * 13;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                Prims.Box(p, basePos + Vector3.up * y + dir * (r - 0.05f), new Vector3(0.9f, 1.8f, 0.3f), windowGlow, false, Quaternion.LookRotation(dir));
            }
    }

    static void GreatHall(Transform c)
    {
        var g = Prims.Group(c, "GreatHall");
        float h = 15f, t = 1.2f;
        // Боковые стены с проёмами в крылья (z=43 и z=67), задняя — в трофейный зал.
        foreach (int s in new[] { -1, 1 })
        {
            float x = s * 14;
            Prims.Wall(g, new Vector3(x, 0, 25), new Vector3(x, 0, 41.5f), h, t, stone, stone);
            Prims.Wall(g, new Vector3(x, 0, 44.5f), new Vector3(x, 0, 65.5f), h, t, stone, stone);
            Prims.Wall(g, new Vector3(x, 0, 68.5f), new Vector3(x, 0, 85), h, t, stone, stone);
            foreach (float z in new[] { 43f, 67f })
            {
                Prims.Box(g, new Vector3(x, (4.1f + h) * 0.5f, z), new Vector3(t, h - 4.1f, 3f), stone);
                DoorFrame(g, new Vector3(x, 0, z), Vector3.forward, t);
            }
        }
        Prims.Wall(g, new Vector3(-14, 0, 85), new Vector3(-1.5f, 0, 85), h, t, stone, stone);
        Prims.Wall(g, new Vector3(1.5f, 0, 85), new Vector3(14, 0, 85), h, t, stone, stone);
        Prims.Box(g, new Vector3(0, (4.1f + h) * 0.5f, 85), new Vector3(3f, h - 4.1f, t), stone);
        DoorFrame(g, new Vector3(0, 0, 85), Vector3.right, t);
        Prims.Wall(g, new Vector3(-14, 0, 25), new Vector3(-4, 0, 25), h, t, stone);
        Prims.Wall(g, new Vector3(4, 0, 25), new Vector3(14, 0, 25), h, t, stone);
        Prims.Box(g, new Vector3(0, 11.5f, 25), new Vector3(8, 7, t), stone);
        Prims.Box(g, new Vector3(0, 0.06f, 55), new Vector3(27, 0.12f, 59), stoneDark);
        Prims.Roof(g, new Vector3(0, h, 55), 30, 9, 62, roof);
        // Тёмный потолок изнутри, чтобы не было видно неба сквозь щели.
        Prims.Box(g, new Vector3(0, h - 0.2f, 55), new Vector3(28, 0.4f, 60), stoneDark, false);

        for (float z = 31; z < 82; z += 8)
            foreach (int s in new[] { -1, 1 })
            {
                Prims.Box(g, new Vector3(s * 13.35f, 7.5f, z), new Vector3(0.12f, 7, 1.4f), windowGlow, false);
            }

        foreach (float x in new[] { -9f, -3f, 3f, 9f })
        {
            Prims.Box(g, new Vector3(x, 0.45f, 52), new Vector3(1.8f, 0.9f, 38), wood);
            Prims.Box(g, new Vector3(x - 1.4f, 0.25f, 52), new Vector3(0.6f, 0.5f, 38), woodDark);
            Prims.Box(g, new Vector3(x + 1.4f, 0.25f, 52), new Vector3(0.6f, 0.5f, 38), woodDark);
        }
        Prims.Box(g, new Vector3(0, 0.15f, 81.5f), new Vector3(26, 0.3f, 6), floor);
        foreach (int s in new[] { -1, 1 }) Prims.Box(g, new Vector3(s * 5.9f, 0.75f, 81), new Vector3(8.2f, 0.9f, 2), wood);

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -10.5f + i * 4.5f : 6f + (i - 2) * 4.5f;
            Prims.Box(g, new Vector3(x, 9, 84.3f), new Vector3(3.2f, 7, 0.1f), MatLib.Solid(Houses.Main[i], 0.1f), false);
            Prims.Box(g, new Vector3(x, 9.5f, 84.2f), new Vector3(1.4f, 1.4f, 0.05f), MatLib.Solid(Houses.Second[i], 0.3f), false);
        }

        // Парящие свечи.
        for (int i = 0; i < 70; i++)
        {
            var p = new Vector3(Random.Range(-12f, 12f), Random.Range(8.5f, 12f), Random.Range(29f, 80f));
            Prims.Cyl(g, p, 0.06f, 0.45f, candle, false);
            Prims.Sphere(g, p + Vector3.up * 0.55f, 0.13f, flame);
        }
        for (int i = 0; i < 4; i++)
        {
            var l = new GameObject("HallLight").AddComponent<Light>();
            l.transform.SetParent(g, false);
            l.transform.localPosition = new Vector3(0, 10, 33 + i * 15);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.78f, 0.5f);
            l.range = 24;
            l.intensity = 2.2f;
        }
    }

    // Деревянная рама дверного проёма (ширина 3, высота 4.1) с обеих сторон стены.
    static void DoorFrame(Transform g, Vector3 c, Vector3 along, float t)
    {
        var rot = Quaternion.LookRotation(along);
        foreach (int s in new[] { -1, 1 })
            Prims.Box(g, c + along * (s * 1.7f) + Vector3.up * 2.15f, new Vector3(t + 0.3f, 4.3f, 0.4f), woodDark, false, rot);
        Prims.Box(g, c + Vector3.up * 4.3f, new Vector3(t + 0.3f, 0.4f, 3.8f), woodDark, false, rot);
    }

    // ---------------- МОСТ ----------------
    static void Bridge()
    {
        var b = Prims.Group(Root, "Bridge");
        Prims.Box(b, new Vector3(0, 0.15f, -63.5f), new Vector3(8, 0.3f, 73), wood);
        for (float z = -98; z <= -28; z += 6)
            foreach (int s in new[] { -1, 1 })
                Prims.Cyl(b, new Vector3(s * 4f, 0, z), 0.25f, 5.2f, woodDark);
        Prims.Box(b, new Vector3(-4, 1.1f, -63.5f), new Vector3(0.2f, 0.15f, 73), woodDark);
        Prims.Box(b, new Vector3(4, 1.1f, -63.5f), new Vector3(0.2f, 0.15f, 73), woodDark);
        Prims.Roof(b, new Vector3(0, 5.2f, -63.5f), 9.6f, 2.6f, 74, roof);

        // Река под мостом, впадающая в озеро.
        Prims.Box(Root, new Vector3(50, 0.03f, -60), new Vector3(300, 0.04f, 20), sand, false);
        Prims.Box(Root, new Vector3(50, 0.22f, -60), new Vector3(300, 0.04f, 14), water, false);
    }

    // ---------------- ТРЕНИРОВОЧНЫЙ ДВОР ----------------
    static void TrainingYard()
    {
        var y = Prims.Group(Root, "TrainingYard");
        Prims.Box(y, new Vector3(50, 0.04f, 0), new Vector3(50, 0.08f, 44), dirt, false);
        Prims.Wall(y, new Vector3(27, 0, 22), new Vector3(48, 0, 22), 2.2f, 1, stone);
        Prims.Wall(y, new Vector3(56, 0, 22), new Vector3(74, 0, 22), 2.2f, 1, stone);
        Prims.Wall(y, new Vector3(27, 0, -22), new Vector3(74, 0, -22), 2.2f, 1, stone);
        Prims.Wall(y, new Vector3(74, 0, -22), new Vector3(74, 0, -4), 2.2f, 1, stone);
        Prims.Wall(y, new Vector3(74, 0, 4), new Vector3(74, 0, 22), 2.2f, 1, stone);
        Tower(y, new Vector3(74, 0, 22), 2f, 9f, 5f);
        Tower(y, new Vector3(74, 0, -22), 2f, 9f, 5f);
        for (int i = 0; i < 3; i++)
            Prims.Box(y, new Vector3(30, 0.6f, -15 + i * 1.6f), new Vector3(1.2f, 1.2f, 1.2f), wood);
    }

    static void DuelArena()
    {
        var a = Prims.Group(Root, "DuelArena");
        Prims.Cyl(a, DuelCenter, 12f, 0.4f, stone);
        Prims.Cyl(a, DuelCenter + Vector3.up * 0.4f, 4f, 0.03f, gold, false);
        var orb = MatLib.Glow(new Color(0.35f, 0.55f, 1f), 1.1f);
        for (int i = 0; i < 10; i++)
        {
            var d = Quaternion.Euler(0, i * 36, 0) * Vector3.forward;
            var p = DuelCenter + d * 11.5f;
            Prims.Cyl(a, p, 0.45f, 5f, stoneDark);
            Prims.Sphere(a, p + Vector3.up * 5.5f, 0.8f, orb);
        }
    }

    // ---------------- ПОЛЕ ДЛЯ ПОЛЁТОВ ----------------
    static void Pitch()
    {
        var p = Prims.Group(Root, "Pitch");
        var c = PitchCenter;
        Prims.Make(PrimitiveType.Cylinder, p, c + Vector3.up * 0.04f, new Vector3(70, 0.03f, 40), MatLib.Solid(new Color(0.25f, 0.45f, 0.18f), 0.05f, 0.15f, 0.6f), false);
        Prims.Box(p, c + new Vector3(0, 4, -20), new Vector3(56, 8, 4), wood);
        Prims.Box(p, c + new Vector3(0, 4, 20), new Vector3(56, 8, 4), wood);
        Prims.Box(p, c + new Vector3(-33, 4, 0), new Vector3(4, 8, 32), wood);
        Prims.Box(p, c + new Vector3(33, 4, 0), new Vector3(4, 8, 32), wood);
        for (int i = 0; i < 4; i++)
        {
            var m = MatLib.Solid(Houses.Main[i], 0.2f);
            Vector3 off = i switch { 0 => new(-14, 8.4f, -20), 1 => new(14, 8.4f, -20), 2 => new(-14, 8.4f, 20), _ => new(14, 8.4f, 20) };
            Prims.Box(p, c + off, new Vector3(26, 0.8f, 4.2f), m, false);
            Vector3 tw = i switch { 0 => new(-34, 0, -21), 1 => new(34, 0, -21), 2 => new(-34, 0, 21), _ => new(34, 0, 21) };
            Prims.Cyl(p, c + tw, 1.4f, 16, wood);
            Prims.Cone(p, c + tw + Vector3.up * 16, 2.2f, 6, m);
        }
        foreach (int s in new[] { -1, 1 })
            for (int k = -1; k <= 1; k++)
            {
                float hh = k == 0 ? 16 : 12;
                var b = c + new Vector3(s * 27, 0, k * 8);
                Prims.Cyl(p, b, 0.25f, hh, gold);
                Prims.Torus(p, b + Vector3.up * (hh + 2), 2f, gold, Quaternion.Euler(0, 0, 90));
            }
    }

    // ---------------- ХОГСМИД ----------------
    static void Hogsmeade()
    {
        var hg = Prims.Group(Root, "Hogsmeade");
        var c = HogsmeadeCenter;
        Prims.Box(hg, c + Vector3.up * 0.03f, new Vector3(92, 0.06f, 16), MatLib.Solid(new Color(0.5f, 0.47f, 0.42f), 0.1f, 0.6f, 0.6f), false);
        Color[] walls = { new(0.85f, 0.8f, 0.68f), new(0.7f, 0.6f, 0.45f), new(0.6f, 0.65f, 0.7f), new(0.65f, 0.72f, 0.6f), new(0.8f, 0.6f, 0.55f), new(0.55f, 0.45f, 0.4f) };
        Color[] roofs = { new(0.2f, 0.25f, 0.34f), new(0.45f, 0.15f, 0.12f), new(0.3f, 0.22f, 0.15f), new(0.25f, 0.3f, 0.25f) };
        for (int i = 0; i < 7; i++)
            foreach (int side in new[] { -1, 1 })
            {
                float x = c.x - 38 + i * 12.5f + Random.Range(-1f, 1f);
                float w = Random.Range(8f, 10.5f), d = 8f, h = Random.Range(5f, 8.5f);
                var center = new Vector3(x, 0, c.z + side * 16);
                var wm = MatLib.Solid(walls[Random.Range(0, walls.Length)], 0.1f, 0.4f, 0.4f);
                var rm = MatLib.Solid(roofs[Random.Range(0, roofs.Length)], 0.2f, 0.4f, 0.4f);
                Prims.Box(hg, center + Vector3.up * h * 0.5f, new Vector3(w, h, d), wm);
                Prims.Roof(hg, center + Vector3.up * h, d + 1.2f, Random.Range(3f, 4.5f), w + 1f, rm, 90);
                float front = c.z + side * (16 - d * 0.5f) - side * 0.05f;
                Prims.Box(hg, new Vector3(x, 1.2f, front), new Vector3(1.4f, 2.4f, 0.2f), woodDark, false);
                Prims.Box(hg, new Vector3(x - w * 0.3f, h * 0.55f, front), new Vector3(1.4f, 1.4f, 0.15f), windowGlow, false);
                Prims.Box(hg, new Vector3(x + w * 0.3f, h * 0.55f, front), new Vector3(1.4f, 1.4f, 0.15f), windowGlow, false);
                Prims.Box(hg, new Vector3(x + w * 0.3f, h + 2.5f, c.z + side * 17), new Vector3(0.9f, 3, 0.9f), stoneDark, false);
                if (Random.value < 0.6f)
                    Prims.Box(hg, new Vector3(x, 3.1f, front - side * 0.6f), new Vector3(2.2f, 0.7f, 0.1f), MatLib.Solid(Houses.Main[Random.Range(0, 4)], 0.3f), false);
            }
        for (int i = 0; i < 6; i++)
        {
            float x = c.x - 35 + i * 14;
            foreach (int side in new[] { -1, 1 })
            {
                var lp = new Vector3(x, 0, c.z + side * 7.5f);
                Prims.Cyl(hg, lp, 0.1f, 3.5f, woodDark);
                Prims.Sphere(hg, lp + Vector3.up * 3.7f, 0.4f, flame);
            }
        }
        for (int i = 0; i < 3; i++)
        {
            var l = new GameObject("VillageLight").AddComponent<Light>();
            l.transform.SetParent(hg, false);
            l.transform.position = c + new Vector3(-28 + i * 28, 5, 0);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.75f, 0.45f);
            l.range = 20;
            l.intensity = 1.2f;
        }
    }

    // ---------------- ОЗЕРО ----------------
    static void Lake()
    {
        var l = Prims.Group(Root, "Lake");
        Prims.Make(PrimitiveType.Cylinder, l, LakeCenter + Vector3.up * 0.02f, new Vector3(150, 0.02f, 150), sand, false);
        Prims.Make(PrimitiveType.Cylinder, l, LakeCenter + Vector3.up * 0.25f, new Vector3(140, 0.03f, 140), water, false);
        for (int i = 0; i < 26; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / 26 + Random.Range(-5f, 5f), 0) * Vector3.forward;
            var p = LakeCenter + d * Random.Range(70f, 76f);
            float s = Random.Range(1.5f, 4f);
            Prims.Make(PrimitiveType.Sphere, l, p, new Vector3(s, s * 0.6f, s * 1.2f), rock, true, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        }
        // Щупальца гигантского кальмара.
        var squid = MatLib.Solid(new Color(0.45f, 0.3f, 0.4f), 0.6f);
        for (int i = 0; i < 3; i++)
        {
            var p = LakeCenter + new Vector3(-10 + i * 6, 0, 8 - i * 4);
            Prims.Make(PrimitiveType.Capsule, l, p + Vector3.up * 1.5f, new Vector3(0.7f, 2.2f, 0.7f), squid, false, Quaternion.Euler(Random.Range(-25f, 25f), 0, Random.Range(-25f, 25f)));
        }
    }

    // ---------------- ЗАПРЕТНЫЙ ЛЕС ----------------
    static void Forest()
    {
        var f = Prims.Group(Root, "Forest");
        Prims.Make(PrimitiveType.Cylinder, f, ForestCenter + Vector3.up * 0.025f, new Vector3(170, 0.02f, 170), grassDark, false);
        int placed = 0;
        for (int i = 0; i < 1000 && placed < 230; i++)
        {
            var off = Random.insideUnitCircle * 82f;
            var p = ForestCenter + new Vector3(off.x, 0, off.y);
            if ((p - ForestClearing).magnitude < 17f) continue;
            if (DistToSegment(p, new Vector3(-100, 0, 90), ForestClearing) < 4f) continue;
            Pine(f, p, Random.Range(0.9f, 1.8f));
            placed++;
        }
        var shroom = MatLib.Glow(new Color(0.3f, 1f, 0.7f), 2f);
        for (int i = 0; i < 50; i++)
        {
            var off = Random.insideUnitCircle * 75f;
            Prims.Sphere(f, ForestCenter + new Vector3(off.x, 0.15f, off.y), Random.Range(0.2f, 0.4f), shroom);
        }
        // Поляна с постаментом реликвии.
        Prims.Cyl(f, ForestClearing, 1.1f, 1.2f, stone);
        for (int i = 0; i < 8; i++)
        {
            var d = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
            Prims.Make(PrimitiveType.Cube, f, ForestClearing + d * 9 + Vector3.up * 1.5f, new Vector3(1.2f, 3f, 0.8f), stoneDark, true, Quaternion.Euler(Random.Range(-8f, 8f), i * 45, Random.Range(-8f, 8f)));
        }
        var lt = new GameObject("ClearingLight").AddComponent<Light>();
        lt.transform.SetParent(f, false);
        lt.transform.position = ForestClearing + Vector3.up * 6;
        lt.type = LightType.Point;
        lt.color = new Color(0.4f, 0.8f, 1f);
        lt.range = 22;
        lt.intensity = 1.5f;
    }

    static void Pine(Transform p, Vector3 pos, float s)
    {
        Prims.Cyl(p, pos, 0.35f * s, 3f * s, woodDark);
        Prims.Cone(p, pos + Vector3.up * 2f * s, 2.6f * s, 4f * s, pine);
        Prims.Cone(p, pos + Vector3.up * 4.2f * s, 2.0f * s, 3.5f * s, pine);
        Prims.Cone(p, pos + Vector3.up * 6.2f * s, 1.3f * s, 3f * s, pine);
    }

    static void Tree(Transform p, Vector3 pos, float s)
    {
        Prims.Cyl(p, pos, 0.3f * s, 3.5f * s, wood);
        Prims.Sphere(p, pos + Vector3.up * 4.5f * s, 3.6f * s, leaf);
        Prims.Sphere(p, pos + new Vector3(0.9f, 5.5f, 0.4f) * s, 2.6f * s, leaf);
        Prims.Sphere(p, pos + new Vector3(-0.8f, 5.2f, -0.6f) * s, 2.4f * s, leaf);
    }

    // ---------------- РУИНЫ ----------------
    static void Ruins()
    {
        var r = Prims.Group(Root, "Ruins");
        var c = RuinsCenter;
        Prims.Cyl(r, c, 26f, 0.5f, stoneDark);
        Prims.Cyl(r, c, 10f, 0.55f, stone);
        Prims.Torus(r, c + Vector3.up * 0.6f, 6f, MatLib.Glow(new Color(0.3f, 0.7f, 1f), 2f));
        Prims.Box(r, c + Vector3.up * 1.5f, new Vector3(3, 2, 3), stone);
        for (int i = 0; i < 14; i++)
        {
            var d = Quaternion.Euler(0, i * 360f / 14, 0) * Vector3.forward;
            var p = c + d * 22;
            if (i % 5 == 2)
                Prims.Box(r, p + Vector3.up * 0.8f + d * 2, new Vector3(1.6f, 1.6f, 7), stone, true, Quaternion.LookRotation(Quaternion.Euler(0, 70, 0) * d));
            else
            {
                float ph = Random.Range(4f, 11f);
                Prims.Box(r, p + Vector3.up * ph * 0.5f, new Vector3(1.8f, ph, 1.8f), stone);
            }
        }
        Prims.Box(r, c + new Vector3(-5, 5, -26), new Vector3(2, 10, 2), stone);
        Prims.Box(r, c + new Vector3(5, 5, -26), new Vector3(2, 10, 2), stone);
        Prims.Box(r, c + new Vector3(0, 10.8f, -26), new Vector3(13, 1.6f, 2.4f), stone);
        var blue = new Color(0.3f, 0.6f, 1f);
        for (int i = 0; i < 4; i++)
        {
            var d = Quaternion.Euler(0, 45 + i * 90, 0) * Vector3.forward;
            var p = c + d * 14;
            Prims.Cyl(r, p, 0.7f, 1.6f, stoneDark);
            var holder = Prims.Group(r, "Brazier");
            holder.position = p + Vector3.up * 1.6f;
            FX.Loop(holder, Vector3.zero, blue, 30, 0.9f, 2f);
            var l = holder.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = blue;
            l.range = 14;
            l.intensity = 2f;
        }
    }

    // ---------------- ДЕКОР ----------------
    static void Scenery()
    {
        var s = Prims.Group(Root, "Scenery");
        var mtn = MatLib.Solid(new Color(0.38f, 0.42f, 0.36f), 0.05f, 0.03f, 0.6f);
        var snow = MatLib.Solid(new Color(0.92f, 0.94f, 0.97f), 0.3f, 0.05f, 0.2f);
        for (int i = 0; i < 24; i++)
        {
            var d = Quaternion.Euler(0, i * 15 + Random.Range(-4f, 4f), 0) * Vector3.forward;
            var p = d * Random.Range(440f, 480f);
            float w = Random.Range(140f, 220f), h = Random.Range(110f, 200f);
            Prims.Make(PrimitiveType.Sphere, s, p, new Vector3(w, h, w), mtn);
            Prims.Make(PrimitiveType.Sphere, s, p + Vector3.up * h * 0.36f, new Vector3(w * 0.42f, h * 0.3f, w * 0.42f), snow, false);
        }
        int trees = 0;
        for (int i = 0; i < 2000 && trees < 90; i++)
        {
            var off = Random.insideUnitCircle * 380f;
            var p = new Vector3(off.x, 0, off.y);
            if (Blocked(p)) continue;
            if (Random.value < 0.5f) Tree(s, p, Random.Range(0.8f, 1.5f)); else Pine(s, p, Random.Range(0.8f, 1.4f));
            trees++;
        }
        int rocks = 0;
        for (int i = 0; i < 2000 && rocks < 70; i++)
        {
            var off = Random.insideUnitCircle * 380f;
            var p = new Vector3(off.x, 0, off.y);
            if (Blocked(p)) continue;
            float k = Random.Range(1f, 3.5f);
            Prims.Make(PrimitiveType.Sphere, s, p, new Vector3(k * 1.3f, k * 0.7f, k), rock, true, Quaternion.Euler(0, Random.Range(0, 360f), 0));
            rocks++;
        }
        int bushes = 0;
        var bush = MatLib.Solid(new Color(0.2f, 0.36f, 0.14f), 0.05f, 0.6f, 0.6f);
        for (int i = 0; i < 3000 && bushes < 140; i++)
        {
            var off = Random.insideUnitCircle * 380f;
            var p = new Vector3(off.x, 0, off.y);
            if (Blocked(p)) continue;
            float k = Random.Range(1f, 2.2f);
            Prims.Make(PrimitiveType.Sphere, s, p + Vector3.up * 0.2f, new Vector3(k * 1.4f, k, k * 1.2f), bush, false);
            bushes++;
        }
    }

    static bool Blocked(Vector3 p)
    {
        if (p.x > -55 && p.x < 80 && p.z > -35 && p.z < 185) return true;        // замок и двор
        if (Mathf.Abs(p.x) < 12 && p.z > -135 && p.z < -20) return true;           // мост
        if (Mathf.Abs(p.z + 60) < 12 && p.x > -100 && p.x < 200) return true;     // река
        if ((p - PitchCenter).magnitude < 50) return true;
        if (p.x > 120 && p.x < 220 && p.z > -175 && p.z < -125) return true;
        if ((p - LakeCenter).magnitude < 80) return true;
        if ((p - ForestCenter).magnitude < 90) return true;
        if ((p - RuinsCenter).magnitude < 35) return true;
        if ((p - DuelCenter).magnitude < 18) return true;
        foreach (var s in Trials.Sites) if ((p - s).magnitude < 22) return true;
        if ((p - SideQuests.CryptPos).magnitude < 20) return true;
        if ((p - QuestManager.ConvoyPos).magnitude < 25 || (p - QuestManager.RescuePos).magnitude < 25 || (p - QuestManager.RiftPos).magnitude < 30) return true;
        foreach (var r in Rings) if ((new Vector3(r.x, 0, r.z) - p).magnitude < 8) return true;
        foreach (var m in Activities.MerlinSites) if ((m - p).magnitude < 12) return true;
        foreach (var b in Activities.BeastSpots) if ((b.home - p).magnitude < 10) return true;
        if ((Activities.SnitchStand - p).magnitude < 6) return true;
        return false;
    }

    // ---------------- ТРОПИНКИ ----------------
    static void Paths()
    {
        var p = Prims.Group(Root, "Paths");
        PathLine(p, new Vector3(0, 0, -100), new Vector3(0, 0, -128), new Vector3(60, 0, -140), new Vector3(125, 0, -150));
        PathLine(p, new Vector3(-26, 0, 0), new Vector3(-60, 0, 15), new Vector3(-100, 0, 90), ForestClearing);
        PathLine(p, new Vector3(75, 0, 0), new Vector3(100, 0, 40), KogawaPos, PitchCenter + new Vector3(0, 0, -24));
        PathLine(p, new Vector3(52, 0, 22), DuelCenter, new Vector3(40, 0, 120), new Vector3(10, 0, 200), RuinsCenter + new Vector3(0, 0, -26));
    }

    static void PathLine(Transform p, params Vector3[] pts)
    {
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            a.y = b.y = 0;
            var d = b - a;
            Prims.Box(p, (a + b) * 0.5f + Vector3.up * 0.035f, new Vector3(4, 0.04f, d.magnitude + 4), dirt, false, Quaternion.LookRotation(d.normalized));
        }
    }

    public static readonly Vector3[] ChestSpots =
    {
        new(-20, 0.1f, -20), new(10, 0.12f, 77), new(70, 0, -18), new(52, 0.4f, 56), new(160, 0, 92), new(205, 0, -142),
        new(-80, 0, -78), new(-160, 0, 130), new(-110, 0, 215), new(-200, 0, 190), new(12, 0.55f, 280), new(250, 0, -30),
        new(-240, 0, -150), new(100, 0, 250), new(-60, 0, -180), new(140, 0, -60),
    };

    static void Chests()
    {
        foreach (var p in ChestSpots) Chest.Spawn(p, Random.Range(0f, 360f));
    }

    public static readonly Vector3 ShadyPos = new(202, 0, -181);

    static void Camps()
    {
        Camp(new Vector3(-90, 0, -150), QuestManager.Step.MeetNatsai, EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.DarkWizard);
        Camp(new Vector3(240, 0, 40), QuestManager.Step.MeetNatsai, EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.Spider);
        Camp(new Vector3(-230, 0, 60), QuestManager.Step.MeetNatsai, EnemyKind.Spider, EnemyKind.Spider, EnemyKind.Spider);
        Camp(new Vector3(110, 0, 230), QuestManager.Step.GoForest, EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.Troll);
        Camp(new Vector3(205, 0, 135), QuestManager.Step.GoForest, EnemyKind.Goblin, EnemyKind.Goblin, EnemyKind.Goblin, EnemyKind.Goblin);
        Camp(new Vector3(-40, 0, -235), QuestManager.Step.GoForest, EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.DarkWizard, EnemyKind.Spider);
    }

    static void Camp(Vector3 c, QuestManager.Step unlock, params EnemyKind[] kinds)
    {
        var g = Prims.Group(Root, "Camp");
        var tent = MatLib.Solid(new Color(0.35f, 0.12f, 0.1f), 0.1f, 0.6f, 0.5f);
        Prims.Roof(g, c + new Vector3(-5, 0, 3), 4f, 3f, 5f, tent, 30);
        Prims.Roof(g, c + new Vector3(5, 0, 2), 4f, 3f, 5f, tent, -40);
        Prims.Make(PrimitiveType.Cylinder, g, c + Vector3.up * 0.2f, new Vector3(0.3f, 0.6f, 0.3f), woodDark, false, Quaternion.Euler(90, 30, 0));
        Prims.Make(PrimitiveType.Cylinder, g, c + Vector3.up * 0.2f, new Vector3(0.3f, 0.6f, 0.3f), woodDark, false, Quaternion.Euler(90, -40, 0));
        var fire = Prims.Group(g, "Campfire");
        fire.position = c + Vector3.up * 0.3f;
        FX.Loop(fire, Vector3.zero, new Color(1f, 0.5f, 0.15f), 35, 0.7f, 2f);
        var l = fire.gameObject.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.55f, 0.2f);
        l.range = 12;
        l.intensity = 2f;
        EnemyCamp.Create(c, kinds).unlockStep = unlock;
    }

    static void TrialSites()
    {
        for (int i = 0; i < 3; i++)
        {
            var g = Prims.Group(Root, "TrialSite");
            var c = Trials.Sites[i];
            Prims.Cyl(g, c, 13f, 0.4f, stoneDark);
            Prims.Torus(g, c + Vector3.up * 0.45f, 10f, MatLib.Glow(Trials.Colors[i], 1.2f));
            var orb = MatLib.Glow(Trials.Colors[i], 1.5f);
            for (int k = 0; k < 6; k++)
            {
                var d = Quaternion.Euler(0, k * 60 + 30, 0) * Vector3.forward;
                Prims.Cyl(g, c + d * 12f, 0.6f, 4.5f, stone);
                Prims.Sphere(g, c + d * 12f + Vector3.up * 5.1f, 0.9f, orb);
            }
            Prims.Box(g, c + new Vector3(0, 1.2f, -9.5f), new Vector3(3.2f, 2.4f, 0.6f), stone);
        }
    }

    static void Crypt()
    {
        var g = Prims.Group(Root, "Crypt");
        var c = SideQuests.CryptPos;
        Prims.Box(g, c + new Vector3(0, 3.5f, 6), new Vector3(10, 7, 9), stoneDark);
        Prims.Roof(g, c + new Vector3(0, 7, 6), 11, 3.5f, 10, roof, 90);
        Prims.Box(g, c + new Vector3(0, 1.6f, 1.45f), new Vector3(2.4f, 3.2f, 0.2f), MatLib.Solid(new Color(0.05f, 0.05f, 0.06f)), false);
        foreach (int s in new[] { -1, 1 }) Prims.Cyl(g, c + new Vector3(s * 3.5f, 0, 1f), 0.45f, 6f, stone);
        for (int i = 0; i < 10; i++)
        {
            var p = c + new Vector3(Random.Range(-14f, 14f), 0, Random.Range(-14f, -2f));
            Prims.Box(g, p + Vector3.up * 0.6f, new Vector3(0.8f, 1.2f, 0.25f), stone, true, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(-20f, 20f), 0));
        }
        var l = new GameObject("CryptLight").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = c + new Vector3(0, 3, 0);
        l.type = LightType.Point;
        l.color = new Color(0.5f, 1f, 0.6f);
        l.range = 14;
        l.intensity = 1.5f;
    }

    static void ShadyTent()
    {
        var g = Prims.Group(Root, "ShadyTent");
        Prims.Roof(g, ShadyPos + new Vector3(0, 0, -3), 5f, 3.2f, 4f, MatLib.Solid(new Color(0.08f, 0.1f, 0.08f), 0.1f), 90);
        Prims.Cyl(g, ShadyPos + new Vector3(1.8f, 0, 0), 0.08f, 2.2f, woodDark);
        Prims.Sphere(g, ShadyPos + new Vector3(1.8f, 2.4f, 0), 0.35f, MatLib.Glow(new Color(0.3f, 1f, 0.35f), 2f));
    }

    public static float DistToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        p.y = a.y = b.y = 0;
        var ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        return (a + ab * t - p).magnitude;
    }
}

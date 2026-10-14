using UnityEngine;

// Локации глав IV–VI: часовня безмолвных, Врата Завесы, поместье Мракс, каменоломня,
// ледяная пещера и Утёс ветров.
public static partial class WorldBuilder
{
    public static readonly Vector3 ManorPos = new(-285, 0, -10);
    public static readonly Vector3 QuarryPos = new(110, 0, -240);
    public static readonly Vector3 IceCavePos = new(-150, 0, -238);
    public static readonly Vector3 PeakPos = new(285, 0, -110);

    static bool BlockedHorror(Vector3 p)
    {
        foreach (var c in new[] { QuestManager.Chapel, QuestManager.Veil, ManorPos, QuarryPos, IceCavePos, PeakPos })
            if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(c.x, 0, c.z)) < 30) return true;
        return false;
    }

    static void HorrorSites()
    {
        Chapel(QuestManager.Chapel - Vector3.up * 0.3f);
        VeilGate(QuestManager.Veil - Vector3.up * 0.3f);
        Manor(ManorPos);
        Quarry(QuarryPos);
        IceCave(IceCavePos);
        Peak(PeakPos);
    }

    static void SiteLight(Transform g, Vector3 at, Color c, float range, float intensity)
    {
        var l = new GameObject("SiteLight").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = at;
        l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity;
    }

    // Глава IV: заброшенная часовня с колокольней, погост и колокол на раме у входа.
    static void Chapel(Vector3 c)
    {
        var g = Prims.Group(Root, "Часовня безмолвных");
        var wall = MatLib.Solid(new Color(0.3f, 0.31f, 0.34f), 0.1f, 0.3f, 0.6f);
        var dark = MatLib.Solid(new Color(0.12f, 0.12f, 0.15f));
        var bronze = MatLib.Solid(new Color(0.55f, 0.4f, 0.18f), 0.7f);
        var ghost = new Color(0.55f, 0.2f, 0.85f);
        Prims.Box(g, c + Vector3.up * 0.03f, new Vector3(46, 0.06f, 46), MatLib.Solid(new Color(0.2f, 0.22f, 0.2f), 0.05f), false);
        // Неф: стены вокруг (0, 0, 22), вход с юга.
        var n = c + new Vector3(0, 0, 22);
        Prims.Box(g, n + new Vector3(0, 4, 7), new Vector3(11, 8, 1), wall);
        Prims.Box(g, n + new Vector3(-5.5f, 4, 0), new Vector3(1, 8, 15), wall);
        Prims.Box(g, n + new Vector3(5.5f, 4, 0), new Vector3(1, 8, 15), wall);
        Prims.Box(g, n + new Vector3(-3.5f, 4, -7), new Vector3(4, 8, 1), wall);
        Prims.Box(g, n + new Vector3(3.5f, 4, -7), new Vector3(4, 8, 1), wall);
        Prims.Box(g, n + new Vector3(0, 6.5f, -7), new Vector3(3, 3, 1), wall);
        Prims.Roof(g, n + new Vector3(0, 8, 0), 12.5f, 4.5f, 16f, MatLib.Solid(new Color(0.18f, 0.16f, 0.2f)), 90);
        foreach (int s in new[] { -1, 1 })
            for (int k = -1; k <= 1; k++)
                Prims.Box(g, n + new Vector3(s * 5.6f, 4.5f, k * 4f), new Vector3(0.12f, 2.6f, 1.1f), MatLib.Glow(ghost, 1.6f), false);
        // Колокольня.
        var t = n + new Vector3(0, 0, 9);
        Prims.Box(g, t + new Vector3(0, 7, 0), new Vector3(4, 14, 4), wall);
        Prims.Cone(g, t + new Vector3(0, 14, 0), 3f, 5f, dark);
        Prims.Sphere(g, t + new Vector3(0, 12f, -2.1f), 1.4f, MatLib.Glow(ghost, 2.5f));
        // Колокол на раме — у него снимают печать.
        var b = c + new Vector3(0, 0, 9);
        foreach (int s in new[] { -1, 1 }) Prims.Box(g, b + new Vector3(s * 1.8f, 1.8f, 0), new Vector3(0.3f, 3.6f, 0.3f), woodDark);
        Prims.Box(g, b + new Vector3(0, 3.6f, 0), new Vector3(4f, 0.3f, 0.3f), woodDark, false);
        Prims.Cone(g, b + new Vector3(0, 1.6f, 0), 1.1f, 1.9f, bronze);
        // Погост и туман.
        var grave = MatLib.Solid(new Color(0.4f, 0.42f, 0.44f), 0.1f);
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2 / 18;
            var p = c + new Vector3(Mathf.Cos(a) * 19, 0, Mathf.Sin(a) * 19);
            if (p.z > c.z + 12) continue;
            Prims.Box(g, p + Vector3.up * 0.55f, new Vector3(0.8f, 1.1f, 0.2f), grave, false, Quaternion.Euler(Random.Range(-10f, 10f), a * -Mathf.Rad2Deg + 90, 0));
        }
        FX.Loop(g, c + Vector3.up * 0.4f, new Color(0.5f, 0.45f, 0.65f, 0.2f), 14, 6f, 0.3f);
        SiteLight(g, c + new Vector3(0, 6, 4), ghost, 30, 1.6f);
    }

    // Глава V: разлом в кольце столбов, якорь справа, память Хранителя слева.
    static void VeilGate(Vector3 c)
    {
        var g = Prims.Group(Root, "Врата Завесы");
        var stone = MatLib.Solid(new Color(0.15f, 0.17f, 0.22f));
        var violet = new Color(0.5f, 0.12f, 0.7f);
        Prims.Box(g, c + new Vector3(0, -0.1f, 0), new Vector3(44, 0.2f, 44), stone);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2 / 10;
            var pos = c + new Vector3(Mathf.Cos(a) * 19, 0, Mathf.Sin(a) * 19);
            Prims.Cyl(g, pos, 0.8f, 5 + i % 3, stone);
            Prims.Sphere(g, pos + Vector3.up * (5 + i % 3), 0.6f, MatLib.Glow(violet));
        }
        // Арка разлома на севере.
        Prims.Box(g, c + new Vector3(-5, 4, 15), new Vector3(1.5f, 8, 2), stone);
        Prims.Box(g, c + new Vector3(5, 4, 15), new Vector3(1.5f, 8, 2), stone);
        Prims.Box(g, c + new Vector3(0, 8, 15), new Vector3(12, 1.5f, 2), stone);
        Prims.Make(PrimitiveType.Sphere, g, c + new Vector3(0, 4, 15), new Vector3(8, 7.5f, 0.6f), MatLib.Glow(new Color(0.25f, 0.05f, 0.45f), 2.5f), false);
        FX.Loop(g, c + new Vector3(0, 3, 15), new Color(0.6f, 0.2f, 0.9f, 0.35f), 25, 1.2f, 1.5f);
        // Парящие обломки.
        for (int i = 0; i < 12; i++)
        {
            var p = c + new Vector3(Random.Range(-16f, 16f), Random.Range(4f, 10f), Random.Range(-10f, 18f));
            Prims.Make(PrimitiveType.Cube, g, p, Vector3.one * Random.Range(0.5f, 1.4f), stone, false, Random.rotation);
        }
        // Якорь: обелиск с цепями; память: чаша со светом.
        var anchor = c + new Vector3(12, 0, 0);
        Prims.Box(g, anchor + Vector3.up * 2.5f, new Vector3(1.2f, 5f, 1.2f), MatLib.Solid(new Color(0.08f, 0.06f, 0.1f)));
        for (int i = 0; i < 3; i++) Prims.Torus(g, anchor + Vector3.up * (1.2f + i * 1.4f), 1f, MatLib.Glow(violet, 2f), Quaternion.Euler(90, 0, 0));
        var memory = c + new Vector3(-12, 0, 0);
        Prims.Cyl(g, memory, 0.9f, 1.1f, stone);
        Prims.Sphere(g, memory + Vector3.up * 1.6f, 0.9f, MatLib.Glow(new Color(0.6f, 0.85f, 1f), 2.5f));
        FX.Loop(g, c + Vector3.up * 0.3f, new Color(0.4f, 0.2f, 0.55f, 0.25f), 18, 6f, 0.4f);
        SiteLight(g, c + new Vector3(0, 6, 8), violet, 34, 2f);
    }

    // Глава VI: поместье Мракс — особняк с башнями, кованая ограда и гробы у крыльца.
    static void Manor(Vector3 c)
    {
        var g = Prims.Group(Root, "Поместье Мракс");
        var wall = MatLib.Solid(new Color(0.22f, 0.18f, 0.2f), 0.1f, 0.3f, 0.5f);
        var roofM = MatLib.Solid(new Color(0.1f, 0.08f, 0.12f), 0.2f);
        var iron = MatLib.Solid(new Color(0.08f, 0.08f, 0.09f), 0.6f);
        var red = new Color(1f, 0.15f, 0.12f);
        Prims.Box(g, c + Vector3.up * 0.03f, new Vector3(44, 0.06f, 44), MatLib.Solid(new Color(0.16f, 0.18f, 0.14f), 0.05f), false);
        var h = c + new Vector3(0, 0, 21);
        Prims.Box(g, h + new Vector3(0, 5, 0), new Vector3(22, 10, 10), wall);
        Prims.Roof(g, h + new Vector3(0, 10, 0), 11f, 4f, 23f, roofM);
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Cyl(g, h + new Vector3(s * 12, 0, -3), 2.4f, 14f, wall);
            Prims.Cone(g, h + new Vector3(s * 12, 14, -3), 2.9f, 5f, roofM);
            for (int k = 0; k < 3; k++)
            {
                Prims.Box(g, h + new Vector3(s * (3 + k * 2.8f), 3f, -5.05f), new Vector3(1.1f, 1.8f, 0.1f), MatLib.Glow(red, 1.8f), false);
                Prims.Box(g, h + new Vector3(s * (3 + k * 2.8f), 7f, -5.05f), new Vector3(1.1f, 1.8f, 0.1f), MatLib.Glow(red, 1.4f), false);
            }
        }
        Prims.Box(g, h + new Vector3(0, 1.8f, -5.1f), new Vector3(2.4f, 3.6f, 0.2f), MatLib.Solid(new Color(0.15f, 0.06f, 0.04f)), false);
        Prims.Box(g, h + new Vector3(0, 0.25f, -6.5f), new Vector3(6, 0.5f, 3), MatLib.Solid(new Color(0.3f, 0.28f, 0.3f)));
        // Ограда с воротами на юге.
        for (int i = -20; i <= 20; i += 2)
        {
            if (Mathf.Abs(i) > 3) Prims.Box(g, c + new Vector3(i, 1.1f, -20), new Vector3(0.12f, 2.2f, 0.12f), iron, false);
            Prims.Box(g, c + new Vector3(-20, 1.1f, i), new Vector3(0.12f, 2.2f, 0.12f), iron, false);
            Prims.Box(g, c + new Vector3(20, 1.1f, i), new Vector3(0.12f, 2.2f, 0.12f), iron, false);
        }
        // Гробы и мёртвые деревья.
        for (int i = 0; i < 4; i++)
            Prims.Box(g, c + new Vector3(-12 + i * 3.2f, 0.3f, 10 - (i % 2) * 2), new Vector3(0.9f, 0.6f, 2.2f), MatLib.Solid(new Color(0.12f, 0.07f, 0.05f)), false, Quaternion.Euler(0, Random.Range(-15f, 15f), 0));
        foreach (var p in new[] { new Vector3(-15, 0, -12), new Vector3(15, 0, -10), new Vector3(16, 0, 6) })
        {
            Prims.Cyl(g, c + p, 0.35f, 5f, woodDark);
            for (int k = 0; k < 3; k++)
                Prims.Box(g, c + p + new Vector3(0, 3.5f + k * 0.6f, 0), new Vector3(0.15f, 2.2f, 0.15f), woodDark, false, Quaternion.Euler(0, k * 120, 50));
        }
        SiteLight(g, h + new Vector3(0, 4, -8), red, 22, 0.8f);
        SiteLight(g, c + new Vector3(0, 5, 0), new Color(0.5f, 0.55f, 0.9f), 26, 0.8f);
    }

    // Каменоломня: скальная стена полукругом, тёсаные блоки, кран и рунный круг.
    static void Quarry(Vector3 c)
    {
        var g = Prims.Group(Root, "Каменоломня");
        var cut = MatLib.Solid(new Color(0.62f, 0.6f, 0.56f), 0.05f, 0.4f, 0.6f);
        Prims.Box(g, c + Vector3.up * 0.03f, new Vector3(44, 0.06f, 44), MatLib.Solid(new Color(0.55f, 0.5f, 0.42f), 0.05f), false);
        for (int i = 0; i < 9; i++)
        {
            float a = Mathf.PI * (0.05f + i * 0.9f / 8);
            var p = c + new Vector3(Mathf.Cos(a) * 21, 0, Mathf.Sin(a) * 21 + 2);
            float k = Random.Range(7f, 11f);
            Prims.Make(PrimitiveType.Sphere, g, p, new Vector3(k * 1.3f, k, k), rock, true, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        }
        for (int i = 0; i < 8; i++)
        {
            var p = c + new Vector3(Random.Range(-17f, 17f), 0, Random.Range(-16f, -4f));
            if (Mathf.Abs(p.x) < 9 && p.z > c.z - 10) continue;
            float s = Random.Range(1f, 1.8f);
            Prims.Box(g, p + Vector3.up * s * 0.5f, new Vector3(s * 1.6f, s, s), cut, true, Quaternion.Euler(0, Random.Range(0, 90f), 0));
        }
        // Деревянный кран.
        var cr = c + new Vector3(-14, 0, 8);
        Prims.Box(g, cr + Vector3.up * 4f, new Vector3(0.5f, 8f, 0.5f), woodDark);
        Prims.Box(g, cr + new Vector3(3, 7.8f, 0), new Vector3(6.5f, 0.4f, 0.4f), woodDark, false);
        Prims.Box(g, cr + new Vector3(6, 5.5f, 0), new Vector3(0.05f, 4.5f, 0.05f), MatLib.Solid(new Color(0.3f, 0.25f, 0.2f)), false);
        Prims.Box(g, cr + new Vector3(6, 3f, 0), new Vector3(1.4f, 1f, 1.4f), cut, false);
        // Рунный круг, которым Мракс будила големов.
        var rune = MatLib.Glow(new Color(1f, 0.55f, 0.15f), 2f);
        Prims.Torus(g, c + new Vector3(0, 0.08f, 8), 4.2f, rune, Quaternion.identity);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3;
            Prims.Box(g, c + new Vector3(Mathf.Cos(a) * 3f, 0.08f, 8 + Mathf.Sin(a) * 3f), new Vector3(0.5f, 0.04f, 0.18f), rune, false, Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
        }
        SiteLight(g, c + new Vector3(0, 3, 8), new Color(1f, 0.6f, 0.25f), 18, 1.2f);
    }

    // Ледяная пещера: снег, ледяные шипы и холм с тёмным входом.
    static void IceCave(Vector3 c)
    {
        var g = Prims.Group(Root, "Ледяная пещера");
        var snowM = MatLib.Solid(new Color(0.88f, 0.92f, 0.97f), 0.3f, 0.2f, 0.2f);
        var ice = MatLib.Glow(new Color(0.55f, 0.85f, 1f), 1.2f);
        Prims.Make(PrimitiveType.Cylinder, g, c + Vector3.up * 0.02f, new Vector3(46, 0.03f, 46), snowM, false);
        Prims.Make(PrimitiveType.Sphere, g, c + new Vector3(0, 0, 22), new Vector3(30, 16, 16), snowM, true);
        Prims.Box(g, c + new Vector3(0, 2.4f, 14.4f), new Vector3(6f, 4.8f, 1f), MatLib.Solid(new Color(0.03f, 0.05f, 0.08f), 0f), false);
        for (int i = 0; i < 26; i++)
        {
            float a = Random.Range(0, Mathf.PI * 2), r = Random.Range(12f, 21f);
            var p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            if (p.z > c.z + 10 && Mathf.Abs(p.x - c.x) < 6) continue;
            Prims.Cone(g, p, Random.Range(0.4f, 1f), Random.Range(1.5f, 4.5f), ice, false, Quaternion.Euler(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f)));
        }
        foreach (int s in new[] { -1, 1 })
            for (int k = 0; k < 4; k++)
                Prims.Cone(g, c + new Vector3(s * (1.2f + k * 0.6f), 4.8f, 14), 0.25f, 1.4f, ice, false, Quaternion.Euler(180, 0, 0));
        Prims.Make(PrimitiveType.Cylinder, g, c + new Vector3(10, 0.05f, -8), new Vector3(9, 0.04f, 7), MatLib.Fade(new Color(0.6f, 0.85f, 1f, 0.6f), 1.5f, 0.95f), false);
        FX.Loop(g, c + Vector3.up * 6, new Color(0.95f, 0.97f, 1f, 0.6f), 30, 0.25f, 0.6f);
        SiteLight(g, c + new Vector3(0, 4, 10), new Color(0.5f, 0.8f, 1f), 26, 1.6f);
    }

    // Утёс ветров: скалы на севере, кольцо менгиров и алтарь с осколком.
    static void Peak(Vector3 c)
    {
        var g = Prims.Group(Root, "Утёс ветров");
        var stoneM = MatLib.Solid(new Color(0.42f, 0.42f, 0.46f), 0.1f, 0.4f, 0.6f);
        var blue = new Color(0.45f, 0.8f, 1f);
        Prims.Make(PrimitiveType.Cylinder, g, c + Vector3.up * 0.03f, new Vector3(46, 0.04f, 46), MatLib.Solid(new Color(0.36f, 0.38f, 0.34f), 0.05f), false);
        for (int i = 0; i < 7; i++)
        {
            float a = Mathf.PI * (0.15f + i * 0.7f / 6);
            var p = c + new Vector3(Mathf.Cos(a) * 26, 0, Mathf.Sin(a) * 26);
            float k = Random.Range(12f, 20f);
            Prims.Make(PrimitiveType.Sphere, g, p, new Vector3(k, k * 1.6f, k), rock, true);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2 / 8;
            var p = c + new Vector3(Mathf.Cos(a) * 16, 0, Mathf.Sin(a) * 16);
            Prims.Box(g, p + Vector3.up * 2.5f, new Vector3(1.4f, 5f, 0.9f), stoneM, true, Quaternion.Euler(0, -a * Mathf.Rad2Deg, Random.Range(-5f, 5f)));
            Prims.Box(g, p + new Vector3(0, 3.5f, 0), new Vector3(0.2f, 1.2f, 0.95f), MatLib.Glow(blue, 1.8f), false, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
        }
        var alt = c + new Vector3(0, 0, 13);
        Prims.Box(g, alt + Vector3.up * 0.6f, new Vector3(4, 1.2f, 2.4f), stoneM);
        Prims.Make(PrimitiveType.Cube, g, alt + Vector3.up * 2.6f, new Vector3(0.6f, 1.4f, 0.6f), MatLib.Glow(blue, 3f), false, Quaternion.Euler(0, 45, 20));
        FX.Loop(g, alt + Vector3.up * 2.6f, new Color(0.5f, 0.85f, 1f, 0.5f), 20, 0.4f, 1.5f);
        foreach (int s in new[] { -1, 1 })
        {
            var br = c + new Vector3(s * 6f, 0, -4f);
            Prims.Cyl(g, br, 0.5f, 1.2f, stoneDark);
            Prims.Sphere(g, br + Vector3.up * 1.5f, 0.7f, MatLib.Glow(blue, 2.5f));
        }
        FX.Loop(g, c + Vector3.up * 1.5f, new Color(0.85f, 0.9f, 1f, 0.15f), 12, 5f, 2.5f);
        SiteLight(g, alt + Vector3.up * 3, blue, 30, 1.8f);
    }
}

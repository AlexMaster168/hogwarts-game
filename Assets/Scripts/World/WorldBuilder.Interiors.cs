using UnityEngine;

// Интерьеры замка из модульных китов: крылья по бокам Большого зала и трофейный зал за ним.
// Сетка: модуль KayKit 4 м × 1.5 = 6 м, высота стен 6 м.
public static partial class WorldBuilder
{
    const float KS = 1.5f, KT = 6f, KH = 6f, FS = 0.8f, FloorY = 0.075f;
    public static readonly Vector3 LibraryPos = new(-20, 0, 43);
    public static readonly Vector3 ClassroomPos = new(-28, 0, 64);
    public static readonly Vector3 CommonRoomPos = new(30, 0, 42);
    public static readonly Vector3 DormPos = new(26, 0, 66);
    public static readonly Vector3 TrophyPos = new(0, 0, 94);

    // Не под Root: меши glTF нечитаемые, статик-батчинг их не возьмёт.
    static Transform kitRoot;

    static void Interiors()
    {
        WestWing();
        EastWing();
        TrophyRoom();
        UpperCastle();
        MoreLocations();
        EastUpper();
        GrandStaircase();
    }

    // ---------------- ЗАПАДНОЕ КРЫЛО: библиотека + класс ЗОТИ ----------------
    static void WestWing()
    {
        KFloor(-38, 30, 4, 4, "floor_wood_large_dark");
        KFloor(-38, 54, 4, 4, "floor_tile_large");
        KWalls(new Vector3(-38, 0, 30), Vector3.right, Vector3.forward, "owow");
        KWalls(new Vector3(-38, 0, 30), Vector3.forward, Vector3.right, "ossoowow");
        KWalls(new Vector3(-38, 0, 78), Vector3.right, Vector3.back, "owwo");
        KWalls(new Vector3(-38, 0, 54), Vector3.right, Vector3.back, "wdww");
        KPillars(new Vector2(-38, 30), new Vector2(-38, 42), new Vector2(-38, 54), new Vector2(-38, 66), new Vector2(-38, 78), new Vector2(-26, 30), new Vector2(-26, 78));
        // Roof is above the new upper floor.

        // Библиотека: ряды шкафов спина к спине, читальная зона у входа из Большого зала.
        foreach (float z in new[] { 38f, 46f })
            for (int k = 0; k < 4; k++)
            {
                float x = -34.2f + k * 2.8f;
                Bookcase(new Vector3(x, FloorY, z - 0.43f), 180);
                Bookcase(new Vector3(x, FloorY, z + 0.43f), 0);
            }
        foreach (float x in new[] { -30.4f, -27.6f }) Bookcase(new Vector3(x, FloorY, 31.7f), 0);
        foreach (float x in new[] { -36.4f, -33.6f, -24.4f, -21.6f }) Bookcase(new Vector3(x, FloorY, 52.8f), 180);
        foreach (float z in new[] { 35f, 50f })
        {
            Kit.Place(kitRoot, "table_medium_decorated_A", new Vector3(-20, FloorY, z), 0, FS, true);
            Kit.Place(kitRoot, "chair", new Vector3(-21.4f, FloorY, z), 90, FS);
            Kit.Place(kitRoot, "chair", new Vector3(-18.6f, FloorY, z), -90, FS);
            Kit.Place(kitRoot, "lamp_standing", new Vector3(-22, FloorY, z), 0, FS);
        }
        Kit.Place(kitRoot, "rug_rectangle_A", new Vector3(-19, FloorY + 0.01f, 43), 90, 1.6f);
        FloatingCandles(kitRoot, new Vector3(-36, 4f, 32), new Vector3(-16, 5.3f, 52), 30);
        KLight(new Vector3(-29, 4.5f, 42), 18, 1.5f);
        KLight(new Vector3(-19, 4.5f, 42), 14, 1.2f);
        Chest.Spawn(new Vector3(-36.2f, 0.1f, 32.2f), 45);

        // Класс ЗОТИ: парты рядами, стол профессора и доска.
        foreach (float x in new[] { -33f, -28f, -23f })
            foreach (float z in new[] { 58f, 62.5f, 67f })
            {
                Kit.Place(kitRoot, "table_medium", new Vector3(x, FloorY, z), 0, FS, true);
                Kit.Place(kitRoot, "stool", new Vector3(x, FloorY, z - 1.3f), 0, FS);
            }
        Kit.Place(kitRoot, "table_long", new Vector3(-28, FloorY, 73), 90, FS, true);
        Kit.Place(kitRoot, "chair", new Vector3(-28, FloorY, 74.6f), 180, FS);
        Kit.Place(kitRoot, "candle_triple", new Vector3(-29.2f, FloorY + 0.8f, 73), 0, FS);
        Kit.Place(kitRoot, "bottle_A_labeled_green", new Vector3(-26.6f, FloorY + 0.8f, 72.8f), 0, FS);
        Prims.Box(Root, new Vector3(-26, 3.2f, 77.2f), new Vector3(7, 2.6f, 0.12f), MatLib.Solid(new Color(0.08f, 0.12f, 0.1f), 0.1f), false);
        Prims.Box(Root, new Vector3(-26, 1.85f, 77.1f), new Vector3(7.2f, 0.12f, 0.3f), woodDark, false);
        Kit.Place(kitRoot, "sword_shield", new Vector3(-37.25f, 3.2f, 63), 90, 1.2f);
        Kit.Place(kitRoot, "sword_shield", new Vector3(-37.25f, 3.2f, 75), 90, 1.2f);
        Kit.Place(kitRoot, "barrel_large", new Vector3(-36, FloorY, 56.5f), 0, FS, true);
        Kit.Place(kitRoot, "crates_stacked", new Vector3(-35.8f, FloorY, 76), 20, FS, true);
        Torch(new Vector3(-35, 3.2f, 77.25f), 180);
        Torch(new Vector3(-17, 3.2f, 77.25f), 180);
        KLight(new Vector3(-28, 4.5f, 64), 18, 1.3f);
    }

    // ---------------- ВОСТОЧНОЕ КРЫЛО: гостиная + спальня ----------------
    static void EastWing()
    {
        KFloor(14, 30, 4, 4, "floor_wood_large");
        KFloor(14, 54, 4, 4, "floor_wood_large");
        KWalls(new Vector3(14, 0, 30), Vector3.right, Vector3.forward, "owow");
        KWalls(new Vector3(38, 0, 30), Vector3.forward, Vector3.left, "owwoowow");
        KWalls(new Vector3(14, 0, 78), Vector3.right, Vector3.back, "woow");
        KWalls(new Vector3(14, 0, 54), Vector3.right, Vector3.forward, "wwdw");
        KPillars(new Vector2(38, 30), new Vector2(38, 42), new Vector2(38, 54), new Vector2(38, 66), new Vector2(38, 78), new Vector2(26, 30), new Vector2(26, 78));
        // Крыша теперь над вторым этажом (EastUpper).

        // Гостиная: камин, диван и кресла вокруг ковра.
        var fp = Prims.Group(Root, "Fireplace");
        Prims.Box(fp, new Vector3(36.6f, 1.6f, 42), new Vector3(1.4f, 3.2f, 4.6f), stone);
        Prims.Box(fp, new Vector3(35.88f, 1f, 42), new Vector3(0.1f, 1.7f, 2.4f), MatLib.Solid(new Color(0.04f, 0.03f, 0.03f)), false);
        Prims.Box(fp, new Vector3(36.2f, 3.3f, 42), new Vector3(2f, 0.3f, 5.2f), woodDark);
        Prims.Box(fp, new Vector3(36.9f, 4.65f, 42), new Vector3(0.9f, 2.7f, 3f), stone);
        FX.Loop(fp, new Vector3(35.7f, 0.4f, 42), new Color(1f, 0.5f, 0.15f), 30, 0.6f, 1.6f);
        KLight(new Vector3(34.5f, 1.4f, 42), 14, 2.2f, new Color(1f, 0.55f, 0.2f));
        Kit.Place(kitRoot, "rug_oval_A", new Vector3(32.5f, FloorY + 0.01f, 42), 90, 1.8f);
        Kit.Place(kitRoot, "couch_pillows", new Vector3(29.5f, FloorY, 42), 90, FS, true);
        Kit.Place(kitRoot, "armchair_pillows", new Vector3(33, FloorY, 37.4f), 25, FS, true);
        Kit.Place(kitRoot, "armchair_pillows", new Vector3(33, FloorY, 46.6f), 155, FS, true);
        Kit.Place(kitRoot, "table_low", new Vector3(32.4f, FloorY, 42), 90, FS, true);
        Kit.Place(kitRoot, "candle_triple", new Vector3(32.4f, FloorY + 0.4f, 42.3f), 0, FS);
        Kit.Place(kitRoot, "book_set", new Vector3(32.3f, FloorY + 0.5f, 41.5f), 30, FS);
        foreach (float x in new[] { 15.6f, 18.4f, 21.2f }) Bookcase(new Vector3(x, FloorY, 52.8f), 180);
        Kit.Place(kitRoot, "table_medium_decorated_A", new Vector3(20, FloorY, 36), 0, FS, true);
        foreach (int s in new[] { -1, 1 }) Kit.Place(kitRoot, "chair_A_wood", new Vector3(20 + s * 1.4f, FloorY, 36), s * -90, FS);
        // Южная стена занята лестницей на второй этаж — лампы и знамёна у перегородки.
        Kit.Place(kitRoot, "lamp_standing", new Vector3(16, FloorY, 48), 0, FS);
        Kit.Place(kitRoot, "lamp_standing", new Vector3(36, FloorY, 50), 0, FS);
        Kit.Place(kitRoot, "banner_patternA_red", new Vector3(23.4f, 0.3f, 54), 180, KS);
        Kit.Place(kitRoot, "banner_patternA_yellow", new Vector3(35, 0.3f, 54), 180, KS);
        Kit.Place(kitRoot, "pictureframe_large_A", new Vector3(37.25f, 4.2f, 37), -90, 1.4f);
        Kit.Place(kitRoot, "pictureframe_medium", new Vector3(37.25f, 4.2f, 47), -90, 1.4f);
        KLight(new Vector3(24, 4.5f, 42), 16, 1.2f);
        Chest.Spawn(new Vector3(16.2f, 0.1f, 44f), 90);

        // Спальня: кровати с балдахином, сундуки у изножья.
        foreach (float z in new[] { 57.5f, 62.5f, 69.5f, 74.5f })
        {
            PosterBed(new Vector3(35.6f, FloorY, z), -90);
            Kit.Place(kitRoot, "trunk_medium_B", new Vector3(33.1f, FloorY, z), 90, 1f, true);
        }
        foreach (float x in new[] { 19f, 24.5f, 30f })
        {
            PosterBed(new Vector3(x, FloorY, 75.6f), 180);
            Kit.Place(kitRoot, "trunk_medium_B", new Vector3(x, FloorY, 73.1f), 0, 1f, true);
        }
        Kit.Place(kitRoot, "rug_rectangle_stripes_A", new Vector3(27, FloorY + 0.01f, 65), 90, 2f);
        Kit.Place(kitRoot, "cabinet_medium_decorated", new Vector3(17.5f, FloorY, 55.3f), 0, FS, true);
        Kit.Place(kitRoot, "banner_patternA_red", new Vector3(14.6f, 0.3f, 60), 90, KS);
        KLight(new Vector3(26, 4.5f, 66), 18, 1.1f);
        FloatingCandles(kitRoot, new Vector3(18, 4.2f, 57), new Vector3(34, 5.3f, 75), 14);
    }

    // ---------------- ТРОФЕЙНЫЙ ЗАЛ (за Большим залом) ----------------
    static void TrophyRoom()
    {
        KFloor(-9, 85, 3, 3, "floor_tile_large");
        KWalls(new Vector3(-9, 0, 85), Vector3.forward, Vector3.right, "sws");
        KWalls(new Vector3(9, 0, 85), Vector3.forward, Vector3.left, "sws");
        KWalls(new Vector3(-9, 0, 103), Vector3.right, Vector3.back, "wdw");
        KPillars(new Vector2(-9, 103), new Vector2(9, 103), new Vector2(-9, 85), new Vector2(9, 85));
        KRoof(new Vector3(0, 0, 94), 18, 18);

        // Песочные часы факультетов.
        var glass = MatLib.Fade(new Color(0.75f, 0.85f, 0.95f, 0.25f), 0.6f, 0.95f);
        for (int i = 0; i < 4; i++)
        {
            var b = new Vector3(-5.4f + i * 3.6f, FloorY, 100.2f);
            Prims.Box(Root, b + Vector3.up * 0.2f, new Vector3(1.3f, 0.4f, 1.3f), woodDark);
            Prims.Cyl(Root, b + Vector3.up * 0.4f, 0.32f, 1f + i * 0.35f, MatLib.Glow(Houses.Main[i], 1.3f), false);
            Prims.Cyl(Root, b + Vector3.up * 0.4f, 0.45f, 3f, glass, false);
            Prims.Box(Root, b + Vector3.up * 3.5f, new Vector3(1.3f, 0.25f, 1.3f), gold, false);
        }
        foreach (int s in new[] { -1, 1 })
        {
            Prims.Box(Root, new Vector3(s * 4, 0.55f, 90), new Vector3(1.4f, 1.1f, 1.4f), stoneDark);
            Kit.Place(kitRoot, s < 0 ? "coin_stack_large" : "sword_shield_gold", new Vector3(s * 4, 1.1f, 90), 0, s < 0 ? FS : 0.7f);
        }
        Kit.Place(kitRoot, "banner_shield_red", new Vector3(-9, 0.3f, 94), 90, KS);
        Kit.Place(kitRoot, "banner_shield_yellow", new Vector3(9, 0.3f, 94), -90, KS);
        Kit.Place(kitRoot, "banner_shield_green", new Vector3(-6, 0.3f, 103), 180, KS);
        Kit.Place(kitRoot, "banner_shield_blue", new Vector3(6, 0.3f, 103), 180, KS);
        KLight(new Vector3(0, 4.5f, 94), 16, 1.5f, new Color(1f, 0.85f, 0.55f));
        FloatingCandles(kitRoot, new Vector3(-7, 4.2f, 87), new Vector3(7, 5.3f, 101), 12);
    }

    // ---------------- ПОМОЩНИКИ ----------------
    static void KFloor(float x0, float z0, int nx, int nz, string tile)
    {
        // Сплошной ровный пол вместо земли под декоративными тайлами.
        Kit.Blocker(kitRoot, new Vector3(x0 + nx * KT / 2f, 0.06f, z0 + nz * KT / 2f), new Vector3(nx * KT, 0.12f, nz * KT), Quaternion.identity);
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                Kit.Place(kitRoot, tile, new Vector3(x0 + (i + 0.5f) * KT, 0.04f, z0 + (j + 0.5f) * KT), 0, KS);
    }

    // Ряд стен по сетке. Символы: w стена, o окно, c закрытое окно, s полки, a арка, d дверной проём, - пропуск.
    static void KWalls(Vector3 start, Vector3 dir, Vector3 inward, string pattern)
    {
        var rot = Quaternion.LookRotation(inward);
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '-') continue;
            var pos = start + dir * ((i + 0.5f) * KT);
            string name = c switch
            {
                'o' => "wall_window_open",
                'c' => "wall_window_closed",
                's' => "wall_shelves",
                'a' => "wall_arched",
                'd' => "wall_doorway",
                _ => "wall",
            };
            var w = Kit.Place(kitRoot, name, pos, rot.eulerAngles.y, KS);
            if (c == 'd')
            {
                // Створку распахиваем, коллайдеры — по бокам и над проёмом (проём 3×4.1 м).
                var door = w != null ? FindDeep(w.transform, "wall_doorway_door") : null;
                if (door != null) door.localRotation = Quaternion.Euler(0, 100, 0) * door.localRotation;
                float side = (KT - 3f) * 0.5f;
                foreach (int s in new[] { -1, 1 })
                    Kit.Blocker(kitRoot, pos + rot * new Vector3(s * (1.5f + side * 0.5f), KH * 0.5f, 0), new Vector3(side, KH, 1.5f), rot);
                Kit.Blocker(kitRoot, pos + rot * new Vector3(0, (4.1f + KH) * 0.5f, 0), new Vector3(3f, KH - 4.1f, 1.5f), rot);
            }
            else Kit.Blocker(kitRoot, pos + Vector3.up * KH * 0.5f, new Vector3(KT, KH, 1.5f), rot);
        }
    }

    static void KPillars(params Vector2[] pts)
    {
        foreach (var p in pts) Kit.Place(kitRoot, "pillar", new Vector3(p.x, 0, p.y), 0, KS, true);
    }

    static void KRoof(Vector3 c, float w, float l)
    {
        Prims.Box(Root, c + Vector3.up * (KH + 0.15f), new Vector3(w + 1.5f, 0.3f, l + 1.5f), woodDark, false);
        for (float z = c.z - l * 0.5f + KT; z < c.z + l * 0.5f - 0.1f; z += KT)
            Prims.Box(Root, new Vector3(c.x, c.y + KH - 0.25f, z), new Vector3(w, 0.5f, 0.5f), woodDark, false);
        Prims.Roof(Root, c + Vector3.up * (KH + 0.3f), w + 2.5f, 5f, l + 2.5f, roof);
    }

    static void Bookcase(Vector3 pos, float yaw)
    {
        Kit.Place(kitRoot, "kenney/bookcaseClosedWide", pos, yaw, 3.4f, true, true);
        // Корешки книг на трёх полках (полки на 0.24/1.05/1.87 м, проём ~0.7 м, ширина ~2.4 м).
        bookMats ??= new[]
        {
            MatLib.Solid(new Color(0.5f, 0.12f, 0.1f), 0.2f), MatLib.Solid(new Color(0.12f, 0.25f, 0.45f), 0.2f),
            MatLib.Solid(new Color(0.15f, 0.35f, 0.18f), 0.2f), MatLib.Solid(new Color(0.55f, 0.42f, 0.2f), 0.2f),
            MatLib.Solid(new Color(0.3f, 0.18f, 0.1f), 0.2f), MatLib.Solid(new Color(0.35f, 0.15f, 0.35f), 0.2f),
        };
        var rot = Quaternion.Euler(0, yaw, 0);
        foreach (float shelf in new[] { 0.24f, 1.05f, 1.87f })
        {
            float x = -1.16f;
            while (x < 1.1f)
            {
                float w = Random.Range(0.08f, 0.17f), hh = Random.Range(0.42f, 0.64f);
                if (Random.value < 0.08f) { x += w * 2f; continue; }
                Prims.Box(Root, pos + rot * new Vector3(x + w * 0.5f, shelf + hh * 0.5f, 0), new Vector3(w * 0.9f, hh, 0.55f),
                    bookMats[Random.Range(0, bookMats.Length)], false, rot);
                x += w;
            }
        }
    }
    static Material[] bookMats;

    static void Torch(Vector3 pos, float yaw)
    {
        Kit.Place(kitRoot, "torch_mounted", pos, yaw, KS);
        KLight(pos + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0.8f, 0.8f), 9, 1.4f, new Color(1f, 0.6f, 0.25f));
    }

    // Кровать с балдахином: матрас Furniture Bits + столбики и полог цвета Гриффиндора.
    static void PosterBed(Vector3 pos, float yaw)
    {
        Kit.Place(kitRoot, "bed_single_A", pos, yaw, 1.1f, true, true);
        var rot = Quaternion.Euler(0, yaw, 0);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Prims.Box(Root, pos + rot * new Vector3(sx * 0.95f, 1.3f, sz * 1.7f), new Vector3(0.14f, 2.6f, 0.14f), woodDark, false, rot);
        var red = MatLib.Solid(Houses.Main[0], 0.1f);
        Prims.Box(Root, pos + rot * new Vector3(0, 2.62f, 0), new Vector3(2.05f, 0.1f, 3.55f), red, false, rot);
        foreach (int sx in new[] { -1, 1 })
            Prims.Box(Root, pos + rot * new Vector3(sx * 1.0f, 2.3f, 0), new Vector3(0.05f, 0.6f, 3.5f), red, false, rot);
        Prims.Box(Root, pos + rot * new Vector3(0, 1.5f, -1.72f), new Vector3(1.9f, 2.2f, 0.06f), red, false, rot);
    }

    static void KLight(Vector3 p, float range, float intensity, Color? col = null)
    {
        var l = new GameObject("RoomLight").AddComponent<Light>();
        l.transform.SetParent(kitRoot, false);
        l.transform.position = p;
        l.type = LightType.Point;
        l.renderMode = LightRenderMode.ForceVertex;
        l.color = col ?? new Color(1f, 0.78f, 0.5f);
        l.range = range;
        l.intensity = intensity;
    }

    static void FloatingCandles(Transform g, Vector3 min, Vector3 max, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var p = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            Prims.Cyl(g, p, 0.06f, 0.45f, candle, false);
            Prims.Sphere(g, p + Vector3.up * 0.55f, 0.13f, flame);
        }
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    // Флажок Kenney на шпиле башни.
    static void TowerFlag(Vector3 top, float scale)
    {
        if (kitRoot == null) return;
        Kit.Place(kitRoot, "kenney/flag-pennant", top, Random.Range(0f, 360f), scale);
    }
}

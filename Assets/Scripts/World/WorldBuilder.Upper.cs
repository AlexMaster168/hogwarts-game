using UnityEngine;

public static partial class WorldBuilder
{
    public static readonly Vector3 UpperGalleryPos = new(-26, 6.2f, 60);
    public static readonly Vector3 TowerLookoutPos = new(-26, 24.2f, 90);

    static void UpperCastle()
    {
        // Leave a full-height opening over the staircase; solid slabs elsewhere.
        Prims.Box(Root, new Vector3(-29, 6, 54), new Vector3(18, 0.3f, 48), floor);
        Prims.Box(Root, new Vector3(-17, 6, 64), new Vector3(6, 0.3f, 28), floor);
        for (int i = 0; i < 30; i++)
        {
            float top = FloorY + (6.15f - FloorY) * (i + 1) / 30f;
            Prims.Box(Root, new Vector3(-17, top - 0.12f, 32 + i * 0.6f), new Vector3(3, 0.24f, 0.65f), stone);
        }
        Rail(new Vector3(-19.7f, 6.15f, 30), new Vector3(-19.7f, 6.15f, 50));
        KWalls(new Vector3(-38, 6.15f, 30), Vector3.right, Vector3.forward, "owow");
        KWalls(new Vector3(-38, 6.15f, 30), Vector3.forward, Vector3.right, "owowowow");
        KWalls(new Vector3(-14, 6.15f, 30), Vector3.forward, Vector3.left, "wowowowo");
        KWalls(new Vector3(-38, 6.15f, 78), Vector3.right, Vector3.back, "wdww");
        KRoof(new Vector3(-26, 6.15f, 54), 24, 48);
        for (float z = 34; z < 75; z += 6)
            Bookcase(new Vector3(-36.2f, 6.15f, z), 90);
        foreach (float z in new[] { 56f, 68f })
        {
            Kit.Place(kitRoot, "table_medium_decorated_A", new Vector3(-27, 6.15f, z), 0, FS, true);
            Kit.Place(kitRoot, "chair", new Vector3(-28.5f, 6.15f, z), 90, FS);
            KLight(new Vector3(-26, 10, z), 18, 1.5f);
        }
        FloatingCandles(kitRoot, new Vector3(-34, 10, 32), new Vector3(-22, 11, 76), 24);
        // Raised passage from the library to the astronomy tower.
        Prims.Box(Root, new Vector3(-29, 6, 81), new Vector3(3, 0.3f, 6), stone);
        Rail(new Vector3(-30.5f, 6.15f, 78), new Vector3(-30.5f, 6.15f, 84));
        Rail(new Vector3(-27.5f, 6.15f, 78), new Vector3(-27.5f, 6.15f, 84));
        var tower = Prims.Group(Root, "AstronomyTower");
        Prims.Box(tower, new Vector3(-26, 6, 90), new Vector3(12, 0.3f, 12), stone);
        for (int level = 0; level < 3; level++)
        {
            float y = 6.15f + level * 6;
            KWalls(new Vector3(-32, y, 84), Vector3.right, Vector3.forward, level == 0 ? "dw" : "oo");
            KWalls(new Vector3(-32, y, 84), Vector3.forward, Vector3.right, "oo");
            KWalls(new Vector3(-20, y, 84), Vector3.forward, Vector3.left, "oo");
            KWalls(new Vector3(-32, y, 96), Vector3.right, Vector3.back, "oo");
            KLight(new Vector3(-26, y + 3, 90), 14, 1.3f);
        }
        // Broad overlapping treads: 15 cm rise, below the controller step limit.
        for (int i = 0; i < 120; i++)
        {
            float a = i * 9f;
            var rotation = Quaternion.Euler(0, a, 0);
            float top = 6.15f + (i + 1) * 0.15f;
            var center = new Vector3(-26, top - 0.12f, 90) + rotation * new Vector3(0, 0, 3.9f);
            Prims.Box(tower, center, new Vector3(0.85f, 0.24f, 2.1f), stone, true, rotation);
        }
        Prims.Cyl(tower, new Vector3(-26, 6.15f, 90), 0.45f, 18, stoneDark);
        // Arrival tread at the north edge meets this observation deck.
        Prims.Box(tower, new Vector3(-26, 24, 90), new Vector3(5, 0.3f, 5), floor);
        Prims.Box(tower, new Vector3(-26.6f, 24, 92.65f), new Vector3(1.6f, 0.3f, 0.5f), floor);
        Rail(new Vector3(-32, 24.15f, 84), new Vector3(-20, 24.15f, 84));
        Rail(new Vector3(-32, 24.15f, 84), new Vector3(-32, 24.15f, 96));
        Rail(new Vector3(-20, 24.15f, 84), new Vector3(-20, 24.15f, 96));
        Rail(new Vector3(-32, 24.15f, 96), new Vector3(-20, 24.15f, 96));
        Kit.Place(kitRoot, "table_medium", new Vector3(-26, 24.15f, 89), 0, FS, true);
        Kit.Place(kitRoot, "book_set", new Vector3(-26, 24.95f, 89), 0, FS);
        Chest.Spawn(new Vector3(-27.5f, 24.15f, 88.5f), 90);
    }

    static void Rail(Vector3 a, Vector3 b)
    {
        var delta = b - a;
        var rot = Quaternion.LookRotation(delta);
        Prims.Box(Root, (a + b) * 0.5f + Vector3.up * 0.65f, new Vector3(0.15f, 1.3f, delta.magnitude), woodDark);
        // Horizontal beam must follow the passage direction.
        var beam = Root.GetChild(Root.childCount - 1);
        beam.rotation = rot;
    }
}

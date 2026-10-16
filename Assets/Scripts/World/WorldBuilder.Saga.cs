using UnityEngine;

public static partial class WorldBuilder
{
    public static readonly Vector3[] SagaPositions = { new(-275, 0, -100), new(280, 0, 80), new(-10, 0, -215), new(180, 0, 285) };
    public static readonly string[] SagaNames = { "Сад пепла", "Бронзовая обсерватория", "Зеркальный монастырь", "Архив гроз" };
    static void SagaSites()
    {
        for (int i = 0; i < SagaPositions.Length; i++)
        {
            var at = SagaPositions[i];
            var g = Prims.Group(Root, SagaNames[i]);
            var color = SagaEnemy.Tint((EnemyKind)((int)EnemyKind.AshQueen + i));
            Prims.Box(g, at + Vector3.down * 0.2f, new Vector3(36, 0.8f, 36), stoneDark);
            for (int j = 0; j < 8; j++)
            {
                float a = j * Mathf.PI / 4f;
                var p = at + new Vector3(Mathf.Sin(a) * 17, 0, Mathf.Cos(a) * 17);
                Prims.Cyl(g, p + Vector3.up * 0.2f, i == 1 ? 1.2f : 0.7f, 6f, stone);
                Prims.Sphere(g, p + Vector3.up * 6.3f, 0.8f, MatLib.Glow(color), false);
            }
            if (i == 0)
                for (int j = 0; j < 6; j++) Prims.Cone(g, at + new Vector3(j < 3 ? -12 : 12, 0.2f, (j % 3 - 1) * 9), 1.8f, 4f, woodDark, false);
            if (i == 1)
                for (int j = 0; j < 3; j++) Prims.Torus(g, at + Vector3.up * (4 + j * 2), 5f - j, gold);
            if (i == 2)
                foreach (float x in new[] { -12f, 12f }) Prims.Box(g, at + new Vector3(x, 3, 0), new Vector3(0.4f, 6, 20), MatLib.Solid(new Color(0.45f, 0.65f, 0.85f), 0.9f));
            if (i == 3)
                for (int j = 0; j < 4; j++) Prims.Box(g, at + new Vector3(-11 + j * 7, 2, 13), new Vector3(3, 4, 1), woodDark);
        }
    }
}

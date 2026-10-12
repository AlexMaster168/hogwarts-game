using System.Collections.Generic;
using UnityEngine;

// Второй этаж восточного крыла (кабинет директора и Выручай-комната) и Большая лестница с движущимися пролётами.
public static partial class WorldBuilder
{
    const float UpperY = 6.15f;
    public static readonly Vector3 PensievePos = new(21.5f, UpperY, 44f);
    public static readonly Vector3 RoomOfRequirementPos = new(26f, UpperY, 66f);
    public static readonly Vector3 StaircaseHall = new(0, 0, 163);
    public static readonly float[] GalleryHeights = { 7f, 14f, 21f };
    public static readonly List<MovingStair> Stairs = new();

    // ---------------- ВОСТОЧНОЕ КРЫЛО, 2 ЭТАЖ ----------------
    static void EastUpper()
    {
        // Лестница из гостиной вдоль южной стены: 30 ступеней по 0,6 м.
        for (int i = 0; i < 30; i++)
        {
            float top = FloorY + (UpperY - FloorY) * (i + 1) / 30f;
            Prims.Box(Root, new Vector3(16.3f + i * 0.6f, top - 0.12f, 32.4f), new Vector3(0.65f, 0.24f, 3f), stone);
        }
        // Перекрытие с проёмом над лестницей.
        Prims.Box(Root, new Vector3(26, 6, 56), new Vector3(24, 0.3f, 44), floor);
        Prims.Box(Root, new Vector3(36.5f, 6, 32), new Vector3(3, 0.3f, 4), floor);
        Prims.Box(Root, new Vector3(15, 6, 32), new Vector3(2, 0.3f, 4), floor);
        Rail(new Vector3(16.5f, UpperY, 34.1f), new Vector3(34.8f, UpperY, 34.1f));

        KWalls(new Vector3(14, UpperY, 30), Vector3.right, Vector3.forward, "owow");
        KWalls(new Vector3(38, UpperY, 30), Vector3.forward, Vector3.left, "owwoowow");
        KWalls(new Vector3(14, UpperY, 78), Vector3.right, Vector3.back, "woow");
        KWalls(new Vector3(14, UpperY, 54), Vector3.right, Vector3.forward, "wwdw");
        foreach (var p in new[] { new Vector2(38, 30), new Vector2(38, 54), new Vector2(38, 78), new Vector2(26, 30), new Vector2(26, 78) })
            Kit.Place(kitRoot, "pillar", new Vector3(p.x, UpperY, p.y), 0, KS, true);
        KRoof(new Vector3(26, UpperY, 54), 24, 48);

        HeadmasterOffice();
        RoomOfRequirement();
    }

    static void HeadmasterOffice()
    {
        float y = UpperY;
        Kit.Place(kitRoot, "rug_oval_A", new Vector3(27, y + 0.01f, 43), 0, 2.4f);
        // Стол директора на возвышении.
        // Стол стоит в стороне от прохода к двери Выручай-комнаты (x = 29).
        Prims.Box(Root, new Vector3(31.5f, y + 0.2f, 45f), new Vector3(8, 0.4f, 5), woodDark);
        Kit.Place(kitRoot, "table_long_tablecloth_decorated_A", new Vector3(31.5f, y + 0.4f, 44.5f), 90, FS, true);
        Kit.Place(kitRoot, "armchair_pillows", new Vector3(31.5f, y + 0.4f, 46.7f), 180, FS, true);
        Kit.Place(kitRoot, "candle_triple", new Vector3(30.1f, y + 1.2f, 44.5f), 0, FS);
        Kit.Place(kitRoot, "book_set", new Vector3(33f, y + 1.2f, 44.3f), 20, FS);
        foreach (float x in new[] { 15.6f, 18.4f, 33.6f, 36.4f }) Bookcase(new Vector3(x, y, 52.8f), 180);
        foreach (float z in new[] { 37.4f, 40.2f, 46.6f }) Bookcase(new Vector3(36.8f, y, z), -90);
        // Портреты бывших директоров.
        for (int i = 0; i < 6; i++)
        {
            var p = new Vector3(14.75f, y + 3f + (i % 2) * 1.4f, 36f + i * 2.4f);
            Kit.Place(kitRoot, i % 2 == 0 ? "pictureframe_large_A" : "pictureframe_medium", p, 90, 1.5f);
        }
        // Омут памяти: каменная чаша с серебристым светом.
        var pv = PensievePos;
        Prims.Cyl(Root, pv, 0.45f, 0.9f, stone);
        Prims.Cyl(Root, pv + Vector3.up * 0.9f, 0.95f, 0.25f, stone);
        Prims.Cyl(Root, pv + Vector3.up * 1.12f, 0.82f, 0.04f, MatLib.Glow(new Color(0.75f, 0.85f, 1f), 2.4f), false);
        FX.Loop(kitRoot, pv + Vector3.up * 1.2f, new Color(0.75f, 0.85f, 1f, 0.6f), 22, 0.35f, 0.6f);
        KLight(pv + Vector3.up * 1.8f, 7, 1.6f, new Color(0.7f, 0.82f, 1f));
        // Насест феникса.
        var ph = new Vector3(34.5f, y, 34f);
        Prims.Cyl(Root, ph, 0.08f, 2f, gold);
        Prims.Box(Root, ph + new Vector3(0, 2f, 0), new Vector3(1f, 0.08f, 0.08f), gold, false);
        var fire = MatLib.Glow(new Color(1f, 0.35f, 0.1f), 3f);
        Prims.Make(PrimitiveType.Sphere, Root, ph + new Vector3(0, 2.35f, 0), new Vector3(0.45f, 0.55f, 0.6f), fire, false);
        Prims.Sphere(Root, ph + new Vector3(0, 2.72f, 0.12f), 0.28f, fire);
        Prims.Cone(Root, ph + new Vector3(0, 2.2f, -0.3f), 0.18f, 0.8f, MatLib.Glow(new Color(1f, 0.7f, 0.2f), 3f), false, Quaternion.Euler(-120, 0, 0));
        FX.Loop(kitRoot, ph + new Vector3(0, 2.4f, 0), new Color(1f, 0.55f, 0.2f), 14, 0.3f, 0.8f);
        // Серебряные приборы на столиках крутятся.
        foreach (var t in new[] { new Vector3(17.5f, y, 36f), new Vector3(17.5f, y, 47f) })
        {
            Kit.Place(kitRoot, "table_small_decorated_A", t, 0, FS, true);
            var sp = new GameObject("Instrument").transform;
            sp.SetParent(kitRoot, false);
            sp.position = t + Vector3.up * 1.9f;
            Prims.Torus(sp, Vector3.zero, 0.35f, gold);
            Prims.Torus(sp, Vector3.zero, 0.26f, MatLib.Glow(new Color(0.7f, 0.85f, 1f), 1.5f), Quaternion.Euler(90, 0, 0));
            sp.gameObject.AddComponent<Spinner>().speed = new Vector3(20, 55, 10);
        }
        Kit.Place(kitRoot, "banner_patternA_red", new Vector3(23, y + 0.3f, 54), 180, KS);
        KLight(new Vector3(26, y + 4.5f, 42), 18, 1.4f);
        FloatingCandles(kitRoot, new Vector3(17, y + 4f, 33), new Vector3(36, y + 5.3f, 52), 18);
    }

    static void RoomOfRequirement()
    {
        float y = UpperY;
        var c = RoomOfRequirementPos;
        // Рунический круг и пьедестал вызова.
        // Плоские кольца: обычный тор такого радиуса толстый и просвечивал бы в спальню внизу.
        Prims.MeshObj(Root, Prims.TorusMesh, c + Vector3.up * 0.04f, new Vector3(7f, 0.12f, 7f), MatLib.Glow(new Color(0.55f, 0.4f, 1f), 1.6f), false, null, "RuneRing");
        Prims.MeshObj(Root, Prims.TorusMesh, c + Vector3.up * 0.04f, new Vector3(4.5f, 0.12f, 4.5f), MatLib.Glow(new Color(0.55f, 0.4f, 1f), 1.2f), false, null, "RuneRing");
        Prims.Cyl(Root, c, 0.6f, 1.1f, stoneDark);
        Prims.Sphere(Root, c + Vector3.up * 1.45f, 0.6f, MatLib.Glow(new Color(0.65f, 0.45f, 1f), 2.6f));
        KLight(c + Vector3.up * 2f, 10, 1.8f, new Color(0.65f, 0.5f, 1f));
        // Высокие зеркала и колонны по краям.
        var mirror = MatLib.Fade(new Color(0.65f, 0.75f, 0.9f, 0.55f), 1.2f, 0.98f);
        foreach (int s in new[] { -1, 1 })
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(c.x + s * 10.5f, y, 58 + i * 7);
                Prims.Box(Root, p + Vector3.up * 2.2f, new Vector3(0.2f, 4f, 2.2f), mirror, false);
                Prims.Box(Root, p + Vector3.up * 2.2f, new Vector3(0.25f, 4.3f, 2.5f), gold, false);
                Kit.Place(kitRoot, "pillar_decorated", new Vector3(p.x - s * 1.5f, y, p.z + 3.5f), 0, 1f, true);
            }
        Kit.Place(kitRoot, "banner_shield_blue", new Vector3(20, y + 0.3f, 78), 180, KS);
        Kit.Place(kitRoot, "banner_shield_green", new Vector3(32, y + 0.3f, 78), 180, KS);
        KLight(new Vector3(26, y + 4.5f, 66), 18, 1.2f, new Color(0.8f, 0.7f, 1f));
    }

    // ---------------- БОЛЬШАЯ ЛЕСТНИЦА ----------------
    // Башня 30×30 за северным коридором. Галереи-кольца на 7, 14 и 21 м, между ними — пролёты,
    // которые сами поворачиваются: то соединяют этажи, то уводят в тупик над пустотой.
    static void GrandStaircase()
    {
        const float x0 = -15, z0 = 148, size = 30;
        var g = Prims.Group(Root, "GrandStaircase");
        KFloor(x0, z0, 5, 5, "floor_tile_large");
        for (int lv = 0; lv < 5; lv++)
        {
            float y = lv * KH;
            KWalls(new Vector3(x0, y, z0), Vector3.right, Vector3.forward, lv == 0 ? "wwdww" : "wowow");
            KWalls(new Vector3(x0 + size, y, z0), Vector3.forward, Vector3.left, lv == 0 ? "wwwww" : "owowo");
            KWalls(new Vector3(x0, y, z0 + size), Vector3.right, Vector3.back, lv == 0 ? "wwwww" : "owowo");
            KWalls(new Vector3(x0, y, z0), Vector3.forward, Vector3.right, lv == 0 ? "wwwww" : "owowo");
        }
        foreach (var p in new[] { new Vector2(x0, z0), new Vector2(x0 + size, z0), new Vector2(x0, z0 + size), new Vector2(x0 + size, z0 + size) })
            for (int lv = 0; lv < 5; lv++) Kit.Place(kitRoot, "pillar", new Vector3(p.x, lv * KH, p.y), 0, KS, true);
        // Крыша-шпиль.
        Prims.Box(g, new Vector3(0, 30.15f, 163), new Vector3(31.5f, 0.3f, 31.5f), woodDark);
        Prims.Cone(g, new Vector3(0, 30.3f, 163), 21f, 16f, roof);
        TowerFlag(new Vector3(0, 46f, 163), 6f);

        // Галереи: кольцо шириной 3,5 м вдоль стен, с перилами по внутреннему краю.
        const float inner = 11f;
        foreach (float gy in GalleryHeights)
        {
            float top = gy;
            Prims.Box(g, new Vector3(0, top - 0.15f, z0 + 1.75f + 0.25f), new Vector3(30, 0.3f, 4f), floor);
            Prims.Box(g, new Vector3(0, top - 0.15f, z0 + size - 2f), new Vector3(30, 0.3f, 4f), floor);
            Prims.Box(g, new Vector3(-13f, top - 0.15f, 163), new Vector3(4f, 0.3f, 22f), floor);
            Prims.Box(g, new Vector3(13f, top - 0.15f, 163), new Vector3(4f, 0.3f, 22f), floor);
            // Перила оставляют разрывы там, куда причаливают пролёты.
            Rail(new Vector3(-inner, top, 152), new Vector3(inner, top, 152));
            Rail(new Vector3(-inner, top, 174), new Vector3(inner, top, 174));
            Rail(new Vector3(-inner, top, 152), new Vector3(-inner, top, 156.5f));
            Rail(new Vector3(-inner, top, 169.5f), new Vector3(-inner, top, 174));
            Rail(new Vector3(inner, top, 152), new Vector3(inner, top, 156.5f));
            Rail(new Vector3(inner, top, 169.5f), new Vector3(inner, top, 174));
            // Портреты на стенах галерей.
            for (int i = 0; i < 4; i++)
            {
                Kit.Place(kitRoot, i % 2 == 0 ? "pictureframe_large_A" : "pictureframe_medium", new Vector3(-14.25f, top + 2.6f, 152 + i * 7), 90, 1.6f);
                Kit.Place(kitRoot, i % 2 == 0 ? "pictureframe_medium" : "pictureframe_large_A", new Vector3(14.25f, top + 2.6f, 154 + i * 7), -90, 1.6f);
            }
            KLight(new Vector3(0, top + 3.5f, 163), 22, 1.3f);
        }
        KLight(new Vector3(0, 4f, 163), 20, 1.4f);
        FloatingCandles(kitRoot, new Vector3(-9, 4f, 153), new Vector3(9, 27f, 173), 60);

        // Пролёты: опора внизу, вторая позиция — тупик.
        Stairs.Clear();
        Stairs.Add(MovingStair.Create(new Vector3(0, 0.05f, 163), 11f, 7f, 90f, 40f, 0f));
        // Пролёты 2 и 3 идут по разным полосам, чтобы не нависать друг над другом.
        Stairs.Add(MovingStair.Create(new Vector3(-12.6f, 7f, 158.5f), 23.6f, 7f, 90f, 70f, 3f));
        Stairs.Add(MovingStair.Create(new Vector3(12.6f, 14f, 167.5f), 23.6f, 7f, -90f, -110f, 6f));

        // Награда наверху.
        Chest.Spawn(new Vector3(-12.5f, 21.1f, 175.5f), 180);
        Kit.Place(kitRoot, "sword_shield_gold", new Vector3(0, 21f + 2.5f, 177.2f), 180, 0.8f);
    }
}

// Медленное вращение декоративных предметов.
public class Spinner : MonoBehaviour
{
    public Vector3 speed = new(0, 40, 0);
    void Update() => transform.Rotate(speed * Time.deltaTime, Space.Self);
}

// Движущийся лестничный пролёт: поворачивается вокруг нижней опоры между «соединён» и «тупик».
// Пока игрок стоит на пролёте, он не двигается.
public class MovingStair : MonoBehaviour
{
    public float connectedYaw, deadYaw, length, rise;
    public bool forceConnected;
    float timer, from, to, moveT = -1f, currentYaw;
    const float Hold = 8f, MoveTime = 3f;
    public bool Connected => moveT < 0f && Mathf.Abs(Mathf.DeltaAngle(currentYaw, connectedYaw)) < 0.5f;
    public Vector3 Bottom => transform.position;
    public Vector3 Top => transform.position + Quaternion.Euler(0, connectedYaw, 0) * Vector3.forward * length + Vector3.up * rise;

    public static MovingStair Create(Vector3 pivot, float length, float rise, float connectedYaw, float deadYaw, float phase)
    {
        var go = new GameObject("MovingStair");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pivot;
        var s = go.AddComponent<MovingStair>();
        s.length = length;
        s.rise = rise;
        s.connectedYaw = connectedYaw;
        s.deadYaw = deadYaw;
        s.currentYaw = connectedYaw;
        s.timer = Hold - phase;
        go.transform.rotation = Quaternion.Euler(0, connectedYaw, 0);
        float slope = Mathf.Sqrt(length * length + rise * rise);
        float pitch = -Mathf.Atan2(rise, length) * Mathf.Rad2Deg;
        var stone = MatLib.Solid(new Color(0.58f, 0.55f, 0.5f), 0.1f, 0.35f, 0.5f);
        var wood = MatLib.Solid(new Color(0.26f, 0.17f, 0.1f), 0.2f, 0.6f, 0.5f);
        var flight = Prims.Group(go.transform, "Flight");
        flight.localPosition = new Vector3(0, rise * 0.5f, length * 0.5f);
        flight.localRotation = Quaternion.Euler(pitch, 0, 0);
        Prims.Box(flight, Vector3.zero, new Vector3(3f, 0.3f, slope), stone);
        // Ступени-рёбра для вида и низкие перила по бокам.
        int steps = Mathf.RoundToInt(slope / 0.7f);
        for (int i = 0; i < steps; i++)
            Prims.Box(flight, new Vector3(0, 0.17f, -slope * 0.5f + (i + 0.5f) * slope / steps), new Vector3(3f, 0.06f, 0.12f), wood, false);
        foreach (int sd in new[] { -1, 1 })
            Prims.Box(flight, new Vector3(sd * 1.55f, 0.55f, 0), new Vector3(0.12f, 0.9f, slope), wood);
        return s;
    }

    // Сразу начать поворот (для проверок).
    public void Kick()
    {
        from = currentYaw;
        to = Mathf.Abs(Mathf.DeltaAngle(currentYaw, connectedYaw)) < 1f ? deadYaw : connectedYaw;
        moveT = 0f;
    }

    bool PlayerOn()
    {
        var gm = GameManager.I;
        if (gm == null || gm.player == null) return false;
        var p = gm.player.transform.position;
        foreach (var h in Physics.RaycastAll(p + Vector3.up * 0.6f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
            if (h.collider.transform.IsChildOf(transform)) return true;
        return false;
    }

    void Update()
    {
        if (forceConnected)
        {
            moveT = -1f;
            currentYaw = connectedYaw;
            transform.rotation = Quaternion.Euler(0, currentYaw, 0);
            return;
        }
        if (PlayerOn()) return;
        float dt = Time.deltaTime;
        if (moveT >= 0f)
        {
            moveT += dt / MoveTime;
            currentYaw = Mathf.LerpAngle(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveT)));
            transform.rotation = Quaternion.Euler(0, currentYaw, 0);
            if (moveT >= 1f) { moveT = -1f; timer = Hold; }
            return;
        }
        timer -= dt;
        if (timer > 0f) return;
        from = currentYaw;
        to = Mathf.Abs(Mathf.DeltaAngle(currentYaw, connectedYaw)) < 1f ? deadYaw : connectedYaw;
        moveT = 0f;
        var cam = Camera.main;
        if (cam != null && (cam.transform.position - transform.position).magnitude < 40f) SFX.Play("heavy", 0.25f);
    }
}

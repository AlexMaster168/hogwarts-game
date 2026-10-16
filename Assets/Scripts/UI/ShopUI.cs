using UnityEngine;

// Окно лавки и инвентаря (I). Игра на паузе, курсор свободен.
public class ShopUI : MonoBehaviour
{
    public static ShopUI I;
    public static int ChangedFrame = -1;
    string shopId;
    GUIStyle sTitle, sName, sText, sSmall, sBtn;
    int styleH = -1;
    float S;
    Texture2D white;
    Vector2 shopScroll, inventoryScroll;
    bool skinPicker;

    static readonly Color Gold = new(1f, 0.83f, 0.45f);

    void Awake()
    {
        I = this;
        white = Texture2D.whiteTexture;
    }

    public static void Open(string id, bool lastItem = false)
    {
        I.shopId = id;
        I.shopScroll = lastItem ? new Vector2(0, 100000) : Vector2.zero;
        ChangedFrame = Time.frameCount;
        SFX.Play("book", 0.6f);
        GameState.Set(GameMode.Shop);
    }

    public static void OpenInventory()
    {
        ChangedFrame = Time.frameCount;
        SFX.Play("book", 0.6f);
        GameState.Set(GameMode.Inventory);
    }

    static void Close()
    {
        ChangedFrame = Time.frameCount;
        SFX.Play("click", 0.4f);
        GameState.Set(GameMode.Play);
    }

    void Update()
    {
        if (ChangedFrame == Time.frameCount) return;
        switch (GameState.Mode)
        {
            case GameMode.Play:
                if (Input.GetKeyDown(KeyCode.I)) OpenInventory();
                break;
            case GameMode.Shop:
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) Close();
                break;
            case GameMode.Inventory:
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I)) Close();
                break;
        }
    }

    void Styles()
    {
        if (styleH == Screen.height && sText != null) return;
        styleH = Screen.height;
        S = Mathf.Max(0.5f, Screen.height / 1080f);
        sTitle = new GUIStyle(GUI.skin.label) { fontSize = (int)(40 * S), fontStyle = FontStyle.Bold };
        sName = new GUIStyle(GUI.skin.label) { fontSize = (int)(25 * S), fontStyle = FontStyle.Bold };
        sText = new GUIStyle(GUI.skin.label) { fontSize = (int)(20 * S), wordWrap = true };
        sSmall = new GUIStyle(GUI.skin.label) { fontSize = (int)(17 * S), wordWrap = true };
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = (int)(20 * S), fontStyle = FontStyle.Bold };
    }

    void Box(Rect r, Color c)
    {
        var o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = o;
    }

    void Label(Rect r, string t, GUIStyle s, Color c, TextAnchor a = TextAnchor.UpperLeft)
    {
        s.alignment = a;
        s.normal.textColor = c;
        GUI.Label(r, t, s);
    }

    void OnGUI()
    {
        if (GameState.Mode != GameMode.Shop && GameState.Mode != GameMode.Inventory) return;
        var p = GameManager.I.player;
        if (p == null) return;
        Styles();
        GUI.depth = -10;
        float W = Screen.width, H = Screen.height;
        Box(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.55f));
        var win = new Rect(W / 2 - 600 * S, H / 2 - 400 * S, 1200 * S, 800 * S);
        Box(win, new Color(0.06f, 0.05f, 0.08f, 0.96f));
        Box(new Rect(win.x, win.y, win.width, 5 * S), Gold);
        if (GameState.Mode == GameMode.Shop) DrawShop(win, p);
        else DrawInventory(win, p);
    }

    void DrawShop(Rect win, PlayerController p)
    {
        var inv = p.inventory;
        var shop = ItemDB.Shops[shopId];
        Label(new Rect(win.x + 30 * S, win.y + 20 * S, win.width, 50 * S), shop.title, sTitle, Gold);
        Label(new Rect(win.x, win.y + 26 * S, win.width - 30 * S, 40 * S), $"Галлеоны: {inv.galleons}", sName, new Color(1f, 0.85f, 0.35f), TextAnchor.UpperRight);
        float y = win.y + 95 * S;
        bool compact = shop.items.Length > 5;
        float rowH = compact ? 88 * S : 112 * S;
        shopScroll = GUI.BeginScrollView(new Rect(win.x + 10 * S, y, win.width - 20 * S, win.height - 155 * S), shopScroll,
            new Rect(win.x + 10 * S, y, win.width - 45 * S, shop.items.Length * (rowH + 8 * S)));
        foreach (var id in shop.items)
        {
            var d = ItemDB.Get(id);
            var row = new Rect(win.x + 30 * S, y, win.width - 60 * S, rowH);
            Box(row, new Color(1, 1, 1, 0.05f));
            Box(new Rect(row.x, row.y, 6 * S, row.height), d.color);
            Label(new Rect(row.x + 22 * S, row.y + (compact ? 4 : 10) * S, 640 * S, 34 * S), d.name, sName, d.forbidden ? new Color(0.5f, 1f, 0.5f) : Color.white);
            string kind = ItemDB.KindName(d.kind) + (d.kind == ItemKind.Potion ? $" — у тебя {inv.potions[d.id]}/{Inventory.MaxStack}" : "");
            if (compact)
            {
                Label(new Rect(row.x + 380 * S, row.y + 10 * S, 400 * S, 26 * S), kind, sSmall, new Color(0.7f, 0.7f, 0.8f));
                Label(new Rect(row.x + 22 * S, row.y + 38 * S, 860 * S, 46 * S), d.desc, sSmall, new Color(0.9f, 0.9f, 0.9f));
            }
            else
            {
                Label(new Rect(row.x + 22 * S, row.y + 44 * S, 700 * S, 26 * S), kind, sSmall, new Color(0.7f, 0.7f, 0.8f));
                Label(new Rect(row.x + 22 * S, row.y + 70 * S, 760 * S, 40 * S), d.desc, sText, new Color(0.9f, 0.9f, 0.9f));
            }
            Label(new Rect(row.x + 780 * S, row.y + (compact ? 6 : 14) * S, 140 * S, 34 * S), d.price + " г.", sName, new Color(1f, 0.85f, 0.35f), TextAnchor.UpperRight);
            var reason = inv.CantBuyReason(d);
            var btn = new Rect(row.x + 945 * S, row.y + (rowH - 50 * S) / 2, 180 * S, 50 * S);
            if (reason == null)
            {
                if (GUI.Button(btn, "Купить", sBtn)) inv.Buy(d);
            }
            else Label(btn, reason, sSmall, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter);
            y += rowH + 8 * S;
        }
        GUI.EndScrollView();
        Label(new Rect(win.x, win.yMax - 50 * S, win.width - 30 * S, 40 * S), "Колесо — список товаров     [Esc] или [E] — уйти", sSmall, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleRight);
    }

    void DrawInventory(Rect win, PlayerController p)
    {
        var inv = p.inventory;
        var c = p.caster;
        Label(new Rect(win.x + 30 * S, win.y + 20 * S, win.width, 50 * S), "Инвентарь", sTitle, Gold);
        Label(new Rect(win.x, win.y + 26 * S, win.width - 30 * S, 40 * S), $"Галлеоны: {inv.galleons}", sName, new Color(1f, 0.85f, 0.35f), TextAnchor.UpperRight);

        // Снаряжение.
        float x = win.x + 30 * S, y = win.y + 95 * S, colW = 560 * S;
        inventoryScroll = GUI.BeginScrollView(new Rect(win.x + 10 * S, y, win.width - 20 * S, win.height - 155 * S), inventoryScroll,
            new Rect(win.x + 10 * S, y, win.width - 45 * S, Mathf.Max(1000, 500 + inv.owned.Count * 48, 700 + c.Pages * 65) * S));
        Label(new Rect(x, y, 170 * S, 34 * S), "Скин героя", sName, Gold);
        if (GUI.Button(new Rect(x + 170 * S, y, 350 * S, 34 * S), PlayerController.SkinNames[p.skin] + "  ▾", sBtn)) skinPicker = !skinPicker;
        y += 44 * S;
        if (skinPicker)
        {
            int selectedSkin = GUI.SelectionGrid(new Rect(x, y, 540 * S, 110 * S), p.skin, PlayerController.SkinNames, 3, GUI.skin.button);
            if (selectedSkin != p.skin) { p.SetSkin(selectedSkin); skinPicker = false; }
            y += 120 * S;
        }
        Label(new Rect(x, y, colW, 34 * S), "Снаряжение", sName, Gold);
        y += 40 * S;
        foreach (var kind in new[] { ItemKind.Robe, ItemKind.Hat, ItemKind.Gloves, ItemKind.Broom, ItemKind.Artifact })
        {
            inv.equipped.TryGetValue(kind, out var eqId);
            var eq = eqId != null ? ItemDB.Get(eqId) : null;
            Box(new Rect(x, y, colW, 44 * S), new Color(1, 1, 1, 0.05f));
            Label(new Rect(x + 12 * S, y + 8 * S, 150 * S, 30 * S), ItemDB.KindName(kind) + ":", sText, new Color(0.7f, 0.7f, 0.8f));
            Label(new Rect(x + 160 * S, y + 8 * S, 280 * S, 30 * S), eq != null ? eq.name : "—", sText, eq != null ? Color.white : new Color(0.5f, 0.5f, 0.5f));
            if (eq != null && kind != ItemKind.Broom && GUI.Button(new Rect(x + colW - 110 * S, y + 6 * S, 100 * S, 32 * S), "Снять", sBtn)) inv.Unequip(kind);
            y += 50 * S;
        }
        y += 10 * S;
        Label(new Rect(x, y, colW, 30 * S), "Купленные вещи", sName, Gold);
        y += 38 * S;
        bool any = false;
        foreach (var id in inv.owned)
        {
            var d = ItemDB.Get(id);
            if (d == null) continue;
            any = true;
            bool on = inv.equipped.TryGetValue(d.kind, out var e) && e == id;
            Label(new Rect(x + 12 * S, y + 4 * S, 400 * S, 30 * S), d.name, sText, on ? Gold : Color.white);
            if (!on && GUI.Button(new Rect(x + colW - 110 * S, y, 100 * S, 32 * S), "Надеть", sBtn)) inv.Equip(d);
            y += 38 * S;
            if (y > win.yMax - 180 * S) break;
        }
        if (!any) { Label(new Rect(x + 12 * S, y, colW, 30 * S), "Пусто. Загляни в лавки Хогсмида!", sText, new Color(0.6f, 0.6f, 0.6f)); y += 38 * S; }

        // Характеристики.
        y = win.yMax - 150 * S;
        Label(new Rect(x, y, colW, 30 * S), "Характеристики", sName, Gold);
        Label(new Rect(x, y + 36 * S, colW, 100 * S),
            $"Здоровье: {Mathf.CeilToInt(p.health.current)}/{p.health.max}    Урон: x{inv.DamageMult:0.00}\n" +
            $"Входящий урон: x{inv.DefenseMult:0.00}    Перезарядка: x{inv.CooldownMult:0.00}\n" +
            $"Скорость метлы: x{inv.BroomMult:0.00}    Страницы: {QuestManager.I.pages}/{QuestManager.TotalPages}", sText, Color.white);

        // Зелья.
        float x2 = win.x + 630 * S;
        y = win.y + 95 * S;
        Label(new Rect(x2, y, colW, 34 * S), "Зелья", sName, Gold);
        y += 40 * S;
        foreach (var (id, key) in Inventory.PotionKeys)
        {
            var d = ItemDB.Get(id);
            Box(new Rect(x2, y, colW - 20 * S, 36 * S), new Color(1, 1, 1, 0.05f));
            Box(new Rect(x2, y, 5 * S, 36 * S), d.color);
            Label(new Rect(x2 + 14 * S, y + 4 * S, 400 * S, 30 * S), $"{d.name} x{(inv.InfinitePotions ? "∞" : inv.potions[id].ToString())}  [{key}]", sText, Color.white);
            if ((inv.InfinitePotions || inv.potions[id] > 0) && GUI.Button(new Rect(x2 + colW - 140 * S, y + 3 * S, 110 * S, 30 * S), "Выпить", sBtn))
            {
                GameState.Set(GameMode.Play);
                inv.UsePotion(id);
            }
            y += 40 * S;
        }

        // Заклинания по наборам.
        y += 14 * S;
        Label(new Rect(x2, y, colW, 34 * S), "Заклинания (Tab — сменить набор)", sName, Gold);
        y += 40 * S;
        for (int pg = 0; pg < c.Pages; pg++)
        {
            var line = $"Набор {pg + 1}:  ";
            for (int i = 0; i < 4; i++)
            {
                int k = pg * 4 + i;
                if (k < c.learned.Count) line += $"[{i + 1}] {c.learned[k].name}   ";
            }
            Label(new Rect(x2, y, colW, 60 * S), line, sText, pg == c.page ? Color.white : new Color(0.65f, 0.65f, 0.7f));
            y += 60 * S;
        }
        if (c.learned.Count == 0) Label(new Rect(x2, y, colW, 30 * S), "Пока только базовое заклинание.", sText, new Color(0.6f, 0.6f, 0.6f));
        GUI.EndScrollView();
        Label(new Rect(win.x, win.yMax - 50 * S, win.width - 30 * S, 40 * S), "Колесо — прокрутка    [I] или [Esc] — закрыть", sSmall, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleRight);
    }
}

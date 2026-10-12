using System.Collections.Generic;
using UnityEngine;

public enum ItemKind { Potion, Broom, Robe, Hat, Gloves, Book }

public class ItemDef
{
    public string id, name, desc;
    public ItemKind kind;
    public int price;
    public Color color = Color.white;
    public float dmgBonus, defBonus, hpBonus, cdBonus, broomBonus;
    public SpellType spell;
    public bool forbidden;
}

// Каталог всех предметов и лавок.
public static class ItemDB
{
    public static readonly List<ItemDef> All = new();
    static readonly Dictionary<string, ItemDef> byId = new();

    public static readonly Dictionary<string, (string title, string[] items)> Shops = new()
    {
        ["pippin"] = ("Зелья Дж. Пиппина", new[] { "wiggenweld", "rowan", "maxima", "edurus", "focus", "invis", "thunder", "felix" }),
        ["spintwitches"] = ("Спинтвитчес — всё для полётов", new[] { "comet", "nimbus", "firebolt" }),
        ["gladrags"] = ("Гладрэгс — волшебная одежда", new[] { "robe_duel", "robe_dragon", "hat_sage", "hat_dark", "gloves_seeker" }),
        ["tomes"] = ("Свитки и фолианты", new[] { "book_expelliarmus", "book_accio", "book_diffindo", "book_glacius", "book_bombarda" }),
        ["shady"] = ("Тёмный торговец", new[] { "book_imperio", "book_crucio", "book_avada" }),
    };

    static ItemDB()
    {
        Add(new ItemDef { id = "wiggenweld", name = "Вигенвельд", desc = "Восстанавливает 70 здоровья. [H]", kind = ItemKind.Potion, price = 25, color = new Color(0.3f, 1f, 0.4f) });
        Add(new ItemDef { id = "rowan", name = "Отвар рябины", desc = "Лечит 60 здоровья постепенно за 10 секунд. Дёшево и сердито. [G]", kind = ItemKind.Potion, price = 15, color = new Color(0.9f, 0.3f, 0.2f) });
        Add(new ItemDef { id = "maxima", name = "Зелье Максимы", desc = "+50% к урону на 20 секунд. [J]", kind = ItemKind.Potion, price = 60, color = new Color(1f, 0.4f, 0.2f) });
        Add(new ItemDef { id = "focus", name = "Фокусирующее зелье", desc = "12 секунд заклинания без перезарядки. [L]", kind = ItemKind.Potion, price = 90, color = new Color(0.3f, 0.9f, 1f) });
        Add(new ItemDef { id = "invis", name = "Зелье невидимости", desc = "15 секунд враги тебя не видят. Каст снимает эффект. [N]", kind = ItemKind.Potion, price = 70, color = new Color(0.75f, 0.8f, 0.95f) });
        Add(new ItemDef { id = "thunder", name = "Грозовое зелье", desc = "Вызывает грозу: молнии 8 секунд бьют врагов рядом с целью. [M]", kind = ItemKind.Potion, price = 80, color = new Color(0.55f, 0.5f, 1f) });
        Add(new ItemDef { id = "felix", name = "Феликс Фелицис", desc = "Жидкая удача: двойные галлеоны 60 секунд. [U]", kind = ItemKind.Potion, price = 200, color = new Color(1f, 0.85f, 0.2f) });
        Add(new ItemDef { id = "edurus", name = "Зелье Эдурус", desc = "-50% входящего урона на 20 секунд. [K]", kind = ItemKind.Potion, price = 60, color = new Color(0.5f, 0.7f, 1f) });

        Add(new ItemDef { id = "cleansweep", name = "Чистомёт-7", desc = "Школьная метла.", kind = ItemKind.Broom, price = 0, color = new Color(0.42f, 0.27f, 0.13f) });
        Add(new ItemDef { id = "comet", name = "Комета-260", desc = "+25% к скорости полёта.", kind = ItemKind.Broom, price = 150, broomBonus = 0.25f, color = new Color(0.55f, 0.35f, 0.2f) });
        Add(new ItemDef { id = "nimbus", name = "Нимбус-1000", desc = "+50% к скорости полёта.", kind = ItemKind.Broom, price = 350, broomBonus = 0.5f, color = new Color(0.3f, 0.15f, 0.08f) });
        Add(new ItemDef { id = "firebolt", name = "Молния", desc = "+100% к скорости полёта. Мечта любого ловца.", kind = ItemKind.Broom, price = 800, broomBonus = 1f, color = new Color(0.15f, 0.08f, 0.04f) });

        Add(new ItemDef { id = "robe_duel", name = "Мантия дуэлянта", desc = "+20% к урону заклинаний.", kind = ItemKind.Robe, price = 120, dmgBonus = 0.2f, color = new Color(0.35f, 0.08f, 0.1f) });
        Add(new ItemDef { id = "robe_dragon", name = "Мантия из драконьей кожи", desc = "-30% входящего урона.", kind = ItemKind.Robe, price = 220, defBonus = 0.3f, color = new Color(0.15f, 0.25f, 0.15f) });
        Add(new ItemDef { id = "hat_sage", name = "Шляпа мудреца", desc = "+40 к максимальному здоровью.", kind = ItemKind.Hat, price = 90, hpBonus = 40, color = new Color(0.25f, 0.2f, 0.45f) });
        Add(new ItemDef { id = "hat_dark", name = "Шляпа тёмного мага", desc = "+10% к урону, +20 здоровья.", kind = ItemKind.Hat, price = 150, dmgBonus = 0.1f, hpBonus = 20, color = new Color(0.07f, 0.07f, 0.08f) });
        Add(new ItemDef { id = "gloves_seeker", name = "Перчатки ловца", desc = "-20% к перезарядке заклинаний.", kind = ItemKind.Gloves, price = 160, cdBonus = 0.2f, color = new Color(0.5f, 0.35f, 0.2f) });

        Book("book_expelliarmus", SpellType.Expelliarmus, "Экспеллиармус", "Обезоруживает магов на 5 секунд.", 80);
        Book("book_accio", SpellType.Accio, "Акцио", "Притягивает врага к тебе.", 60);
        Book("book_diffindo", SpellType.Diffindo, "Диффиндо", "Быстрое режущее заклятие, высокий урон.", 110);
        Book("book_glacius", SpellType.Glacius, "Глациус", "Замораживает врага, он получает +50% урона.", 130);
        Book("book_bombarda", SpellType.Bombarda, "Бомбарда", "Мощный взрыв по площади.", 200);
        Book("book_imperio", SpellType.Imperio, "Империо", "НЕПРОСТИТЕЛЬНОЕ. Враг 15 секунд сражается за тебя.", 400, true);
        Book("book_crucio", SpellType.Crucio, "Круцио", "НЕПРОСТИТЕЛЬНОЕ. Враг корчится в муках и получает урон.", 550, true);
        Book("book_avada", SpellType.AvadaKedavra, "Авада Кедавра", "НЕПРОСТИТЕЛЬНОЕ. Мгновенная смерть (кроме Ранрока).", 900, true);
    }

    static void Book(string id, SpellType s, string name, string desc, int price, bool forbidden = false)
    {
        Add(new ItemDef { id = id, name = "Книга: " + name, desc = desc, kind = ItemKind.Book, price = price, spell = s, forbidden = forbidden,
            color = forbidden ? new Color(0.4f, 1f, 0.4f) : new Color(0.85f, 0.75f, 0.5f) });
    }

    static void Add(ItemDef d) { All.Add(d); byId[d.id] = d; }
    public static ItemDef Get(string id) => byId.TryGetValue(id, out var d) ? d : null;

    public static string KindName(ItemKind k) => k switch
    {
        ItemKind.Potion => "Зелье",
        ItemKind.Broom => "Метла",
        ItemKind.Robe => "Мантия",
        ItemKind.Hat => "Шляпа",
        ItemKind.Gloves => "Перчатки",
        _ => "Книга",
    };
}

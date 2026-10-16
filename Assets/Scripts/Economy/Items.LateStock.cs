using System.Linq;
using UnityEngine;

public static partial class ItemDB
{
    static void Stock(string shop, params string[] ids)
    {
        var old = Shops[shop]; Shops[shop] = (old.title, old.items.Concat(ids).ToArray());
    }
    static void AddLateStock()
    {
        Add(new ItemDef { id = "bottomless_satchel", name = "Бездонная сумка зелий", kind = ItemKind.Artifact, price = 15000, desc = "Все 8 зелий доступны даже при нулевом запасе и не расходуются. Надень в слот артефакта. Пауза между глотками — 1 сек.", color = new Color(0.5f, 1f, 0.7f) });
        Stock("pippin", "bottomless_satchel");
        Add(new ItemDef { id = "broom_silver", name = "Серебряная стрела", kind = ItemKind.Broom, price = 6500, broomBonus = 1.25f, desc = "Скорость полёта +125%.", color = Color.gray });
        Add(new ItemDef { id = "broom_thunder", name = "Громовой странник", kind = ItemKind.Broom, price = 16000, broomBonus = 1.5f, desc = "Скорость +150%, здоровье +80.", hpBonus = 80, color = Color.cyan });
        Add(new ItemDef { id = "broom_keeper", name = "Наследие Хранителя", kind = ItemKind.Broom, price = 28000, broomBonus = 1.75f, cdBonus = 0.1f, desc = "Скорость +175%, перезарядка −10%.", color = new Color(0.6f, 0.4f, 1f) });
        Stock("spintwitches", "broom_silver", "broom_thunder", "broom_keeper");
        Gear("robe_auror", "Мантия аврора", ItemKind.Robe, 4500, 0.2f, 0.2f, 60, 0, new Color(0.15f, 0.25f, 0.45f));
        Gear("robe_keeper", "Одеяние Хранителя", ItemKind.Robe, 12000, 0.35f, 0.25f, 120, 0, new Color(0.55f, 0.4f, 0.2f));
        Gear("robe_storm", "Мантия грозы", ItemKind.Robe, 18000, 0.6f, 0, 0, 0.15f, new Color(0.25f, 0.45f, 0.7f));
        Gear("hat_archivist", "Капюшон архивариуса", ItemKind.Hat, 5500, 0, 0, 140, 0.15f, new Color(0.7f, 0.65f, 0.5f));
        Gear("hat_auror", "Шляпа старшего аврора", ItemKind.Hat, 9000, 0.25f, 0.15f, 60, 0, new Color(0.15f, 0.2f, 0.3f));
        Gear("gloves_duelist", "Перчатки мастера дуэлей", ItemKind.Gloves, 7000, 0.2f, 0, 0, 0.3f, new Color(0.5f, 0.2f, 0.2f));
        Gear("gloves_warden", "Рукавицы часового", ItemKind.Gloves, 11000, 0, 0.2f, 80, 0.25f, new Color(0.7f, 0.5f, 0.25f));
        Stock("gladrags", "robe_auror", "robe_keeper", "robe_storm", "hat_archivist", "hat_auror", "gloves_duelist", "gloves_warden");
        Gear("relic_phoenix", "Перо феникса", ItemKind.Artifact, 8000, 0.2f, 0, 100, 0, new Color(1f, 0.5f, 0.1f));
        Gear("relic_hourglass", "Песочные часы Хранителя", ItemKind.Artifact, 12500, 0, 0, 0, 0.25f, Color.yellow);
        Gear("relic_aegis", "Печать несокрушимости", ItemKind.Artifact, 20000, 0, 0.3f, 180, 0, Color.cyan);
        Stock("tomes", "relic_phoenix", "relic_hourglass", "relic_aegis");
        Gear("relic_hunger", "Осколок голода", ItemKind.Artifact, 24000, 0.75f, 0, 0, 0, new Color(0.8f, 0.1f, 0.3f));
        Stock("darkgear", "relic_hunger");
        Book("book_lumos", SpellType.Lumos, "Люмос", "Свет на 35 секунд; проявляет узлы и снимает щит Регента.", 1200);
        Book("book_reparo", SpellType.Reparo, "Репаро", "Восстанавливает узлы механизма в радиусе 10 м.", 1800);
        Book("book_aguamenti", SpellType.Aguamenti, "Агуаменти", "Водяной заряд; двойной урон пепельникам, снимает огненный щит.", 2200);
        Book("book_reducto", SpellType.Reducto, "Редукто", "75 урона; пробивает рунную броню.", 4000);
        Book("book_incarcerous", SpellType.Incarcerous, "Инкарцеро", "Связывает обычного врага на 4 с, тяжёлого на 0,8 с.", 3000);
        Book("book_rictusempra", SpellType.Rictusempra, "Риктусемпра", "Прерывает атаку и оглушает на 2,8 с (тяжёлого на 0,6 с).", 2000);
        Stock("tomes", "book_lumos", "book_reparo", "book_aguamenti", "book_reducto", "book_incarcerous", "book_rictusempra");
    }
    static void Gear(string id, string name, ItemKind kind, int price, float damage, float defense, float hp, float cd, Color color)
    {
        Add(new ItemDef { id = id, name = name, kind = kind, price = price, dmgBonus = damage, defBonus = defense, hpBonus = hp, cdBonus = cd, color = color,
            desc = $"Урон +{damage:P0}; защита +{defense:P0}; здоровье +{hp:0}; перезарядка −{cd:P0}." });
    }
}

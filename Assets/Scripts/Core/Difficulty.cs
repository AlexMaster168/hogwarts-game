using UnityEngine;

// Уровни сложности: множители для врагов и наград. Выбирается на титульном экране и в паузе, хранится в сохранении.
public static class Difficulty
{
    public struct Level
    {
        public string name, desc;
        public float hp, dmg, attack, speed, tele, reward;
        public int extra;   // дополнительных врагов в каждой сюжетной схватке
        public Color color;
    }

    public static readonly Level[] Levels =
    {
        new() { name = "Легко", desc = "Враги слабее и медленнее, урон по тебе вдвое меньше", hp = 0.65f, dmg = 0.5f, attack = 0.8f, speed = 0.9f, tele = 0.8f, reward = 0.8f, extra = 0, color = new Color(0.5f, 0.9f, 0.5f) },
        new() { name = "Нормально", desc = "Задуманный баланс", hp = 1f, dmg = 1f, attack = 1f, speed = 1f, tele = 1f, reward = 1f, extra = 0, color = new Color(0.9f, 0.9f, 0.95f) },
        new() { name = "Сложно", desc = "HP врагов ×1,8, урон ×1,6, атакуют чаще, +1 враг в схватках", hp = 1.8f, dmg = 1.6f, attack = 1.3f, speed = 1.12f, tele = 1.15f, reward = 1.3f, extra = 1, color = new Color(1f, 0.65f, 0.3f) },
        new() { name = "Кошмар", desc = "HP врагов ×3, урон ×2,5, почти без пауз между атаками, +2 врага", hp = 3f, dmg = 2.5f, attack = 1.7f, speed = 1.3f, tele = 1.4f, reward = 1.8f, extra = 2, color = new Color(1f, 0.25f, 0.25f) },
    };

    public static int Index = 1;
    public static Level Cur => Levels[Mathf.Clamp(Index, 0, Levels.Length - 1)];

    public static void Set(int i)
    {
        Index = Mathf.Clamp(i, 0, Levels.Length - 1);
        // Уже живые враги пересчитываются под новый уровень, сохраняя долю здоровья.
        foreach (var e in Enemy.All)
            if (e != null && !e.IsDead) e.ApplyDifficulty();
    }
}

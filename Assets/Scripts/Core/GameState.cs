using UnityEngine;

public enum GameMode { Title, Play, Dialogue, Paused, Dead, Victory, Shop, Inventory, Map, Service, Journal }

// Глобальный режим игры. Всё, кроме Play, ставит время на паузу.
public static class GameState
{
    public static GameMode Mode { get; private set; } = GameMode.Title;
    public static bool Playing => Mode == GameMode.Play;

    public static void Reset()
    {
        Mode = GameMode.Title;
        Time.timeScale = 1f;
    }

    public static void Set(GameMode m)
    {
        Mode = m;
        bool locked = m == GameMode.Play;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        Time.timeScale = m == GameMode.Play ? 1f : 0f;
    }
}

// "Паучье чутьё" как в оригинале: жёлтое — можно блокировать Протего, красное — только уворот.
public static class ThreatSense
{
    public static float yellowUntil, redUntil;

    public static void Warn(bool unblockable, float duration)
    {
        float t = Time.time + duration;
        if (unblockable) redUntil = Mathf.Max(redUntil, t);
        else yellowUntil = Mathf.Max(yellowUntil, t);
    }

    public static bool Red => Time.time < redUntil;
    public static bool Yellow => Time.time < yellowUntil;

    public static void Clear() { yellowUntil = redUntil = 0f; }
}

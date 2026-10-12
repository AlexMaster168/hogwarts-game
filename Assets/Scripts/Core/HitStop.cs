using System.Collections;
using UnityEngine;

// Короткая «заморозка» кадра при сильном попадании — удар ощущается весомее.
public static class HitStop
{
    static float until;
    static bool running;
    const float Slow = 0.06f;

    public static void Do(float seconds)
    {
        if (GameManager.I == null || GameState.Mode != GameMode.Play) return;
        until = Mathf.Max(until, Time.unscaledTime + seconds);
        if (running) return;
        running = true;
        GameManager.I.StartCoroutine(Run());
    }

    static IEnumerator Run()
    {
        if (Mathf.Approximately(Time.timeScale, 1f)) Time.timeScale = Slow;
        while (Time.unscaledTime < until && GameState.Mode == GameMode.Play) yield return null;
        // Режимы меню сами ставят timeScale = 0 — возвращаем 1 только если всё ещё идёт игра и мы его меняли.
        if (GameState.Mode == GameMode.Play && Mathf.Approximately(Time.timeScale, Slow)) Time.timeScale = 1f;
        running = false;
    }
}

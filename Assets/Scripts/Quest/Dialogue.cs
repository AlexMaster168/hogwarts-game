using System;
using System.Collections.Generic;
using UnityEngine;

public struct Line
{
    public string who, text;
    public Line(string who, string text) { this.who = who; this.text = text; }
}

// Очередь реплик + выбор вариантов. Пока диалог открыт — игра на паузе.
public static class Dialogue
{
    static readonly Queue<Line> lines = new();
    static Action onDone;
    static Action<int> onChoice;
    static int openedFrame;

    static Line cur;
    public static Line Current => new Line(cur.who, Gender.Fix(cur.text));
    public static string[] Choices { get; private set; }
    public static bool Active => GameState.Mode == GameMode.Dialogue;

    public static void Reset()
    {
        lines.Clear();
        onDone = null;
        onChoice = null;
        Choices = null;
    }

    public static void Show(Action done, params Line[] ls)
    {
        lines.Clear();
        foreach (var l in ls) lines.Enqueue(l);
        onDone = done;
        Choices = null;
        openedFrame = Time.frameCount;
        cur = lines.Count > 0 ? lines.Dequeue() : new Line("", "");
        GameState.Set(GameMode.Dialogue);
    }

    public static void Choice(string who, string prompt, string[] options, Action<int> cb)
    {
        lines.Clear();
        onDone = null;
        onChoice = cb;
        Choices = options;
        openedFrame = Time.frameCount;
        cur = new Line(who, prompt);
        GameState.Set(GameMode.Dialogue);
    }

    public static void Advance()
    {
        if (!Active || Choices != null || Time.frameCount == openedFrame) return;
        SFX.Play("click", 0.4f);
        if (lines.Count > 0)
        {
            cur = lines.Dequeue();
            return;
        }
        GameState.Set(GameMode.Play);
        var d = onDone;
        onDone = null;
        d?.Invoke();
    }

    public static void Pick(int i)
    {
        if (!Active || Choices == null || i < 0 || i >= Choices.Length || Time.frameCount == openedFrame) return;
        SFX.Play("click", 0.5f);
        var cb = onChoice;
        onChoice = null;
        Choices = null;
        GameState.Set(GameMode.Play);
        cb?.Invoke(i);
    }
}

using System.Collections.Generic;
using UnityEngine;

// Звуки: сначала ищем файлы Resources/Audio/<имя>_1..N (случайный вариант),
// если их нет — используем синтезированный запасной клип.
// Плюс музыка по ситуации (замок/лес/бой) и петля ветра на метле.
public class SFX : MonoBehaviour
{
    static SFX I;
    const int SR = 44100;
    AudioSource src, windSrc;
    AudioSource[] music;
    int musicActive;
    string musicTrack;
    readonly Dictionary<string, AudioClip> synth = new();
    readonly Dictionary<string, AudioClip[]> files = new();
    readonly System.Random rng = new(7);

    delegate float Gen(float t, float u);

    public static void Play(string name, float vol = 0.6f)
    {
        if (I == null || I.src == null) return;
        var arr = I.Files(name);
        if (arr.Length > 0)
        {
            I.src.PlayOneShot(arr[Random.Range(0, arr.Length)], vol);
            return;
        }
        if (I.synth.TryGetValue(name, out var c)) I.src.PlayOneShot(c, vol);
    }

    // Произнесение заклинания голосом героя: Resources/Audio/voice_m_<Spell> или voice_f_<Spell>.
    // Отдельный источник, чтобы новая фраза обрывала предыдущую, а не накладывалась.
    public static void Voice(string spell, float vol = 0.9f)
    {
        if (I == null) return;
        var arr = I.Files((Gender.Female ? "voice_f_" : "voice_m_") + spell);
        if (arr.Length == 0) return;
        if (I.voiceSrc == null)
        {
            I.voiceSrc = I.gameObject.AddComponent<AudioSource>();
            I.voiceSrc.playOnAwake = false;
            I.voiceSrc.spatialBlend = 0f;
        }
        I.voiceSrc.Stop();
        I.voiceSrc.clip = arr[0];
        I.voiceSrc.volume = vol;
        I.voiceSrc.pitch = 1f;
        I.voiceSrc.Play();
        LastVoice = arr[0].name;
    }

    public static string LastVoice;
    AudioSource voiceSrc;

    AudioClip[] Files(string name)
    {
        if (files.TryGetValue(name, out var arr)) return arr;
        var list = new List<AudioClip>();
        var single = Resources.Load<AudioClip>("Audio/" + name);
        if (single != null) list.Add(single);
        for (int i = 1; i <= 10; i++)
        {
            var c = Resources.Load<AudioClip>($"Audio/{name}_{i}");
            if (c == null) break;
            list.Add(c);
        }
        arr = list.ToArray();
        files[name] = arr;
        return arr;
    }

    void Awake()
    {
        I = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        music = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].loop = true;
            music[i].volume = 0f;
            music[i].playOnAwake = false;
        }
        windSrc = gameObject.AddComponent<AudioSource>();
        windSrc.loop = true;
        windSrc.volume = 0f;
        var wind = Files("wind");
        windSrc.clip = wind.Length > 0 ? wind[0] : null;
        if (windSrc.clip != null) windSrc.Play();
        Build();
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.player == null) return;
        var p = gm.player;
        float dt = Time.unscaledDeltaTime;

        // Ветер на метле — громче на скорости.
        float wantWind = p.onBroom && GameState.Playing ? Mathf.Clamp01(p.Velocity.magnitude / 30f) * 0.6f : 0f;
        windSrc.volume = Mathf.MoveTowards(windSrc.volume, wantWind, dt);

        // Музыка: бой, если рядом агрессивный враг; лес — в лесу; иначе замок.
        string track = "music_castle";
        if (GameState.Mode != GameMode.Title && GameState.Mode != GameMode.Victory)
        {
            foreach (var e in Enemy.All)
                if (e != null && !e.IsDead && e.aggro && !e.Charmed && (e.transform.position - p.transform.position).sqrMagnitude < 45f * 45f)
                {
                    track = "music_battle";
                    break;
                }
            if (track != "music_battle" && (p.transform.position - WorldBuilder.ForestCenter).magnitude < 95f) track = "music_forest";
        }
        if (track != musicTrack)
        {
            var arr = Files(track);
            if (arr.Length > 0)
            {
                musicTrack = track;
                musicActive = 1 - musicActive;
                music[musicActive].clip = arr[0];
                music[musicActive].Play();
            }
        }
        float vol = track == "music_battle" ? 0.32f : 0.26f;
        for (int i = 0; i < 2; i++)
        {
            float target = i == musicActive ? vol : 0f;
            music[i].volume = Mathf.MoveTowards(music[i].volume, target, dt * 0.4f);
            if (i != musicActive && music[i].volume <= 0f && music[i].isPlaying) music[i].Stop();
        }
    }

    float N() => (float)(rng.NextDouble() * 2 - 1);
    static float S(float f, float t) => Mathf.Sin(2 * Mathf.PI * f * t);

    void Make(string name, float dur, Gen g)
    {
        int n = Mathf.Max(1, (int)(SR * dur));
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(g(i / (float)SR, i / (float)n), -1f, 1f);
        var c = AudioClip.Create(name, n, 1, SR, false);
        c.SetData(d, 0);
        synth[name] = c;
    }

    float lp;
    float Low(float k) { lp += (N() - lp) * k; return lp; }

    // Запасные синтезированные звуки.
    void Build()
    {
        Make("cast", 0.28f, (t, u) => (S(500 + 900 * u, t) * 0.3f + Low(0.3f) * 0.6f) * (1 - u) * (1 - u));
        Make("heavy", 0.5f, (t, u) => (S(200 + 300 * u, t) * 0.4f + Low(0.2f) * 0.8f) * (1 - u));
        Make("hit", 0.22f, (t, u) => S(160 - 90 * u, t) * Mathf.Exp(-8 * u) * 0.8f + N() * Mathf.Exp(-20 * u) * 0.35f);
        Make("boom", 0.9f, (t, u) => Low(0.06f) * 3.5f * Mathf.Exp(-4 * u) + S(55, t) * Mathf.Exp(-5 * u) * 0.6f);
        Make("shield", 0.35f, (t, u) => (S(330, t) + S(495, t)) * 0.22f * Mathf.Sin(Mathf.PI * u));
        Make("parry", 0.5f, (t, u) => (S(1250, t) + S(1870, t) * 0.7f + S(2600, t) * 0.5f) * Mathf.Exp(-6 * u) * 0.28f);
        Make("pickup", 0.4f, (t, u) => S(u < 0.33f ? 880 : u < 0.66f ? 1108 : 1318, t) * 0.3f * (1 - (u * 3f) % 1f));
        Make("coin", 0.25f, (t, u) => (S(1800, t) + S(2700, t) * 0.5f) * Mathf.Exp(-10 * u) * 0.25f);
        Make("buy", 0.4f, (t, u) => S(u < 0.5f ? 1046 : 1568, t) * 0.3f * (1 - (u * 2f) % 1f));
        Make("chest", 0.5f, (t, u) => Low(0.1f) * 1.5f * Mathf.Exp(-3 * u) + S(120, t) * 0.2f * Mathf.Exp(-6 * u));
        Make("book", 0.3f, (t, u) => Low(0.4f) * 0.8f * Mathf.Sin(Mathf.PI * u));
        Make("freeze", 0.5f, (t, u) => (S(2400, t) * 0.3f + N() * 0.3f) * Mathf.Exp(-6 * u));
        Make("curse", 1.0f, (t, u) => (S(70 + 30 * u, t) * 0.6f + Low(0.05f) * 2f) * Mathf.Exp(-2.5f * u));
        Make("ring", 0.7f, (t, u) => (S(1046, t) + S(1568, t) * 0.6f) * Mathf.Exp(-4 * u) * 0.3f);
        Make("quest", 1.0f, (t, u) =>
        {
            float[] notes = { 523, 659, 784, 1046 };
            int k = Mathf.Min(3, (int)(u * 4));
            return (S(notes[k], t) + S(notes[k] * 2, t) * 0.3f) * 0.25f * (1 - ((u * 4f) % 1f) * 0.7f);
        });
        Make("victory", 1.5f, (t, u) => (S(523, t) + S(659, t) + S(784, t)) * 0.15f * (1 - u));
        Make("dodge", 0.3f, (t, u) => Low(0.15f + 0.4f * u) * 1.6f * Mathf.Sin(Mathf.PI * u));
        Make("hurt", 0.25f, (t, u) => Mathf.Sign(S(180 - 90 * u, t)) * Mathf.Exp(-6 * u) * 0.25f);
        Make("fire", 0.6f, (t, u) => Low(0.2f) * 1.8f * Mathf.Exp(-3 * u));
        Make("broom", 0.7f, (t, u) => Low(0.05f) * 3f * Mathf.Sin(Mathf.PI * u));
        Make("ancient", 1.4f, (t, u) => Low(0.05f) * 3f * Mathf.Exp(-3 * u) + S(220 + 660 * u, t) * 0.25f * Mathf.Sin(Mathf.PI * u));
        Make("potion", 0.5f, (t, u) => S(300 + 500 * u, t) * 0.25f * Mathf.Sin(Mathf.PI * u));
        Make("click", 0.06f, (t, u) => S(1500, t) * (1 - u) * 0.3f);
        Make("step", 0.08f, (t, u) => Low(0.3f) * Mathf.Exp(-12 * u));
        Make("stepgrass", 0.1f, (t, u) => Low(0.5f) * 0.6f * Mathf.Exp(-10 * u));
    }
}

using System.Collections.Generic;
using UnityEngine;

// Весь интерфейс на IMGUI: титульник, HUD, маркер цели, полоски врагов, диалоги, пауза, смерть, победа.
public class HUD : MonoBehaviour
{
    public static HUD I;

    class Floater { public Vector3 pos; public string text; public Color color; public float t; }
    class Toast { public string text; public Color color; public float t; }
    static readonly List<Floater> floaters = new();
    static readonly List<Toast> toasts = new();

    static readonly Color Gold = new(1f, 0.83f, 0.45f);
    static readonly Color Panel = new(0.05f, 0.05f, 0.09f, 0.72f);

    public const string Controls =
        "WASD — движение    Shift — бег    Пробел — прыжок    Ctrl — перекат\n" +
        "ЛКМ — базовое заклинание    1–4 — заклинания    Tab — сменить набор    Q — Протего\n" +
        "F — Древняя магия    H/J/K — зелья    E — взаимодействие    I — инвентарь    V — карта    F5/F9 — сохранить/загрузить    B — метла    T — промотать время    O — журнал    Z — сменить задание    Esc — пауза";

    Texture2D white, fadeLeft;
    GUIStyle sSmall, sMed, sBig, sHuge, sTiny, sMicro;
    int styleH = -1;
    float S;

    public static void Float(Vector3 pos, string text, Color c)
    {
        floaters.Add(new Floater { pos = pos, text = text, color = c, t = Time.unscaledTime });
        if (floaters.Count > 40) floaters.RemoveAt(0);
    }

    public static void ShowToast(string text, Color? c = null)
    {
        toasts.Add(new Toast { text = Gender.Fix(text), color = c ?? Color.white, t = Time.unscaledTime });
        if (toasts.Count > 4) toasts.RemoveAt(0);
    }

    public static void Clear() { floaters.Clear(); toasts.Clear(); }

    void Awake()
    {
        I = this;
        white = Texture2D.whiteTexture;
        fadeLeft = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < 256; i++)
        {
            float k = 1f - i / 255f;
            fadeLeft.SetPixel(i, 0, new Color(0.02f, 0.02f, 0.06f, Mathf.Clamp01(k * 1.3f) * 0.85f));
        }
        fadeLeft.Apply();
    }

    void Update()
    {
        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        if (previewCam != null && GameState.Mode != GameMode.Title) { previewCam.enabled = false; previewLight.enabled = false; }
        switch (GameState.Mode)
        {
            case GameMode.Title:
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) PickGender(false);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) PickGender(true);
                for (int i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha3 + i)) PickDifficulty(i);
                if (enter || Input.GetKeyDown(KeyCode.Space)) StartGame();
                break;
            case GameMode.Play:
                if (Input.GetKeyDown(KeyCode.Escape) && ShopUI.ChangedFrame != Time.frameCount && CastleMap.ChangedFrame != Time.frameCount) GameState.Set(GameMode.Paused);
                break;
            case GameMode.Paused:
                if (Input.GetKeyDown(KeyCode.Escape)) GameState.Set(GameMode.Play);
                break;
            case GameMode.Dialogue:
                if (Dialogue.Choices != null)
                {
                    for (int i = 0; i < Dialogue.Choices.Length; i++)
                        if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) Dialogue.Pick(i);
                }
                else if (enter || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    Dialogue.Advance();
                break;
            case GameMode.Dead:
                if (enter || Input.GetKeyDown(KeyCode.Space)) QuestManager.I.Respawn();
                break;
            case GameMode.Victory:
                if (enter)
                {
                    GameState.Set(GameMode.Play);
                    GameManager.I.camRig.SnapBehind();
                }
                break;
        }
    }

    const int HeroLayer = 30;
    Camera previewCam;
    Light previewLight;
    RenderTexture previewRT;

    // Живое превью героя справа на титульном экране (отдельная камера в текстуру).
    void DrawHeroPreview(Rect r)
    {
        var p = GameManager.I.player;
        if (previewCam == null)
        {
            previewRT = new RenderTexture(420, 620, 24) { antiAliasing = 4 };
            previewCam = new GameObject("HeroPreviewCam").AddComponent<Camera>();
            previewCam.targetTexture = previewRT;
            previewCam.fieldOfView = 30f;
            previewCam.nearClipPlane = 0.1f;
            previewCam.farClipPlane = 50f;
            // Студийный фон: камера видит только слой героя.
            previewCam.cullingMask = 1 << HeroLayer;
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0.07f, 0.08f, 0.14f);
            // Студийный свет только для слоя героя.
            previewLight = new GameObject("HeroPreviewLight").AddComponent<Light>();
            var key = previewLight;
            key.transform.SetParent(previewCam.transform, false);
            key.transform.localPosition = new Vector3(-1.2f, 1.2f, 0.5f);
            key.type = LightType.Point; key.range = 12f; key.intensity = 2.2f;
            key.color = new Color(1f, 0.95f, 0.85f);
            key.cullingMask = 1 << HeroLayer;
        }
        foreach (var tr in p.model.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = HeroLayer;
        previewCam.enabled = true;
        previewLight.enabled = true;
        var t = p.transform;
        previewCam.transform.position = t.position + t.forward * 4.2f + Vector3.up * 1.15f;
        previewCam.transform.LookAt(t.position + Vector3.up * 0.95f);
        Box(new Rect(r.x - 4 * S, r.y - 4 * S, r.width + 8 * S, r.height + 8 * S), Gold);
        GUI.DrawTexture(r, previewRT, ScaleMode.ScaleAndCrop, false);
        Text(new Rect(r.x, r.yMax - 50 * S, r.width, 40 * S), p.female ? "Студентка пятого курса" : "Студент пятого курса", sSmall, Color.white, TextAnchor.MiddleCenter);
    }

    // Четыре кнопки сложности; под ними — что меняется.
    void DifficultyRow(float x, float y, bool keys)
    {
        for (int i = 0; i < Difficulty.Levels.Length; i++)
        {
            var lv = Difficulty.Levels[i];
            var b = new Rect(x + i * 190 * S, y, 175 * S, 52 * S);
            bool on = Difficulty.Index == i;
            Box(b, on ? new Color(lv.color.r * 0.45f, lv.color.g * 0.45f, lv.color.b * 0.45f, 0.95f) : Panel);
            if (on) Box(new Rect(b.x, b.yMax - 4 * S, b.width, 4 * S), lv.color);
            Text(b, lv.name + (keys ? $" [{i + 3}]" : ""), sSmall, on ? Color.white : Color.Lerp(lv.color, Color.white, 0.45f), TextAnchor.MiddleCenter);
            if (GUI.Button(b, GUIContent.none, GUIStyle.none)) PickDifficulty(i);
        }
        Text(new Rect(x, y + 56 * S, 760 * S, 30 * S), Difficulty.Cur.desc, sTiny, Difficulty.Cur.color);
    }

    public static void PickDifficulty(int i)
    {
        if (Difficulty.Index == i) return;
        Difficulty.Set(i);
        SFX.Play("click", 0.5f);
        if (GameState.Mode != GameMode.Title) ShowToast("Сложность: " + Difficulty.Cur.name, Difficulty.Cur.color);
    }

    public static void PickGender(bool female)
    {
        var p = GameManager.I.player;
        if (p.female == female) return;
        p.SetGender(female);
        SFX.Voice("Incendio", 0.7f);
    }

    public void StartGame()
    {
        GameState.Set(GameMode.Play);
        GameManager.I.camRig.SnapBehind();
        ShowToast("Добро пожаловать в Хогвартс!", Gold);
    }

    // ---------------- helpers ----------------
    void EnsureStyles()
    {
        if (styleH == Screen.height && sMed != null) return;
        styleH = Screen.height;
        S = Mathf.Max(0.5f, Screen.height / 1080f);
        sTiny = Mk(17, FontStyle.Normal);
        sMicro = Mk(14, FontStyle.Normal);
        sMicro.wordWrap = false;
        sSmall = Mk(21, FontStyle.Normal);
        sMed = Mk(27, FontStyle.Bold);
        sBig = Mk(44, FontStyle.Bold);
        sHuge = Mk(92, FontStyle.Bold);
    }

    GUIStyle Mk(int size, FontStyle fs)
    {
        var s = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * S),
            fontStyle = fs,
            wordWrap = true,
            richText = false,
            alignment = TextAnchor.UpperLeft,
        };
        return s;
    }

    void Box(Rect r, Color c)
    {
        var o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = o;
    }

    void Text(Rect r, string s, GUIStyle st, Color c, TextAnchor a = TextAnchor.UpperLeft)
    {
        st.alignment = a;
        float off = Mathf.Max(1f, 2f * S);
        st.normal.textColor = new Color(0, 0, 0, c.a * 0.85f);
        GUI.Label(new Rect(r.x + off, r.y + off, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    void Bar(Rect r, float frac, Color fill, Color back)
    {
        Box(r, back);
        Box(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), fill);
    }

    bool ToScreen(Camera cam, Vector3 world, out Vector2 p)
    {
        var sp = cam.WorldToScreenPoint(world);
        p = new Vector2(sp.x, Screen.height - sp.y);
        return sp.z > 0;
    }

    // ---------------- OnGUI ----------------
    void OnGUI()
    {
        EnsureStyles();
        var cam = Camera.main;
        var gm = GameManager.I;
        if (cam == null || gm == null || gm.player == null) return;
        GUI.depth = 0;

        if (GameState.Mode == GameMode.Map || GameState.Mode == GameMode.Service || GameState.Mode == GameMode.Journal) return;
        if (GameState.Mode == GameMode.Title) { DrawTitle(); return; }
        if (GameState.Mode == GameMode.Victory) { DrawVictory(); return; }

        DrawWorldUI(cam, gm.player);
        DrawPlayerHUD(gm.player);
        DrawClock();
        DrawQuest(cam);
        DrawToasts();
        DrawHurt(gm.player);

        switch (GameState.Mode)
        {
            case GameMode.Dialogue: DrawDialogue(); break;
            case GameMode.Paused: DrawPause(); break;
            case GameMode.Dead: DrawDead(); break;
        }
    }

    void DrawTitle()
    {
        float W = Screen.width, H = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, W * 0.7f, H), fadeLeft);
        Text(new Rect(90 * S, H * 0.2f, W, 120 * S), "HOGWARTS LEGACY", sHuge, Gold);
        Text(new Rect(96 * S, H * 0.2f + 110 * S, W, 60 * S), "мини-версия: метла, заклинания, Ранрок и Руквуд", sBig, new Color(0.85f, 0.85f, 0.95f));
        float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f);
        Text(new Rect(96 * S, H * 0.2f + 390 * S, W, 50 * S), "ENTER — новая игра · F9 — загрузить прогресс", sMed, new Color(1, 1, 1, pulse));
        Text(new Rect(96 * S, H * 0.2f + 270 * S, 300 * S, 40 * S), "Сложность:", sSmall, new Color(0.85f, 0.85f, 0.95f));
        DifficultyRow(250 * S, H * 0.2f + 260 * S, true);
        // Выбор героя: мальчик или девочка.
        bool fem = GameManager.I.player.female;
        Text(new Rect(96 * S, H * 0.2f + 190 * S, 300 * S, 40 * S), "Твой герой:", sSmall, new Color(0.85f, 0.85f, 0.95f));
        string[] opts = { "Мальчик [1]", "Девочка [2]" };
        for (int i = 0; i < 2; i++)
        {
            var b = new Rect(250 * S + i * 250 * S, H * 0.2f + 180 * S, 230 * S, 60 * S);
            bool on = fem == (i == 1);
            Box(b, on ? new Color(0.55f, 0.42f, 0.15f, 0.9f) : Panel);
            if (on) Box(new Rect(b.x, b.yMax - 4 * S, b.width, 4 * S), Gold);
            Text(b, opts[i], sMed, on ? Color.white : new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter);
            if (GUI.Button(b, GUIContent.none, GUIStyle.none)) PickGender(i == 1);
        }
        DrawHeroPreview(new Rect(W - 520 * S, H * 0.12f, 420 * S, 620 * S));
        var r = new Rect(90 * S, H - 215 * S, 1000 * S, 160 * S);
        Box(r, Panel);
        Text(new Rect(r.x + 20 * S, r.y + 15 * S, r.width - 40 * S, r.height), Controls, sSmall, new Color(0.9f, 0.9f, 0.9f));
    }

    void DrawWorldUI(Camera cam, PlayerController pc)
    {
        var q = QuestManager.I;
        // NPC: имена и восклицательный знак над квестовым.
        foreach (var n in NPC.All)
        {
            float d = (n.transform.position - pc.transform.position).magnitude;
            bool quest = q != null && n.id == q.talkTarget;
            string side = SideQuests.I != null ? SideQuests.I.Indicator(n.id) : null;
            if (d > (quest || side != null ? 60f : 14f)) continue;
            if (!ToScreen(cam, n.transform.position + Vector3.up * 2.25f, out var p)) continue;
            if (d < 14f) Text(new Rect(p.x - 150 * S, p.y - 14 * S, 300 * S, 30 * S), n.displayName, sTiny, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleCenter);
            float bounce = Mathf.Sin(Time.unscaledTime * 4f) * 5f * S;
            if (quest)
                Text(new Rect(p.x - 30 * S, p.y - 70 * S + bounce, 60 * S, 60 * S), "!", sBig, Gold, TextAnchor.MiddleCenter);
            else if (side != null)
                Text(new Rect(p.x - 30 * S, p.y - 70 * S + bounce, 60 * S, 60 * S), side, sBig,
                    side == "!" ? new Color(0.5f, 1f, 0.6f) : new Color(0.5f, 0.8f, 1f), TextAnchor.MiddleCenter);
        }

        // Полоски здоровья врагов.
        foreach (var t in Targetable.All)
        {
            if (!t.Alive || t.isBoss) continue;
            float d = (t.transform.position - cam.transform.position).magnitude;
            if (d > 45f) continue;
            var e = t.GetComponent<Enemy>();
            if (e != null && !e.aggro && t.health.Frac >= 1f) continue;
            if (!ToScreen(cam, t.AimPoint + Vector3.up * 0.95f, out var p)) continue;
            float w = 90 * S;
            Text(new Rect(p.x - 120 * S, p.y - 32 * S, 240 * S, 26 * S), t.displayName, sTiny, new Color(1f, 0.85f, 0.8f), TextAnchor.MiddleCenter);
            Bar(new Rect(p.x - w / 2, p.y - 6 * S, w, 8 * S), t.health.Frac, new Color(0.85f, 0.15f, 0.12f), new Color(0, 0, 0, 0.6f));
            if (e != null && e.Telegraphing)
                Text(new Rect(p.x - 30 * S, p.y - 80 * S, 60 * S, 50 * S), "!", sBig, e.TelegraphRed ? new Color(1f, 0.2f, 0.15f) : new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            string status = e == null ? null : e.Charmed ? "под Империо" : e.frozenT > 0f ? "заморожен" : e.crucioT > 0f ? "Круцио" : e.disarmT > 0f ? "обезоружен" : e.slowT > 0f ? "замедлен" : null;
            if (status != null)
                Text(new Rect(p.x - 80 * S, p.y + 4 * S, 160 * S, 24 * S), status, sTiny, e.Charmed ? new Color(0.95f, 0.95f, 0.5f) : new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleCenter);
            else if (e != null && e.levitateT > 0f)
                Text(new Rect(p.x - 80 * S, p.y + 4 * S, 160 * S, 24 * S), "левитация", sTiny, new Color(1f, 0.95f, 0.5f), TextAnchor.MiddleCenter);
            else if (e != null && e.stunT > 0f)
                Text(new Rect(p.x - 80 * S, p.y + 4 * S, 160 * S, 24 * S), "оглушён", sTiny, new Color(0.6f, 0.85f, 1f), TextAnchor.MiddleCenter);
        }

        // Рамка цели.
        var tg = pc.caster.target;
        if (tg != null && GameState.Playing && ToScreen(cam, tg.AimPoint, out var tp))
        {
            float s = 26 * S, l = 10 * S, th = 3 * S;
            var c = new Color(1f, 0.9f, 0.5f, 0.9f);
            Box(new Rect(tp.x - s, tp.y - s, l, th), c); Box(new Rect(tp.x - s, tp.y - s, th, l), c);
            Box(new Rect(tp.x + s - l, tp.y - s, l, th), c); Box(new Rect(tp.x + s - th, tp.y - s, th, l), c);
            Box(new Rect(tp.x - s, tp.y + s - th, l, th), c); Box(new Rect(tp.x - s, tp.y + s - l, th, l), c);
            Box(new Rect(tp.x + s - l, tp.y + s - th, l, th), c); Box(new Rect(tp.x + s - th, tp.y + s - l, th, l), c);
        }

        // Чутьё опасности над головой.
        if ((ThreatSense.Red || ThreatSense.Yellow) && ToScreen(cam, pc.transform.position + Vector3.up * 2.3f, out var hp))
        {
            var c = ThreatSense.Red ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.85f, 0.15f);
            c.a = 0.7f + 0.3f * Mathf.Sin(Time.time * 20f);
            Text(new Rect(hp.x - 50 * S, hp.y - 60 * S, 100 * S, 60 * S), "!!", sBig, c, TextAnchor.MiddleCenter);
        }

        // Всплывающий урон.
        for (int i = floaters.Count - 1; i >= 0; i--)
        {
            var f = floaters[i];
            float age = Time.unscaledTime - f.t;
            if (age > 1.1f) { floaters.RemoveAt(i); continue; }
            if (!ToScreen(cam, f.pos + Vector3.up * age * 1.2f, out var fp)) continue;
            var c = f.color;
            c.a = 1f - age / 1.1f;
            Text(new Rect(fp.x - 100 * S, fp.y - 20 * S, 200 * S, 40 * S), f.text, sMed, c, TextAnchor.MiddleCenter);
        }

        // Прицел и счётчик комбо базовой атаки.
        if (GameState.Playing && !pc.onBroom)
        {
            Box(new Rect(Screen.width / 2f - 2 * S, Screen.height / 2f - 2 * S, 4 * S, 4 * S), new Color(1, 1, 1, 0.8f));
            int combo = pc.caster.Combo;
            if (combo > 0)
            {
                int k = (combo - 1) % 3 + 1;
                for (int i = 0; i < 3; i++)
                    Box(new Rect(Screen.width / 2f - 16 * S + i * 12 * S, Screen.height / 2f + 14 * S, 8 * S, 4 * S),
                        i < k ? (k == 3 ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.9f, 0.5f)) : new Color(1, 1, 1, 0.25f));
            }
        }
    }

    void DrawPlayerHUD(PlayerController pc)
    {
        float H = Screen.height, W = Screen.width;
        var q = QuestManager.I;

        // Панель заклинаний — считаем заранее, чтобы левый блок не залезал на неё.
        var caster = pc.caster;
        int n = 7;
        float sw = 92 * S, gap = 8 * S, total = n * sw + (n - 1) * gap;
        float sx = (W - total) / 2f, sy = H - sw - 30 * S;

        // Левый нижний блок, снизу вверх: древняя магия → здоровье → зелья → галлеоны → факультет → баффы.
        float x = 30 * S;
        float colW = Mathf.Min(460 * S, sx - x - 16 * S);
        float y = H - 16 * S;
        var inv = pc.inventory;
        if (pc.ancientUnlocked)
        {
            bool full = pc.ancient >= 100f;
            var c = full ? Color.Lerp(new Color(0.3f, 0.7f, 1f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 6f)) : new Color(0.3f, 0.7f, 1f);
            y -= 24 * S;
            Text(new Rect(x, y, colW, 24 * S), full ? "Древняя магия готова — [F]" : "Древняя магия", sTiny, c);
            y -= 14 * S;
            Bar(new Rect(x, y, colW, 11 * S), pc.ancient / 100f, c, new Color(0, 0, 0, 0.6f));
            y -= 8 * S;
        }
        y -= 24 * S;
        Bar(new Rect(x, y, colW, 24 * S), pc.health.Frac, new Color(0.75f, 0.12f, 0.12f), new Color(0, 0, 0, 0.6f));
        Text(new Rect(x, y, colW, 24 * S), $"{Mathf.CeilToInt(pc.health.current)} / {pc.health.max}", sTiny, Color.white, TextAnchor.MiddleCenter);
        var prog = Progression.I;
        if (prog != null)
        {
            var xc = new Color(0.55f, 0.8f, 1f);
            y -= 11 * S;
            Bar(new Rect(x, y, colW, 7 * S), prog.Frac, xc, new Color(0, 0, 0, 0.6f));
            y -= 24 * S;
            Text(new Rect(x, y, colW, 24 * S), prog.level >= Progression.MaxLevel ? $"Уровень {prog.level} (максимум)" : $"Уровень {prog.level}   опыт {prog.xp}/{prog.XpToNext}", sTiny, xc);
        }

        var sb = new System.Text.StringBuilder();
        foreach (var (pid, key) in Inventory.PotionKeys)
        {
            int cnt = inv.potions[pid];
            if (cnt <= 0 && pid != "wiggenweld") continue;
            if (sb.Length > 0) sb.Append("   ");
            sb.Append(ItemDB.Get(pid).name.Replace("Зелье ", "").Replace("зелье", "").Trim()).Append(' ').Append(cnt).Append(" [").Append(key).Append(']');
        }
        string potionText = sb.ToString();
        float potH = sTiny.CalcHeight(new GUIContent(potionText), colW);
        y -= potH + 4 * S;
        Text(new Rect(x, y, colW, potH), potionText, sTiny, new Color(0.6f, 1f, 0.6f));

        string gold = $"Галлеоны: {inv.galleons}" + (pc.hasBroom ? "   Метла [B]" : "") + "   Инвентарь [I]";
        float goldH = sSmall.CalcHeight(new GUIContent(gold), colW);
        y -= goldH + 2 * S;
        Text(new Rect(x, y, colW, goldH), gold, sSmall, new Color(1f, 0.85f, 0.35f));
        y -= 30 * S;
        Text(new Rect(x, y, colW, 30 * S), q != null && q.house >= 0 ? Houses.Names[q.house] : "Без факультета", sSmall,
            q != null && q.house >= 0 ? Color.Lerp(Houses.Main[q.house], Color.white, 0.35f) : new Color(0.8f, 0.8f, 0.8f));

        string buffs = (inv.maximaT > 0 ? $"Максима {inv.maximaT:0}с  " : "") + (inv.edurusT > 0 ? $"Эдурус {inv.edurusT:0}с  " : "")
            + (inv.focusT > 0 ? $"Фокус {inv.focusT:0}с  " : "") + (inv.invisT > 0 ? $"Невидимость {inv.invisT:0}с  " : "")
            + (inv.regenT > 0 ? $"Рябина {inv.regenT:0}с  " : "") + (inv.felixT > 0 ? $"Феликс {inv.felixT:0}с" : "");
        if (buffs.Length > 0)
        {
            float bh = sTiny.CalcHeight(new GUIContent(buffs), colW);
            y -= bh + 2 * S;
            Text(new Rect(x, y, colW, bh), buffs, sTiny, new Color(1f, 0.7f, 0.5f));
        }
        DrawSlot(new Rect(sx, sy, sw, sw), "ЛКМ", "Базовое", SpellCaster.BasicColor, true, caster.basicCd, 0.38f);
        for (int i = 0; i < 4; i++)
        {
            var s = caster.PageSlot(i);
            var r = new Rect(sx + (i + 1) * (sw + gap), sy, sw, sw);
            if (s != null) DrawSlot(r, (i + 1).ToString(), s.hud, s.color, true, s.cd, s.cooldown);
            else DrawSlot(r, (i + 1).ToString(), "", Color.gray, false, 0, 1);
        }
        if (caster.Pages > 1)
            Text(new Rect(sx + sw + gap, sy - 30 * S, 4 * sw + 3 * gap, 28 * S), $"Набор {caster.page + 1}/{caster.Pages}  [Tab]", sTiny, Gold, TextAnchor.MiddleCenter);
        var prot = new Rect(sx + 5 * (sw + gap), sy, sw, sw);
        DrawSlot(prot, "Q", "Протего", new Color(0.45f, 0.75f, 1f), caster.protegoLearned, 0, 1);
        if (caster.ShieldActive) Box(new Rect(prot.x, prot.y + prot.height - 5 * S, prot.width, 5 * S), new Color(0.5f, 0.85f, 1f));
        DrawSlot(new Rect(sx + 6 * (sw + gap), sy, sw, sw), "F", "Древняя", new Color(0.4f, 0.85f, 1f), pc.ancientUnlocked,
            pc.ancientUnlocked ? (100f - pc.ancient) / 100f : 0, 1f, pc.ancientUnlocked && pc.ancient < 100f);

        // Подсказка "поговорить".
        if (pc.nearbyNpc != null && GameState.Playing)
            Text(new Rect(0, H * 0.64f, W, 40 * S), (pc.nearbyNpc.shopId != null && pc.nearbyNpc.id != q.talkTarget ? "[E] Торговать — " : "[E] Говорить — ") + pc.nearbyNpc.displayName, sMed, Gold, TextAnchor.MiddleCenter);
        else if (pc.nearbyChest != null && GameState.Playing)
            Text(new Rect(0, H * 0.64f, W, 40 * S), "[E] Открыть сундук", sMed, Gold, TextAnchor.MiddleCenter);

        // Шкала здоровья босса.
        foreach (var t in Targetable.All)
        {
            if (!t.isBoss || !t.Alive) continue;
            float bw = 760 * S;
            var r = new Rect((W - bw) / 2f, 60 * S, bw, 22 * S);
            Text(new Rect(0, 18 * S, W, 40 * S), t.displayName.ToUpper(), sMed, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleCenter);
            Bar(r, t.health.Frac, new Color(0.8f, 0.15f, 0.1f), new Color(0, 0, 0, 0.7f));
        }
    }

    void DrawClock()
    {
        if (DayNight.I == null) return;
        var dn = DayNight.I;
        var c = dn.IsNight ? new Color(0.7f, 0.8f, 1f) : new Color(1f, 0.92f, 0.7f);
        Text(new Rect(16, 262, 420, 26), dn.ClockText() + "   [T] ждать", sTiny, c);
    }

    void DrawSlot(Rect r, string key, string name, Color color, bool learned, float cd, float maxCd, bool chargeMode = false)
    {
        Box(r, new Color(0.04f, 0.04f, 0.08f, 0.78f));
        var c = learned ? color : new Color(0.35f, 0.35f, 0.35f);
        Box(new Rect(r.x, r.y, r.width, 5 * S), c);
        Text(new Rect(r.x + 6 * S, r.y + 6 * S, r.width, 26 * S), key, sTiny, learned ? Color.white : new Color(0.5f, 0.5f, 0.5f));
        Text(new Rect(r.x - 12 * S, r.y + r.height * 0.45f, r.width + 24 * S, 30 * S), learned ? name : "—", name.Length > 9 ? sMicro : sTiny, c, TextAnchor.MiddleCenter);
        if (learned && cd > 0f)
        {
            float k = Mathf.Clamp01(cd / maxCd);
            Box(new Rect(r.x, r.y + r.height * (1 - k), r.width, r.height * k), new Color(0, 0, 0, 0.6f));
            if (!chargeMode && maxCd > 1f)
                Text(new Rect(r.x, r.y + r.height * 0.62f, r.width, 30 * S), cd.ToString("0.0"), sSmall, Color.white, TextAnchor.MiddleCenter);
        }
    }

    void DrawQuest(Camera cam)
    {
        var q = QuestManager.I;
        if (q == null) return;
        float W = Screen.width, H = Screen.height;
        float w = 470 * S;
        var r = new Rect(W - w - 30 * S, 30 * S, w, 150 * S);
        float objH = sSmall.CalcHeight(new GUIContent(q.objective), w - 30 * S);
        r.height = 60 * S + objH + (string.IsNullOrEmpty(q.progress) ? 0 : 30 * S);
        Box(r, Panel);
        Box(new Rect(r.x, r.y, 4 * S, r.height), Gold);
        Text(new Rect(r.x + 16 * S, r.y + 10 * S, w - 30 * S, 34 * S), q.title, sMed, Gold);
        Text(new Rect(r.x + 16 * S, r.y + 46 * S, w - 30 * S, objH), q.objective, sSmall, Color.white);
        if (!string.IsNullOrEmpty(q.progress))
            Text(new Rect(r.x + 16 * S, r.y + 50 * S + objH, w - 30 * S, 28 * S), q.progress, sSmall, new Color(0.7f, 0.9f, 1f));

        // Поручения не мешают сюжету: показываем только отслеживаемое и счётчик остальных; [Z] переключает фокус.
        var sq = SideQuests.I;
        bool mainFocus = sq == null || sq.tracked == null;
        if (!mainFocus) Box(new Rect(r.x, r.y, w, r.height), new Color(0, 0, 0, 0.35f));
        if (sq != null)
        {
            float y = r.yMax + 8 * S;
            var open = sq.Open;
            var s = sq.tracked;
            if (s != null)
            {
                var sr = new Rect(r.x, y, w, 50 * S);
                Box(sr, new Color(0.04f, 0.06f, 0.1f, 0.75f));
                Box(new Rect(sr.x, sr.y, 4 * S, sr.height), new Color(0.5f, 0.8f, 1f));
                Text(new Rect(sr.x + 14 * S, sr.y + 3 * S, w - 20 * S, 24 * S), "Поручение: " + s.title, sTiny, s.state == SQState.Ready ? new Color(0.5f, 1f, 0.6f) : new Color(0.75f, 0.88f, 1f));
                Text(new Rect(sr.x + 14 * S, sr.y + 25 * S, w - 20 * S, 24 * S), s.desc + "  " + s.Progress, sTiny, new Color(0.8f, 0.8f, 0.85f));
                y += 56 * S;
            }
            if (open.Count > 0)
                Text(new Rect(r.x + 4 * S, y, w, 26 * S), (mainFocus ? $"Поручений: {open.Count}" : "Сюжет — в фоне") + "  ·  [Z] сменить задание  ·  [O] журнал", sTiny, new Color(0.7f, 0.75f, 0.85f));
            var rt = sq.RaceTimer();
            if (rt != null) Text(new Rect(0, 150 * S, W, 40 * S), rt, sBig, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            if (!mainFocus)
            {
                var tm = sq.TrackedMarker();
                if (tm.HasValue && GameState.Playing) DrawMarker(cam, tm.Value, new Color(0.5f, 0.8f, 1f));
                return;
            }
        }

        if (!q.hasMarker || !GameState.Playing) return;
        DrawMarker(cam, q.markerPos, Gold);
    }

    void DrawMarker(Camera cam, Vector3 target, Color col)
    {
        float W = Screen.width, H = Screen.height;
        var gm = GameManager.I;
        var world = target + Vector3.up * 2.5f;
        var sp = cam.WorldToScreenPoint(world);
        var p = new Vector2(sp.x, H - sp.y);
        bool behind = sp.z < 0f;
        if (behind) p = new Vector2(W - p.x, H - p.y);
        float m = 50 * S;
        var c = new Vector2(W / 2f, H / 2f);
        if (behind || p.x < m || p.x > W - m || p.y < m || p.y > H - m)
        {
            var d = p - c;
            if (d.sqrMagnitude < 1f) d = Vector2.up;
            float k = Mathf.Min((W / 2f - m) / Mathf.Max(1e-3f, Mathf.Abs(d.x)), (H / 2f - m) / Mathf.Max(1e-3f, Mathf.Abs(d.y)));
            p = c + d * k;
        }
        float s = 18 * S;
        var mat = GUI.matrix;
        GUIUtility.RotateAroundPivot(45, p);
        Box(new Rect(p.x - s / 2 - 2 * S, p.y - s / 2 - 2 * S, s + 4 * S, s + 4 * S), new Color(0, 0, 0, 0.6f));
        Box(new Rect(p.x - s / 2, p.y - s / 2, s, s), col);
        GUI.matrix = mat;
        float dist = (target - gm.player.transform.position).magnitude;
        Text(new Rect(p.x - 60 * S, p.y + 14 * S, 120 * S, 26 * S), Mathf.RoundToInt(dist) + " м", sTiny, col, TextAnchor.MiddleCenter);
    }

    void DrawToasts()
    {
        float y = 200 * S;
        for (int i = 0; i < toasts.Count; i++)
        {
            var t = toasts[i];
            float age = Time.unscaledTime - t.t;
            if (age > 4f) { toasts.RemoveAt(i); i--; continue; }
            var c = t.color;
            c.a = Mathf.Clamp01(4f - age) * Mathf.Clamp01(age * 5f);
            float h = Mathf.Max(44 * S, sMed.CalcHeight(new GUIContent(t.text), Screen.width * 0.38f) + 8 * S);
            var r = new Rect(Screen.width * 0.3f, y, Screen.width * 0.38f, h);
            Box(r, new Color(0, 0, 0, 0.45f * c.a));
            Text(r, t.text, sMed, c, TextAnchor.MiddleCenter);
            y += h + 6 * S;
        }
    }

    void DrawHurt(PlayerController pc)
    {
        float cold = Mathf.Clamp01(1f - (Time.time - Enemy.ColdTime) / 0.8f) * 0.28f;
        if (cold > 0.001f)
        {
            float W0 = Screen.width, H0 = Screen.height, b0 = 110 * S;
            var cc = new Color(0.55f, 0.75f, 1f, cold);
            Box(new Rect(0, 0, W0, b0), cc); Box(new Rect(0, H0 - b0, W0, b0), cc);
            Box(new Rect(0, 0, b0, H0), cc); Box(new Rect(W0 - b0, 0, b0, H0), cc);
        }
        float a = Mathf.Clamp01(1f - (Time.time - pc.lastHurt) / 0.4f) * 0.3f;
        if (pc.health.Frac < 0.25f && !pc.health.Dead) a = Mathf.Max(a, 0.12f + 0.08f * Mathf.Sin(Time.time * 5f));
        if (a <= 0.001f) return;
        float W = Screen.width, H = Screen.height, b = 70 * S;
        var c = new Color(0.8f, 0f, 0f, a);
        Box(new Rect(0, 0, W, b), c); Box(new Rect(0, H - b, W, b), c);
        Box(new Rect(0, 0, b, H), c); Box(new Rect(W - b, 0, b, H), c);
    }

    void DrawDialogue()
    {
        float W = Screen.width, H = Screen.height;
        var line = Dialogue.Current;
        var choices = Dialogue.Choices;
        float h = choices != null ? 210 * S + choices.Length * 50 * S : 220 * S;
        var r = new Rect(W * 0.14f, H - h - 40 * S, W * 0.72f, h);
        Box(r, new Color(0.03f, 0.03f, 0.07f, 0.88f));
        Box(new Rect(r.x, r.y, r.width, 4 * S), Gold);
        Text(new Rect(r.x + 30 * S, r.y + 18 * S, r.width - 60 * S, 40 * S), line.who, sMed, Gold);
        Text(new Rect(r.x + 30 * S, r.y + 62 * S, r.width - 60 * S, 130 * S), line.text, sSmall, Color.white);
        if (choices == null)
        {
            Text(new Rect(r.x, r.y + r.height - 40 * S, r.width - 30 * S, 30 * S), "[Пробел] далее", sTiny, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleRight);
            return;
        }
        for (int i = 0; i < choices.Length; i++)
        {
            var cr = new Rect(r.x + 30 * S, r.y + 150 * S + i * 50 * S, 520 * S, 44 * S);
            bool hover = cr.Contains(Event.current.mousePosition);
            Box(cr, hover ? new Color(1f, 1f, 1f, 0.15f) : new Color(1f, 1f, 1f, 0.05f));
            var col = i < Houses.Main.Length ? Color.Lerp(Houses.Main[i], Color.white, 0.45f) : Color.white;
            Text(new Rect(cr.x + 14 * S, cr.y, cr.width, cr.height), $"[{i + 1}]  {choices[i]}", sMed, col, TextAnchor.MiddleLeft);
            if (Event.current.type == EventType.MouseDown && hover) { Dialogue.Pick(i); Event.current.Use(); return; }
        }
    }

    void DrawPause()
    {
        float W = Screen.width, H = Screen.height;
        Box(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.6f));
        Text(new Rect(0, H * 0.18f, W, 100 * S), "ПАУЗА", sHuge, Gold, TextAnchor.MiddleCenter);
        Text(new Rect(W * 0.2f, H * 0.36f, W * 0.6f, 160 * S), Controls, sSmall, Color.white, TextAnchor.UpperCenter);
        var bw = 360 * S;
        Text(new Rect(0, H * 0.47f, W, 36 * S), "Сложность (можно менять в любой момент):", sSmall, new Color(0.85f, 0.85f, 0.95f), TextAnchor.MiddleCenter);
        DifficultyRow((W - 4 * 190 * S) / 2f + 7 * S, H * 0.47f + 38 * S, false);
        if (GUI.Button(new Rect((W - bw) / 2, H * 0.62f, bw, 56 * S), "Продолжить (Esc)")) GameState.Set(GameMode.Play);
        if (GUI.Button(new Rect((W - bw) / 2, H * 0.62f + 70 * S, bw, 56 * S), "Выйти из игры"))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void DrawDead()
    {
        float W = Screen.width, H = Screen.height;
        Box(new Rect(0, 0, W, H), new Color(0.25f, 0f, 0f, 0.6f));
        Text(new Rect(0, H * 0.35f, W, 110 * S), "ТЫ ПОВЕРЖЕН", sHuge, new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter);
        Text(new Rect(0, H * 0.35f + 120 * S, W, 50 * S), "[Enter] — вернуться к последней точке", sMed, Color.white, TextAnchor.MiddleCenter);
    }

    void DrawVictory()
    {
        var q = QuestManager.I;
        float W = Screen.width, H = Screen.height;
        Box(new Rect(0, 0, W, H), new Color(0.02f, 0.02f, 0.06f, 0.55f));
        Text(new Rect(0, H * 0.2f, W, 120 * S), "ПОБЕДА!", sHuge, Gold, TextAnchor.MiddleCenter);
        Text(new Rect(0, H * 0.2f + 120 * S, W, 60 * S), "Разлом закрыт. Хогвартс спасён.", sBig, Color.white, TextAnchor.MiddleCenter);
        int sec = Mathf.RoundToInt(q.playTime);
        string stats = $"Факультет: {(q.house >= 0 ? Houses.Names[q.house] : "—")}\n" +
                       $"Время: {sec / 60}:{sec % 60:00}\n" +
                       $"Повержено врагов: {q.enemiesDefeated}\n" +
                       $"Заработано галлеонов: {GameManager.I.player.inventory.earnedTotal}\n" +
                       $"Страниц путеводителя: {q.pages}/{QuestManager.TotalPages}";
        Text(new Rect(0, H * 0.45f, W, 200 * S), stats, sMed, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperCenter);
        Text(new Rect(0, H * 0.78f, W, 50 * S), "[Enter] — свободная прогулка по миру", sMed, new Color(1, 1, 1, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f)), TextAnchor.MiddleCenter);
    }
}

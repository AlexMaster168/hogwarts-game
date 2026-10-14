using System.Collections.Generic;
using UnityEngine;

// Командный квиддич 4 на 4: герой — охотник своего факультета, с ним два охотника и вратарь.
// Квоффл забивают в три кольца соперника (10 очков), бладжер сбивает игроков и выбивает мяч.
public class QuidditchMatch : MonoBehaviour
{
    public class Flyer
    {
        public Transform t;
        public int team;
        public bool keeper, isPlayer;
        public Vector3 vel;
        public float stun, grabCd;
        public string name;
    }

    public const float Duration = 150f;
    public static Vector3 C => WorldBuilder.PitchCenter;
    public readonly List<Flyer> flyers = new();
    public Flyer player, holder;
    public Transform quaffle, bludger;
    public int[] score = new int[2];
    public int[] house = new int[2];
    public float timeLeft = Duration;
    Vector3 ballVel, bludgerVel;
    Flyer bludgerTarget, lastThrower;
    float bludgerRetarget, tackleT;
    Vector3 attackHoop;
    bool finished;

    // Кольца: команда 0 атакует восточные (x = +27), команда 1 — западные.
    public static Vector3 Hoop(int side, int k) => C + new Vector3(side * 27f, k == 0 ? 18f : 14f, k * 8f);
    static int AttackSide(int team) => team == 0 ? 1 : -1;

    public string Status
    {
        get
        {
            int m = Mathf.CeilToInt(timeLeft);
            string hold = holder == null ? "мяч свободен" : holder.isPlayer ? "мяч у тебя, ЛКМ" : holder.team == 0 ? "мяч у наших" : "мяч у соперника";
            return $"{Houses.Names[house[0]]} {score[0]}:{score[1]} {Houses.Names[house[1]]} · {m / 60}:{m % 60:00} · {hold}";
        }
    }

    void Awake()
    {
        var q = QuestManager.I;
        house[0] = q != null && q.house >= 0 && q.house < 4 ? q.house : 0;
        house[1] = house[0] == 1 ? 0 : 1;
        var p = GameManager.I.player;
        player = new Flyer { t = p.transform, team = 0, isPlayer = true, name = "Ты" };
        flyers.Add(player);
        string[] mates = { "Охотник", "Охотница", "Вратарь" };
        for (int team = 0; team < 2; team++)
            for (int i = 0; i < 4; i++)
            {
                if (team == 0 && i == 0) continue;
                bool keeper = i == 3;
                var pos = keeper ? Hoop(-AttackSide(team), 0) + new Vector3(AttackSide(team) * 3f, -4f, 0) : C + new Vector3(-AttackSide(team) * 10f, 10f, (i - 1) * 7f);
                flyers.Add(new Flyer { t = MakeFlyer(pos, house[team], keeper), team = team, keeper = keeper, name = team == 0 ? mates[Mathf.Min(i, 2)] : "Соперник" });
            }
        quaffle = new GameObject("Quaffle").transform;
        quaffle.SetParent(WorldBuilder.Dynamic, false);
        quaffle.position = C + Vector3.up * 12f;
        Prims.Sphere(quaffle, Vector3.zero, 0.55f, MatLib.Solid(new Color(0.7f, 0.12f, 0.1f), 0.3f));
        var ql = quaffle.gameObject.AddComponent<Light>();
        ql.type = LightType.Point; ql.color = new Color(1f, 0.4f, 0.3f); ql.range = 5f; ql.intensity = 1.5f;
        bludger = new GameObject("Bludger").transform;
        bludger.SetParent(WorldBuilder.Dynamic, false);
        bludger.position = C + new Vector3(0, 6f, 14f);
        Prims.Sphere(bludger, Vector3.zero, 0.5f, MatLib.Solid(new Color(0.1f, 0.1f, 0.1f), 0.8f));
        var tr = bludger.gameObject.AddComponent<TrailRenderer>();
        tr.sharedMaterial = MatLib.Additive(Color.white); tr.time = 0.3f; tr.startWidth = 0.35f; tr.endWidth = 0f;
        tr.startColor = new Color(0.3f, 0.3f, 0.3f, 0.7f); tr.endColor = new Color(0.3f, 0.3f, 0.3f, 0f);
        ballVel = Vector3.up * 6f;
        SFX.Play("quest", 0.6f);
        HUD.ShowToast("Квиддич! Подлети к красному квоффлу, ЛКМ — бросок в кольца соперника. Берегись чёрного бладжера", new Color(1f, 0.85f, 0.3f));
    }

    static Transform MakeFlyer(Vector3 pos, int houseIdx, bool keeper)
    {
        var t = new GameObject(keeper ? "Keeper" : "Chaser").transform;
        t.SetParent(WorldBuilder.Dynamic, false);
        t.position = pos;
        var model = Prims.Group(t, "Model");
        Figures.Humanoid(model, Houses.Main[houseIdx], Figures.Skin, Random.value < 0.5f ? new Color(0.3f, 0.2f, 0.1f) : new Color(0.85f, 0.75f, 0.5f), Houses.Second[houseIdx]);
        Figures.Broom(model).SetActive(true);
        if (keeper) model.localScale = Vector3.one * 1.15f;
        return t;
    }

    Vector3 Pos(Flyer f) => f.isPlayer ? f.t.position + Vector3.up : f.t.position;

    void Update()
    {
        if (finished || !GameState.Playing) return;
        float dt = Time.deltaTime;
        timeLeft -= dt;
        if (timeLeft <= 0f) { Finish(); return; }
        if (!GameManager.I.player.onBroom) GameManager.I.player.ToggleBroom();
        foreach (var f in flyers) { if (f.stun > 0f) f.stun -= dt; if (f.grabCd > 0f) f.grabCd -= dt; }
        foreach (var f in flyers) if (!f.isPlayer) Think(f, dt);
        UpdateBall(dt);
        UpdateBludger(dt);
        if (holder != null && holder.isPlayer && Input.GetMouseButtonDown(0))
        {
            var cam = GameManager.I.player.cam.transform;
            Throw(player, AssistAim(Pos(player), cam.forward));
        }
    }

    // Подруливание: если бросок почти в кольцо — летит точно в него.
    Vector3 AssistAim(Vector3 from, Vector3 dir)
    {
        int side = AttackSide(0);
        Vector3 best = dir;
        float bestAng = 14f;
        for (int k = -1; k <= 1; k++)
        {
            var to = Hoop(side, k) - from;
            float a = Vector3.Angle(dir, to);
            if (a < bestAng) { bestAng = a; best = to.normalized; }
        }
        return best;
    }

    void Think(Flyer f, float dt)
    {
        if (f.stun > 0f)
        {
            f.vel = Vector3.Lerp(f.vel, Vector3.down * 2f, 2f * dt);
            f.t.position += f.vel * dt;
            f.t.Rotate(0, 0, 400 * dt, Space.Self);
            return;
        }
        float speed = (f.keeper ? 11f : 14f) * (f.team == 1 ? Difficulty.Cur.speed : 1f);
        Vector3 target;
        var ball = quaffle.position;
        int own = -AttackSide(f.team);
        if (f.keeper)
        {
            var g = Hoop(own, 0);
            target = new Vector3(g.x - own * 3f, Mathf.Clamp(ball.y, 11f, 19f), C.z + Mathf.Clamp(ball.z - C.z, -9f, 9f));
            // Вратарь ловит летящий в кольца мяч.
            if (holder == null && f.grabCd <= 0f && (ball - f.t.position).magnitude < 3.2f && Vector3.Dot(ballVel, new Vector3(own, 0, 0)) > 0f && Random.value < 0.65f)
            {
                Grab(f);
                HUD.ShowToast(f.team == 0 ? "Наш вратарь отбил мяч!" : "Вратарь соперника отбил бросок!", f.team == 0 ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.6f, 0.4f));
            }
            if (holder == f)
            {
                tackleT += dt;
                if (tackleT > 1.2f) Pass(f);
            }
        }
        else if (holder == f)
        {
            target = attackHoop + new Vector3(-AttackSide(f.team) * 14f, -2f, 0);
            speed *= 0.9f;
            if ((f.t.position - target).magnitude < 5f || Mathf.Abs(f.t.position.x - attackHoop.x) < 16f)
            {
                var aim = attackHoop + Random.insideUnitSphere * (f.team == 0 ? 1.3f : 2.2f / Difficulty.Cur.attack);
                Throw(f, (aim - f.t.position).normalized);
            }
        }
        else if (holder == null)
        {
            // К свободному мячу летят двое ближайших охотников каждой команды.
            target = Rank(f) < 2 ? ball : C + new Vector3(AttackSide(f.team) * 8f, 11f, f.t.position.z > C.z ? 8f : -8f);
        }
        else if (holder.team == f.team)
            target = Pos(holder) + new Vector3(AttackSide(f.team) * 8f, 1f, f.t.position.z > holder.t.position.z ? 6f : -6f);
        else
            target = Pos(holder);
        var to = target - f.t.position;
        f.vel = Vector3.Lerp(f.vel, Vector3.ClampMagnitude(to * 1.5f, speed), 2.5f * dt);
        var np = f.t.position + f.vel * dt;
        np.y = Mathf.Clamp(np.y, 2f, 24f);
        f.t.position = np;
        var flat = new Vector3(f.vel.x, 0, f.vel.z);
        if (flat.sqrMagnitude > 0.5f) f.t.rotation = Quaternion.Slerp(f.t.rotation, Quaternion.LookRotation(flat), 5f * dt);
        if (holder == null && f.grabCd <= 0f && (ball - f.t.position).magnitude < 2.2f) Grab(f);
        // Отбор мяча у соперника.
        if (holder != null && holder.team != f.team && !f.keeper && (Pos(holder) - f.t.position).magnitude < 2.4f)
        {
            tackleT += dt;
            if (tackleT > 0.6f)
            {
                tackleT = 0f;
                if (Random.value < (holder.isPlayer ? 0.3f : 0.4f)) { Grab(f); if (holder.team == 1) HUD.ShowToast("Соперник отобрал квоффл!", new Color(1f, 0.6f, 0.4f)); }
            }
        }
    }

    int Rank(Flyer f)
    {
        float d = (quaffle.position - f.t.position).sqrMagnitude;
        int r = 0;
        foreach (var o in flyers)
            if (o != f && o.team == f.team && !o.keeper && !o.isPlayer && (quaffle.position - o.t.position).sqrMagnitude < d) r++;
        return r;
    }

    public void Grab(Flyer f)
    {
        holder = f;
        tackleT = 0f;
        f.grabCd = 0.8f;
        if (!f.isPlayer && !f.keeper) attackHoop = Hoop(AttackSide(f.team), Random.Range(-1, 2));
        SFX.Play("click", 0.5f);
        if (f.isPlayer) HUD.ShowToast("Квоффл у тебя! Лети к кольцам соперника, ЛКМ — бросок", new Color(1f, 0.85f, 0.4f));
    }

    void Pass(Flyer from)
    {
        Flyer best = null;
        float bd = float.MaxValue;
        foreach (var o in flyers)
        {
            if (o == from || o.team != from.team || o.keeper) continue;
            float d = (o.t.position - from.t.position).sqrMagnitude;
            if (d < bd) { bd = d; best = o; }
        }
        if (best != null) Throw(from, (Pos(best) - from.t.position).normalized * 0.9f + Vector3.up * 0.1f);
    }

    public void Throw(Flyer f, Vector3 dir)
    {
        if (holder != f) return;
        holder = null;
        lastThrower = f;
        f.grabCd = 0.7f;
        quaffle.position = Pos(f) + dir * 1.5f;
        ballVel = dir.normalized * 30f;
        SFX.Play("cast", 0.4f);
    }

    void UpdateBall(float dt)
    {
        if (holder != null)
        {
            var hp = Pos(holder) + holder.t.forward * 0.6f + Vector3.up * 0.4f;
            quaffle.position = hp;
            ballVel = Vector3.zero;
            return;
        }
        var prev = quaffle.position;
        ballVel += Vector3.down * 4f * dt;
        ballVel *= 1f - 0.25f * dt;
        var np = prev + ballVel * dt;
        // Гол: мяч пересёк плоскость кольца рядом с его центром.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = C.x + side * 27f;
            if ((prev.x - x) * (np.x - x) > 0f) continue;
            for (int k = -1; k <= 1; k++)
            {
                var h = Hoop(side, k);
                float u = Mathf.InverseLerp(prev.x, np.x, x);
                var cross = Vector3.Lerp(prev, np, u);
                if (new Vector2(cross.y - h.y, cross.z - h.z).magnitude < 1.9f) { Goal(side == 1 ? 0 : 1, h); return; }
            }
        }
        if (np.y < 1f) { np.y = 1f; ballVel.y = Mathf.Abs(ballVel.y) * 0.5f; ballVel.x *= 0.7f; ballVel.z *= 0.7f; }
        var rel = np - C;
        if (Mathf.Abs(rel.x) > 34f) { ballVel.x = -ballVel.x * 0.5f; np.x = C.x + Mathf.Sign(rel.x) * 34f; }
        if (Mathf.Abs(rel.z) > 19f) { ballVel.z = -ballVel.z * 0.5f; np.z = C.z + Mathf.Sign(rel.z) * 19f; }
        quaffle.position = np;
        if (player.grabCd <= 0f && (np - Pos(player)).magnitude < 3f) Grab(player);
    }

    void Goal(int team, Vector3 hoop)
    {
        score[team] += 10;
        FX.Burst(hoop, Houses.Main[house[team]], 80, 9f, 0.5f, 1f, 0.2f);
        FX.Flash(hoop, Houses.Main[house[team]], 5f, 16f);
        SFX.Play(team == 0 ? "victory" : "hurt", 0.6f);
        bool mine = lastThrower != null && lastThrower.isPlayer;
        HUD.ShowToast(team == 0 ? (mine ? "ГОЛ! Ты забил!" : "Гол! Твоя команда забила") : "Соперник забил…", team == 0 ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.55f, 0.4f));
        if (mine) Progression.I.Gain(15);
        // Мяч переходит вратарю пропустившей команды.
        var k = flyers.Find(f => f.keeper && f.team != team);
        quaffle.position = Pos(k);
        Grab(k);
    }

    void UpdateBludger(float dt)
    {
        bludgerRetarget -= dt;
        if (bludgerTarget == null || bludgerRetarget <= 0f || bludgerTarget.stun > 0f)
        {
            bludgerRetarget = Random.Range(3f, 5f);
            var pool = flyers.FindAll(f => !f.keeper && f.stun <= 0f);
            bludgerTarget = Random.value < 0.35f ? player : pool[Random.Range(0, pool.Count)];
        }
        var to = Pos(bludgerTarget) - bludger.position;
        bludgerVel = Vector3.Lerp(bludgerVel, to.normalized * 15f, 1.4f * dt);
        bludger.position += bludgerVel * dt;
        if (to.magnitude < 1.5f)
        {
            var f = bludgerTarget;
            FX.Burst(bludger.position, new Color(0.4f, 0.4f, 0.4f), 30, 6f, 0.4f, 0.5f);
            SFX.Play("heavy", 0.6f);
            if (f.isPlayer)
            {
                var pc = GameManager.I.player;
                pc.ReceiveMelee(8f, true, null, bludgerVel.normalized * 8f);
                HUD.ShowToast("Бладжер!", new Color(1f, 0.5f, 0.4f));
                f.grabCd = 1.5f;
            }
            else f.stun = 1.5f;
            if (holder == f) { holder = null; ballVel = bludgerVel * 0.4f + Vector3.up * 4f; }
            bludgerVel = -bludgerVel * 0.8f;
            bludgerTarget = null;
        }
    }

    public void Finish()
    {
        if (finished) return;
        finished = true;
        Activities.I.QuidditchFinished(score[0], score[1]);
        foreach (var f in flyers) if (!f.isPlayer && f.t != null) Destroy(f.t.gameObject);
        Destroy(quaffle.gameObject);
        Destroy(bludger.gameObject);
        Destroy(gameObject);
    }

    // Загрузка сохранения: убрать матч без награды.
    public void Abort()
    {
        finished = true;
        foreach (var f in flyers) if (!f.isPlayer && f.t != null) Destroy(f.t.gameObject);
        Destroy(quaffle.gameObject);
        Destroy(bludger.gameObject);
        Destroy(gameObject);
    }

    public Flyer Keeper(int team) => flyers.Find(f => f.keeper && f.team == team);
}

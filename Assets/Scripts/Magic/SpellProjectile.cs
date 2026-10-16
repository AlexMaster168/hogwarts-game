using System.Collections.Generic;
using UnityEngine;

public enum SpellType
{
    Basic, Incendio, Levioso, Depulso, Confringo, Ancient, Stupefy, EnemyBolt, EnemyCurse,
    Expelliarmus, Accio, Diffindo, Glacius, Bombarda, Imperio, Crucio, AvadaKedavra, Patronus, Arresto,
    Lumos, Reparo, Aguamenti, Reducto, Incarcerous, Rictusempra
}

public struct SpellHit
{
    public SpellType type;
    public float damage;
    public Vector3 point;
    public Vector3 dir;
    public GameObject source;
    public bool unblockable, finisher;
}

public interface ISpellTarget
{
    void OnSpellHit(SpellHit hit);
}

public class SpellProjectile : MonoBehaviour
{
    public SpellType type;
    public float speed, damage, radius = 0.3f, life = 4f, homing, homingHeight = 1.1f;
    public GameObject owner;
    public Transform homingTarget;
    public bool hostile, unblockable, finisher;
    Vector3 dir;
    Color color;
    bool done;
    public System.Action<bool> resolved;
    bool reported, affected;
    void Report(bool hit)
    {
        if (reported) return;
        reported = true;
        resolved?.Invoke(hit);
    }
    void OnDestroy() { Report(false); }
    GameObject core;
    Light lightC;
    TrailRenderer trail;
    static readonly List<ISpellTarget> aoeBuf = new();

    public static SpellProjectile Spawn(SpellType type, Vector3 pos, Vector3 dir, GameObject owner, float damage, float speed, Color color,
        bool hostile, Transform homingTarget = null, float homing = 0f, bool unblockable = false, float size = 0.25f)
    {
        var go = new GameObject("Spell_" + type);
        go.transform.position = pos;
        var p = go.AddComponent<SpellProjectile>();
        p.core = Prims.Sphere(go.transform, Vector3.zero, size, MatLib.Glow(color, 4f));
        p.trail = go.AddComponent<TrailRenderer>();
        p.trail.sharedMaterial = MatLib.Additive(Color.white);
        p.trail.time = 0.22f;
        p.trail.startWidth = size * 1.8f;
        p.trail.endWidth = 0f;
        p.trail.startColor = color;
        p.trail.endColor = new Color(color.r, color.g, color.b, 0f);
        p.trail.numCapVertices = 2;
        p.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.lightC = go.AddComponent<Light>();
        p.lightC.type = LightType.Point;
        p.lightC.color = color;
        p.lightC.range = 6f;
        p.lightC.intensity = 2.5f;
        p.lightC.shadows = LightShadows.None;
        p.type = type;
        p.dir = dir.normalized;
        p.owner = owner;
        p.damage = damage;
        p.speed = speed;
        p.color = color;
        p.hostile = hostile;
        p.homingTarget = homingTarget;
        p.homing = homing;
        p.unblockable = unblockable;
        p.radius = hostile ? Mathf.Max(0.2f, size) : 0.12f;
        return p;
    }

    void Update()
    {
        if (done) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        life -= dt;
        if (life <= 0f) { Impact(transform.position, null); return; }

        if (homingTarget != null && homing > 0f)
        {
            var to = (homingTarget.position + Vector3.up * homingHeight - transform.position).normalized;
            dir = Vector3.RotateTowards(dir, to, homing * dt, 0f).normalized;
        }

        float step = speed * dt;
        var hits = Physics.SphereCastAll(transform.position, radius, dir, step, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var col = h.collider;
            if (owner != null && col.transform.IsChildOf(owner.transform)) continue;
            var target = col.GetComponentInParent<ISpellTarget>();
            if (hostile && target is Enemy) continue;
            if (!hostile && target is PlayerController) continue;
            Vector3 point = h.distance <= 0f ? transform.position : h.point;
            if (hostile && target is PlayerController pc && !unblockable && pc.caster.InParryWindow)
            {
                Reflect(pc);
                return;
            }
            Impact(point, target);
            return;
        }
        transform.position += dir * step;
    }

    void Impact(Vector3 point, ISpellTarget target)
    {
        done = true;
        float aoe = type == SpellType.Confringo ? 4.5f : type == SpellType.Bombarda ? 7f : 0f;
        if (aoe > 0f)
        {
            aoeBuf.Clear();
            foreach (var c in Physics.OverlapSphere(point, aoe, ~0, QueryTriggerInteraction.Ignore))
            {
                var t = c.GetComponentInParent<ISpellTarget>();
                if (t == null || t is PlayerController || aoeBuf.Contains(t)) continue;
                aoeBuf.Add(t);
            }
            foreach (var t in aoeBuf) Apply(t, point);
            bool big = type == SpellType.Bombarda;
            FX.Burst(point, color, big ? 120 : 60, big ? 16f : 10f, big ? 1.4f : 0.9f, 0.8f, -0.5f);
            if (type == SpellType.Confringo)
            {
                FX.Burst(point, new Color(1f, 0.55f, 0.1f), 50, 5f, 0.7f, 1.1f, -2.5f);
                FX.Burst(point, new Color(1f, 0.85f, 0.3f), 25, 3f, 0.5f, 0.9f, -3f);
                SFX.Play("fire", 0.7f);
            }
            if (big) FX.Shockwave(point - Vector3.up * 0.5f, color, aoe);
            FX.Flash(point, color, big ? 9f : 6f, big ? 20f : 14f, 0.4f);
            SFX.Play("boom", big ? 1f : 0.7f);
            GameManager.I.camRig.Shake(big ? 0.5f : 0.25f);
        }
        else
        {
            if (target != null) Apply(target, point);
            bool curse = type == SpellType.AvadaKedavra || type == SpellType.Crucio || type == SpellType.Imperio;
            FX.Burst(point, color, curse ? 60 : 22, curse ? 8f : 5f, curse ? 0.7f : 0.35f, 0.5f);
            if (curse) FX.Flash(point, color, 6f, 12f, 0.4f);
            SFX.Play(type == SpellType.Glacius ? "freeze" : "hit", 0.4f);
        }
        if (core != null) core.SetActive(false);
        Report(affected);
        if (lightC != null) lightC.enabled = false;
        Destroy(gameObject, trail != null ? trail.time : 0f);
    }

    void Apply(ISpellTarget t, Vector3 point)
    {
        if (t is Enemy enemy && enemy.IsDead) return;
        affected = true;
        t.OnSpellHit(new SpellHit { type = type, damage = damage, point = point, dir = dir, source = owner, unblockable = unblockable, finisher = finisher });
    }

    // Идеальный Протего: отражаем снаряд обратно как Остолбеней.
    void Reflect(PlayerController pc)
    {
        var src = owner;
        owner = pc.gameObject;
        hostile = false;
        type = SpellType.Stupefy;
        damage = 20f;
        color = new Color(0.3f, 0.8f, 1f);
        core.GetComponent<Renderer>().sharedMaterial = MatLib.Glow(color, 4f);
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        lightC.color = color;
        speed *= 1.6f;
        life = 3f;
        homing = 12f;
        homingTarget = null;
        if (src != null)
        {
            homingTarget = src.transform;
            var tg = src.GetComponent<Targetable>();
            homingHeight = tg != null ? tg.aimHeight : 1.2f;
            dir = (src.transform.position + Vector3.up * homingHeight - transform.position).normalized;
        }
        else dir = -dir;
        pc.caster.OnParry();
        FX.Burst(transform.position, color, 30, 6f, 0.4f, 0.4f);
    }
}

using UnityEngine;

// Один способ поиска препятствий для прицела и проверки линии выстрела.
public static class WandAim
{
    // Narrow acquisition cone, wider retention cone: moving targets do not flicker.
    public static Targetable SelectTarget(Vector3 eye, Vector3 forward, Vector3 muzzle, Transform owner, Targetable current)
    {
        bool Visible(Targetable t, float angle)
        {
            if (t == null || !t.Alive || (owner != null && t.transform.IsChildOf(owner))) return false;
            var delta = t.AimPoint - eye;
            if (delta.sqrMagnitude > 60f * 60f || Vector3.Angle(forward, delta) > angle) return false;
            if (Trace(eye, delta.normalized, delta.magnitude, owner, 0f, out var hit)
                && !hit.collider.transform.IsChildOf(t.transform)) return false;
            delta = t.AimPoint - muzzle;
            return !Trace(muzzle, delta.normalized, delta.magnitude, owner, 0f, out hit)
                || hit.collider.transform.IsChildOf(t.transform);
        }
        // Deliberately pointing at another enemy always overrides retention.
        if (Trace(eye, forward, 60f, owner, 0f, out var direct))
        {
            var t = direct.collider.GetComponentInParent<Targetable>();
            if (Visible(t, 24f)) return t;
        }
        if (Visible(current, 24f)) return current;
        Targetable best = null;
        float score = float.PositiveInfinity;
        foreach (var t in Targetable.All)
        {
            if (!Visible(t, 12f)) continue;
            var delta = t.AimPoint - eye;
            float value = Vector3.Angle(forward, delta) + delta.magnitude * .025f;
            var enemy = t.GetComponent<Enemy>();
            if (enemy != null && enemy.Charmed) value += 1000f;
            if (value < score) { score = value; best = t; }
        }
        return best;
    }
    public static bool Trace(Vector3 from, Vector3 direction, float distance, Transform owner, float radius, out RaycastHit nearest)
    {
        nearest = default;
        float best = float.PositiveInfinity;
        var hits = radius > 0 ? Physics.SphereCastAll(from, radius, direction, distance, ~0, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(from, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
            if (hit.distance < best) { best = hit.distance; nearest = hit; }
        }
        return best < float.PositiveInfinity;
    }
}

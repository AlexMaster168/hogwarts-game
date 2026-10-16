using UnityEngine;

// Один способ поиска препятствий для прицела и проверки линии выстрела.
public static class WandAim
{
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

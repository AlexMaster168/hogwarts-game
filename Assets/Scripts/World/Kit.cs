using System.Collections.Generic;
using UnityEngine;

// Модули замка из Resources/Castle (KayKit Dungeon/Furniture Bits, Kenney Castle/Furniture — все CC0).
public static class Kit
{
    static readonly Dictionary<string, GameObject> cache = new();

    public static GameObject Get(string name)
    {
        if (!cache.TryGetValue(name, out var g))
        {
            g = Resources.Load<GameObject>("Castle/" + name);
            if (g == null) Debug.LogWarning("Kit: нет модуля " + name);
            cache[name] = g;
        }
        return g;
    }

    // Ставит модуль. center — сдвинуть модель так, чтобы низ был в pos, а габарит по XZ по центру;
    // col — бокс-коллайдер по габаритам.
    public static GameObject Place(Transform parent, string name, Vector3 pos, float yaw = 0f, float scale = 1f, bool col = false, bool center = false)
    {
        var src = Get(name);
        if (src == null) return null;
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        var m = Object.Instantiate(src, root.transform, false);
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        m.transform.localScale = Vector3.one * scale;
        // Тени от солнца отбрасывают только стены и колонны — мебель и пол внутри под крышей.
        if (!name.StartsWith("wall") && !name.StartsWith("pillar") && !name.StartsWith("kenney/flag"))
            foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (center || col)
        {
            var b = LocalBounds(root.transform);
            if (center)
            {
                var off = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                m.transform.localPosition = off;
                b.center += off;
            }
            if (col)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = b.center;
                bc.size = b.size;
            }
        }
        return root;
    }

    // Габариты всех мешей в локальных координатах root (меши могут быть нечитаемыми — берём mesh.bounds).
    public static Bounds LocalBounds(Transform root)
    {
        bool any = false;
        var b = new Bounds();
        var toLocal = root.worldToLocalMatrix;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            var m = toLocal * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = m.MultiplyPoint3x4(c);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        foreach (var sm in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var p = toLocal.MultiplyPoint3x4(sm.bounds.center);
            if (!any) { b = new Bounds(p, sm.bounds.size); any = true; } else b.Encapsulate(new Bounds(p, sm.bounds.size));
        }
        return b;
    }

    // Невидимый коллайдер-бокс (для стен с проёмами).
    public static void Blocker(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
    {
        var g = new GameObject("Blocker");
        g.transform.SetParent(parent, false);
        g.transform.SetPositionAndRotation(center, rot);
        g.AddComponent<BoxCollider>().size = size;
    }
}

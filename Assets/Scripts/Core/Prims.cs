using System.Collections.Generic;
using UnityEngine;

// Генерация простых мешей (конус, призма-крыша, тор) с плоским шейдингом.
public static class MeshGen
{
    class Builder
    {
        readonly List<Vector3> v = new();
        readonly List<int> t = new();

        // Разворачивает треугольник так, чтобы он смотрел наружу от точки inside.
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0) { var tmp = b; b = c; c = tmp; }
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    // Основание радиуса 0.5 на y=0, вершина в y=1.
    public static Mesh Cone(int seg)
    {
        var b = new Builder();
        var inside = new Vector3(0, 0.3f, 0);
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
            var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
            var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
            b.Tri(p0, Vector3.up, p1, inside);
            b.Tri(Vector3.zero, p0, p1, inside);
        }
        return b.Build("HM_Cone");
    }

    // Двускатная крыша: ширина 1 по X, высота 1, длина 1 по Z, основание на y=0.
    public static Mesh Prism()
    {
        var b = new Builder();
        var inside = new Vector3(0, 0.33f, 0);
        Vector3 l0 = new(-0.5f, 0, -0.5f), r0 = new(0.5f, 0, -0.5f), t0 = new(0, 1, -0.5f);
        Vector3 l1 = new(-0.5f, 0, 0.5f), r1 = new(0.5f, 0, 0.5f), t1 = new(0, 1, 0.5f);
        b.Tri(l0, t0, r0, inside);
        b.Tri(l1, t1, r1, inside);
        b.Tri(l0, l1, t1, inside); b.Tri(l0, t1, t0, inside);
        b.Tri(r0, r1, t1, inside); b.Tri(r0, t1, t0, inside);
        b.Tri(l0, r0, r1, inside); b.Tri(l0, r1, l1, inside);
        return b.Build("HM_Prism");
    }

    // Тор в плоскости XZ, радиус кольца 1, толщина трубки tube.
    public static Mesh Torus(int seg, int side, float tube)
    {
        var b = new Builder();
        for (int i = 0; i < seg; i++)
        {
            float u0 = i * Mathf.PI * 2 / seg, u1 = (i + 1) * Mathf.PI * 2 / seg;
            for (int j = 0; j < side; j++)
            {
                float w0 = j * Mathf.PI * 2 / side, w1 = (j + 1) * Mathf.PI * 2 / side;
                Vector3 p00 = TP(u0, w0, tube), p10 = TP(u1, w0, tube), p01 = TP(u0, w1, tube), p11 = TP(u1, w1, tube);
                float um = (u0 + u1) * 0.5f;
                var inside = new Vector3(Mathf.Cos(um), 0, Mathf.Sin(um));
                b.Tri(p00, p10, p11, inside);
                b.Tri(p00, p11, p01, inside);
            }
        }
        return b.Build("HM_Torus");
    }

    static Vector3 TP(float u, float w, float r)
    {
        float rr = 1 + r * Mathf.Cos(w);
        return new Vector3(rr * Mathf.Cos(u), r * Mathf.Sin(w), rr * Mathf.Sin(u));
    }
}

// Быстрые конструкторы объектов из примитивов. Позиции — локальные относительно parent.
public static class Prims
{
    static Mesh cone, prism, torus, cylinder, sphere;

    public static Mesh ConeMesh { get { if (cone == null) cone = MeshGen.Cone(18); return cone; } }
    public static Mesh PrismMesh { get { if (prism == null) prism = MeshGen.Prism(); return prism; } }
    public static Mesh TorusMesh { get { if (torus == null) torus = MeshGen.Torus(28, 8, 0.09f); return torus; } }

    public static GameObject Make(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        var col = go.GetComponent<Collider>();
        bool nonUniform = Mathf.Abs(scale.x - scale.y) > 0.01f || Mathf.Abs(scale.x - scale.z) > 0.01f;
        if (!collider)
            Object.DestroyImmediate(col);
        else if (type == PrimitiveType.Cylinder || (type == PrimitiveType.Sphere && nonUniform))
        {
            // Капсула на плоском цилиндре превращается в огромную сферу — меняем на выпуклый меш.
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(col);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
        return go;
    }

    public static GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m, bool col = true, Quaternion? rot = null)
        => Make(PrimitiveType.Cube, p, pos, size, m, col, rot);

    // pos — центр основания цилиндра.
    public static GameObject Cyl(Transform p, Vector3 basePos, float radius, float height, Material m, bool col = true)
        => Make(PrimitiveType.Cylinder, p, basePos + Vector3.up * height * 0.5f, new Vector3(radius * 2, height * 0.5f, radius * 2), m, col);

    public static GameObject Sphere(Transform p, Vector3 pos, float diameter, Material m, bool col = false)
        => Make(PrimitiveType.Sphere, p, pos, Vector3.one * diameter, m, col);

    public static GameObject MeshObj(Transform p, Mesh mesh, Vector3 pos, Vector3 scale, Material m, bool col, Quaternion? rot = null, string name = "Mesh")
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        if (col)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
        }
        return go;
    }

    public static GameObject Cone(Transform p, Vector3 basePos, float radius, float height, Material m, bool col = false, Quaternion? rot = null)
        => MeshObj(p, ConeMesh, basePos, new Vector3(radius * 2, height, radius * 2), m, col, rot, "Cone");

    // Крыша: basePos — центр низа, ширина по X, конёк вдоль Z (поворачивай yaw).
    public static GameObject Roof(Transform p, Vector3 basePos, float width, float height, float length, Material m, float yaw = 0f, bool col = true)
        => MeshObj(p, PrismMesh, basePos, new Vector3(width, height, length), m, col, Quaternion.Euler(0, yaw, 0), "Roof");

    public static GameObject Torus(Transform p, Vector3 pos, float radius, Material m, Quaternion? rot = null)
        => MeshObj(p, TorusMesh, pos, Vector3.one * radius, m, false, rot, "Ring");

    // Стена от a до b (по земле) с зубцами по верху.
    public static void Wall(Transform p, Vector3 a, Vector3 b, float h, float t, Material m, Material crenel = null)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        var rot = Quaternion.LookRotation(d.normalized);
        Box(p, (a + b) * 0.5f + Vector3.up * h * 0.5f, new Vector3(t, h, len), m, true, rot);
        if (crenel == null) return;
        int n = Mathf.FloorToInt(len / 2.5f);
        for (int i = 0; i < n; i++)
        {
            Vector3 c = a + d.normalized * (1.25f + i * 2.5f) + Vector3.up * (h + 0.6f);
            Box(p, c, new Vector3(t, 1.2f, 1.3f), crenel, false, rot);
        }
    }

    public static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }
}

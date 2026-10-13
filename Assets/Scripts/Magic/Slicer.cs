using System.Collections.Generic;
using UnityEngine;

// Диффиндо: светящийся разрез по врагу, а добивающий удар режет модель на две половины.
// Меши запекаются (SkinnedMeshRenderer.BakeMesh), треугольники раскладываются по сторонам плоскости разреза,
// оригинал прячется, половины разлетаются и через пару секунд уходят под землю.
public static class Slicer
{
    public static readonly Color CutColor = new(0.75f, 1f, 1f);

    // Плоскость разреза — наискосок поперёк направления удара.
    public static Vector3 CutNormal(Vector3 dir)
    {
        var flat = new Vector3(dir.x, 0, dir.z);
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        var side = Vector3.Cross(Vector3.up, flat.normalized);
        return (Vector3.up + side * 0.55f).normalized;
    }

    public static void Slash(Vector3 point, Vector3 dir)
    {
        var flat = new Vector3(dir.x, 0, dir.z);
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        var go = new GameObject("DiffindoSlash");
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = point;
        // Полоса лежит в плоскости разреза и смотрит на удар.
        go.transform.rotation = Quaternion.LookRotation(-flat.normalized, CutNormal(dir)) * Quaternion.Euler(0, 0, 90);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(go.transform, false);
        q.transform.localScale = new Vector3(0.14f, 2.6f, 1f);
        q.GetComponent<Renderer>().sharedMaterial = MatLib.Additive(CutColor);
        var back = Object.Instantiate(q, go.transform);
        back.transform.localRotation = Quaternion.Euler(0, 180, 0);
        go.AddComponent<SlashFx>();
        FX.Burst(point, CutColor, 18, 7f, 0.18f, 0.35f, 0.4f);
        FX.Flash(point, CutColor, 3f, 6f, 0.15f);
    }

    public static bool Slice(Transform model, Vector3 point, Vector3 normal, Vector3 dir, float groundY, float capRadius = 0.3f)
    {
        var upper = new GameObject("SliceUpper");
        var lower = new GameObject("SliceLower");
        foreach (var h in new[] { upper, lower })
        {
            h.transform.SetParent(WorldBuilder.Dynamic, false);
            h.transform.position = point;
        }
        int pieces = 0;
        var hide = new List<Renderer>();
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            Mesh src;
            Matrix4x4 l2w;
            if (r is SkinnedMeshRenderer smr)
            {
                src = new Mesh();
                // Без масштаба трансформа: кости уже дают итоговый размер, остаётся только позиция и поворот.
                smr.BakeMesh(src, false);
                l2w = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            }
            else if (r is MeshRenderer)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                src = mf.sharedMesh;
                l2w = r.transform.localToWorldMatrix;
            }
            else continue;
            if (Split(src, l2w, point, normal, r.sharedMaterials, upper.transform, lower.transform)) { pieces++; hide.Add(r); }
            if (r is SkinnedMeshRenderer) Object.Destroy(src);
        }
        if (pieces == 0)
        {
            Object.Destroy(upper);
            Object.Destroy(lower);
            return false;
        }
        foreach (var r in hide) r.enabled = false;
        // Срез: тёмно-красная крышка по плоскости разреза на каждой половине, чтобы половины не были полыми.
        capMat ??= MatLib.Solid(new Color(0.45f, 0.04f, 0.05f), 0.5f);
        rimMat ??= MatLib.Glow(CutColor, 1.5f);
        foreach (var (h, up) in new[] { (upper, true), (lower, false) })
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(cap.GetComponent<Collider>());
            cap.transform.SetParent(h.transform, false);
            cap.transform.rotation = Quaternion.FromToRotation(Vector3.up, up ? -normal : normal);
            cap.transform.localScale = new Vector3(capRadius * 2f, 0.012f, capRadius * 2f);
            cap.GetComponent<Renderer>().sharedMaterial = capMat;
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(rim.GetComponent<Collider>());
            rim.transform.SetParent(cap.transform, false);
            rim.transform.localScale = new Vector3(1.08f, 0.5f, 1.08f);
            rim.transform.localPosition = Vector3.down * 0.6f;
            rim.GetComponent<Renderer>().sharedMaterial = rimMat;
        }
        var flat = new Vector3(dir.x, 0, dir.z).normalized;
        var side = Vector3.Cross(Vector3.up, flat);
        upper.AddComponent<SliceHalf>().Init(normal * 2.2f + flat * 2.5f + side * 1.2f, Random.onUnitSphere * 220f, groundY);
        lower.AddComponent<SliceHalf>().Init(-side * 0.6f + flat * 0.4f, new Vector3(Random.Range(-60f, 60f), 0, Random.Range(-90f, 90f)), groundY);
        FX.Burst(point, CutColor, 40, 8f, 0.25f, 0.6f, 0.6f);
        FX.Flash(point, CutColor, 5f, 9f, 0.3f);
        return true;
    }

    static Material capMat, rimMat;
    static readonly List<Vector3> vu = new(), vl = new(), nu = new(), nl = new();
    static readonly List<Vector2> uu = new(), ul = new();

    static bool Split(Mesh m, Matrix4x4 l2w, Vector3 point, Vector3 normal, Material[] mats, Transform up, Transform low)
    {
        var verts = m.vertices;
        var norms = m.normals;
        var uvs = m.uv;
        bool hasN = norms != null && norms.Length == verts.Length, hasUv = uvs != null && uvs.Length == verts.Length;
        var world = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++) world[i] = l2w.MultiplyPoint3x4(verts[i]);
        var plane = new Plane(normal, point);
        vu.Clear(); vl.Clear(); nu.Clear(); nl.Clear(); uu.Clear(); ul.Clear();
        var subU = new List<List<int>>();
        var subL = new List<List<int>>();
        for (int s = 0; s < m.subMeshCount; s++)
        {
            var tu = new List<int>();
            var tl = new List<int>();
            var tris = m.GetTriangles(s);
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                var c = (world[tris[t]] + world[tris[t + 1]] + world[tris[t + 2]]) / 3f;
                bool top = plane.GetSide(c);
                var vv = top ? vu : vl;
                var nn = top ? nu : nl;
                var uv = top ? uu : ul;
                var list = top ? tu : tl;
                for (int k = 0; k < 3; k++)
                {
                    int i = tris[t + k];
                    list.Add(vv.Count);
                    vv.Add(world[i] - point);
                    nn.Add(hasN ? l2w.MultiplyVector(norms[i]).normalized : Vector3.up);
                    uv.Add(hasUv ? uvs[i] : Vector2.zero);
                }
            }
            subU.Add(tu);
            subL.Add(tl);
        }
        bool any = false;
        any |= Build(up, vu, nu, uu, subU, mats);
        any |= Build(low, vl, nl, ul, subL, mats);
        return any;
    }

    static bool Build(Transform parent, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<List<int>> subs, Material[] mats)
    {
        if (v.Count == 0) return false;
        var mesh = new Mesh { indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s);
        mesh.RecalculateBounds();
        var go = new GameObject("Piece");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        var use = new Material[subs.Count];
        for (int s = 0; s < use.Length; s++) use[s] = mats.Length > 0 ? mats[Mathf.Min(s, mats.Length - 1)] : MatLib.Solid(Color.gray);
        r.sharedMaterials = use;
        return true;
    }
}

// Половина разрезанного врага: летит, падает на землю, лежит и уходит под землю.
public class SliceHalf : MonoBehaviour
{
    Vector3 vel, spin;
    float groundY, t;

    Renderer[] rends;

    public void Init(Vector3 v, Vector3 angular, float ground)
    {
        rends = GetComponentsInChildren<Renderer>();
        vel = v;
        spin = angular;
        groundY = ground;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (t > 3.2f)
        {
            transform.position += Vector3.down * dt * 0.8f;
            if (t > 5f) Destroy(gameObject);
            return;
        }
        vel.y -= 14f * dt;
        transform.position += vel * dt;
        transform.Rotate(spin * dt, Space.World);
        // Опора — нижний край самого меша, а не точка разреза: иначе ноги уходили под землю.
        float minY = float.MaxValue;
        foreach (var r in rends) if (r != null) minY = Mathf.Min(minY, r.bounds.min.y);
        if (minY < groundY)
        {
            transform.position += Vector3.up * (groundY - minY);
            if (vel.y < 0f) vel = new Vector3(vel.x * 0.45f, -vel.y * 0.15f, vel.z * 0.45f);
            spin *= 0.6f;
        }
    }
}

// Вспышка разреза: быстро вытягивается и гаснет.
public class SlashFx : MonoBehaviour
{
    float t;

    void Update()
    {
        t += Time.deltaTime;
        float k = t / 0.28f;
        transform.localScale = new Vector3(1f, Mathf.Lerp(0.3f, 1.15f, Mathf.Min(1f, k * 3f)), 1f) * (1f - Mathf.Max(0f, k - 0.5f) * 1.6f);
        if (k >= 1f) Destroy(gameObject);
    }
}

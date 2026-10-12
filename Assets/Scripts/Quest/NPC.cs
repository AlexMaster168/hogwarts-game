using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public static readonly List<NPC> All = new();

    public string id;
    public string displayName;
    public string[] idleLines;
    public string shopId;
    public FigureParts parts;
    Transform model;
    float t;
    Vector3 home;
    Vector3 wanderTarget;
    float wanderWait;
    public bool wanders;
    public CharacterVisual visual;

    public bool ghost;

    // Призрак Хранителя: полупрозрачный, светится и парит.
    public void MakeGhost(Color c)
    {
        ghost = true;
        var m = MatLib.Fade(new Color(c.r, c.g, c.b, 0.35f), 2f, 0.8f);
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            var arr = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = m;
            r.sharedMaterials = arr;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var l = new GameObject("GhostLight").AddComponent<Light>();
        l.transform.SetParent(transform, false);
        l.transform.localPosition = Vector3.up * 1.5f;
        l.type = LightType.Point;
        l.color = c;
        l.range = 8f;
        l.intensity = 2f;
    }

    public NPC UseModel(string role, float height = 1.8f)
    {
        visual = CharacterVisual.Attach(model, role, height, parts, parts != null);
        return this;
    }

    public static NPC Spawn(string id, string name, Vector3 pos, Color robe, Color skin, Color hair, Color accent,
        bool hat = false, Color hatColor = default, params string[] idle)
    {
        var npc = Base(id, name, pos, idle);
        npc.parts = Figures.Humanoid(npc.model, robe, skin, hair, accent, hat, hatColor);
        return npc;
    }

    public static NPC SpawnHat(Vector3 pos)
    {
        var npc = Base("hat", "Распределяющая шляпа", pos, "Хм-м... Я уже сказала своё слово.");
        var wood = MatLib.Solid(new Color(0.35f, 0.22f, 0.12f), 0.3f);
        Prims.Cyl(npc.model, Vector3.zero, 0.35f, 0.9f, wood, false);
        var hatM = MatLib.Solid(new Color(0.4f, 0.3f, 0.2f), 0.05f, 1f, 0.7f);
        Prims.Make(PrimitiveType.Cylinder, npc.model, new Vector3(0, 0.92f, 0), new Vector3(1.0f, 0.02f, 1.0f), hatM, false);
        Prims.Cone(npc.model, new Vector3(0, 0.92f, 0), 0.38f, 1.0f, hatM, false, Quaternion.Euler(-14, 0, 6));
        var dark = MatLib.Solid(new Color(0.1f, 0.07f, 0.05f), 0.1f);
        Prims.Make(PrimitiveType.Cube, npc.model, new Vector3(0, 1.22f, 0.2f), new Vector3(0.32f, 0.06f, 0.1f), dark, false);
        return npc;
    }

    static NPC Base(string id, string name, Vector3 pos, params string[] idle)
    {
        var go = new GameObject("NPC_" + id);
        go.transform.SetParent(WorldBuilder.Dynamic, false);
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.9f, 0);
        col.height = 1.8f;
        col.radius = 0.4f;
        var npc = go.AddComponent<NPC>();
        npc.id = id;
        npc.displayName = name;
        npc.idleLines = idle;
        npc.home = pos;
        npc.wanderTarget = pos;
        npc.model = Prims.Group(go.transform, "Model");
        return npc;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public string RandomIdle() => idleLines != null && idleLines.Length > 0 ? idleLines[Random.Range(0, idleLines.Length)] : "...";

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        var p = GameManager.I != null ? GameManager.I.player : null;
        bool near = p != null && (p.transform.position - transform.position).sqrMagnitude < 64f;
        Vector3 look = transform.forward;
        float moving = 0;
        if (near)
        {
            look = p.transform.position - transform.position;
        }
        else if (wanders)
        {
            var to = wanderTarget - transform.position;
            to.y = 0;
            if (to.magnitude < 0.5f)
            {
                wanderWait -= dt;
                if (wanderWait <= 0)
                {
                    var r = Random.insideUnitCircle * 8f;
                    wanderTarget = home + new Vector3(r.x, 0, r.y);
                    wanderWait = Random.Range(2f, 5f);
                }
            }
            else
            {
                transform.position += to.normalized * 1.5f * dt;
                look = to;
                moving = 1;
            }
        }
        look.y = 0;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 5f * dt);
        if (ghost) model.localPosition = Vector3.up * (0.4f + Mathf.Sin(t * 1.5f) * 0.15f);
        if (visual != null) { visual.SetSpeed(moving > 0 ? 1.5f : 0f); return; }
        model.localPosition = Vector3.up * (moving > 0 ? Mathf.Abs(Mathf.Sin(t * 8)) * 0.05f : Mathf.Sin(t * 2) * 0.01f);
    }
}

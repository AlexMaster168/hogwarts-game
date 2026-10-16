using System.Collections.Generic;
using UnityEngine;

// Мелкая мебель за стенами соседних комнат не участвует в рендеринге Большого зала.
public class CastleInteriorVisibility : MonoBehaviour
{
    readonly List<Renderer> decorations = new();
    float next;
    public int ActiveDecorations { get; private set; }
    void Start()
    {
        var kit = GameObject.Find("CastleKit").transform;
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var b = renderer.bounds;
            if (!renderer.transform.IsChildOf(WorldBuilder.Root) && !renderer.transform.IsChildOf(kit)) continue;
            if (Mathf.Abs(b.center.x) > 42 || b.center.z < 29 || b.center.z > 105 || b.center.y > 13) continue;
            if (b.size.x > 8 || b.size.z > 8 || b.size.y > 6) continue;
            decorations.Add(renderer);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    static int Room(Vector3 p) => p.x < -14 ? (p.z < 54 ? 1 : 2) : p.x > 14 ? (p.z < 54 ? 3 : 4) : p.z > 85 ? 5 : 0;
    void LateUpdate()
    {
        if (Time.unscaledTime < next || Camera.main == null) return;
        next = Time.unscaledTime + 0.15f;
        var cam = Camera.main.transform.position;
        int room = Room(cam); ActiveDecorations = 0;
        foreach (var renderer in decorations)
        {
            if (renderer == null) continue;
            var at = renderer.bounds.center;
            bool show = (Room(at) == room && Mathf.Abs(at.y - cam.y) < 12) || (at - cam).sqrMagnitude < 18 * 18;
            renderer.enabled = show;
            if (show) ActiveDecorations++;
        }
    }
    void OnDisable() { foreach (var renderer in decorations) if (renderer != null) renderer.enabled = true; }
}

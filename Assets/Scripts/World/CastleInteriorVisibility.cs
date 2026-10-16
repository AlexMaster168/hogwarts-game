using System.Collections.Generic;
using UnityEngine;

// Однократная настройка теней. Видимость комнат не зависит от положения камеры.
public class CastleInteriorVisibility : MonoBehaviour
{
    readonly List<Renderer> decorations = new();
    public int ActiveDecorations { get; private set; }
    void Start()
    {
        var kitObject = GameObject.Find("CastleKit");
        var kit = kitObject != null ? kitObject.transform : null;
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var b = renderer.bounds;
            if (!renderer.transform.IsChildOf(WorldBuilder.Root) && (kit == null || !renderer.transform.IsChildOf(kit))) continue;
            if (Mathf.Abs(b.center.x) > 42 || b.center.z < 29 || b.center.z > 105 || b.center.y > 13) continue;
            if (b.size.x > 8 || b.size.z > 8 || b.size.y > 6) continue;
            decorations.Add(renderer);
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        ActiveDecorations = decorations.Count;
    }
    void OnDisable() { foreach (var renderer in decorations) if (renderer != null) renderer.enabled = true; }
}

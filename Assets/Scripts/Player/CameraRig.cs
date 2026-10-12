using UnityEngine;

// Камера от третьего лица с обходом стен. На титульнике/победе — облёт замка.
public class CameraRig : MonoBehaviour
{
    public Transform target;
    public float yaw, pitch = 12f;
    public float sensitivity = 2.2f;
    PlayerController pc;
    Vector3 pivot;
    float dist = 5f, shake;
    bool init;

    public Vector3 Forward => transform.forward;
    public Vector3 Right => transform.right;
    public Vector3 FlatForward
    {
        get
        {
            var f = transform.forward;
            f.y = 0;
            return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized;
        }
    }

    public void Shake(float a) { shake = Mathf.Max(shake, a); }

    public void SnapBehind()
    {
        yaw = target.eulerAngles.y;
        pitch = 12f;
        init = false;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (pc == null) pc = target.GetComponent<PlayerController>();

        if (GameState.Mode == GameMode.Title || GameState.Mode == GameMode.Victory)
        {
            float t = Time.unscaledTime * 0.05f;
            var focus = new Vector3(0, 18, 30);
            transform.position = focus + new Vector3(Mathf.Cos(t) * 150f, 45f, Mathf.Sin(t) * 150f);
            transform.LookAt(focus);
            init = false;
            return;
        }

        if (GameState.Playing)
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -40f, 70f);
        }

        float dt = Time.unscaledDeltaTime;
        var want = target.position + Vector3.up * 1.6f;
        if (!init) { pivot = want; init = true; }
        pivot = Vector3.Lerp(pivot, want, 1f - Mathf.Exp(-18f * dt));
        float wantDist = pc != null && pc.onBroom ? 7.5f : 5f;
        dist = Mathf.Lerp(dist, wantDist, 1f - Mathf.Exp(-4f * dt));

        var rot = Quaternion.Euler(pitch, yaw, 0);
        var offset = rot * new Vector3(0.6f, 0.2f, -dist);
        var dir = offset.normalized;
        float len = offset.magnitude;
        foreach (var h in Physics.SphereCastAll(pivot, 0.25f, dir, len, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider is CharacterController || h.collider.transform.IsChildOf(target)) continue;
            if (h.collider.GetComponentInParent<NPC>() != null) continue;
            if (h.distance > 0f && h.distance < len) len = h.distance;
        }
        len = Mathf.Max(len, 0.5f);

        shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
        var jitter = shake > 0 ? Random.insideUnitSphere * shake * 0.4f : Vector3.zero;
        transform.SetPositionAndRotation(pivot + dir * len + jitter, rot);
    }
}

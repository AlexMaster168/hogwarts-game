using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float max = 100f;
    public float current = 100f;
    public float minHealth;          // >0 — нельзя убить (дуэль)
    public bool invulnerable;

    public event System.Action<float> Damaged;
    public event System.Action Died;

    public bool Dead => current <= 0f;
    public float Frac => max > 0 ? current / max : 0f;

    public void Init(float m) { max = m; current = m; }

    public void TakeDamage(float amount)
    {
        if (Dead || invulnerable || amount <= 0f) return;
        current = Mathf.Max(minHealth, current - amount);
        Damaged?.Invoke(amount);
        if (current <= 0f)
        {
            current = 0f;
            Died?.Invoke();
        }
    }

    public void Heal(float a)
    {
        if (Dead) return;
        current = Mathf.Min(max, current + a);
    }

    public void Revive() { current = max; }
}

// Всё, во что можно целиться: враги и манекены.
public class Targetable : MonoBehaviour
{
    public static readonly List<Targetable> All = new();

    public string displayName = "";
    public Health health;
    public float aimHeight = 1.2f;
    public bool isBoss;

    public ISpellTarget Target { get; private set; }
    public Vector3 AimPoint => transform.position + Vector3.up * aimHeight;
    public bool Alive => isActiveAndEnabled && health != null && !health.Dead;

    void Awake() { Target = GetComponent<ISpellTarget>(); }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
}

using UnityEngine;

// Настоящая модель персонажа поверх «примитивного» скелета. Префабы готовит ModelSetup (Resources/Models/<role>).
// Если префаба нет — остаётся примитивная фигурка, игра работает как раньше.
public class CharacterVisual : MonoBehaviour
{
    public Animator anim;
    public Transform hand, head;
    float hitCd;

    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int AttackId = Animator.StringToHash("Attack");
    static readonly int HitId = Animator.StringToHash("Hit");
    static readonly int RollId = Animator.StringToHash("Roll");
    static readonly int DeadId = Animator.StringToHash("Dead");

    public static CharacterVisual Attach(Transform model, string role, float height, FigureParts parts = null, bool wand = false, Color? tint = null)
    {
        var prefab = Resources.Load<GameObject>("Models/" + role);
        if (prefab == null) return null;

        // Прячем примитивы, кроме шарфа (цвет факультета) и того, что добавится позже (метла, шляпа).
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (parts != null && r == parts.scarf) continue;
            if (Under(r.transform, model, "Broom")) continue;
            r.enabled = false;
        }

        var inst = Instantiate(prefab, model);
        inst.name = "Visual_" + role;
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;

        // Подгоняем рост и ставим ногами на землю.
        var b = Bounds(inst);
        if (b.size.y > 0.001f)
        {
            float k = height * model.lossyScale.y / b.size.y;
            inst.transform.localScale *= k;
            b = Bounds(inst);
            inst.transform.position += Vector3.up * (model.position.y - b.min.y);
        }

        if (tint.HasValue)
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                foreach (var m in mats)
                    if (m.HasProperty("_Color")) m.color *= tint.Value;
                    else if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", m.GetColor("baseColorFactor") * tint.Value);
                r.materials = mats;
            }

        var v = inst.AddComponent<CharacterVisual>();
        v.anim = inst.GetComponent<Animator>();
        v.hand = FindDeep(inst.transform, "Fist.R") ?? FindDeep(inst.transform, "Arm.R_end");
        v.head = FindDeep(inst.transform, "Head");
        if (v.head != null && parts != null && parts.scarf != null)
        {
            parts.scarf.transform.SetParent(v.head, true);
            parts.scarf.transform.position = v.head.position - Vector3.up * 0.12f * model.lossyScale.y;
        }
        if (wand && v.hand != null && parts != null && parts.wandTip != null)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(w.GetComponent<Collider>());
            w.name = "Wand";
            w.GetComponent<Renderer>().sharedMaterial = MatLib.Solid(new Color(0.25f, 0.15f, 0.08f));
            w.transform.SetParent(v.hand, true);
            w.transform.rotation = v.hand.rotation;
            w.transform.position = v.hand.position + v.hand.up * 0.17f;
            var ls = v.hand.lossyScale;
            w.transform.localScale = new Vector3(0.03f / ls.x, 0.17f / ls.y, 0.03f / ls.z);
            parts.wandTip.SetParent(v.hand, true);
            parts.wandTip.position = v.hand.position + v.hand.up * 0.36f;
        }
        return v;
    }

    static bool Under(Transform t, Transform root, string name)
    {
        for (; t != null && t != root; t = t.parent)
            if (t.name.StartsWith(name)) return true;
        return false;
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // Вешает объект (шляпу) на голову: точка anchorY внутри объекта встаёт на макушку.
    public void PutOnHead(Transform obj, float anchorY)
    {
        if (head == null) return;
        obj.position = head.position + Vector3.up * (0.32f - anchorY);
        obj.SetParent(head, true);
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var f = FindDeep(c, name);
            if (f != null) return f;
        }
        return null;
    }

    void Update()
    {
        if (hitCd > 0) hitCd -= Time.deltaTime;
    }

    public void SetSpeed(float s) { if (anim != null) anim.SetFloat(SpeedId, s, 0.1f, Time.deltaTime); }
    public void Attack() { if (anim != null) anim.SetTrigger(AttackId); }
    public void Roll() { if (anim != null) anim.SetTrigger(RollId); }
    public void SetDead(bool d) { if (anim != null) anim.SetBool(DeadId, d); }

    public void Hit()
    {
        if (anim == null || hitCd > 0) return;
        hitCd = 0.6f;
        anim.SetTrigger(HitId);
    }
}

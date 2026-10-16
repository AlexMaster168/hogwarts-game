using UnityEngine;

public static class SagaMagic
{
    public static bool Cast(PlayerController p, SpellType spell, Vector3 aim)
    {
        if (spell != SpellType.Lumos && spell != SpellType.Reparo) return false;
        if (spell == SpellType.Lumos && QuestManager.I.SagaActive && QuestManager.I.SagaChapter == 2)
            QuestManager.I.sagaRevealed = true;
        if (spell == SpellType.Lumos)
        {
            var old = p.GetComponentInChildren<WandLantern>();
            if (old != null) Object.Destroy(old.gameObject);
            var lamp = new GameObject("Люмос");
            lamp.transform.SetParent(p.caster.wandTip, false);
            lamp.AddComponent<WandLantern>();
        }
        foreach (var rune in Object.FindObjectsByType<SagaRune>(FindObjectsSortMode.None))
            if (rune.required == spell && rune.order == rune.quest.sagaOrder && (rune.transform.position - p.transform.position).sqrMagnitude < 100f)
                rune.OnSpellHit(new SpellHit { type = spell, source = p.gameObject });
        if (spell == SpellType.Lumos)
            foreach (var foe in Object.FindObjectsByType<SagaEnemy>(FindObjectsSortMode.None))
                if ((foe.transform.position - p.transform.position).sqrMagnitude < 225f && foe.enemy.kind == EnemyKind.MirrorRegent) foe.Reveal();
        FX.Shockwave(p.transform.position, spell == SpellType.Lumos ? Color.yellow : Color.green, 5f);
        SFX.Play("cast", 0.5f);
        return true;
    }
}

public class WandLantern : MonoBehaviour
{
    void Start()
    {
        var light = gameObject.AddComponent<Light>();
        light.range = 12f; light.intensity = 2.5f; light.color = new Color(1f, 0.95f, 0.65f);
        Prims.Sphere(transform, Vector3.zero, 0.15f, MatLib.Glow(light.color), false);
        Destroy(gameObject, 35f);
    }
}

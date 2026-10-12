using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Готовит скачанные модели (Assets/Models/*.fbx|*.glb): generic-риг, копии клипов с зацикливанием,
// AnimatorController и префаб в Resources/Models/<имя> — его подхватывает CharacterVisual в рантайме.
public static class ModelSetup
{
    const string Src = "Assets/Models";
    const string Out = "Assets/Resources/Models";

    [MenuItem("Hogwarts/Setup Models")]
    public static void Run()
    {
        Directory.CreateDirectory(Out);
        Directory.CreateDirectory(Out + "/Clips");
        var files = Directory.GetFiles(Src).Where(f => f.EndsWith(".fbx") || f.EndsWith(".glb")).Select(f => f.Replace('\\', '/')).ToList();
        foreach (var f in files)
        {
            if (f.EndsWith(".fbx")) ConfigureFbx(f); else ConfigureGlb(f);
        }
        AssetDatabase.Refresh();
        foreach (var f in files) BuildPrefab(f);
        AssetDatabase.SaveAssets();
        Debug.Log("HM_MODELS_OK " + files.Count);
    }

    static void ConfigureFbx(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;
        bool changed = imp.animationType != ModelImporterAnimationType.Generic || !imp.importAnimation;
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.importAnimation = true;
        imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
        if (changed) imp.SaveAndReimport();
    }

    static void ConfigureGlb(string path)
    {
        var imp = AssetImporter.GetAtPath(path);
        if (imp == null) { Debug.LogWarning("No importer for " + path); return; }
        var so = new SerializedObject(imp);
        var it = so.GetIterator();
        bool found = false, changed = false;
        while (it.Next(true))
        {
            if (it.name == "animationMethod" && it.propertyType == SerializedPropertyType.Enum)
            {
                found = true;
                if (it.intValue != 2) { it.intValue = 2; changed = true; }   // 2 = Mecanim
            }
        }
        if (!found) Debug.LogWarning("animationMethod not found for " + path);
        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            imp.SaveAndReimport();
        }
    }

    static bool IsLoop(string n)
    {
        n = Short(n).ToLowerInvariant();
        return n.Contains("idle") || n.Contains("walk") || n.Contains("run");
    }

    static string Short(string n)
    {
        int i = n.LastIndexOf('|');
        return i >= 0 ? n.Substring(i + 1) : n;
    }

    static Dictionary<AnimationClip, string> names;

    static AnimationClip Find(List<AnimationClip> clips, params string[] keys)
    {
        foreach (var k in keys)
        {
            var c = clips.FirstOrDefault(x => string.Equals(names[x], k, System.StringComparison.OrdinalIgnoreCase));
            if (c != null) return c;
        }
        return null;
    }

    static void BuildPrefab(string path)
    {
        string role = Path.GetFileNameWithoutExtension(path);
        var main = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
        if (main == null) { Debug.LogWarning("Model not loaded: " + path); return; }
        var src = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();

        // Копируем клипы в отдельные ассеты, чтобы можно было включить зацикливание.
        var clips = new List<AnimationClip>();
        var shortNames = new Dictionary<AnimationClip, string>();
        foreach (var c in src)
        {
            var copy = Object.Instantiate(c);
            copy.name = Short(c.name);
            var st = AnimationUtility.GetAnimationClipSettings(copy);
            st.loopTime = IsLoop(c.name);
            AnimationUtility.SetAnimationClipSettings(copy, st);
            string cp = $"{Out}/Clips/{role}_{copy.name}.anim";
            AssetDatabase.DeleteAsset(cp);
            AssetDatabase.CreateAsset(copy, cp);
            clips.Add(copy);
            shortNames[copy] = Short(c.name);
        }
        Debug.Log($"HM_MODEL {role}: clips = {string.Join(", ", clips.Select(c => shortNames[c]))}");
        names = shortNames;

        var idle = Find(clips, "Idle", "Spider_Idle");
        var walk = Find(clips, "Walk", "Spider_Walk") ?? idle;
        var run = Find(clips, "Run") ?? walk;
        var attack = Find(clips, "Spell1", "Shoot_OneHanded", "Spider_Attack", "Attack", "Punch", "Bite_Front");
        var hit = Find(clips, "RecieveHit", "HitRecieve");
        var death = Find(clips, "Death", "Defeat", "Spider_Death");
        var roll = Find(clips, "Roll");

        string ctrlPath = $"{Out}/{role}.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        var sm = ctrl.layers[0].stateMachine;

        var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 2.2f);
        if (run != null && run != walk) tree.AddChild(run, 6.5f);
        sm.defaultState = loco;

        AddAction(sm, loco, attack, "Attack", 0.85f);
        AddAction(sm, loco, hit, "Hit", 0.8f);
        AddAction(sm, loco, roll, "Roll", 0.8f);
        if (death != null)
        {
            var ds = sm.AddState("Death");
            ds.motion = death;
            var t = sm.AddAnyStateTransition(ds);
            t.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            t.canTransitionToSelf = false;
            t.duration = 0.1f;
            var back = ds.AddTransition(loco);
            back.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            back.duration = 0.2f;
        }

        string prefabPath = $"{Out}/{role}.prefab";
        var inst = (GameObject)Object.Instantiate(main);
        inst.name = role;
        var legacy = inst.GetComponent<Animation>();
        if (legacy != null) Object.DestroyImmediate(legacy);
        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        ApplyTexture(inst, role);
        PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);
    }

    // У RPG-персонажей текстура лежит отдельно — собираем материал с ней.
    static void ApplyTexture(GameObject inst, string role)
    {
        string tex = role == "rpg_wizard" ? "Wizard_Texture" : role == "rpg_rogue" ? "Rogue_Texture" : null;
        if (tex == null) return;
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/Textures/{tex}.png");
        if (t == null) { Debug.LogWarning("Texture not found: " + tex); return; }
        string mp = $"{Out}/{role}_mat.mat";
        AssetDatabase.DeleteAsset(mp);
        var m = new Material(Shader.Find("Standard")) { mainTexture = t };
        m.SetFloat("_Glossiness", 0.15f);
        AssetDatabase.CreateAsset(m, mp);
        foreach (var r in inst.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
        }
    }

    static void AddAction(AnimatorStateMachine sm, AnimatorState loco, AnimationClip clip, string trigger, float exit)
    {
        if (clip == null) return;
        var s = sm.AddState(trigger);
        s.motion = clip;
        var t = sm.AddAnyStateTransition(s);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        t.duration = 0.08f;
        t.canTransitionToSelf = true;
        var back = s.AddTransition(loco);
        back.hasExitTime = true;
        back.exitTime = exit;
        back.duration = 0.15f;
    }
}

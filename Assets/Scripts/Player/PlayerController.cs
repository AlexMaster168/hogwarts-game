using System.Collections.Generic;
using UnityEngine;

// Игрок: ходьба/бег/прыжок/перекат, метла, зелья, получение урона.
public class PlayerController : MonoBehaviour, ISpellTarget
{
    public CameraRig cam;
    public CharacterController cc;
    public Health health;
    public SpellCaster caster;
    public Inventory inventory;
    public FigureParts parts;
    public Transform model;
    public GameObject broom;
    public CharacterVisual visual;

    public bool hasBroom, onBroom;
    public bool ancientUnlocked;
    public float ancient;
    public NPC nearbyNpc;
    public Chest nearbyChest;
    public float lastHurt = -10f;

    public bool Dodging => dodgeT > 0f;
    public Vector3 Velocity => velocity;

    Vector3 velocity, dodgeDir;
    float dodgeT, dodgeCd, animT, stepT;
    const float DodgeTime = 0.38f;

    public static PlayerController Create(Vector3 pos)
    {
        var go = new GameObject("Player");
        go.transform.position = pos;
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.05f;
        var pc = go.AddComponent<PlayerController>();
        pc.cc = cc;
        pc.health = go.AddComponent<Health>();
        pc.health.Init(150);
        pc.health.Died += pc.OnDied;
        pc.model = Prims.Group(go.transform, "Model");
        pc.parts = Figures.Humanoid(pc.model, new Color(0.08f, 0.08f, 0.1f), Figures.Skin, new Color(0.35f, 0.2f, 0.1f), new Color(0.45f, 0.45f, 0.45f));
        pc.robeMat = MatLib.Solid(new Color(0.08f, 0.08f, 0.1f), 0.15f);
        pc.broom = Figures.Broom(pc.model);
        pc.broom.SetActive(false);
        pc.SetGender(Gender.Female, true);
        pc.caster = go.AddComponent<SpellCaster>();
        pc.caster.Init(pc, pc.parts.wandTip);
        pc.inventory = go.AddComponent<Inventory>();
        pc.inventory.pc = pc;
        var prog = go.AddComponent<Progression>();
        prog.pc = pc;
        return pc;
    }

    public void SetHouse(int h)
    {
        var m = MatLib.Solid(Houses.Main[h], 0.2f);
        parts.scarf.sharedMaterial = m;
        parts.trim.sharedMaterial = m;
    }

    public bool female;

    // Мальчик — модель волшебника, девочка — ведьмы. Шарф и кончик палочки переезжают на новую модель.
    public void SetGender(bool f, bool force = false)
    {
        if (!force && f == female && visual != null) return;
        female = f;
        Gender.Female = f;
        if (visual != null)
        {
            parts.scarf.transform.SetParent(model, true);
            parts.wandTip.SetParent(model, true);
            DestroyImmediate(visual.gameObject);
        }
        var rot = model.localRotation;
        var pos = model.localPosition;
        model.localRotation = Quaternion.identity;
        model.localPosition = Vector3.zero;
        visual = CharacterVisual.Attach(model, f ? "witch" : "rpg_wizard", f ? 1.7f : 1.75f, parts, true);
        model.localRotation = rot;
        model.localPosition = pos;
        if (visual != null) visual.broom = broom.transform;
        if (inventory != null) inventory.ApplyVisuals();
    }

    public void SetRobeColor(Color c)
    {
        var m = MatLib.Solid(c, 0.15f);
        foreach (var r in model.GetComponentsInChildren<Renderer>())
            if (r.sharedMaterial == robeMat) r.sharedMaterial = m;
        robeMat = m;
    }

    Material robeMat;

    readonly Dictionary<Renderer, Material[]> ghostBackup = new();

    // Невидимость: тело становится полупрозрачным призраком.
    public void SetGhost(bool on)
    {
        if (on)
        {
            if (ghostBackup.Count > 0) return;
            var gm = MatLib.Fade(new Color(0.6f, 0.75f, 1f, 0.12f), 2f, 0.9f);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                ghostBackup[r] = r.sharedMaterials;
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = gm;
                r.sharedMaterials = arr;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        else
        {
            foreach (var kv in ghostBackup)
                if (kv.Key != null)
                {
                    kv.Key.sharedMaterials = kv.Value;
                    kv.Key.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            ghostBackup.Clear();
        }
    }

    public void Teleport(Vector3 pos)
    {
        cc.enabled = false;
        transform.position = pos;
        // Автосинхронизация трансформов выключена: без этого следующий cc.Move может стартовать со старой позиции.
        Physics.SyncTransforms();
        cc.enabled = true;
        velocity = Vector3.zero;
    }

    void Update()
    {
        if (!GameState.Playing) return;
        float dt = Time.deltaTime;
        if (dodgeCd > 0) dodgeCd -= dt;

        if (Input.GetKeyDown(KeyCode.B)) ToggleBroom();

        if (onBroom) UpdateBroom(dt); else UpdateGround(dt);
        Animate(dt);
        FindNpc();
        if (Input.GetKeyDown(KeyCode.E))
        {
            var door = CastleDoor.Nearby(transform.position);
            if (door != null) door.Use(this);
            else if (QuestManager.I.InteractExtension()) { }
            else if (Activities.TryInteract(this)) { }
            else if (RoomServices.TryUse(this)) { }
            else if (nearbyNpc != null) QuestManager.I.Talk(nearbyNpc);
            else if (nearbyChest != null) nearbyChest.Open(this);
        }

        // Границы мира и страховка от падения под карту.
        var p = transform.position;
        var flat = new Vector2(p.x, p.z);
        if (flat.magnitude > 400f)
        {
            flat = flat.normalized * 400f;
            Teleport(new Vector3(flat.x, p.y, flat.y));
        }
        if (p.y < -10f) Teleport(QuestManager.I.checkpoint + Vector3.up);
    }

    void UpdateGround(float dt)
    {
        var inp = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")), 1f);
        var fwd = cam.FlatForward;
        var right = new Vector3(fwd.z, 0, -fwd.x);
        var move = fwd * inp.z + right * inp.x;
        bool shield = caster.ShieldActive;
        float speed = shield ? 2.5f : Input.GetKey(KeyCode.LeftShift) ? 8.5f : 5f;

        if (Input.GetKeyDown(KeyCode.LeftControl) && dodgeCd <= 0f && cc.isGrounded) StartDodge(move.sqrMagnitude > 0.01f ? move.normalized : -fwd);

        Vector3 horiz;
        if (dodgeT > 0f)
        {
            dodgeT -= dt;
            horiz = dodgeDir * 15f;
        }
        else
        {
            var cur = new Vector3(velocity.x, 0, velocity.z);
            horiz = Vector3.MoveTowards(cur, move * speed, 60f * dt);
        }
        velocity.x = horiz.x;
        velocity.z = horiz.z;
        if (cc.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && dodgeT <= 0f) velocity.y = 7.5f;
        }
        velocity.y -= 22f * dt;
        cc.Move(velocity * dt);

        Vector3 face = caster.IsAiming || shield ? cam.FlatForward : horiz;
        if (caster.IsAiming) face = caster.AimFlatDir;
        if (dodgeT > 0f) face = dodgeDir;
        face.y = 0;
        if (face.sqrMagnitude > 0.05f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized), 14f * dt);
    }

    public void StartDodge(Vector3 dir)
    {
        dodgeT = DodgeTime;
        dodgeCd = 0.75f;
        dodgeDir = dir;
        if (visual != null) visual.Roll();
        SFX.Play("dodge", 0.5f);
    }

    void UpdateBroom(float dt)
    {
        var inp = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        float maxSpeed = (Input.GetKey(KeyCode.LeftShift) ? 32f : 18f) * inventory.BroomMult;
        var dir = cam.Forward * inp.z + cam.Right * inp.x * 0.8f;
        float vert = (Input.GetKey(KeyCode.Space) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftControl) ? 1f : 0f);
        dir += Vector3.up * vert * 0.8f;
        var target = Vector3.ClampMagnitude(dir, 1f) * maxSpeed;
        velocity = Vector3.Lerp(velocity, target, 2.5f * dt);
        if (transform.position.y > 160f && velocity.y > 0) velocity.y = 0;
        cc.Move(velocity * dt);

        var flat = new Vector3(velocity.x, 0, velocity.z);
        var face = flat.sqrMagnitude > 1f ? flat : cam.FlatForward;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized), 5f * dt);
    }

    public void ToggleBroom()
    {
        if (!hasBroom)
        {
            HUD.ShowToast("Метлы пока нет — её выдаст мадам Когава", new Color(0.8f, 0.8f, 0.8f));
            return;
        }
        onBroom = !onBroom;
        broom.SetActive(onBroom);
        if (onBroom) velocity = Vector3.up * 5f + new Vector3(velocity.x, 0, velocity.z);
        else velocity.y = Mathf.Min(velocity.y, 0f);
        SFX.Play("broom", 0.6f);
        FX.Burst(transform.position + Vector3.up, new Color(0.8f, 0.8f, 1f), 20, 3f);
    }

    void Animate(float dt)
    {
        animT += dt;
        float hs = new Vector3(velocity.x, 0, velocity.z).magnitude;
        if (visual != null) visual.SetSpeed(onBroom || !cc.isGrounded ? 0f : hs);
        if (visual != null) visual.riding = onBroom;
        if (onBroom)
        {
            var local = transform.InverseTransformDirection(velocity);
            float pitch = Mathf.Clamp(-local.y * 1.5f + local.z * 0.5f, -30f, 30f);
            float roll = Mathf.Clamp(-local.x * 2f, -35f, 35f);
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(pitch, 0, roll), 6f * dt);
            model.localPosition = Vector3.up * Mathf.Sin(animT * 2f) * 0.08f;
        }
        else if (dodgeT > 0f && visual == null)
        {
            float k = 1f - dodgeT / DodgeTime;
            model.localRotation = Quaternion.Euler(360f * k, 0, 0);
            model.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.5f;
        }
        else
        {
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.identity, 12f * dt);
            float bob = visual == null && cc.isGrounded && hs > 0.5f ? Mathf.Abs(Mathf.Sin(animT * (hs > 6 ? 13f : 9f))) * 0.07f : 0f;
            model.localPosition = Vector3.up * bob;
            if (cc.isGrounded && hs > 0.5f)
            {
                stepT -= dt;
                if (stepT <= 0f)
                {
                    stepT = hs > 6f ? 0.3f : 0.42f;
                    var pos = transform.position;
                    bool stone = (pos.x > -27 && pos.x < 27 && pos.z > -27 && pos.z < 87) || (Mathf.Abs(pos.x) < 5 && pos.z < -25 && pos.z > -101)
                        || Mathf.Abs(pos.z - WorldBuilder.HogsmeadeCenter.z) < 8 && Mathf.Abs(pos.x - WorldBuilder.HogsmeadeCenter.x) < 46;
                    SFX.Play(stone ? "step" : "stepgrass", 0.22f);
                }
            }
            if (parts.armL != null)
                parts.armL.localRotation = Quaternion.Euler(-15f + Mathf.Sin(animT * 9f) * Mathf.Min(1f, hs / 5f) * 25f, 0, 0);
        }
    }

    void FindNpc()
    {
        nearbyNpc = null;
        float best = 3.5f * 3.5f;
        foreach (var n in NPC.All)
        {
            float d = (n.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearbyNpc = n; }
        }
        nearbyChest = null;
        best = 2.8f * 2.8f;
        foreach (var c in Chest.All)
        {
            if (c.opened) continue;
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; nearbyChest = c; }
        }
    }

    public void AddAncient(float v)
    {
        if (!ancientUnlocked) return;
        bool wasFull = ancient >= 100f;
        ancient = Mathf.Min(100f, ancient + v);
        if (!wasFull && ancient >= 100f) HUD.ShowToast("Древняя магия готова! [F]", new Color(0.4f, 0.8f, 1f));
    }

    // Снаряды врагов.
    public void OnSpellHit(SpellHit h)
    {
        if (health.Dead || Dodging) return;
        if (caster.ShieldActive && !h.unblockable)
        {
            caster.OnBlocked();
            return;
        }
        Hurt(h.damage, h.dir * 3f);
    }

    // Ближние атаки и площадные удары. Возвращает true, если удар прошёл.
    public bool ReceiveMelee(float dmg, bool unblockable, Enemy src, Vector3 push)
    {
        if (health.Dead) return false;
        if (Dodging)
        {
            HUD.Float(transform.position + Vector3.up * 2.2f, "Уклонение", new Color(0.8f, 0.9f, 1f));
            return false;
        }
        if (!unblockable && caster.ShieldActive)
        {
            if (caster.InParryWindow && src != null)
            {
                src.Stun(2.5f);
                src.Knock((src.transform.position - transform.position).normalized * 10f);
                caster.OnParry();
            }
            else caster.OnBlocked();
            return false;
        }
        Hurt(dmg, push);
        return true;
    }

    void Hurt(float dmg, Vector3 push)
    {
        if (onBroom) dmg *= 0.5f;
        dmg *= inventory.DefenseMult;
        health.TakeDamage(dmg);
        lastHurt = Time.time;
        if (visual != null) visual.Hit();
        velocity += new Vector3(push.x, Mathf.Max(0, push.y), push.z);
        SFX.Play("hurt", 0.6f);
        cam.Shake(0.2f);
        HUD.Float(transform.position + Vector3.up * 2.2f, "-" + Mathf.RoundToInt(dmg), new Color(1f, 0.35f, 0.3f));
    }

    void OnDied()
    {
        if (onBroom) ToggleBroom();
        if (visual != null) visual.SetDead(true);
        GameState.Set(GameMode.Dead);
    }

    public void Respawn(Vector3 pos)
    {
        Teleport(pos);
        health.Revive();
        if (visual != null) visual.SetDead(false);
        if (inventory.potions["wiggenweld"] < 2) inventory.potions["wiggenweld"] = 2;
        ThreatSense.Clear();
        GameState.Set(GameMode.Play);
    }
}

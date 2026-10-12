using System.Collections.Generic;
using UnityEngine;

public class FigureParts
{
    public Transform root, head, armR, armL, wandTip;
    public Renderer scarf, trim;
    public readonly List<Transform> legs = new();
    public readonly List<float> legYaw = new();
}

public static class Houses
{
    public static readonly string[] Names = { "Гриффиндор", "Слизерин", "Когтевран", "Пуффендуй" };
    public static readonly Color[] Main =
    {
        new(0.62f, 0.08f, 0.08f), new(0.08f, 0.42f, 0.2f), new(0.1f, 0.2f, 0.55f), new(0.9f, 0.72f, 0.12f)
    };
    public static readonly Color[] Second =
    {
        new(0.95f, 0.75f, 0.2f), new(0.75f, 0.78f, 0.8f), new(0.7f, 0.5f, 0.3f), new(0.08f, 0.08f, 0.08f)
    };
}

// Модельки персонажей из примитивов. Модель стоит ногами в (0,0,0) и смотрит в +Z.
public static class Figures
{
    public static readonly Color Skin = new(0.96f, 0.8f, 0.68f);

    public static FigureParts Humanoid(Transform parent, Color robe, Color skin, Color hair, Color accent,
        bool pointyHat = false, Color hatColor = default, Color? eyeGlow = null)
    {
        var f = new FigureParts { root = parent };
        var mRobe = MatLib.Solid(robe, 0.15f);
        var mSkin = MatLib.Solid(skin, 0.3f, 0.3f, 0.1f);
        var mHair = MatLib.Solid(hair, 0.25f);
        var mAcc = MatLib.Solid(accent, 0.2f);

        Prims.Cone(parent, Vector3.zero, 0.48f, 1.15f, mRobe);
        Prims.Make(PrimitiveType.Capsule, parent, new Vector3(0, 1.1f, 0), new Vector3(0.62f, 0.42f, 0.5f), mRobe, false);
        f.trim = Prims.Make(PrimitiveType.Cube, parent, new Vector3(0, 1.05f, 0.235f), new Vector3(0.12f, 0.75f, 0.04f), mAcc, false).GetComponent<Renderer>();
        f.head = Prims.Sphere(parent, new Vector3(0, 1.68f, 0), 0.38f, mSkin).transform;
        Prims.Make(PrimitiveType.Sphere, parent, new Vector3(0, 1.75f, -0.04f), new Vector3(0.41f, 0.32f, 0.41f), mHair, false);
        var eye = eyeGlow.HasValue ? MatLib.Glow(eyeGlow.Value, 3f) : MatLib.Solid(new Color(0.05f, 0.05f, 0.05f), 0.6f);
        Prims.Sphere(parent, new Vector3(-0.075f, 1.7f, 0.17f), 0.055f, eye);
        Prims.Sphere(parent, new Vector3(0.075f, 1.7f, 0.17f), 0.055f, eye);
        f.scarf = Prims.Make(PrimitiveType.Cylinder, parent, new Vector3(0, 1.48f, 0), new Vector3(0.44f, 0.05f, 0.44f), mAcc, false).GetComponent<Renderer>();
        f.armR = Arm(parent, new Vector3(0.33f, 1.42f, 0), mRobe, mSkin, true, out f.wandTip);
        f.armL = Arm(parent, new Vector3(-0.33f, 1.42f, 0), mRobe, mSkin, false, out _);
        if (pointyHat)
        {
            var hm = MatLib.Solid(hatColor, 0.1f);
            Prims.Make(PrimitiveType.Cylinder, parent, new Vector3(0, 1.86f, 0), new Vector3(0.62f, 0.015f, 0.62f), hm, false);
            Prims.Cone(parent, new Vector3(0, 1.86f, 0), 0.24f, 0.62f, hm, false, Quaternion.Euler(-8, 0, 0));
        }
        return f;
    }

    static Transform Arm(Transform parent, Vector3 shoulder, Material robe, Material skin, bool wand, out Transform tip)
    {
        var pivot = Prims.Group(parent, wand ? "ArmR" : "ArmL");
        pivot.localPosition = shoulder;
        pivot.localRotation = Quaternion.Euler(-15, 0, 0);
        Prims.Make(PrimitiveType.Capsule, pivot, new Vector3(0, -0.25f, 0), new Vector3(0.15f, 0.27f, 0.15f), robe, false);
        Prims.Sphere(pivot, new Vector3(0, -0.52f, 0), 0.11f, skin);
        tip = null;
        if (wand)
        {
            Prims.Make(PrimitiveType.Cylinder, pivot, new Vector3(0, -0.7f, 0), new Vector3(0.03f, 0.17f, 0.03f), MatLib.Solid(new Color(0.25f, 0.15f, 0.08f)), false);
            tip = Prims.Group(pivot, "WandTip");
            tip.localPosition = new Vector3(0, -0.88f, 0);
        }
        return pivot;
    }

    public static FigureParts Spider(Transform parent)
    {
        var f = new FigureParts { root = parent };
        var body = MatLib.Solid(new Color(0.12f, 0.09f, 0.08f), 0.3f, 1f, 0.6f);
        var eyes = MatLib.Glow(new Color(1f, 0.1f, 0.05f), 3f);
        Prims.Make(PrimitiveType.Sphere, parent, new Vector3(0, 0.85f, -0.65f), new Vector3(1.1f, 0.9f, 1.3f), body, false);
        f.head = Prims.Make(PrimitiveType.Sphere, parent, new Vector3(0, 0.72f, 0.25f), Vector3.one * 0.7f, body, false).transform;
        Prims.Sphere(parent, new Vector3(-0.1f, 0.84f, 0.57f), 0.09f, eyes);
        Prims.Sphere(parent, new Vector3(0.1f, 0.84f, 0.57f), 0.09f, eyes);
        Prims.Sphere(parent, new Vector3(-0.2f, 0.78f, 0.53f), 0.07f, eyes);
        Prims.Sphere(parent, new Vector3(0.2f, 0.78f, 0.53f), 0.07f, eyes);
        float[] yaws = { -50, -18, 18, 50 };
        foreach (int side in new[] { -1, 1 })
            foreach (float y in yaws)
            {
                var pivot = Prims.Group(parent, "Leg");
                pivot.localPosition = new Vector3(0, 0.7f, 0.15f);
                float yaw = side < 0 ? y : -y;
                pivot.localRotation = Quaternion.Euler(0, yaw, 0);
                Prims.Make(PrimitiveType.Cylinder, pivot, new Vector3(side * 0.65f, -0.35f, 0), new Vector3(0.09f, 0.74f, 0.09f), body, false,
                    Quaternion.Euler(0, 0, side * 62f));
                f.legs.Add(pivot);
                f.legYaw.Add(yaw);
            }
        return f;
    }

    public static FigureParts Troll(Transform parent)
    {
        var f = new FigureParts { root = parent };
        var skin = MatLib.Solid(new Color(0.42f, 0.47f, 0.36f), 0.15f, 0.6f, 0.6f);
        var cloth = MatLib.Solid(new Color(0.35f, 0.25f, 0.15f), 0.05f);
        var eyes = MatLib.Glow(new Color(1f, 0.8f, 0.1f), 2.5f);
        Prims.Make(PrimitiveType.Capsule, parent, new Vector3(-0.6f, 0.8f, 0), new Vector3(0.75f, 0.8f, 0.75f), skin, false);
        Prims.Make(PrimitiveType.Capsule, parent, new Vector3(0.6f, 0.8f, 0), new Vector3(0.75f, 0.8f, 0.75f), skin, false);
        Prims.Make(PrimitiveType.Capsule, parent, new Vector3(0, 2.4f, 0), new Vector3(2.1f, 1.5f, 1.7f), skin, false);
        Prims.Make(PrimitiveType.Cylinder, parent, new Vector3(0, 1.5f, 0), new Vector3(2.0f, 0.35f, 1.7f), cloth, false);
        f.head = Prims.Sphere(parent, new Vector3(0, 4.0f, 0.35f), 1.0f, skin).transform;
        Prims.Sphere(parent, new Vector3(-0.2f, 4.1f, 0.8f), 0.14f, eyes);
        Prims.Sphere(parent, new Vector3(0.2f, 4.1f, 0.8f), 0.14f, eyes);
        f.armL = Prims.Group(parent, "ArmL");
        f.armL.localPosition = new Vector3(-1.35f, 3.2f, 0);
        Prims.Make(PrimitiveType.Capsule, f.armL, new Vector3(0, -1.0f, 0), new Vector3(0.6f, 1.1f, 0.6f), skin, false);
        f.armR = Prims.Group(parent, "ArmR");
        f.armR.localPosition = new Vector3(1.35f, 3.2f, 0);
        Prims.Make(PrimitiveType.Capsule, f.armR, new Vector3(0, -1.0f, 0), new Vector3(0.6f, 1.1f, 0.6f), skin, false);
        Prims.Make(PrimitiveType.Cylinder, f.armR, new Vector3(0, -2.0f, 0.9f), new Vector3(0.45f, 1.3f, 0.45f), MatLib.Solid(new Color(0.3f, 0.2f, 0.1f)), false,
            Quaternion.Euler(70, 0, 0));
        return f;
    }

    // Ранрок: гоблин в багровой мантии с золотом.
    public static FigureParts Goblin(Transform parent)
    {
        var skin = new Color(0.5f, 0.55f, 0.4f);
        var f = Humanoid(parent, new Color(0.35f, 0.05f, 0.06f), skin, new Color(0.2f, 0.2f, 0.2f), new Color(0.9f, 0.7f, 0.2f),
            false, default, new Color(1f, 0.15f, 0.05f));
        var ms = MatLib.Solid(skin, 0.3f, 0.3f, 0.1f);
        Prims.Make(PrimitiveType.Sphere, parent, new Vector3(-0.26f, 1.72f, 0), new Vector3(0.28f, 0.07f, 0.12f), ms, false, Quaternion.Euler(0, 0, -15));
        Prims.Make(PrimitiveType.Sphere, parent, new Vector3(0.26f, 1.72f, 0), new Vector3(0.28f, 0.07f, 0.12f), ms, false, Quaternion.Euler(0, 0, 15));
        Prims.Sphere(parent, new Vector3(0, 1.64f, 0.2f), 0.11f, ms);
        Prims.Make(PrimitiveType.Cylinder, parent, new Vector3(0, 1.88f, 0), new Vector3(0.36f, 0.06f, 0.36f), MatLib.Glow(new Color(0.9f, 0.7f, 0.2f), 0.8f), false);
        return f;
    }

    public static GameObject Broom(Transform parent)
    {
        var g = Prims.Group(parent, "Broom");
        Prims.Make(PrimitiveType.Cylinder, g, new Vector3(0, 0.8f, 0.1f), new Vector3(0.07f, 1.15f, 0.07f), MatLib.Solid(new Color(0.42f, 0.27f, 0.13f), 0.4f), false,
            Quaternion.Euler(90, 0, 0));
        Prims.Cone(g, new Vector3(0, 0.8f, -1.6f), 0.26f, 0.65f, MatLib.Solid(new Color(0.75f, 0.6f, 0.3f)), false, Quaternion.Euler(90, 0, 0));
        return g.gameObject;
    }
}

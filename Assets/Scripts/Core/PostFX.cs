using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Постобработка: свечение заклинаний и свечей, ACES-тонмаппинг, затенение углов и сглаживание.
public static class PostFX
{
    public static PostProcessProfile Profile { get; private set; }
    public static Bloom Bloom { get; private set; }
    public static ColorGrading Grading { get; private set; }
    public static Vignette Vignette { get; private set; }

    public static void Setup(Camera cam)
    {
        var holder = Resources.Load<PostResHolder>("HM_PostRes");
        if (holder == null || holder.resources == null)
        {
            Debug.LogWarning("PostFX: нет HM_PostRes, постобработка выключена");
            return;
        }
        cam.allowHDR = true;
        var layer = cam.gameObject.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
        layer.Init(holder.resources);
        layer.volumeTrigger = cam.transform;
        layer.volumeLayer = ~0;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

        Profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        Bloom = Profile.AddSettings<Bloom>();
        Bloom.intensity.Override(1.6f);
        Bloom.threshold.Override(1.05f);
        Bloom.softKnee.Override(0.55f);
        Bloom.diffusion.Override(7.5f);
        Grading = Profile.AddSettings<ColorGrading>();
        Grading.tonemapper.Override(Tonemapper.ACES);
        Grading.postExposure.Override(0.45f);
        Grading.saturation.Override(10f);
        Grading.contrast.Override(6f);
        Vignette = Profile.AddSettings<Vignette>();
        Vignette.intensity.Override(0.26f);
        Vignette.smoothness.Override(0.4f);
        var ao = Profile.AddSettings<AmbientOcclusion>();
        ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
        ao.intensity.Override(0.55f);

        var go = new GameObject("PostVolume");
        Object.DontDestroyOnLoad(go);
        var vol = go.AddComponent<PostProcessVolume>();
        vol.isGlobal = true;
        vol.priority = 10;
        vol.sharedProfile = Profile;
    }
}

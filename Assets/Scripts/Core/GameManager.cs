using UnityEngine;
using UnityEngine.Rendering;

// Точка входа: при старте любой сцены собирает мир, игрока, камеру, сюжет и интерфейс.
public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public PlayerController player;
    public CameraRig camRig;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindAnyObjectByType<GameManager>() == null) new GameObject("Game").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        GameState.Reset();
        ThreatSense.Clear();
        Dialogue.Reset();
        HUD.Clear();

        SetupRendering();
        WorldBuilder.Build();

        var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.farClipPlane = 1500f;
        cam.nearClipPlane = 0.1f;
        cam.fieldOfView = 62f;
        cam.clearFlags = CameraClearFlags.Skybox;

        player = PlayerController.Create(WorldBuilder.Spawn);
        camRig = camGo.GetComponent<CameraRig>();
        if (camRig == null) camRig = camGo.AddComponent<CameraRig>();
        camRig.target = player.transform;
        player.cam = camRig;

        gameObject.AddComponent<SFX>();
        gameObject.AddComponent<QuestManager>();
        gameObject.AddComponent<Trials>();
        gameObject.AddComponent<SideQuests>();
        gameObject.AddComponent<HUD>();
        gameObject.AddComponent<ShopUI>();
        gameObject.AddComponent<CastleMap>();
        gameObject.AddComponent<RoomServices>();
        gameObject.AddComponent<SaveGame>();
        if (SmokeTest.Requested) gameObject.AddComponent<SmokeTest>();
        GameState.Set(GameMode.Title);
    }

    static void SetupRendering()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.8f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        RenderSettings.sun = sun;
        RenderSettings.skybox = MatLib.Sky();
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.18f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.76f);
        QualitySettings.shadowDistance = 160f;
    }
}

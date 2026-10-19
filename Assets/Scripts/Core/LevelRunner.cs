using UnityEngine;

/// <summary>
/// Level runner for the Game.unity scene. Reads the level index from
/// PlayerPrefs ("CurrentLevel", default 0) and loads it via LevelLoader.
/// </summary>
[RequireComponent(typeof(LevelLoader))]
public class LevelRunner : MonoBehaviour
{
    [Header("Levels in order")]
    public LevelData[] levels;

    /// <summary>Index of the level loaded by Start (0-based).</summary>
    public int CurrentLevelIndex { get; private set; }
    /// <summary>LevelData asset loaded by Start (null until Start ran).</summary>
    public LevelData CurrentLevel { get; private set; }

    void Start()
    {
        SetupJuice();
        SetupGroundVisual();

        // Keep both entry points in sync: when no explicit list is wired on
        // this component, fall back to the shared LevelCatalog resource.
        if (levels == null || levels.Length == 0)
        {
            var catalog = LevelCatalog.Load();
            if (catalog != null) levels = catalog.levels;
        }

        int idx = PlayerPrefs.GetInt("CurrentLevel", 0);
        if (idx < 0 || levels == null || idx >= levels.Length)
            idx = 0;
        CurrentLevelIndex = idx;
        CurrentLevel = levels != null && idx < levels.Length ? levels[idx] : null;

        var loader = GetComponent<LevelLoader>();

        // The Game scene is minimal: make sure a swipe cutter exists before the
        // level loads. RopeCutter.Awake also defaults cutMask when it is zero,
        // but set it explicitly so the object is self-contained.
        if (FindFirstObjectByType<RopeCutter>() == null)
        {
            var cutterGo = new GameObject("RopeCutter");
            var cutter = cutterGo.AddComponent<RopeCutter>();
            cutter.cutMask = LayerMask.GetMask("Rope");
            cutter.Enabled = true;
        }

        if (levels[idx] == null)
        {
            Debug.LogError("[LevelRunner] LevelData asset missing for index " + idx, this);
            return;
        }
        Debug.Log("[LevelRunner] Loading level " + idx + ": " + levels[idx].name);
        loader.Load(levels[idx]);

        // Runtime-built UI: created once here if no scene/persistent one exists.
        if (FindFirstObjectByType<GameUI>() == null)
        {
            var uiGo = new GameObject("GameUI");
            uiGo.AddComponent<GameUI>();
        }
    }

    // ---- presentation setup (visual only, no gameplay logic) ---------------

    void SetupJuice()
    {
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraShake>() == null)
            cam.gameObject.AddComponent<CameraShake>();
        var fx = FindObjectOfType<JuiceFX>();
        if (fx == null)
        {
            var go = new GameObject("~JuiceFX");
            go.transform.SetParent(transform, false);
            go.AddComponent<JuiceFX>();
        }
    }

    void SetupGroundVisual()
    {
        var ground = GameObject.Find("Ground");
        if (ground == null || ground.GetComponentInChildren<SpriteRenderer>() != null) return;

        // Body: dark brown slab, top edge aligned to y = -5.
        var body = new GameObject("GroundVisual");
        body.transform.SetParent(ground.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprites.WhiteSprite();
        sr.color = new Color(0.35f, 0.22f, 0.12f);
        sr.sortingOrder = -5;
        body.transform.position = new Vector3(0f, -5.5f, 0f);
        body.transform.localScale = new Vector3(30f, 1f, 1f);

        // Thin lighter top strip.
        var strip = new GameObject("GroundVisualTop");
        strip.transform.SetParent(ground.transform, false);
        var tsr = strip.AddComponent<SpriteRenderer>();
        tsr.sprite = RuntimeSprites.WhiteSprite();
        tsr.color = new Color(0.48f, 0.32f, 0.18f);
        tsr.sortingOrder = -5;
        strip.transform.position = new Vector3(0f, -5.06f, 0f);
        strip.transform.localScale = new Vector3(30f, 0.12f, 1f);
    }
}

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
}

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

    void Start()
    {
        int idx = PlayerPrefs.GetInt("CurrentLevel", 0);
        if (idx < 0 || levels == null || idx >= levels.Length)
            idx = 0;

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
    }
}

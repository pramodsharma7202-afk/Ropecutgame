using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global game-loop state: Playing -> Won | Lost. Win/Lose are guarded so
/// they can only fire once per level and only while Playing. Survives scene
/// loads (DontDestroyOnLoad); state resets to Playing whenever a scene loads
/// (OnLevelLoaded fires). Also watches the hero for out-of-bounds losses.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    private static GameManager instance;
    public static GameManager Instance => instance;

    [Header("Out of bounds")]
    [Tooltip("Hero below this y is out of bounds (lost).")]
    [SerializeField] private float heroKillY = -8f;
    [Tooltip("|Hero x| beyond this is out of bounds (lost).")]
    [SerializeField] private float heroAbsKillX = 8f;

    [Header("Level progression (scene paths, in order)")]
    [SerializeField] private string[] levelScenes;

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>Number of successful state transitions since app start (diagnostics).</summary>
    public int WinCount { get; private set; }
    /// <summary>Number of successful state transitions since app start (diagnostics).</summary>
    public int LoseCount { get; private set; }

    /// <summary>Raised once, when the level is won.</summary>
    public event Action OnWon;
    /// <summary>Raised once, when the level is lost.</summary>
    public event Action OnLost;
    /// <summary>Raised whenever a scene finishes loading (state reset to Playing).</summary>
    public event Action OnLevelLoaded;

    private Transform hero;
    private float heroRefindTimer;
    private int currentLevelIndex;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hero = null;
        State = GameState.Playing;
        Debug.Log("[GameManager] Level loaded " + scene.path + " state=Playing");
        OnLevelLoaded?.Invoke();
    }

    /// <summary>Mark the level won. Only the first call while Playing does anything.</summary>
    public bool Win()
    {
        if (State != GameState.Playing) return false;
        State = GameState.Won;
        WinCount++;
        Debug.Log("[GameManager] Win #" + WinCount + " state=Won");
        OnWon?.Invoke();
        return true;
    }

    /// <summary>Mark the level lost. Only the first call while Playing does anything.</summary>
    public bool Lose()
    {
        if (State != GameState.Playing) return false;
        State = GameState.Lost;
        LoseCount++;
        Debug.Log("[GameManager] Lose #" + LoseCount + " state=Lost");
        OnLost?.Invoke();
        return true;
    }

    /// <summary>Reload the current level scene.</summary>
    public void Restart()
    {
        var active = SceneManager.GetActiveScene();
        Debug.Log("[GameManager] Restart -> " + active.path);
        SceneManager.LoadScene(active.path);
    }

    /// <summary>Advance to the next level in levelScenes, if any.</summary>
    public void NextLevel()
    {
        if (levelScenes != null && currentLevelIndex + 1 < levelScenes.Length)
        {
            currentLevelIndex++;
            Debug.Log("[GameManager] NextLevel -> " + levelScenes[currentLevelIndex]);
            SceneManager.LoadScene(levelScenes[currentLevelIndex]);
        }
        else
        {
            Debug.Log("[GameManager] NextLevel: no more levels");
        }
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        TrackHero();
        if (hero == null) return;

        var p = hero.position;
        if (p.y < heroKillY || Mathf.Abs(p.x) > heroAbsKillX)
        {
            Debug.Log("[GameManager] Hero out of bounds at " + p.ToString("F1"));
            Lose();
        }
    }

    /// <summary>Lazily re-find the hero (scenes reload, heroes respawn).</summary>
    private void TrackHero()
    {
        if (hero != null && hero.gameObject.activeInHierarchy) return;
        heroRefindTimer -= Time.unscaledDeltaTime;
        if (heroRefindTimer > 0f) return;
        heroRefindTimer = 0.5f;
        var go = GameObject.FindWithTag("Hero");
        hero = go != null ? go.transform : null;
    }
}

using UnityEngine;

/// <summary>
/// Runs once before the first scene: makes sure the global AudioManager
/// prefab exists and the GameManager singleton is present, so every
/// scene (Boot, Menu, Game) has audio and game-loop state available.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        if (AudioManager.Instance == null)
        {
            var prefab = Resources.Load<AudioManager>("AudioManager");
            if (prefab != null)
                Object.Instantiate(prefab); // its Awake sets Instance and DontDestroyOnLoad
        }

        GameManager.EnsureInstance();
    }
}

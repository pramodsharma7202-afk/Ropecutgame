using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Quick-play menu: RopeCut/Play Level 1..5. Each entry stores the level index
/// in PlayerPrefs ("CurrentLevel", zero-based) and starts Play mode in the
/// Game scene, where LevelRunner loads the selected level.
/// </summary>
public static class PlayLevelMenu
{
    const string GameScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("RopeCut/Play Level 1")]
    static void PlayLevel1() => Play(0);

    [MenuItem("RopeCut/Play Level 2")]
    static void PlayLevel2() => Play(1);

    [MenuItem("RopeCut/Play Level 3")]
    static void PlayLevel3() => Play(2);

    [MenuItem("RopeCut/Play Level 4")]
    static void PlayLevel4() => Play(3);

    [MenuItem("RopeCut/Play Level 5")]
    static void PlayLevel5() => Play(4);

    static void Play(int index)
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        PlayerPrefs.SetInt("CurrentLevel", index);
        PlayerPrefs.Save();

        EditorSceneManager.OpenScene(GameScenePath);
        EditorApplication.isPlaying = true;
    }
}

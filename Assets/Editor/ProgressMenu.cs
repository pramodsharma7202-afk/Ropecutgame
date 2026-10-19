using UnityEditor;
using UnityEngine;

/// <summary>
/// Test helpers for progress PlayerPrefs.
/// </summary>
public static class ProgressMenu
{
    [MenuItem("RopeCut/Reset Progress")]
    static void ResetProgress()
    {
        for (int i = 0; i < 32; i++)
            PlayerPrefs.DeleteKey("Stars_" + i);
        PlayerPrefs.DeleteKey("Unlocked");
        PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.Save();
        Debug.Log("[ProgressMenu] progress reset (Unlocked, CurrentLevel, Stars_*)");
    }

    [MenuItem("RopeCut/Unlock All Levels")]
    static void UnlockAll()
    {
        PlayerPrefs.SetInt("Unlocked", 4);
        PlayerPrefs.Save();
        Debug.Log("[ProgressMenu] Unlocked set to 4");
    }
}

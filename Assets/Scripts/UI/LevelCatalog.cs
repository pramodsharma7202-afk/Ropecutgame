using UnityEngine;

/// <summary>
/// Central list of levels so menus can work without the Game scene.
/// The asset lives at Resources/LevelCatalog.asset.
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "RopeCut/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [Tooltip("All playable levels, in play order.")]
    public LevelData[] levels = new LevelData[0];

    /// <summary>Loads the catalog from Resources; caching done per call site.</summary>
    public static LevelCatalog Load()
    {
        return Resources.Load<LevelCatalog>("LevelCatalog");
    }
}

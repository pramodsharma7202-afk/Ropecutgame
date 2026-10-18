using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

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

    /// <summary>Called by RopeCutGoal when the hero reaches the door.</summary>
    public void OnLevelComplete()
    {
        // TODO scene progression / star rating
        Debug.Log("[GameManager] Level complete");
    }
}
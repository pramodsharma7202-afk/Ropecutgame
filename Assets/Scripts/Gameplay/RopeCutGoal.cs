using UnityEngine;

/// <summary>Level completion trigger (door/goal zone).</summary>
public class RopeCutGoal : MonoBehaviour
{
    public string requiredTag = "Hero";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(requiredTag))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("Win");
            GameManager.Instance?.OnLevelComplete();
        }
    }
}

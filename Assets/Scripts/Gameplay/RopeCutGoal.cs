using UnityEngine;

/// <summary>
/// Level completion trigger (door/goal zone). When the hero enters, the
/// level is won: GameManager.Win() (guarded, once), the "Win" sound plays,
/// and swipe cutting is disabled.
/// </summary>
public class RopeCutGoal : MonoBehaviour
{
    public string requiredTag = "Hero";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(requiredTag)) return;
        if (GameManager.Instance == null || !GameManager.Instance.Win()) return;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("Win");

        var cutter = FindFirstObjectByType<RopeCutter>();
        if (cutter != null) cutter.Enabled = false;
    }
}

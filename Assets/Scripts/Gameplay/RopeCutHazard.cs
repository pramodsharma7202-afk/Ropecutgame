using UnityEngine;

/// <summary>
/// Deadly hazard (spikes, fire, water). When the hero touches it (trigger
/// or solid contact) the level is lost: GameManager.Lose() (guarded, once)
/// and the "Lose" sound play exactly once.
/// </summary>
public class RopeCutHazard : MonoBehaviour
{
    public const string Tag = "Hazard";
    public float damage = 999f;

    [Tooltip("Tag of the object that dies on contact.")]
    public string killTag = "Hero";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(killTag)) Kill();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider != null && other.collider.CompareTag(killTag)) Kill();
    }

    private void Kill()
    {
        if (GameManager.Instance == null || !GameManager.Instance.Lose()) return;
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Lose");
    }
}

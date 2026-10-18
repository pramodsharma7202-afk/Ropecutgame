using System.Collections;
using UnityEngine;

/// <summary>
/// Hero win/lose feedback. If happy/scared sprite variants are assigned
/// (e.g. from Assets/Art/Characters) the sprite is swapped; otherwise the
/// hero scale-bounces 1 -> 1.15 -> 1 over 0.3 s on win (and squashes on lose).
/// </summary>
public class HeroReaction : MonoBehaviour
{
    [Header("Optional sprite variants")]
    [Tooltip("Swapped in on win, if provided.")]
    public Sprite happySprite;
    [Tooltip("Swapped in on lose, if provided.")]
    public Sprite scaredSprite;

    [Header("Bounce (win)")]
    public float bounceScale = 1.15f;
    public float bounceTime = 0.3f;

    [Header("Squash (lose)")]
    public float squashScale = 0.85f;
    public float squashTime = 0.2f;

    private SpriteRenderer visual;
    private Sprite baseSprite;
    private Coroutine running;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        if (visual != null) baseSprite = visual.sprite;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWon += OnWon;
            GameManager.Instance.OnLost += OnLost;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWon -= OnWon;
            GameManager.Instance.OnLost -= OnLost;
        }
    }

    private void OnWon()
    {
        if (visual != null && happySprite != null) visual.sprite = happySprite;
        RunPulse(bounceScale, bounceTime);
    }

    private void OnLost()
    {
        if (visual != null && scaredSprite != null) visual.sprite = scaredSprite;
        RunPulse(squashScale, squashTime);
    }

    private void RunPulse(float target, float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Pulse(target, duration));
    }

    /// <summary>Scale eases 1 -> target -> 1 over duration (half-sine).</summary>
    private IEnumerator Pulse(float target, float duration)
    {
        var tr = transform;
        Vector3 baseScale = tr.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = 1f + (target - 1f) * Mathf.Sin(k * Mathf.PI);
            tr.localScale = baseScale * s;
            yield return null;
        }
        tr.localScale = baseScale;
        running = null;
    }
}

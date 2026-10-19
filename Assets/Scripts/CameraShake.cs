using UnityEngine;

/// <summary>
/// Small camera shake: temporarily offsets the camera in LateUpdate and
/// restores its original position when the shake decays. Triggered by
/// rope cuts and losses via CameraShake.Trigger().
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    Vector3 basePos;
    float shakeLeft;
    float shakeDuration;
    float shakeMagnitude;

    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeLeft = duration;
        shakeMagnitude = magnitude;
    }

    /// <summary>Convenience hook from anywhere (safe to call when absent).</summary>
    public static void Trigger(float duration, float magnitude)
    {
        if (Instance != null) Instance.Shake(duration, magnitude);
    }

    void LateUpdate()
    {
        if (shakeLeft > 0f)
        {
            shakeLeft -= Time.deltaTime;
            if (shakeLeft <= 0f)
            {
                transform.localPosition = basePos;
                return;
            }
            float decay = shakeLeft / shakeDuration;
            var offset = Random.insideUnitCircle * (shakeMagnitude * decay);
            transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
        }
    }

    void OnDisable()
    {
        transform.localPosition = basePos;
    }
}

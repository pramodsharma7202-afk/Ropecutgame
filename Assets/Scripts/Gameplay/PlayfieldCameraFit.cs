using UnityEngine;

/// <summary>
/// Keeps an orthographic camera fitted to the playfield on any screen
/// aspect: the visible width never drops below referenceWidth world units
/// (portrait phones are the reference). Re-applies on every level load.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PlayfieldCameraFit : MonoBehaviour
{
    [Tooltip("Playfield width in world units that must always be visible.")]
    public float referenceWidth = 10f;

    [Tooltip("Minimum orthographic size (half height), keeps the view sensible in landscape/editors.")]
    public float minOrthoSize = 6f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
        if (GameManager.Instance != null) GameManager.Instance.OnLevelLoaded += Apply;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnLevelLoaded -= Apply;
    }

    public void Apply()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (!cam.orthographic) cam.orthographic = true;

        float aspect = cam.aspect;
        if (aspect <= 0.01f) aspect = 1080f / 1920f; // portrait reference before first frame
        cam.orthographicSize = Mathf.Max(minOrthoSize, referenceWidth / (2f * aspect));
    }
}

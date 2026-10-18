using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Swipe cutting. Uses the Input System (Pointer.current covers both the
/// Mouse and Touchscreen): every frame while the pointer is pressed, the
/// movement from the previous to the current pointer position is converted
/// to world space and swept with Physics2D.LayerMask "Rope" linecasts,
/// subdivided in steps of at most 0.15 world units so fast swipes cannot
/// skip a rope segment. Only the first hit per frame per rope is cut.
/// </summary>
public class RopeCutter : MonoBehaviour
{
    [Header("Cutting")]
    [Tooltip("Disable to stop swipe cutting (e.g. after win/lose).")]
    public bool Enabled = true;

    [Tooltip("Layers that can be cut.")]
    public LayerMask cutMask; // set to Rope on the component in the scene

    [Tooltip("Max length of one sub-step, so fast swipes cannot skip segments.")]
    public float maxStepLength = 0.15f;

    [Header("Finger trail")]
    public Color trailColor = Color.white;
    public float trailWidth = 0.15f;
    public float trailTime = 0.2f;
    public int trailSortingOrder = 30;

    TrailRenderer trail;
    Camera cam;
    Vector2 lastWorld;
    bool wasPressed;

    void Awake()
    {
        // Physics queries must hit trigger colliders (the rope capsules are
        // triggers). Queries ignore the layer collision matrix, so cutting
        // works even though Rope/Default physical collisions are disabled.
        if (!Physics2D.queriesHitTriggers)
            Debug.LogWarning("[RopeCutter] Physics2D.queriesHitTriggers was false - forcing it on!", this);
        Physics2D.queriesHitTriggers = true;

        if (cutMask.value == 0)
            cutMask = LayerMask.GetMask("Rope");

        EnsureTrail();
    }

    void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) { SetTrailEmitting(false); wasPressed = false; return; }

        bool pressed = Enabled && pointer.press.isPressed;
        if (pressed)
        {
            cam = cam != null ? cam : Camera.main;
            if (cam == null) return;

            Vector2 screen = pointer.position.ReadValue();
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

            if (!wasPressed)
            {
                SetTrailEmitting(true);
                trail.transform.position = world; // avoid a streak from the old start point
                lastWorld = world;
            }
            else
            {
                if ((world - lastWorld).sqrMagnitude > 0.0001f)
                    CutAt(lastWorld, world);
                lastWorld = world;
            }
        }

        if (!pressed || !Enabled) SetTrailEmitting(false);
        wasPressed = pressed;
    }

    /// <summary>
    /// Sweeps the line a→b and cuts the first RopeSegment hit per rope.
    /// Also used by tests to simulate a swipe.
    /// </summary>
    public int CutAt(Vector2 a, Vector2 b)
    {
        var delta = b - a;
        float dist = delta.magnitude;
        if (dist < 0.0001f) return 0;
        var dir = delta / dist;

        // Subdivide so fast pointer motion cannot skip over a capsule.
        int steps = Mathf.Max(1, Mathf.CeilToInt(dist / maxStepLength));

        var cutsThisFrame = 0;
        var ropesCut = new HashSet<Rope>();
        for (int i = 0; i < steps; i++)
        {
            var start = a + dir * (dist * i / steps);
            var end = a + dir * (dist * (i + 1) / steps);
            var hits = Physics2D.LinecastAll(start, end, cutMask);
            foreach (var hit in hits)
            {
                var seg = hit.collider != null ? hit.collider.GetComponent<RopeSegment>() : null;
                if (seg == null || seg.rope == null || ropesCut.Contains(seg.rope)) continue;
                ropesCut.Add(seg.rope); // only the first hit per frame per rope
                seg.Cut();
                cutsThisFrame++;
            }
        }
        return cutsThisFrame;
    }

    void SetTrailEmitting(bool on)
    {
        if (trail == null) EnsureTrail();
        if (trail != null) trail.emitting = on;
    }

    void EnsureTrail()
    {
        if (trail != null) return;
        var go = new GameObject("FingerTrail");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.sortingOrder = trailSortingOrder;
        trail.numCapVertices = 2;

        var curve = new AnimationCurve(new Keyframe(0f, trailWidth), new Keyframe(1f, 0f));
        trail.widthCurve = curve;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = grad;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        trail.material = new Material(shader);
    }
}

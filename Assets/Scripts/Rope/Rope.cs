using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chain-based rope: N RopeSegment links between a static anchor and a target Rigidbody2D.
/// Every segment renders itself (sprite stretched/rotated), so cutting one link makes
/// the rope split with zero extra rendering code. No LineRenderer.
/// </summary>
[DisallowMultipleComponent]
public class Rope : MonoBehaviour
{
    [Header("Wiring")]
    public Transform anchor;
    public Rigidbody2D target;

    [Header("Layout")]
    public int segmentCount = 12;
    public float segmentLength = 0.3f;
    public float thickness = 0.12f;
    public Color ropeColor = new Color(0.75f, 0.6f, 0.25f);

    [Header("Prefabs")]
    public GameObject segmentPrefab;

    [Tooltip("If true the rope builds itself in Start(). Disable when building/laying the rope manually.")]
    public bool buildOnStart = true;

    public event Action<RopeSegment> OnCut;

    readonly List<RopeSegment> segments = new List<RopeSegment>();

    /// <summary>All links, from anchor-side to target-side.</summary>
    public IReadOnlyList<RopeSegment> Segments => segments;

    Rigidbody2D anchorBody;

    void Start()
    {
        if (buildOnStart && segments.Count == 0) Build();
    }

    void OnDestroy()
    {
        OnCut = null;
    }

    /// <summary>Creates the full chain between anchor and target.</summary>
    public void Build()
    {
        Clear();
        if (segmentPrefab == null) { Debug.LogError("[Rope] segmentPrefab not assigned"); return; }
        if (anchor == null || target == null) { Debug.LogError("[Rope] anchor/target not assigned"); return; }

        // The rope ties to the hero's UPPER body at its local (0, 0.4) point
        // (see the tail joint below), so the chain must span anchor -> tie
        // point. Spanning anchor -> centre instead leaves 0.4 of slack and the
        // tail hinge stays permanently violated (solver limbo: hovering hero).
        Vector2 a = anchor.position;
        Vector2 tie = target.transform.TransformPoint(new Vector3(0f, 0.4f, 0f));
        float dist = Vector2.Distance(a, tie);
        if (dist < segmentLength * 0.5f) { Debug.LogError("[Rope] target too close to anchor", this); return; }

        // Effective per-link rest length; total chain length == distance so the
        // rope reads taut at first frame and hangs without visible stretching.
        float spacing = dist / segmentCount;
        Vector2 dir = (tie - a) / dist;

        // First joint connects to a static, immovable anchor body.
        anchorBody = anchor.GetComponent<Rigidbody2D>();
        if (anchorBody == null) anchorBody = anchor.gameObject.AddComponent<Rigidbody2D>();
        anchorBody.bodyType = RigidbodyType2D.Static;

        RopeSegment prev = null;
        for (int i = 0; i < segmentCount; i++)
        {
            var go = (GameObject)Instantiate(segmentPrefab, a + dir * (spacing * (i + 0.5f)), Quaternion.FromToRotation(Vector3.up, dir));
            go.name = segmentPrefab.name + "_" + i;
            go.transform.SetParent(transform, true);

            var seg = go.GetComponent<RopeSegment>();
            seg.index = i;
            seg.rope = this;
            segments.Add(seg);

            Stretch(seg, spacing);
            // EVERY segment keeps its own hinge to the one above it.
            ConfigureJoint(seg, i == 0 ? anchorBody : prev.body, i == 0 ? a : (Vector2?)null);
            prev = seg;
        }

        // Attach the tail with a SECOND hinge on the last segment; its original
        // hinge stays connected to the previous segment.
        var last = segments[segments.Count - 1];
        var tail = last.gameObject.AddComponent<HingeJoint2D>();
        tail.useLimits = false;
        tail.enableCollision = false;
        tail.autoConfigureConnectedAnchor = false;
        tail.connectedBody = target;
        // Anchor on the last link's target-side end, tied to the hero's upper body.
        tail.anchor = new Vector2(0f, last.halfLocalY);
        tail.connectedAnchor = new Vector2(0f, 0.4f);
        last.tailJoint = tail;
    }

    /// <summary>Stretches/rotates one segment piece so the chain reads as a continuous rope.</summary>
    void Stretch(RopeSegment seg, float spacing)
    {
        var sprite = seg.visual.sprite;
        if (sprite == null) { Debug.LogError("[Rope] RopeSegment prefab has no sprite", seg); return; }

        Vector2 pieceSize = sprite.bounds.size; // world units at scale 1
        seg.transform.localScale = new Vector3(thickness / pieceSize.x, spacing / pieceSize.y, 1f);
        seg.halfLocalY = pieceSize.y * 0.5f;

        seg.visual.color = ropeColor;

        // Capsule spans the piece in local units; world size = thickness x spacing.
        seg.capsule.direction = CapsuleDirection2D.Vertical;
        seg.capsule.size = new Vector2(pieceSize.x, pieceSize.y);
        seg.capsule.offset = Vector2.zero;
    }

    void ConfigureJoint(RopeSegment seg, Rigidbody2D connected, Vector2? worldAnchor)
    {
        var hj = seg.GetComponent<HingeJoint2D>(); // the ORIGINAL hinge = upper joint
        hj.enableCollision = false;
        hj.autoConfigureConnectedAnchor = false;
        hj.useLimits = false;
        seg.upperJoint = hj;

        hj.connectedBody = connected;
        // Local +Y points toward the target (along dir), so the joint that
        // attaches this piece to the one ABOVE it sits at local -Y...
        hj.anchor = new Vector2(0f, -seg.halfLocalY);

        if (worldAnchor.HasValue)
            hj.connectedAnchor = connected.transform.InverseTransformPoint(worldAnchor.Value);
        else
            // ...and grabs the piece above at ITS target-side end (+Y).
            hj.connectedAnchor = new Vector2(0f, seg.halfLocalY);
    }

    /// <summary>Destroys every built link (used by Build before re-laying the chain).</summary>
    public void Clear()
    {
        foreach (var s in segments)
            if (s != null) Destroy(s.gameObject);
        segments.Clear();
    }

    /// <summary>Called by RopeSegment.Cut — raises OnCut for level logic/UX.</summary>
    public void NotifyCut(RopeSegment seg) => OnCut?.Invoke(seg);
}

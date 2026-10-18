using UnityEngine;

/// <summary>
/// One link in the rope chain. Holds the hinge to the segment above it
/// (upperJoint). The LAST link additionally holds a second hinge (tailJoint)
/// that attaches it to the rope's target body — see Rope.Build.
/// </summary>
public class RopeSegment : MonoBehaviour
{
    [HideInInspector] public int index;
    [HideInInspector] public Rope rope;

    [HideInInspector] public Rigidbody2D body;
    [HideInInspector] public CapsuleCollider2D capsule;
    [HideInInspector] public SpriteRenderer visual;

    /// <summary>Hinge attaching this piece to the piece ABOVE it (or the anchor).</summary>
    [HideInInspector] public HingeJoint2D upperJoint;

    /// <summary>ONLY set on the last segment: its second hinge to the target body.</summary>
    [HideInInspector] public HingeJoint2D tailJoint;

    /// <summary>Local-space half length of the sprite piece (set by Rope.Stretch).</summary>
    [HideInInspector] public float halfLocalY;

    [Header("Effects")]
    [SerializeField] ParticleSystem sparkBurst;

    bool cut;

    public bool IsCut => cut;

    void Awake()
    {
        CacheComponents();
    }

    public void CacheComponents()
    {
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        visual = GetComponent<SpriteRenderer>();

        var joints = GetComponents<HingeJoint2D>();
        upperJoint = joints.Length > 0 ? joints[0] : null;
        tailJoint = joints.Length > 1 ? joints[1] : null; // last segment only
    }

    /// <summary>
    /// Sever this segment from the piece above it. Safe to call twice:
    /// the second call is a no-op. Only the UPPER joint is destroyed, so the
    /// lower chain (and the last link's tailJoint to the hero) stays intact.
    /// </summary>
    public void Cut()
    {
        if (cut) return;
        cut = true;

        if (upperJoint != null)
        {
            Destroy(upperJoint);
            upperJoint = null;
        }

        if (sparkBurst != null)
        {
            var fx = Instantiate(sparkBurst, transform.position, Quaternion.identity);
            fx.gameObject.tag = "Untagged";
        }

        if (AudioManager.Instance != null) AudioManager.Instance.Play("Cut");

        if (rope != null) rope.NotifyCut(this);
    }

    /// <summary>Editor-time hook so RopeSegment always has fresh component refs when rebuilt by Rope.</summary>
    public void ForceComponentRefresh() => CacheComponents();
}

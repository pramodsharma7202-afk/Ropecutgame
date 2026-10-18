using UnityEngine;

/// <summary>
/// One link in the rope chain. Owns its hinge to the segment above it,
/// triggers a spark + cut sound when severed, and informs its Rope.
/// </summary>
public class RopeSegment : MonoBehaviour
{
    [HideInInspector] public int index;
    [HideInInspector] public Rope rope;

    [HideInInspector] public Rigidbody2D body;
    [HideInInspector] public CapsuleCollider2D capsule;
    [HideInInspector] public HingeJoint2D joint;
    [HideInInspector] public SpriteRenderer visual;

    /// <summary>Local-space half length of the sprite piece (set by Rope.StretchVisual).</summary>
    [HideInInspector] public float halfLocalY;

    [Header("Effects")]
    [SerializeField] ParticleSystem sparkBurst;

    bool cut;

    public bool IsCut => cut;

    void Awake()
    {
        CacheComponents();
    }

    void CacheComponents()
    {
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        joint = GetComponent<HingeJoint2D>();
        visual = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Sever this segment from the piece above it. Safe to call twice:
    /// the second call is a no-op.
    /// </summary>
    public void Cut()
    {
        if (cut) return;
        cut = true;

        // Destroying the hinge freezes this segment onto whatever is BELOW it,
        // so the rope splits at exactly this link. No rendering code needed:
        // every piece draws itself at its physics position.
        if (joint != null) Destroy(joint);

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

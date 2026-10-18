using UnityEngine;

/// <summary>
/// One-shot spark burst played by RopeSegment.Cut.
/// The ParticleSystem handles its own visuals; this only guarantees cleanup.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SparkBurst : MonoBehaviour
{
    [SerializeField] float lifetime = 1.5f;

    void Awake()
    {
        var ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }
}

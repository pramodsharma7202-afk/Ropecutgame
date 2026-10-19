using UnityEngine;

/// <summary>
/// Bobbing arrow above the Door so the player can spot the goal.
/// Component is added at runtime by LevelLoader.
/// </summary>
public class GoalArrow : MonoBehaviour
{
    public float amplitude = 0.15f;

    Vector3 basePos;

    void Start()
    {
        basePos = transform.localPosition;
    }

    void Update()
    {
        float y = basePos.y + Mathf.Sin(Time.time * 3f) * amplitude;
        transform.localPosition = new Vector3(basePos.x, y, basePos.z);
    }

    /// <summary>Creates the arrow sprite above the given goal transform.</summary>
    public static void AttachTo(Transform goal)
    {
        if (goal == null || goal.Find("~GoalArrow") != null) return;
        var go = new GameObject("~GoalArrow");
        go.transform.SetParent(goal, false);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprites.TriangleSprite();
        sr.color = new Color(1f, 0.85f, 0.2f);
        sr.sortingOrder = 20;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        go.AddComponent<GoalArrow>();
    }
}

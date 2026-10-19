using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads a LevelData asset into the current scene: hero, anchors, ropes,
/// static objects, and goal. Everything is parented under a single "LevelRoot"
/// so repeated loads never leave leftovers.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject heroPrefab;
    public GameObject anchorPrefab;
    public GameObject ropeSegmentPrefab;
    public GameObject boxPrefab;
    public GameObject spikesPrefab;
    public GameObject firePrefab;
    public GameObject waterPrefab;
    public GameObject doorPrefab;
    public GameObject pinPrefab;

    GameObject levelRoot;
    GameObject hero;
    Rigidbody2D heroBody;

    /// <summary>Builds the whole level from `level` and clears any previous load.</summary>
    public void Load(LevelData level)
    {
        Clear();

        // Extra safety: destroy any stray Hero or LevelRoot left from a prior load.
        DestroyStray("Hero");
        DestroyStray("LevelRoot");

        // Create the single root that holds everything this level creates.
        levelRoot = new GameObject("LevelRoot");
        levelRoot.transform.SetParent(transform, false);

        BuildBackdrop(level);

        // Hero first so ropes can target it.
        hero = Instantiate(heroPrefab, level.heroStart, Quaternion.identity, levelRoot.transform);
        hero.name = "Hero";
        hero.tag = "Hero";
        heroBody = hero.GetComponent<Rigidbody2D>();
        if (heroBody == null) heroBody = hero.AddComponent<Rigidbody2D>();
        heroBody.mass = 1.5f;
        heroBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var reaction = hero.GetComponent<HeroReaction>();
        if (reaction == null) reaction = hero.AddComponent<HeroReaction>();

        // Anchors + ropes (each rope targets the hero).
        foreach (var ropeDef in level.ropes)
        {
            var anchorGo = Instantiate(anchorPrefab, ropeDef.anchorPos, Quaternion.identity, levelRoot.transform);
            anchorGo.name = "Anchor_" + ropeDef.anchorPos.ToString("F1");
            var anchorBody = anchorGo.GetComponent<Rigidbody2D>();
            if (anchorBody == null) anchorBody = anchorGo.AddComponent<Rigidbody2D>();
            anchorBody.bodyType = RigidbodyType2D.Static;

            var ropeGo = new GameObject("Rope");
            ropeGo.transform.SetParent(levelRoot.transform, true);
            var rope = ropeGo.AddComponent<Rope>();
            rope.anchor = anchorGo.transform;
            rope.target = heroBody;
            rope.segmentCount = ropeDef.segmentCount;
            rope.segmentLength = 0.3f;
            rope.thickness = 0.14f;
            rope.ropeColor = new Color(0.55f, 0.38f, 0.2f);
            rope.segmentPrefab = ropeSegmentPrefab;
            rope.Build();

            // Juice: shake on cut + knot sprites (visual only, no physics).
            rope.OnCut += _ => CameraShake.Trigger(0.15f, 0.12f);
            AddKnot(anchorGo.transform, 0.38f);
            AddKnot(heroBody.transform, 0.3f);
        }

        // Static objects (box, spikes, fire, water, pin).
        foreach (var objDef in level.objects)
        {
            var prefab = PrefabFor(objDef.prefabType);
            if (prefab == null)
            {
                Debug.LogWarning("[LevelLoader] missing prefab for " + objDef.prefabType, this);
                continue;
            }
            var go = Instantiate(prefab, objDef.position, Quaternion.Euler(objDef.rotation), levelRoot.transform);
            go.name = objDef.prefabType + "_" + level.objects.IndexOf(objDef);
            go.transform.localScale = objDef.scale;
            if (objDef.isStatic)
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null) rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
            }
        }

        // Goal (door) - already carries RopeCutGoal which calls GameManager.Win().
        var goalGo = Instantiate(doorPrefab, level.goalPos, Quaternion.identity, levelRoot.transform);
        goalGo.name = "Goal";
        GoalArrow.AttachTo(goalGo.transform);

        GameManager.EnsureInstance();
        GameManager.Instance.ResetForLevel();

        // The goal disables swipe cutting after a win; re-arm it for every level.
        var cutter = FindFirstObjectByType<RopeCutter>();
        if (cutter != null) cutter.Enabled = true;

        Debug.Log("[LevelLoader] loaded hero=" + level.heroStart +
            " ropes=" + level.ropes.Count + " objects=" + level.objects.Count);
    }

    // ---- visual helpers (no physics impact) -------------------------------

    static readonly Color[][] backdrops =
    {
        new[] { new Color(0.45f, 0.72f, 0.98f), new Color(0.85f, 0.93f, 1f) }, // sky blue
        new[] { new Color(0.95f, 0.55f, 0.25f), new Color(0.55f, 0.3f, 0.55f) }, // sunset
        new[] { new Color(0.2f, 0.6f, 0.6f), new Color(0.13f, 0.2f, 0.35f) }, // teal
        new[] { new Color(0.55f, 0.95f, 0.75f), new Color(0.35f, 0.75f, 0.4f) }, // mint
        new[] { new Color(0.55f, 0.4f, 0.7f), new Color(0.95f, 0.6f, 0.75f) }, // dusk
    };

    void BuildBackdrop(LevelData level)
    {
        if (levelRoot == null) return;
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(levelRoot.transform, false);

        var cam = Camera.main;
        float h = (cam != null ? cam.orthographicSize : 10f) * 2f + 4f;
        float w = h * (cam != null ? cam.aspect : 0.5625f) + 4f;

        var palette = backdrops[Mathf.Abs(level.backgroundIndex) % backdrops.Length];
        var bg = new GameObject("BackdropGradient");
        bg.transform.SetParent(backdrop.transform, false);
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprites.GradientSprite(palette[0], palette[1]);
        sr.sortingOrder = -20;
        bg.transform.localScale = new Vector3(w, h, 1f);

        // Decorations: deterministic placement per level index, side bands only
        // (never over the play area centre), loaded from Resources for builds.
        var decor = Resources.LoadAll<Sprite>("Decor");
        if (decor == null || decor.Length == 0) return;
        int count = 3 + Mathf.Abs(level.backgroundIndex) % 3; // 3-5 pieces
        var rng = new System.Random(1000 + level.backgroundIndex);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Decor_" + i);
            go.transform.SetParent(backdrop.transform, false);
            var d = go.AddComponent<SpriteRenderer>();
            d.sprite = decor[rng.Next(decor.Length)];
            var c = Color.white;
            c.a = 0.6f;
            d.color = c;
            d.sortingOrder = -15;
            float side = i % 2 == 0 ? -1f : 1f;
            go.transform.position = new Vector3(
                side * (3.4f + (float)rng.NextDouble() * 2.5f),
                0.5f + (float)rng.NextDouble() * 4.5f, 0f);
        }
    }

    static void AddKnot(Transform at, float scale)
    {
        var go = new GameObject("~Knot");
        go.transform.SetParent(at, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprites.CircleSprite(64);
        sr.color = new Color(0.4f, 0.28f, 0.15f); // warm brown knot
        sr.sortingOrder = 6;
    }

    /// <summary>Destroys everything this loader created (the LevelRoot).</summary>
    public void Clear()
    {
        if (levelRoot != null)
        {
            DestroyRoot(levelRoot);
            levelRoot = null;
        }
        hero = null;
        heroBody = null;
    }

    static void DestroyRoot(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(go);
        else
            UnityEngine.Object.DestroyImmediate(go);
    }

    static void DestroyStray(string nameOrTag)
    {
        // Destroy by name "LevelRoot"
        foreach (var go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (go == null) continue;
            if (go.name == "LevelRoot")
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
        }
        // Destroy stray Heroes by tag
        foreach (var go in GameObject.FindGameObjectsWithTag("Hero"))
        {
            if (go == null) continue;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }

    GameObject PrefabFor(LevelData.ObjectDef.PrefabType type)
    {
        switch (type)
        {
            case LevelData.ObjectDef.PrefabType.Box: return boxPrefab;
            case LevelData.ObjectDef.PrefabType.Spikes: return spikesPrefab;
            case LevelData.ObjectDef.PrefabType.Fire: return firePrefab;
            case LevelData.ObjectDef.PrefabType.Water: return waterPrefab;
            case LevelData.ObjectDef.PrefabType.Door: return doorPrefab;
            case LevelData.ObjectDef.PrefabType.Pin: return pinPrefab;
            default: return null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads a LevelData asset into the current scene: hero, anchors, ropes,
/// static objects, and goal. Clears everything from a previous load first
/// so levels can be reloaded without leaking objects.
/// </summary>
[RequireComponent(typeof(GameManager))]
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

    readonly List<GameObject> spawned = new List<GameObject>();
    GameObject hero;
    Rigidbody2D heroBody;

    /// <summary>Builds the whole level from `level` and clears any previous load.</summary>
    public void Load(LevelData level)
    {
        Clear();

        // Hero first so ropes can target it.
        hero = Instantiate(heroPrefab, level.heroStart, Quaternion.identity);
        hero.name = "Hero";
        hero.tag = "Hero";
        heroBody = hero.GetComponent<Rigidbody2D>();
        if (heroBody == null) heroBody = hero.AddComponent<Rigidbody2D>();
        heroBody.mass = 1.5f;
        heroBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var reaction = hero.GetComponent<HeroReaction>();
        if (reaction == null) reaction = hero.AddComponent<HeroReaction>();
        spawned.Add(hero);

        // Anchors + ropes (each rope targets the hero unless defined otherwise).
        foreach (var ropeDef in level.ropes)
        {
            var anchorGo = Instantiate(anchorPrefab, ropeDef.anchorPos, Quaternion.identity);
            anchorGo.name = "Anchor_" + ropeDef.anchorPos.ToString("F1");
            spawned.Add(anchorGo);
            var anchorBody = anchorGo.GetComponent<Rigidbody2D>();
            if (anchorBody == null) anchorBody = anchorGo.AddComponent<Rigidbody2D>();
            anchorBody.bodyType = RigidbodyType2D.Static;

            var ropeGo = new GameObject("Rope");
            ropeGo.transform.SetParent(transform, true);
            var rope = ropeGo.AddComponent<Rope>();
            rope.anchor = anchorGo.transform;
            rope.target = heroBody;
            rope.segmentCount = ropeDef.segmentCount;
            rope.segmentLength = 0.3f;
            rope.thickness = 0.12f;
            rope.ropeColor = new Color(0.75f, 0.6f, 0.25f);
            rope.segmentPrefab = ropeSegmentPrefab;
            rope.Build();
            spawned.Add(ropeGo);
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
            var go = Instantiate(prefab, objDef.position, Quaternion.Euler(objDef.rotation));
            go.name = objDef.prefabType + "_" + spawned.Count;
            go.transform.localScale = objDef.scale;
            if (objDef.isStatic)
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null) rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
            }
            spawned.Add(go);
        }

        // Goal (door) - already carries RopeCutGoal which calls GameManager.Win().
        var goalGo = Instantiate(doorPrefab, level.goalPos, Quaternion.identity);
        goalGo.name = "Goal";
        spawned.Add(goalGo);

        GameManager.EnsureInstance();
        GameManager.Instance.ResetForLevel();
        Debug.Log("[LevelLoader] loaded hero=" + level.heroStart +
            " ropes=" + level.ropes.Count + " objects=" + level.objects.Count);
    }

    /// <summary>Destroys every object spawned by this loader (previous level cleanup).</summary>
    public void Clear()
    {
        foreach (var go in spawned)
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        spawned.Clear();
        hero = null;
        heroBody = null;
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

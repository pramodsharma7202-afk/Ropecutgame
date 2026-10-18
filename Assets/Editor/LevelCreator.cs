using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the 5 level ScriptableObject assets under Assets/Data/Levels and
/// the Game.unity LevelRunner scene. Menu: RopeCut/Create All Levels.
/// </summary>
public static class LevelCreator
{
    const string Dir = "Assets/Data/Levels";
    const string GameScenePath = "Assets/Scenes/Game.unity";

    // ------------------------------------------------------------------
    // Level data creation
    // ------------------------------------------------------------------
    [MenuItem("RopeCut/Create All Levels")]
    public static void CreateAllLevels()
    {
        if (!System.IO.Directory.Exists(Dir))
            System.IO.Directory.CreateDirectory(Dir);

        var levels = new List<(string name, Vector2 hero, LevelData.RopeDef[] ropes,
            LevelData.ObjectDef[] objs, Vector2 goal, float[] stars, int bg)>
        {
            // L1: one rope, Door directly below the Hero, nothing else.
            ("Level_001", new Vector2(0f, 1f),
                new[] { Rd(new Vector2(0f, 4f), 12) },
                new LevelData.ObjectDef[0],
                new Vector2(0f, -4.65f),
                new[] { 30f, 20f, 10f }, 0),

            // L2: one rope, Door offset to the right (needs a swing).
            ("Level_002", new Vector2(0f, 1f),
                new[] { Rd(new Vector2(0f, 4f), 12) },
                new LevelData.ObjectDef[0],
                new Vector2(2.5f, -4.65f),
                new[] { 35f, 25f, 15f }, 1),

            // L3: two ropes holding the Hero, cut both; Spikes on one side.
            ("Level_003", new Vector2(0f, 1f),
                new[] { Rd(new Vector2(-1f, 4f), 12), Rd(new Vector2(1f, 4f), 12) },
                new[] { Od(LevelData.ObjectDef.PrefabType.Spikes, new Vector3(-3f, -4.65f, 0f)) },
                new Vector2(3f, -4.65f),
                new[] { 40f, 30f, 20f }, 2),

            // L4: one rope plus a Box blocker and Spikes (Fire/Box interaction
            // not yet implemented, so Box is just a static blocker).
            ("Level_004", new Vector2(0f, 1f),
                new[] { Rd(new Vector2(0f, 4f), 12) },
                new[]
                {
                    Od(LevelData.ObjectDef.PrefabType.Box, new Vector3(-1f, -2f, 0f)),
                    Od(LevelData.ObjectDef.PrefabType.Spikes, new Vector3(-3f, -4.65f, 0f))
                },
                new Vector2(3f, -4.65f),
                new[] { 45f, 35f, 25f }, 3),

            // L5: three ropes holding the Hero; correct cut order matters.
            ("Level_005", new Vector2(0f, 1f),
                new[]
                {
                    Rd(new Vector2(-1.5f, 4f), 12),
                    Rd(new Vector2(0f, 4f), 12),
                    Rd(new Vector2(1.5f, 4f), 12)
                },
                new[] { Od(LevelData.ObjectDef.PrefabType.Spikes, new Vector3(-4f, -4.65f, 0f)) },
                new Vector2(4f, -4.65f),
                new[] { 50f, 40f, 30f }, 4),
        };

        foreach (var (name, hero, ropes, objs, goal, stars, bg) in levels)
        {
            CreateLevel(name, hero, ropes, objs, goal, stars, bg);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[LevelCreator] Created " + levels.Count + " level assets in " + Dir);
    }

    static LevelData.RopeDef Rd(Vector2 anchor, int count)
    {
        return new LevelData.RopeDef { anchorPos = anchor, segmentCount = count, targetIsHero = true };
    }

    static LevelData.ObjectDef Od(LevelData.ObjectDef.PrefabType type, Vector3 pos)
    {
        return new LevelData.ObjectDef
        {
            prefabType = type,
            position = pos,
            rotation = Vector3.zero,
            scale = Vector3.one,
            isStatic = true
        };
    }

    static void CreateLevel(string name, Vector2 hero, LevelData.RopeDef[] ropes,
        LevelData.ObjectDef[] objs, Vector2 goal, float[] stars, int bg)
    {
        var path = System.IO.Path.Combine(Dir, name + ".asset");
        var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        LevelData ld;
        if (existing != null)
        {
            ld = existing;
        }
        else
        {
            ld = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(ld, path);
        }

        ld.heroStart = hero;
        ld.ropes = new List<LevelData.RopeDef>(ropes);
        ld.objects = new List<LevelData.ObjectDef>(objs);
        ld.goalPos = goal;
        ld.starTimes = stars;
        ld.backgroundIndex = bg;

        EditorUtility.SetDirty(ld);
    }

    // ------------------------------------------------------------------
    // Game.unity LevelRunner scene creation
    // ------------------------------------------------------------------
    [MenuItem("RopeCut/Create Game Scene")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.nearClipPlane = -10f;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<PlayfieldCameraFit>();

        // Ground
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, -5.5f, 0f);
        var gc = ground.AddComponent<BoxCollider2D>();
        gc.size = new Vector2(30f, 1f);

        // Level runner object with LevelLoader + LevelRunner
        var runner = new GameObject("LevelRunner");
        var loader = runner.AddComponent<LevelLoader>();

        // Assign prefabs
        loader.heroPrefab = LoadPrefab("Assets/Prefabs/Hero.prefab");
        loader.anchorPrefab = LoadPrefab("Assets/Prefabs/Anchor.prefab");
        loader.ropeSegmentPrefab = LoadPrefab("Assets/Prefabs/RopeSegment.prefab");
        loader.boxPrefab = LoadPrefab("Assets/Prefabs/Box.prefab");
        loader.spikesPrefab = LoadPrefab("Assets/Prefabs/Spikes.prefab");
        loader.firePrefab = LoadPrefab("Assets/Prefabs/Fire.prefab");
        loader.waterPrefab = LoadPrefab("Assets/Prefabs/Water.prefab");
        loader.doorPrefab = LoadPrefab("Assets/Prefabs/Door.prefab");
        loader.pinPrefab = LoadPrefab("Assets/Prefabs/Pin.prefab");

        var levelRunner = runner.AddComponent<LevelRunner>();
        var levels = new LevelData[5];
        for (int i = 0; i < 5; i++)
        {
            levels[i] = AssetDatabase.LoadAssetAtPath<LevelData>(
                System.IO.Path.Combine(Dir, $"Level_{i + 1:D3}.asset"));
        }
        levelRunner.levels = levels;

        EditorSceneManager.SaveScene(scene, GameScenePath);
        Debug.Log("[LevelCreator] Game.unity scene saved: " + GameScenePath);
    }

    static GameObject LoadPrefab(string path)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogWarning("[LevelCreator] missing prefab: " + path);
        return go;
    }
}

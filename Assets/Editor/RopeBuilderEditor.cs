using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RopeBuilderEditor
{
    const string RopeSegmentPrefab = "Assets/Prefabs/RopeSegment.prefab";
    const string AnchorPrefab = "Assets/Prefabs/Anchor.prefab";
    const string HeroPrefab = "Assets/Prefabs/Hero.prefab";
    const string TestScenePath = "Assets/Scenes/RopeTest.unity";

    [MenuItem("RopeCut/Create Test Rope")]
    public static void CreateTestRope()
    {
        Debug.Log("[RopeBuilder] result: " + CreateTestRopeScene());
    }

    /// <summary>
    /// Builds and saves Scenes/RopeTest: ortho cam (size 6), Anchor at (0,4),
    /// Hero 3 units below (mass 1.5), rope between them, ground at y=-5.
    /// </summary>
    public static string CreateTestRopeScene()
    {
        var segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RopeSegmentPrefab);
        var anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefab);
        var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab);
        if (segmentPrefab == null || anchorPrefab == null || heroPrefab == null)
            return "missing prefab (segment/anchor/hero)";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera: orthographic, size 6
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.nearClipPlane = -10f;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        // Anchor at (0, 4)
        var anchor = (GameObject)PrefabUtility.InstantiatePrefab(anchorPrefab);
        anchor.transform.position = new Vector3(0f, 4f, 0f);
        anchor.name = "Anchor";

        // Hero 3 units below, mass 1.5 (>= 10x segment mass 0.1)
        var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.transform.position = new Vector3(0f, 1f, 0f);
        hero.name = "Hero";
        var heroBody = hero.GetComponent<Rigidbody2D>();
        if (heroBody == null) heroBody = hero.AddComponent<Rigidbody2D>();
        heroBody.mass = 1.5f;
        heroBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Ground: BoxCollider2D whose top surface sits exactly at y = -5
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, -5.5f, 0f);
        var gc = ground.AddComponent<BoxCollider2D>();
        gc.size = new Vector2(30f, 1f);

        // Rope wiring
        var ropeGo = new GameObject("Rope");
        var rope = ropeGo.AddComponent<Rope>();
        rope.anchor = anchor.transform;
        rope.target = heroBody;
        rope.segmentCount = 12;
        rope.segmentLength = 0.3f;
        rope.thickness = 0.12f;
        rope.ropeColor = new Color(0.75f, 0.60f, 0.25f);
        rope.segmentPrefab = segmentPrefab;

        EditorSceneManager.SaveScene(scene, TestScenePath);
        int count = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }).Length;
        return "scene saved: " + TestScenePath;
    }
}

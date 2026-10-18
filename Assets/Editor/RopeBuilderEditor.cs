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
        return "scene saved: " + TestScenePath;
    }

    // ------------------------------------------------------------------
    // Hang test: measures the rope/hero at sim-time t = 1, 3, 5, 8 s,
    // applies an impulse (3, 0) at t = 8 s, then checks the swing return
    // by sim-time t = 18 s. Physics is stepped MANUALLY (SimulationMode2D
    // Script + Physics2D.Simulate) because the editor's normal player
    // loop stalls when the editor window is unfocused (frameCount stays 1),
    // which would freeze the simulation and produce garbage measurements.
    // All times below are SIMULATION time (fixed dt = 0.01), not wall clock.
    // ------------------------------------------------------------------
    public const string HangTestPendingKey = "RopeHangTestPending";

    static bool runT1, runT3, runT5, runT8;
    static bool phaseAPass, swingDone;
    static float swingTime = -1f;
    static float minHeroY = float.PositiveInfinity;
    static float maxXSinceImpulse;
    static float simT;      // accumulated simulated seconds
    static float simBudget; // pending simulation seconds to spend
    static UnityEngine.SimulationMode2D prevSimMode;

    [MenuItem("RopeCut/Run Hang Test")]
    public static void RunHangTest()
    {
        if (!EditorApplication.isPlaying)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TestScenePath)
                EditorSceneManager.OpenScene(TestScenePath);
            // SessionState survives the domain reload that happens when
            // entering Play mode; the hook below starts the runner.
            SessionState.SetBool(HangTestPendingKey, true);
            EditorApplication.isPlaying = true;
            return;
        }
        StartHangTestRunner();
    }

    public static void StartHangTestRunner()
    {
        runT1 = runT3 = runT5 = runT8 = false;
        phaseAPass = false;
        swingDone = false;
        swingTime = -1f;
        minHeroY = float.PositiveInfinity;
        maxXSinceImpulse = 0f;
        simT = 0f;
        simBudget = 0f;
        prevSimMode = Physics2D.simulationMode;
        Physics2D.simulationMode = UnityEngine.SimulationMode2D.Script;
        EditorApplication.update -= HangTestUpdate;
        EditorApplication.update += HangTestUpdate;
        Debug.Log("[HangTest] runner started (script-driven stepping, sim-time based)");
    }

    static void StopHangTest(string reason)
    {
        EditorApplication.update -= HangTestUpdate;
        Physics2D.simulationMode = prevSimMode;
        Debug.Log("[HangTest] runner stopped: " + reason);
    }

    static void HangTestUpdate()
    {
        if (!EditorApplication.isPlaying) { StopHangTest("play mode exited"); return; }

        // Spend accumulated budget: run fixed-dt steps until simT catches up.
        float dt = Time.fixedDeltaTime;
        while (simBudget >= dt)
        {
            Physics2D.Simulate(dt);
            simT += dt;
            simBudget -= dt;
        }
        simBudget += 0.01f; // pace ~1 step per editor tick (min 100 Hz tooling)

        var heroGo = GameObject.Find("Hero");
        var ropeGo = GameObject.Find("Rope");
        if (heroGo == null || ropeGo == null) { StopHangTest("hero/rope missing"); return; }

        var hero = heroGo.GetComponent<Rigidbody2D>();
        var rope = ropeGo.GetComponent<Rope>();
        if (hero == null || rope == null || rope.Segments.Count == 0) return; // rope not built yet

        int n = rope.Segments.Count;
        float t = simT;

        float speed = hero.linearVelocity.magnitude;
        float gap = MaxGap(rope);
        int upper = CountUpper(rope);
        var last = rope.Segments[n - 1];
        bool tailOk = last != null && last.tailJoint != null;

        if (!runT1 && t >= 1f) { runT1 = true; Report(1, hero, speed, gap, upper, n, tailOk); }
        if (!runT3 && t >= 3f) { runT3 = true; Report(3, hero, speed, gap, upper, n, tailOk); }
        if (!runT5 && t >= 5f) { runT5 = true; Report(5, hero, speed, gap, upper, n, tailOk); }
        if (!runT8 && t >= 8f)
        {
            runT8 = true;
            Report(8, hero, speed, gap, upper, n, tailOk);
            float y = hero.transform.position.y;
            phaseAPass = y >= 0.3f && y <= 1.7f && speed < 0.1f && upper == n && tailOk;
            Debug.Log("[HangTest] PHASE A pass=" + phaseAPass + " (y=" + y.ToString("F2") + " speed=" + speed.ToString("F3") + " upper=" + upper + "/" + n + " tail=" + tailOk + ")");
            hero.AddForce(new Vector2(3f, 0f), ForceMode2D.Impulse);
            Debug.Log("[HangTest] Impulse (3,0) applied at sim t=" + t.ToString("F1") + "s");
        }

        Vector3 heroPos = hero.transform.position;
        minHeroY = Mathf.Min(minHeroY, heroPos.y);

        if (runT8 && !swingDone)
        {
            maxXSinceImpulse = Mathf.Max(maxXSinceImpulse, Mathf.Abs(heroPos.x));
            if (maxXSinceImpulse > 0.5f && Mathf.Abs(heroPos.x) < 0.3f)
            {
                swingDone = true;
                swingTime = simT - 8f;
                Debug.Log("[HangTest] swing returned: |x|=" + Mathf.Abs(heroPos.x).ToString("F2") + " after " + swingTime.ToString("F1") + "s (maxX=" + maxXSinceImpulse.ToString("F2") + ")");
            }
        }

        if (t >= 18f)
        {
            Report(18, hero, speed, gap, upper, n, tailOk);
            bool phaseBPass = swingDone && swingTime > 0f && swingTime <= 10f && minHeroY > -3f && upper == n && tailOk;
            Debug.Log("[HangTest] PHASE B pass=" + phaseBPass + " returnTime=" + swingTime.ToString("F1") + "s minY=" + minHeroY.ToString("F2") + " upper=" + upper + "/" + n + " tail=" + tailOk);
            StopHangTest("complete");
            EditorApplication.isPlaying = false;
        }
    }

    /// <summary>Periodic measurement report for the hang test.</summary>
    static void Report(double t, Rigidbody2D hero, float speed, float gap, int upper, int n, bool tailOk)
    {
        Debug.Log("[HangTest] t=" + t.ToString("F1") + "s hero=" + hero.transform.position.ToString("F2") +
            " speed=" + speed.ToString("F3") + " maxGap=" + gap.ToString("F3") +
            " upperJoints=" + upper + "/" + n + " tailJoint=" + tailOk);
    }

    /// <summary>Max distance between consecutive segment centers (stretch indicator).</summary>
    static float MaxGap(Rope rope)
    {
        var segs = rope.Segments;
        float max = 0f;
        for (int i = 1; i < segs.Count; i++)
            if (segs[i - 1] != null && segs[i] != null)
                max = Mathf.Max(max, Vector2.Distance(segs[i - 1].transform.position, segs[i].transform.position));
        return max;
    }

    /// <summary>How many segments still have their hinge to the piece above them.</summary>
    static int CountUpper(Rope rope)
    {
        int count = 0;
        foreach (var s in rope.Segments)
            if (s != null && s.upperJoint != null) count++;
        return count;
    }
}

[InitializeOnLoad]
static class RopeHangTestHook
{
    static RopeHangTestHook()
    {
        EditorApplication.update += () =>
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (!SessionState.GetBool(RopeBuilderEditor.HangTestPendingKey, false)) return;
            SessionState.SetBool(RopeBuilderEditor.HangTestPendingKey, false);
            RopeBuilderEditor.StartHangTestRunner();
        };
    }
}


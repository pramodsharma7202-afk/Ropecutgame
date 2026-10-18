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
        camGo.AddComponent<AudioListener>();

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

    // ------------------------------------------------------------------
    // Swipe-cut test: simulates a horizontal swipe across the middle of
    // the rope via RopeCutter.CutAt, then measures the cut, the sound,
    // the fallback, and the hero falling to the ground. Script-driven
    // stepping (see HangTest note) so measurements are simulation-time.
    // ------------------------------------------------------------------
    [MenuItem("RopeCut/Run Cut Test")]
    public static void RunCutTest()
    {
        if (!EditorApplication.isPlaying)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TestScenePath)
                EditorSceneManager.OpenScene(TestScenePath);
            SessionState.SetBool(HangTestPendingKey, false);
            EditorApplication.isPlaying = true;
            EditorApplication.delayCall += () => Debug.Log("[CutTest] " + RunCutTestNow());
        }
        else
        {
            Debug.Log("[CutTest] " + RunCutTestNow());
        }
    }

    static string RunCutTestNow()
    {
        var report = new System.Collections.Generic.List<string>();
        try
        {
            var ropeGo = GameObject.Find("Rope");
            var cutterGo = GameObject.Find("RopeCutter");
            var heroGo = GameObject.Find("Hero");
            if (ropeGo == null || cutterGo == null || heroGo == null) return "missing scene objects";
            var rope = ropeGo.GetComponent<Rope>();
            var cutter = cutterGo.GetComponent<RopeCutter>();
            var hero = heroGo.GetComponent<Rigidbody2D>();
            if (rope == null || cutter == null || hero == null) return "missing components";

            // AudioManager (RopeTest has no baked-in manager)
            if (AudioManager.Instance == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab");
                if (prefab != null) Object.Instantiate(prefab);
            }

            cutter.Enabled = true;
            // NOTE: no manual SimulationMode switch - with Application.runInBackground
            // enabled the player loop steps normally even when the editor is unfocused.

            report.Add("queriesHitTriggers=" + Physics2D.queriesHitTriggers);

            // Ensure rope built (Start may be deferred in a stalled loop)
            int steps = 0;
            while (rope.Segments.Count == 0 && steps++ < 20) rope.Build();
            if (rope.Segments.Count == 0) return "rope did not build";
            int n = rope.Segments.Count;

            var heroBefore = hero.position;
            report.Add("pre-cut hero=" + heroBefore.ToString("F2") + " preCutCount=" + CountUpper(rope));

            // Swipe line across the middle of the rope (segment 5 area)
            var midSeg = rope.Segments[rope.Segments.Count - 7]; // index 5 of 12
            float midY = midSeg.transform.position.y;
            int cutCalls = cutter.CutAt(new Vector2(-1.5f, midY), new Vector2(1.5f, midY));
            report.Add("CutAt=" + cutCalls + " (line y=" + midY.ToString("F2") + ")");

            // double-cut attempt on the same line must be a no-op
            int secondCutCalls = cutter.CutAt(new Vector2(-1.5f, midY), new Vector2(1.5f, midY));
            report.Add("secondCutAt=" + secondCutCalls);

            int upperAfter = CountUpper(rope);
            report.Add("upperJoints=" + upperAfter + "/" + n + " (was " + n + ")");

            int cutSoundCount = AudioManager.Instance != null && AudioManager.Instance.PlayCounts.TryGetValue("Cut", out var c) ? c : 0;
            report.Add("cutSoundPlays=" + cutSoundCount);

            report.Add("cutPieceAlphaPost=" + (midSeg.visual ? midSeg.visual.color.a.ToString("F2") : "n/a") + " doubleSafe=" + (midSeg.upperJoint == null));

            report.Add("NOTE: fall/settling is measured by editor polling (See Phase callbacks)");
        }
        finally
        {
            // physics mode untouched: the player loop is live
        }
        return string.Join("; ", report.ToArray());
    }

    /// <summary>Phase-B polling call: read the falling hero state once.</summary>
    public static string CutTestSample()
    {
        var hero = GameObject.Find("Hero") ? GameObject.Find("Hero").GetComponent<Rigidbody2D>() : null;
        var ropeGo = GameObject.Find("Rope");
        var rope = ropeGo != null ? ropeGo.GetComponent<Rope>() : null;
        var sb = new System.Collections.Generic.List<string>();
        if (hero != null) sb.Add("hero=" + hero.transform.position.ToString("F2") + " speed=" + hero.linearVelocity.magnitude.ToString("F2"));
        if (rope != null) sb.Add("upper=" + CountUpper(rope) + "/" + rope.Segments.Count);
        int sound = AudioManager.Instance != null && AudioManager.Instance.PlayCounts.TryGetValue("Cut", out var c) ? c : 0;
        sb.Add("cutSoundPlays=" + sound);
        return string.Join("; ", sb.ToArray());
    }

    // ------------------------------------------------------------------
    // RopeTest2: game-loop test scene. Anchor + Hero on a rope, Spikes on
    // the ground to the left, Door (RopeCutGoal) on the ground to the
    // right. A cut with a rightward swing lands the hero in the door (win);
    // a leftward swing lands it on the spikes (lose).
    // ------------------------------------------------------------------
    const string Test2ScenePath = "Assets/Scenes/RopeTest2.unity";

    [MenuItem("RopeCut/Create RopeTest2 Scene")]
    public static string CreateRopeTest2Scene()
    {
        var segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RopeSegmentPrefab);
        var anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefab);
        var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab);
        var spikesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spikes.prefab");
        var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
        var audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab");
        if (segmentPrefab == null || anchorPrefab == null || heroPrefab == null ||
            spikesPrefab == null || doorPrefab == null || audioPrefab == null)
            return "missing prefab(s)";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera: orthographic + AudioListener + playfield fit
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.nearClipPlane = -10f;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();
        var fit = camGo.AddComponent<PlayfieldCameraFit>();
        fit.referenceWidth = 10f;
        fit.minOrthoSize = 6f;

        // Game loop manager + audio
        new GameObject("GameManager").AddComponent<GameManager>();
        PrefabUtility.InstantiatePrefab(audioPrefab);

        // Ground: BoxCollider2D whose top surface sits exactly at y = -5
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, -5.5f, 0f);
        var gc = ground.AddComponent<BoxCollider2D>();
        gc.size = new Vector2(30f, 1f);

        // Spikes on the ground, left of the hero (solid collider + RopeCutHazard)
        var spikes = (GameObject)PrefabUtility.InstantiatePrefab(spikesPrefab);
        spikes.transform.position = new Vector3(-2.5f, -4.65f, 0f);
        spikes.name = "Spikes";

        // Door on the ground, right of the hero (trigger + RopeCutGoal)
        var door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
        door.transform.position = new Vector3(2.5f, -4.65f, 0f);
        door.name = "Door";

        // Anchor + Hero + rope
        var anchor = (GameObject)PrefabUtility.InstantiatePrefab(anchorPrefab);
        anchor.transform.position = new Vector3(0f, 4f, 0f);
        anchor.name = "Anchor";

        var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.transform.position = new Vector3(0f, 1f, 0f);
        hero.name = "Hero";
        var heroBody = hero.GetComponent<Rigidbody2D>();
        if (heroBody == null) heroBody = hero.AddComponent<Rigidbody2D>();
        heroBody.mass = 1.5f;
        heroBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var ropeGo = new GameObject("Rope");
        var rope = ropeGo.AddComponent<Rope>();
        rope.anchor = anchor.transform;
        rope.target = heroBody;
        rope.segmentCount = 12;
        rope.segmentLength = 0.3f;
        rope.thickness = 0.12f;
        rope.ropeColor = new Color(0.75f, 0.60f, 0.25f);
        rope.segmentPrefab = segmentPrefab;

        // Swipe cutter
        var cutterGo = new GameObject("RopeCutter");
        var cutter = cutterGo.AddComponent<RopeCutter>();
        cutter.cutMask = LayerMask.GetMask("Rope");

        EditorSceneManager.SaveScene(scene, Test2ScenePath);
        return "scene saved: " + Test2ScenePath;
    }

    [MenuItem("RopeCut/Run GameLoop Test")]
    public static void RunGameLoopTest()
    {
        if (!EditorApplication.isPlaying)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Test2ScenePath)
                EditorSceneManager.OpenScene(Test2ScenePath);
            SessionState.SetBool(HangTestPendingKey, false);
            EditorApplication.isPlaying = true;
        }
    }

    /// <summary>
    /// Phase 0 (call once, right after entering play): verifies scene wiring,
    /// ensures the rope is built, reports initial game state.
    /// </summary>
    public static string GameLoopTestStart()
    {
        var report = new System.Collections.Generic.List<string>();
        var ropeGo = GameObject.Find("Rope");
        var cutterGo = GameObject.Find("RopeCutter");
        var heroGo = GameObject.Find("Hero");
        if (ropeGo == null || cutterGo == null || heroGo == null) return "missing scene objects";
        var rope = ropeGo.GetComponent<Rope>();
        var cutter = cutterGo.GetComponent<RopeCutter>();
        if (GameManager.Instance == null)
        {
            var gmGo = new GameObject("~GameManager");
            gmGo.AddComponent<GameManager>();
        }
        if (AudioManager.Instance == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AudioManager.prefab");
            if (prefab != null) Object.Instantiate(prefab);
        }

        int steps = 0;
        while (rope.Segments.Count == 0 && steps++ < 20) rope.Build();
        report.Add("rope=" + rope.Segments.Count + " cutterEnabled=" + cutter.Enabled);
        report.Add(GameLoopSample());
        return string.Join("; ", report.ToArray());
    }

    /// <summary>Cut the rope and shove the hero sideways (+1 right, -1 left).</summary>
    public static string GameLoopTestSwing(int dir)
    {
        var ropeGo = GameObject.Find("Rope");
        var cutterGo = GameObject.Find("RopeCutter");
        var heroGo = GameObject.Find("Hero");
        if (ropeGo == null || cutterGo == null || heroGo == null) return "missing scene objects";
        var rope = ropeGo.GetComponent<Rope>();
        var cutter = cutterGo.GetComponent<RopeCutter>();
        var hero = heroGo.GetComponent<Rigidbody2D>();
        if (rope.Segments.Count == 0) return "rope not built";

        var midSeg = rope.Segments[rope.Segments.Count - 7];
        float midY = midSeg.transform.position.y;
        int cuts = cutter.CutAt(new Vector2(-1.5f, midY), new Vector2(1.5f, midY));
        // swing impulse: 3.6 N*s on mass 1.5 => 2.4 m/s, lands on door/spikes at x = +-2.5
        hero.AddForce(new Vector2(3.6f * dir, 0f), ForceMode2D.Impulse);
        return "cuts=" + cuts + " impulse=" + (3.6f * dir).ToString("F1");
    }

    /// <summary>One polling sample of the full game-loop state.</summary>
    public static string GameLoopSample()
    {
        var sb = new System.Collections.Generic.List<string>();
        var hero = GameObject.Find("Hero");
        if (hero != null)
        {
            var rb = hero.GetComponent<Rigidbody2D>();
            sb.Add("hero=" + hero.transform.position.ToString("F2") + " speed=" + rb.linearVelocity.magnitude.ToString("F2"));
        }
        if (GameManager.Instance != null)
        {
            sb.Add("state=" + GameManager.Instance.State + " win=" + GameManager.Instance.WinCount + " lose=" + GameManager.Instance.LoseCount);
        }
        if (AudioManager.Instance != null)
        {
            sb.Add("sndWin=" + (AudioManager.Instance.PlayCounts.TryGetValue("Win", out var w) ? w : 0) +
                   " sndLose=" + (AudioManager.Instance.PlayCounts.TryGetValue("Lose", out var l) ? l : 0));
        }
        var cutterGo = GameObject.Find("RopeCutter");
        if (cutterGo != null) sb.Add("cutter=" + cutterGo.GetComponent<RopeCutter>().Enabled);
        sb.Add("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        return string.Join("; ", sb.ToArray());
    }

    // ------------------------------------------------------------------
    // Play-mode level test (RopeCut/Test All Levels). Runs entirely in
    // Play mode so physics steps naturally. Uses SessionState to survive
    // the domain reload that happens when entering Play mode.
    // ------------------------------------------------------------------
    public const string LevelTestPendingKey = "LevelTestPending";
    static int levelTestIndex;
    static float levelTestTimer;
    static bool levelTestNeedsLoad;
    static LevelLoader levelTestLoader;
    static GameObject levelTestHarness;

    [MenuItem("RopeCut/Test All Levels")]
    public static void TestAllLevels()
    {
        if (!EditorApplication.isPlaying)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Game.unity")
                EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            SessionState.SetBool(LevelTestPendingKey, true);
            EditorApplication.isPlaying = true;
            return;
        }
        StartLevelTestRunner();
    }

    public static void StartLevelTestRunner()
    {
        levelTestIndex = 0;
        levelTestTimer = 0f;
        levelTestNeedsLoad = true;
        levelTestLoader = null;
        levelTestHarness = null;
        EditorApplication.update -= LevelTestUpdate;
        EditorApplication.update += LevelTestUpdate;
        Debug.Log("[LevelTest] runner started");
    }

    static void StopLevelTest(string reason)
    {
        EditorApplication.update -= LevelTestUpdate;
        if (levelTestHarness != null) UnityEngine.Object.Destroy(levelTestHarness);
        levelTestHarness = null;
        levelTestLoader = null;
        Debug.Log("[LevelTest] runner stopped: " + reason);
    }

    static void LevelTestUpdate()
    {
        if (!EditorApplication.isPlaying) { StopLevelTest("play mode exited"); return; }

        // Setup harness on first tick.
        if (levelTestHarness == null)
        {
            levelTestHarness = new GameObject("~LevelTestHarness");
            levelTestLoader = levelTestHarness.AddComponent<LevelLoader>();
            levelTestLoader.heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero.prefab");
            levelTestLoader.anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Anchor.prefab");
            levelTestLoader.ropeSegmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RopeSegment.prefab");
            levelTestLoader.boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Box.prefab");
            levelTestLoader.spikesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spikes.prefab");
            levelTestLoader.firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fire.prefab");
            levelTestLoader.waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Water.prefab");
            levelTestLoader.doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
            levelTestLoader.pinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pin.prefab");
            levelTestIndex = 0;
            levelTestNeedsLoad = true;
            levelTestTimer = 0f;
        }

        // All levels done?
        if (levelTestIndex >= 5)
        {
            // Verify zero Hero objects remain.
            var heroes = GameObject.FindGameObjectsWithTag("Hero");
            if (heroes.Length != 0)
            {
                Debug.LogError("[LevelTest] FAIL: " + heroes.Length + " Hero objects remain after all levels!");
            }
            else
            {
                Debug.Log("[LevelTest] PASS: zero Hero objects remain after all levels.");
            }
            StopLevelTest("complete");
            EditorApplication.isPlaying = false;
            return;
        }

        // Load level if needed.
        if (levelTestNeedsLoad)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Data/Levels/Level_{levelTestIndex + 1:D3}.asset");
            if (level == null)
            {
                Debug.LogError("[LevelTest] Level " + (levelTestIndex + 1) + " asset missing!");
                levelTestIndex++;
                return;
            }
            levelTestLoader.Load(level);
            levelTestNeedsLoad = false;
            levelTestTimer = 0f;
            Debug.Log("[LevelTest] Level " + (levelTestIndex + 1) + " loaded");
            return;
        }

        // Wait 3 seconds (real frames).
        levelTestTimer += Time.deltaTime;
        if (levelTestTimer < 3f) return;

        // Sample and verify.
        var hero = GameObject.Find("Hero");
        var ropes = GameObject.FindObjectsOfType<Rope>();
        int ropeSegs = 0;
        foreach (var r in ropes) ropeSegs += r.Segments.Count;

        var levelAsset = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Data/Levels/Level_{levelTestIndex + 1:D3}.asset");
        // Expected children of LevelRoot: hero(1) + anchors(ropes) + ropeGOs(ropes) + level objects + goal(1)
        int expectedObjs = 1 + levelAsset.ropes.Count + levelAsset.ropes.Count + levelAsset.objects.Count + 1;

        var levelRoot = GameObject.Find("LevelRoot");
        int actualChildCount = levelRoot != null ? levelRoot.transform.childCount : 0;

        string heroPos = hero != null ? hero.transform.position.ToString("F2") : "MISSING";
        bool heroFell = hero != null && hero.transform.position.y < -5f;
        string status = heroFell ? "FELL WITHOUT CUT (BROKEN)" : "held (OK)";

        Debug.Log($"[LevelTest] Level {levelTestIndex + 1} ({levelAsset.name}): hero={heroPos} ropeSegs={ropeSegs} levelRootChildren={actualChildCount} expected={expectedObjs} {status}");

        if (actualChildCount != expectedObjs)
        {
            Debug.LogError($"[LevelTest] FAIL Level {levelTestIndex + 1}: expected {expectedObjs} children in LevelRoot, got {actualChildCount}");
        }
        if (hero == null)
        {
            Debug.LogError($"[LevelTest] FAIL Level {levelTestIndex + 1}: Hero missing!");
        }
        if (heroFell)
        {
            Debug.LogError($"[LevelTest] FAIL Level {levelTestIndex + 1}: Hero fell without cut!");
        }

        // Clear and advance.
        levelTestLoader.Clear();
        levelTestIndex++;
        levelTestNeedsLoad = true;
        levelTestTimer = 0f;
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
            if (SessionState.GetBool(RopeBuilderEditor.HangTestPendingKey, false))
            {
                SessionState.SetBool(RopeBuilderEditor.HangTestPendingKey, false);
                RopeBuilderEditor.StartHangTestRunner();
                return;
            }
            if (SessionState.GetBool(RopeBuilderEditor.LevelTestPendingKey, false))
            {
                SessionState.SetBool(RopeBuilderEditor.LevelTestPendingKey, false);
                RopeBuilderEditor.StartLevelTestRunner();
                return;
            }
        };
    }
}


using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-built UI (no scene edits, no prefabs): HUD with level text and
/// Restart, a Win panel with stars + Next/Retry, and a Lose panel with Retry.
/// Everything is built from the UITheme asset in Awake and driven by the
/// GameManager win/lose/level-loaded events.
/// </summary>
public class GameUI : MonoBehaviour
{
    const string ThemePath = "Assets/Art/UI/UITheme.asset";

    UITheme theme;
    Text levelText;

    GameObject winPanel;
    Text winTitle;
    Image[] stars;
    Button winNextButton;
    Text allCompleteText;

    GameObject losePanel;

    float levelElapsed;
    int earnedStars;

    void Awake()
    {
        theme = Resources.Load<UITheme>("UITheme");
#if UNITY_EDITOR
        if (theme == null)
            theme = UnityEditor.AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
#endif
        if (theme == null)
            theme = ScriptableObject.CreateInstance<UITheme>(); // fallback defaults

        BuildEventSystem();
        BuildCanvas();
        BuildPanels();

        // Elapsed level time is tracked by us (reset on every level load);
        // Time.timeSinceLevelLoad is not reliable across scene reload flow.
        levelElapsed = 0f;

        Debug.Log("[GameUI] built HUD + WinPanel + LosePanel from UITheme");
    }

    void OnEnable()
    {
        GameManager.EnsureInstance();
        GameManager.Instance.OnWon += OnWon;
        GameManager.Instance.OnLost += OnLost;
        GameManager.Instance.OnLevelLoaded += OnLevelLoaded;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWon -= OnWon;
            GameManager.Instance.OnLost -= OnLost;
            GameManager.Instance.OnLevelLoaded -= OnLevelLoaded;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameManager.GameState.Playing)
            levelElapsed += Time.deltaTime;

        // Editor-testing fallback: Enter = Next/Retry (whichever panel shows), R = Restart.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Playing)
        {
            if (kb.enterKey.wasPressedThisFrame) RetryOrRestart();
        }
        if (kb.rKey.wasPressedThisFrame) OnRestartClicked();
    }

    void OnLevelLoaded()
    {
        levelElapsed = 0f;
        earnedStars = 0;
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        var cutter = FindFirstObjectByType<RopeCutter>();
        if (cutter != null) cutter.Enabled = true;
    }

    void OnRetryClicked()
    {
        Debug.Log("[UI] Retry clicked");
        Restart();
    }

    void OnRestartClicked()
    {
        Debug.Log("[UI] Restart clicked");
        Restart();
    }

    void RetryOrRestart()
    {
        if (losePanel != null && losePanel.activeInHierarchy) OnRetryClicked();
        else if (winPanel != null && winPanel.activeInHierarchy)
        {
            if (winNextButton != null && winNextButton.gameObject.activeInHierarchy) NextLevelClicked();
            else OnRetryClicked();
        }
        else OnRestartClicked();
    }

    void NextLevelClicked()
    {
        var runner = FindFirstObjectByType<LevelRunner>();
        int next = 0;
        if (runner != null) next = runner.CurrentLevelIndex + 1;
        if (runner != null && runner.levels != null && runner.levels.Length > 0)
            next = Mathf.Clamp(next, 0, runner.levels.Length - 1); // never past the last level
        Debug.Log("[UI] Next clicked -> level " + (next + 1));
        PlayerPrefs.SetInt("CurrentLevel", next);
        PlayerPrefs.Save();
        Restart();
    }

    void OnWon()
    {
        float t = levelElapsed;

        earnedStars = 1;
        var runner = FindFirstObjectByType<LevelRunner>();
        var level = runner != null ? runner.CurrentLevel : null;
        if (level != null && level.starTimes != null && level.starTimes.Length >= 2)
        {
            if (t <= level.starTimes[0]) earnedStars = 3;
            else if (t <= level.starTimes[1]) earnedStars = 2;
        }

        // Persist progress: best stars per level, and highest unlocked index
        // (current + 1 after a win; never decreased by any other path).
        int idx = runner != null ? runner.CurrentLevelIndex : 0;
        int best = PlayerPrefs.GetInt("Stars_" + idx, 0);
        if (earnedStars > best) PlayerPrefs.SetInt("Stars_" + idx, earnedStars);
        int unlocked = PlayerPrefs.GetInt("Unlocked", 0);
        if (idx + 1 > unlocked) PlayerPrefs.SetInt("Unlocked", idx + 1);
        PlayerPrefs.Save();

        // The goal already disables the cutter on win; be safe while a panel is up.
        var cutter = FindFirstObjectByType<RopeCutter>();
        if (cutter != null) cutter.Enabled = false;

        winTitle.text = "Level Complete";
        for (int i = 0; i < stars.Length; i++)
        {
            bool on = i < earnedStars;
            stars[i].color = on ? theme.accentColour : new Color(0.35f, 0.35f, 0.35f, 0.55f);
        }

        bool isLast = runner == null || runner.levels == null ||
                      runner.CurrentLevelIndex + 1 >= runner.levels.Length;
        winNextButton.gameObject.SetActive(!isLast);
        allCompleteText.gameObject.SetActive(isLast);

        winPanel.SetActive(true);
        Debug.Log("[GameUI] win shown: " + earnedStars + " stars in " + t.ToString("F2") + "s");
    }

    void OnLost()
    {
        var cutter = FindFirstObjectByType<RopeCutter>();
        if (cutter != null) cutter.Enabled = false;
        StartCoroutine(ShowLoseAfterDelay());
    }

    IEnumerator ShowLoseAfterDelay()
    {
        yield return new WaitForSeconds(0.6f);
        losePanel.SetActive(true);
    }

    // ---- button actions -------------------------------------------------

    void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.Restart();
    }

    // ---- construction ---------------------------------------------------

    void BuildEventSystem()
    {
        // Exactly one EventSystem: duplicates swallow click events.
        var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        var es = all.Length > 0 ? all[0] : null;
        for (int i = 1; i < all.Length; i++)
            Destroy(all[i].gameObject);
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            es = go.AddComponent<EventSystem>();
        }

        var module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (module.actionsAsset == null)
            module.AssignDefaultActions(); // without this the module has no input actions
    }

    void BuildCanvas()
    {
        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
    }

    void BuildPanels()
    {
        // ---- HUD (top of screen) ----
        var hud = CreateChild("HUD", AnchorPresets.TopStretch);
        SetOffsets(hud, 0, -240, 0, 0);

        levelText = MakeText(hud, "LevelText", "", AnchorPresets.TopCenter,
            new Vector2(0, -150), new Vector2(400, 90), 50);
        levelText.color = Color.white;

        var hudRestart = MakeButton(hud, "RestartButton", theme.secondaryButton,
            theme.secondaryColour, "Restart", AnchorPresets.TopCenter,
            new Vector2(0, -150), new Vector2(200, 90));
        hudRestart.onClick.AddListener(OnRestartClicked);
        // pin the button to the top right
        var rrt = hudRestart.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(1, 1);
        rrt.anchorMax = new Vector2(1, 1);
        rrt.anchoredPosition = new Vector2(-150, -150);

        // ---- Win panel (hidden) ----
        var win = CreateChild("WinPanel", AnchorPresets.StretchAll);
        winPanel = win.gameObject;
        MakeImage(win, theme.panel,
            new Color(theme.panelColour.r, theme.panelColour.g, theme.panelColour.b, 0.95f));

        winTitle = MakeText(win, "WinTitle", "Level Complete", AnchorPresets.CenterTop,
            new Vector2(0, 380), new Vector2(800, 100), 58);
        winTitle.color = theme.textColour;

        stars = new Image[3];
        float[] starX = { -190f, 0f, 190f };
        float[] starY = { 30f, 140f, 30f };
        for (int i = 0; i < stars.Length; i++)
        {
            var star = CreateChild("Star" + i, AnchorPresets.Center, win);
            star.sizeDelta = new Vector2(160, 160);
            star.anchoredPosition = new Vector2(starX[i], starY[i]);
            stars[i] = MakeImage(star, theme.starIcon, theme.accentColour, raycastOn: false);
        }

        var winNext = MakeButton(win, "NextButton", theme.primaryButton,
            theme.primaryColour, "Next", AnchorPresets.Center,
            new Vector2(-170, -280), new Vector2(260, 110));
        winNext.onClick.AddListener(NextLevelClicked);
        winNextButton = winNext;

        var winRetry = MakeButton(win, "WinRetryButton", theme.secondaryButton,
            theme.secondaryColour, "Retry", AnchorPresets.Center,
            new Vector2(170, -280), new Vector2(260, 110));
        winRetry.onClick.AddListener(OnRetryClicked);

        allCompleteText = MakeText(win, "AllCompleteText", "All levels complete!",
            AnchorPresets.Center, new Vector2(0, -280), new Vector2(800, 90), 44);
        allCompleteText.color = theme.textColour;

        winPanel.SetActive(false);

        // ---- Lose panel (hidden) ----
        var lose = CreateChild("LosePanel", AnchorPresets.StretchAll);
        losePanel = lose.gameObject;
        MakeImage(lose, theme.panel,
            new Color(theme.panelColour.r, theme.panelColour.g, theme.panelColour.b, 0.95f));

        var loseTitle = MakeText(lose, "LoseTitle", "Try Again", AnchorPresets.CenterTop,
            new Vector2(0, 380), new Vector2(800, 100), 58);
        loseTitle.color = theme.textColour;

        var loseRetry = MakeButton(lose, "LoseRetryButton", theme.secondaryButton,
            theme.secondaryColour, "Retry", AnchorPresets.Center,
            new Vector2(0, -280), new Vector2(280, 110));
        loseRetry.onClick.AddListener(OnRetryClicked);

        losePanel.SetActive(false);
    }

    // ---- tiny builder helpers ------------------------------------------

    enum AnchorPresets
    {
        TopStretch, TopCenter, CenterTop, Center, StretchAll
    }

    RectTransform CreateChild(string name, AnchorPresets preset, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        ApplyPreset(go.GetComponent<RectTransform>(), preset);
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    RectTransform CreateChild(string name, AnchorPresets preset)
        => CreateChild(name, preset, (RectTransform)transform);

    void ApplyPreset(RectTransform r, AnchorPresets preset)
    {
        switch (preset)
        {
            case AnchorPresets.TopStretch:
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(1, 1);
                break;
            case AnchorPresets.TopCenter:
                r.anchorMin = new Vector2(0.5f, 1);
                r.anchorMax = new Vector2(0.5f, 1);
                break;
            case AnchorPresets.CenterTop:
                r.anchorMin = new Vector2(0.5f, 1);
                r.anchorMax = new Vector2(0.5f, 1);
                r.anchoredPosition = new Vector2(0, -250);
                r.sizeDelta = new Vector2(800, 100);
                return;
            case AnchorPresets.Center:
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                break;
            case AnchorPresets.StretchAll:
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                return;
        }
    }

    void SetOffsets(RectTransform r, float left, float bottom, float right, float top)
    {
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(right, top);
    }

    Image MakeImage(RectTransform parent, Sprite sprite, Color color, bool raycastOn = true)
    {
        var go = new GameObject("Image", typeof(RectTransform));
        ApplyPreset(go.GetComponent<RectTransform>(), AnchorPresets.StretchAll);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycastOn;
        return img;
    }

    Text MakeText(RectTransform parent, string name, string content,
        AnchorPresets preset, Vector2 pos, Vector2 size, int fontSize)
    {
        var rect = CreateChild(name, preset, parent);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var text = rect.gameObject.AddComponent<Text>();
        text.font = theme.fontFamily;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false; // labels must not eat button clicks
        text.text = content;
        return text;
    }

    Button MakeButton(RectTransform parent, string name, Sprite sprite, Color color,
        string label, AnchorPresets preset, Vector2 pos, Vector2 size)
    {
        var rect = CreateChild(name, preset, parent);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        if (!button.targetGraphic) button.targetGraphic = image;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(rect, false);
        var tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.font = theme.fontFamily;
        text.fontSize = (int)(theme.baseFontSize * 1.1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = theme.textColour;
        text.text = label;
        return button;
    }
}

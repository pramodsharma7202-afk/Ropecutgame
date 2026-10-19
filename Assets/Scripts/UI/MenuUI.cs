using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Boot/Menu UI, built entirely at runtime from the UITheme asset:
/// a MAIN MENU screen (title, Play, Levels, Sound toggle) and a LEVEL
/// SELECT screen (grid of level buttons with best-star display, locks for
/// not-yet-unlocked levels, Back). PlayerPrefs drives unlock state.
/// </summary>
public class MenuUI : MonoBehaviour
{
    LevelCatalog catalog;
    GameObject mainScreen;
    GameObject levelSelectScreen;

    Text soundLabel;
    RectTransform grid;

    void Awake()
    {
        var theme = Resources.Load<UITheme>("UITheme");
        if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();
        catalog = LevelCatalog.Load();

        BuildEventSystem();
        BuildCanvas();

        var builder = new UIBuilder(theme, transform);
        RectTransform gridOut;
        mainScreen = builder.BuildMainScreen(OnPlayClicked, OnLevelsClicked, OnSoundToggled, OnQuit);
        levelSelectScreen = builder.BuildLevelSelect(OnBackClicked, out gridOut);
        grid = gridOut;
        var soundTex = mainScreen.transform.Find("SoundButton/Label");
        if (soundTex != null) soundLabel = soundTex.GetComponent<Text>();

        ShowMain(true); ShowLevelSelect(false);
        Debug.Log("[MenuUI] main menu + level select built");
    }

    void OnEnable()
    {
        Refresh(); // star/unlock state may have changed since build
    }

    void ShowMain(bool on)
    {
        if (mainScreen != null) mainScreen.SetActive(on);
    }

    void ShowLevelSelect(bool on)
    {
        if (levelSelectScreen != null) levelSelectScreen.SetActive(on);
        if (on) Refresh();
    }

    void Update()
    {
        // Android back / Escape: in Level Select go back to main menu, else quit.
        var kb = UnityEngine.InputSystem.Keyboard.current;
        bool back = kb != null && kb.escapeKey.wasPressedThisFrame;
        var pad = UnityEngine.InputSystem.Gamepad.current;
        bool padBack = pad != null && pad.buttonEast.wasPressedThisFrame;
        if (!back && !padBack) return;
        if (levelSelectScreen != null && levelSelectScreen.activeInHierarchy)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play("Click");
            ShowLevelSelect(false); ShowMain(true);
        }
        else
        {
            Application.Quit();
        }
    }

    void Refresh()
    {
        if (soundLabel != null)
            soundLabel.text = "Sound: " + (AudioManager.Instance == null || !AudioManager.Instance.Muted ? "On" : "Off");
        BuildLevelButtons();
    }

    void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Click");
        int count = catalog != null && catalog.levels != null ? catalog.levels.Length : 5;
        int unlocked = PlayerPrefs.GetInt("Unlocked", 0);
        int idx = Mathf.Clamp(unlocked, 0, Mathf.Max(0, count - 1));
        PlayerPrefs.SetInt("CurrentLevel", idx);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
    }

    void OnLevelsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Click");
        ShowMain(false); ShowLevelSelect(true);
    }

    void OnSoundToggled()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        if (soundLabel != null)
            soundLabel.text = "Sound: " + (AudioManager.Instance == null || !AudioManager.Instance.Muted ? "On" : "Off");
    }

    void OnQuit()
    {
        Application.Quit();
    }

    void OnBackClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Click");
        ShowLevelSelect(false); ShowMain(true);
    }

    void OnLevelClicked(int index)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Click");
        PlayerPrefs.SetInt("CurrentLevel", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
    }

    void BuildLevelButtons()
    {
        if (grid == null) return;
        int count = catalog != null && catalog.levels != null && catalog.levels.Length > 0
            ? catalog.levels.Length : 5;
        int unlocked = PlayerPrefs.GetInt("Unlocked", 0);

        for (int i = grid.childCount - 1; i >= 0; i--)
            Destroy(grid.GetChild(i).gameObject);

        float cell = 170f, gap = 20f;
        float startX = -((count - 1) * (cell + gap)) / 2f;
        for (int i = 0; i < count; i++)
        {
            bool locked = i > unlocked;
            var btn = new GameObject("LevelButton_" + (i + 1), typeof(RectTransform));
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cell, cell);
            rt.anchoredPosition = new Vector2(startX + i * (cell + gap), 0);
            btn.transform.SetParent(grid, false);

            var theme = Resources.Load<UITheme>("UITheme");
            var b = btn.AddComponent<Button>();
            var img = btn.AddComponent<Image>();
            if (theme != null) { img.sprite = theme.secondaryButton; img.color = locked ? new Color(0.45f, 0.45f, 0.45f) : theme.secondaryColour; }
            b.targetGraphic = img;
            b.interactable = !locked;

            int captured = i;
            b.onClick.AddListener(() => OnLevelClicked(captured));

            // level number
            var numGo = new GameObject("Num", typeof(RectTransform));
            var nrt = numGo.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0.5f); nrt.anchorMax = nrt.anchorMin;
            nrt.sizeDelta = new Vector2(cell, 60);
            nrt.anchoredPosition = Vector2.up * 24;
            numGo.transform.SetParent(rt, false);
            var num = numGo.AddComponent<Text>();
            var t0 = Resources.Load<UITheme>("UITheme");
            num.font = UIFont.Get(t0);
            num.fontSize = 40;
            num.alignment = TextAnchor.MiddleCenter;
            num.raycastTarget = false;
            num.text = (i + 1).ToString();
            num.color = locked ? new Color(0.6f, 0.6f, 0.6f) : Color.white;

            // three small stars showing best score
            if (!locked)
            {
                int stars = PlayerPrefs.GetInt("Stars_" + i, 0);
                for (int s = 0; s < 3; s++)
                {
                    var starGo = new GameObject("Star" + s, typeof(RectTransform));
                    var srt = starGo.GetComponent<RectTransform>();
                    srt.sizeDelta = new Vector2(40, 40);
                    srt.anchoredPosition = new Vector2((s - 1) * 46, -30);
                    starGo.transform.SetParent(rt, false);
                    var sImg = starGo.AddComponent<Image>();
                    if (theme != null && theme.starIcon != null)
                    {
                        sImg.sprite = theme.starIcon;
                        sImg.color = s < stars ? theme.accentColour : new Color(0.35f, 0.35f, 0.35f, 0.55f);
                    }
                    sImg.raycastTarget = false;
                }
            }
            else
            {
                var lockGo = new GameObject("Lock", typeof(RectTransform));
                var lrt = lockGo.GetComponent<RectTransform>();
                lrt.sizeDelta = new Vector2(40, 40);
                lrt.anchoredPosition = new Vector2(0, -30);
                lockGo.transform.SetParent(rt, false);
                var lTxt = lockGo.AddComponent<Text>();
                var t1 = Resources.Load<UITheme>("UITheme");
                lTxt.font = UIFont.Get(t1);
                lTxt.fontSize = 34;
                lTxt.alignment = TextAnchor.MiddleCenter;
                lTxt.raycastTarget = false;
                lTxt.text = "X";
                lTxt.color = new Color(0.8f, 0.6f, 0.2f);
            }
        }
    }

    // ---- shared canvas / event system builders -------------------------

    void BuildEventSystem()
    {
        var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        var es = all.Length > 0 ? all[0] : null;
        for (int i = 1; i < all.Length; i++) Destroy(all[i].gameObject);
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            es = go.AddComponent<EventSystem>();
        }
        var module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (module.actionsAsset == null) module.AssignDefaultActions();
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
}

/// <summary>
/// Internal screen layout helpers for MenuUI (keeps MenuUI readable).
/// </summary>
internal class UIBuilder
{
    readonly UITheme theme;
    readonly Transform root;

    public UIBuilder(UITheme theme, Transform root) { this.theme = theme; this.root = root; }

    public GameObject BuildMainScreen(
        System.Action play, System.Action levels, System.Action sound, System.Action quit)
    {
        var screen = Panel("MainScreen", root);
        Title(screen.transform, "Title", "Rope Rescue", 96, 520);
        MakeButton(screen.transform, "PlayButton", "Play", new Vector2(0, -200), new Vector2(420, 120), play, true);
        MakeButton(screen.transform, "LevelsButton", "Levels", new Vector2(0, -360), new Vector2(420, 110), levels, false);
        MakeButton(screen.transform, "SoundButton", "Sound: On", new Vector2(0, -500), new Vector2(420, 100), sound, false);
        return screen;
    }

    public GameObject BuildLevelSelect(
        System.Action back, out RectTransform gridOut)
    {
        var screen = Panel("LevelSelectScreen", root);
        Title(screen.transform, "Title", "Levels", 64, 500);

        gridOut = null;
        var gridGo = new GameObject("Grid", typeof(RectTransform));
        var gridRt = gridGo.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f); gridRt.anchorMax = gridRt.anchorMin;
        gridRt.anchoredPosition = new Vector2(0, -80);
        gridRt.sizeDelta = new Vector2(1080, 200);
        gridGo.transform.SetParent(screen.transform, false);
        gridOut = gridRt;

        MakeButton(screen.transform, "BackButton", "Back", new Vector2(0, -620), new Vector2(300, 100), back, false);
        return screen;
    }

    GameObject Panel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (theme != null && theme.panel != null) { img.sprite = theme.panel; img.color = new Color(0.12f, 0.13f, 0.18f, 1f); }
        else img.color = new Color(0.12f, 0.13f, 0.18f, 1f);
        return go;
    }

    void Title(Transform parent, string name, string text, int size, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = rt.anchorMin;
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(900, 140);
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<Text>();
        t.font = UIFont.Get(theme);
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.color = theme != null ? theme.textColour : Color.white;
        t.text = text;
    }

    void MakeButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, System.Action onClick, bool primary)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = rt.anchorMin;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.transform.SetParent(parent.transform, false);

        var b = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        bool have = theme != null;
        if (have && primary && theme.primaryButton != null) img.sprite = theme.primaryButton;
        else if (have && !primary && theme.secondaryButton != null) img.sprite = theme.secondaryButton;
        img.color = have ? (primary ? theme.primaryColour : theme.secondaryColour) : new Color(0.3f, 0.69f, 0.96f);
        b.targetGraphic = img;
        b.onClick.AddListener(() => onClick());

        var lblGo = new GameObject("Label", typeof(RectTransform));
        var lr = lblGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        lblGo.transform.SetParent(rt, false);
        var t = lblGo.AddComponent<Text>();
        t.font = UIFont.Get(theme);
        t.fontSize = (int)(theme != null ? theme.baseFontSize * 1.1f : 36);
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.color = have ? theme.textColour : Color.white;
        t.text = label;
    }
}

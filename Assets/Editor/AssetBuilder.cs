using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RopeCut.EditorTools
{
    /// <summary>
    /// One-shot asset builder executed via reflection from MCP tools.
    /// Creates physics/gameplay prefabs, UI theme + ui prefabs and AudioManager.
    /// Uses only Kenney CC0 assets already imported in the project.
    /// </summary>
    public static class AssetBuilder
    {
        // ---- Kenney sprite paths -------------------------------------------------
        const string HeroSprite = "Assets/Art/Characters/Animal Pack/PNG/Round/panda.png";
        const string AnchorSprite = "Assets/Art/Props/Physics Assets/PNG/Other/rock.png";
        const string PinSprite = "Assets/Art/UI/UI Pack/Blue/Default/icon_circle.png";
        const string BoxSprite = "Assets/Art/Props/Physics Assets/PNG/Wood elements/elementWood000.png";
        const string SpikesSprite = "Assets/Art/Props/Physics Assets/PNG/Other/cactus.png";
        const string FireSprite = "Assets/Art/Particles/fire_01.png";
        const string WaterSprite = "Assets/Art/UI/UI Pack/Blue/Default/button_square_flat.png";
        const string GoalSprite = "Assets/Art/Props/Physics Assets/PNG/Other/flagGreen.png";
        const string PrimaryButtonSprite = "Assets/Art/UI/UI Pack/Blue/Default/button_rectangle_depth_gloss.png";
        const string SecondaryButtonSprite = "Assets/Art/UI/UI Pack/Grey/Default/button_rectangle_depth_gloss.png";
        const string PanelSprite = "Assets/Art/UI/UI Pack/Extra/Default/input_rectangle.png";
        const string StarSprite = "Assets/Art/UI/UI Pack/Yellow/Default/star.png";
        const string LockSprite = "Assets/Art/UI/Game Icons/PNG/Black/2x/locked.png";
        const string FontAsset = "Assets/Art/Fonts/Kenney Bold.ttf";

        // Fallback: elongated wood element for the rope placeholder.
        const string RopeRoot = "Assets/Art/Props/Physics Assets/PNG/Wood elements";

        static Sprite S(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"AssetBuilder: sprite missing: {path}");
            return s;
        }

        static Sprite FindRopeSegment()
        {
            // pick the most elongated wood element sprite to fake a rope piece
            Sprite best = null;
            float bestRatio = 0f;
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { RopeRoot }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s == null) continue;
                float ratio = s.texture.width / (float)Mathf.Max(1, s.texture.height);
                if (ratio > bestRatio) { bestRatio = ratio; best = s; }
            }
            return best;
        }

        static GameObject SavePrefab(string name, GameObject go, string folder = "Assets/Prefabs")
        {
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            go.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        [MenuItem("RopeCut/Build Asset Bundle")]
        public static void BuildAll() => Build("all");

        public static string Build(string mode)
        {
            var results = new System.Collections.Generic.List<string>();
            if (mode == "all" || mode == "prefabs") results.Add(BuildGameplayPrefabs());
            if (mode == "all" || mode == "theme") results.Add(BuildUiTheme());
            if (mode == "all" || mode == "audio") results.Add(BuildAudioManager());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return string.Join(" | ", results.ToArray());
        }

        // ---- Step 6: gameplay prefabs ---------------------------------------------
        public static string BuildGameplayPrefabs()
        {
            int created = 0;

            // HERO
            {
                var go = new GameObject("Hero");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(HeroSprite);
                var rb = go.AddComponent<Rigidbody2D>();
                rb.mass = 1f;
                rb.linearDamping = 0.05f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.45f;
                go.AddComponent<Animator>(); // animator-ready; controller assignable at gameplay time
                SavePrefab("Hero", go);
                Debug.Log("[AssetBuilder] Hero OK");
                created++;
            }

            // ANCHOR (static rope start point)
            {
                var go = new GameObject("Anchor");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(AnchorSprite);
                sr.sortingOrder = 5;
                SavePrefab("Anchor", go);
                Debug.Log("[AssetBuilder] Anchor OK");
                created++;
            }

            // PIN (cut point marker)
            {
                var go = new GameObject("Pin");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(PinSprite);
                sr.sortingOrder = 6;
                sr.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                SavePrefab("Pin", go);
                Debug.Log("[AssetBuilder] Pin OK");
                created++;
            }

            // BOX
            {
                var go = new GameObject("Box");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(BoxSprite);
                var rb = go.AddComponent<Rigidbody2D>();
                rb.mass = 2f;
                var col = go.AddComponent<BoxCollider2D>();
                col.autoTiling = true;
                SavePrefab("Box", go);
                Debug.Log("[AssetBuilder] Box OK");
                created++;
            }

            // SPIKES (deadly static hazard)
            {
                var go = new GameObject("Spikes");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(SpikesSprite);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = false;
                var tag = go.AddComponent<RopeCutHazard>();
                tag.tag = RopeCutHazard.Tag;
                SavePrefab("Spikes", go);
                Debug.Log("[AssetBuilder] Spikes OK");
                created++;
            }

            // FIRE
            {
                var go = new GameObject("Fire");
                var ps = go.AddComponent<ParticleSystem>();
                var sr = go.GetComponent<ParticleSystemRenderer>();
                var fire = S(FireSprite);
                if (fire != null) {
                    var tsa = ps.textureSheetAnimation;
                    tsa.mode = ParticleSystemAnimationMode.Sprites;
                    tsa.AddSprite(fire);
                }
                sr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                var main = ps.main;
                main.duration = 0.8f;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
                main.startColor = new Color(1f, 0.72f, 0.15f);
                main.gravityModifier = -0.15f;
                main.maxParticles = 40;
                var em = ps.emission;
                em.rateOverTime = 18f;
                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.25f;
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
                ps.Play();
                SavePrefab("Fire", go);
                Debug.Log("[AssetBuilder] Fire OK");
                created++;
            }

            // WATER (trigger-only surface zone)
            {
                var go = new GameObject("Water");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(WaterSprite);
                sr.color = new Color(0.20f, 0.62f, 0.95f, 0.75f);
                sr.transform.localScale = new Vector3(4f, 1f, 1f);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true; // trigger: drown-on-contact instead of solid
                SavePrefab("Water", go);
                Debug.Log("[AssetBuilder] Water OK");
                created++;
            }

            // DOOR / GOAL
            {
                var go = new GameObject("Door");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = S(GoalSprite);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.autoTiling = true;
                go.AddComponent<RopeCutGoal>();
                SavePrefab("Door", go);
                Debug.Log("[AssetBuilder] Door OK");
                created++;
            }

            // ROPE SEGMENT placeholder
            {
                var go = new GameObject("RopeSegment");
                var s = FindRopeSegment();
                var sr = go.AddComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = s;
                sr.color = new Color(0.75f, 0.6f, 0.25f);
                go.transform.localScale = new Vector3(0.08f, 0.35f, 1f); // thin vertical link
                var rb = go.AddComponent<Rigidbody2D>();
                rb.mass = 0.1f;
                rb.linearDamping = 0.05f;
                var hj = go.AddComponent<HingeJoint2D>();
                hj.useLimits = true;
                hj.limits = new JointAngleLimits2D { min = -60f, max = 60f };
                hj.enableCollision = false;
                var bc = go.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(0.1f, 1f);
                SavePrefab("RopeSegment", go);
                Debug.Log("[AssetBuilder] RopeSegment OK");
                created++;
            }
            return $"Gameplay prefabs created: {created}";
        }

        // ---- Step 7: UI theme + prefabs --------------------------------------------
        public static string BuildUiTheme()
        {
            // theme asset
            const string themePath = "Assets/Art/UI/UITheme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, themePath);
            }
            theme.fontFamily = AssetDatabase.LoadAssetAtPath<Font>(FontAsset);
            theme.primaryButton = S(PrimaryButtonSprite);
            theme.secondaryButton = S(SecondaryButtonSprite);
            theme.panel = S(PanelSprite);
            theme.starIcon = S(StarSprite);
            theme.lockIcon = S(LockSprite);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            // ui prefabs
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.layer = LayerMask.NameToLayer("UI");
            var panel = new GameObject("Panel");
            var prt = panel.AddComponent<RectTransform>();
            prt.sizeDelta = new Vector2(560, 340);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            var pimg = panel.AddComponent<Image>();
            pimg.sprite = S(PanelSprite);
            pimg.type = Image.Type.Sliced;
            pimg.color = theme.panelColour;
            var ptext = ChildLabelText(panel, "Panel", Color.white);
            ptext.alignment = TextAnchor.MiddleCenter;

            var primary = UiButton(canvas, "PrimaryButton", PrimaryButtonSprite, "PLAY");
            SavePrefab("PrimaryButton", primary);
            var secondary = UiButton(canvas, "SecondaryButton", SecondaryButtonSprite, "TAP");
            SavePrefab("SecondaryButton", secondary);

            var star = IconPrefab(canvas, "StarIcon", StarSprite, theme.accentColour);
            SavePrefab("StarIcon", star);
            var icon = IconPrefab(canvas, "LockIcon", LockSprite, Color.white);
            SavePrefab("LockIcon", icon);
            Object.DestroyImmediate(canvas);

            return "UITheme asset + 5 UI prefabs created";
        }

        static GameObject CreatedUiPrefab(GameObject src, string n, Sprite s, Color c, string label)
        {
            var go = new GameObject(n);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(560, 340);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = s;
            img.type = Image.Type.Sliced;
            img.color = c;
            return SavePrefab(n, go);
        }

        static Text ChildLabelText(GameObject parent, string txt, Color c)
        {
            var child = new GameObject("Text", typeof(RectTransform));
            child.layer = LayerMask.NameToLayer("UI");
            var rt = child.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = child.AddComponent<Text>();
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Art/UI/UITheme.asset");
            t.font = theme != null && theme.fontFamily != null ? theme.fontFamily : AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
            t.text = txt;
            t.color = c;
            t.fontSize = 36;
            t.alignment = TextAnchor.MiddleCenter;
            child.transform.SetParent(parent.transform, false);
            return t;
        }

        static GameObject UiButton(GameObject parent, string name, string spritePath, string label)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 110);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = S(spritePath);
            img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Art/UI/UITheme.asset");
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white * 0.9f;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            btn.colors = colors;
            ChildLabelText(go, label, theme != null ? theme.textColour : Color.white);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject IconPrefab(GameObject parent, string name, string spritePath, Color tint)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(96, 96);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = S(spritePath);
            img.color = tint;
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ---- Step 8: AudioManager prefab --------------------------------------------
        public static string BuildAudioManager()
        {
            var go = new GameObject("AudioManager");
            var am = go.AddComponent<AudioManager>();
            string SfxRoot = "Assets/Audio/SFX/UI", ImpRoot = "Assets/Audio/SFX/Impact", MusRoot = "Assets/Audio/Music";

            am.click = FirstClip(SfxRoot, "click_001.ogg");
            am.cut = FirstClip(ImpRoot, "impactWood_light_000.ogg") ?? FirstClip(ImpRoot, "impactGeneric_light_000.ogg");
            am.fall = FirstClip(ImpRoot, "impactPlank_medium_000.ogg") ?? FirstClip(SfxRoot, "drop_001.ogg");
            am.win = FirstClip(SfxRoot, "confirmation_001.ogg");
            am.lose = FirstClip(SfxRoot, "error_001.ogg");
            am.pop = FirstClip(SfxRoot, "click_005.ogg") ?? FirstClip(SfxRoot, "bong_001.ogg");
            am.music = FirstClip(MusRoot, "Mishief Stroll.ogg") ?? FirstClip(MusRoot, "Alpha Dance.ogg");

            SavePrefab("AudioManager", go);
            return "AudioManager prefab created (with 6 SFX + music wired)";
        }

        static AudioClip FirstClip(string folder, string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}/{name}");
            if (c != null) return c;
            // name-insensitive ux fallback: first matching prefix
            foreach (var guid in AssetDatabase.FindAssets(name.Split('_')[0], new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(name)) return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            }
            // last resort: first clip in folder
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            }
            return null;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Project-wide UI theme: font, sprite and brand colours for buttons, panels and icons.
/// Reference from any MonoBehaviour via TMPro/UI code.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "RopeCut/UITheme")]
public class UITheme : ScriptableObject
{
    [Header("Font")]
    public Font fontFamily;
    public int baseFontSize = 36;

    [Header("Sprites")]
    public Sprite primaryButton;
    public Sprite secondaryButton;
    public Sprite panel;
    public Sprite starIcon;
    public Sprite lockIcon;

    [Header("Colours")]
    public Color primaryColour = new Color(0.30f, 0.69f, 0.96f);   // Kenney Blue
    public Color secondaryColour = new Color(0.60f, 0.60f, 0.62f); // Kenney Grey
    public Color accentColour = new Color(0.98f, 0.86f, 0.15f);    // Kenney Yellow / star
    public Color textColour = Color.white;
    public Color panelColour = new Color(0.30f, 0.69f, 0.96f, 0.85f);
}

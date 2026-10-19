using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Font helper for legacy Text: prefers the UITheme font, falls back to the
/// built-in LegacyRuntime.ttf so no Text is created with a null font.
/// </summary>
public static class UIFont
{
    public static Font Get(UITheme theme)
    {
        if (theme != null && theme.fontFamily != null) return theme.fontFamily;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}

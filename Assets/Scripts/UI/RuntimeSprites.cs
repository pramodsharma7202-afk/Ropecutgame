using UnityEngine;

/// <summary>
/// Runtime sprite builders (no AssetDatabase, works in builds):
/// vertical gradient, white square, round knot circle and a triangle.
/// Sprites are cached so repeated loads reuse them.
/// </summary>
public static class RuntimeSprites
{
    static Sprite gradient;
    static Sprite white;
    static Sprite circle;
    static Sprite triangle;

    /// <summary>1x1 world-unit vertical gradient (bottom->top colour), 4x128 px.</summary>
    public static Sprite GradientSprite(Color bottom, Color top)
    {
        if (gradient != null) return gradient;
        var tex = new Texture2D(4, 128, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 128; y++)
        {
            float t = y / 127f;
            var c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, c);
        }
        tex.Apply();
        gradient = Sprite.Create(tex, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f), 128f);
        return gradient;
    }

    /// <summary>Solid white square (pixelsPerUnit chosen so size = world units).</summary>
    public static Sprite WhiteSprite()
    {
        if (white != null) return white;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var c = Color.white;
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, c);
        tex.Apply();
        white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return white;
    }

    /// <summary>Round filled circle with a soft edge; 1 world unit diameter.</summary>
    public static Sprite CircleSprite(int size = 64)
    {
        if (circle != null) return circle;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f, dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a = Mathf.Clamp01((1f - d) * 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circle;
    }

    /// <summary>Solid upright triangle; 1 world unit wide/tall.</summary>
    public static Sprite TriangleSprite()
    {
        if (triangle != null) return triangle;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            // width shrinks toward the top so it reads as an up-pointing arrow
            float t = y / (float)(size - 1);
            float halfW = Mathf.Lerp(size * 0.5f, 2f, t) - 1f;
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - (size - 1) * 0.5f) <= halfW;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
        }
        tex.Apply();
        triangle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return triangle;
    }
}

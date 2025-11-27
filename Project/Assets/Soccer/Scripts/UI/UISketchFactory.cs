using UnityEngine;

/// <summary>
/// 간단한 드로잉 기반 플레이스홀더 스프라이트를 생성한다.
/// </summary>
public static class UISketchFactory
{
    const int TextureSize = 256;
    const int LineThickness = 4;
    static readonly Color StrokeColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    public static Sprite CreateCharacterSketch()
    {
        return CreateSketch(tex =>
        {
            Vector2 center = new Vector2(TextureSize * 0.5f, TextureSize * 0.68f);
            DrawCircle(tex, center, 60, StrokeColor, LineThickness);

            Vector2 neck = new Vector2(TextureSize * 0.5f, TextureSize * 0.45f);
            DrawLine(tex, center + new Vector2(0, -60), neck, StrokeColor, LineThickness);

            Vector2 bodyBottom = new Vector2(TextureSize * 0.5f, TextureSize * 0.2f);
            DrawLine(tex, neck, bodyBottom, StrokeColor, LineThickness);

            Vector2 leftLeg = new Vector2(TextureSize * 0.35f, TextureSize * 0.05f);
            Vector2 rightLeg = new Vector2(TextureSize * 0.65f, TextureSize * 0.05f);
            DrawLine(tex, bodyBottom, leftLeg, StrokeColor, LineThickness);
            DrawLine(tex, bodyBottom, rightLeg, StrokeColor, LineThickness);

            Vector2 leftArm = new Vector2(TextureSize * 0.3f, TextureSize * 0.38f);
            Vector2 rightArm = new Vector2(TextureSize * 0.7f, TextureSize * 0.38f);
            DrawLine(tex, new Vector2(TextureSize * 0.42f, TextureSize * 0.42f), leftArm, StrokeColor, LineThickness);
            DrawLine(tex, new Vector2(TextureSize * 0.58f, TextureSize * 0.42f), rightArm, StrokeColor, LineThickness);
        });
    }

    public static Sprite CreateShirtSketch()
    {
        return CreateSketch(tex =>
        {
            Vector2 topLeft = new Vector2(TextureSize * 0.25f, TextureSize * 0.75f);
            Vector2 topRight = new Vector2(TextureSize * 0.75f, TextureSize * 0.75f);
            Vector2 bottomRight = new Vector2(TextureSize * 0.7f, TextureSize * 0.2f);
            Vector2 bottomLeft = new Vector2(TextureSize * 0.3f, TextureSize * 0.2f);

            DrawLine(tex, topLeft, topRight, StrokeColor, LineThickness);
            DrawLine(tex, topRight, bottomRight, StrokeColor, LineThickness);
            DrawLine(tex, bottomRight, bottomLeft, StrokeColor, LineThickness);
            DrawLine(tex, bottomLeft, topLeft, StrokeColor, LineThickness);

            // Sleeves
            Vector2 leftSleeveStart = new Vector2(TextureSize * 0.3f, TextureSize * 0.6f);
            Vector2 leftSleeveEnd = new Vector2(TextureSize * 0.18f, TextureSize * 0.55f);
            DrawLine(tex, leftSleeveStart, leftSleeveEnd, StrokeColor, LineThickness);
            DrawLine(tex, leftSleeveEnd, new Vector2(TextureSize * 0.28f, TextureSize * 0.45f), StrokeColor, LineThickness);

            Vector2 rightSleeveStart = new Vector2(TextureSize * 0.7f, TextureSize * 0.6f);
            Vector2 rightSleeveEnd = new Vector2(TextureSize * 0.82f, TextureSize * 0.55f);
            DrawLine(tex, rightSleeveStart, rightSleeveEnd, StrokeColor, LineThickness);
            DrawLine(tex, rightSleeveEnd, new Vector2(TextureSize * 0.72f, TextureSize * 0.45f), StrokeColor, LineThickness);
        });
    }

    public static Sprite CreatePantsSketch()
    {
        return CreateSketch(tex =>
        {
            Vector2 topLeft = new Vector2(TextureSize * 0.3f, TextureSize * 0.75f);
            Vector2 topRight = new Vector2(TextureSize * 0.7f, TextureSize * 0.75f);
            Vector2 leftInner = new Vector2(TextureSize * 0.45f, TextureSize * 0.25f);
            Vector2 rightInner = new Vector2(TextureSize * 0.55f, TextureSize * 0.25f);
            Vector2 bottomLeft = new Vector2(TextureSize * 0.32f, TextureSize * 0.05f);
            Vector2 bottomRight = new Vector2(TextureSize * 0.68f, TextureSize * 0.05f);

            DrawLine(tex, topLeft, topRight, StrokeColor, LineThickness);
            DrawLine(tex, topLeft, bottomLeft, StrokeColor, LineThickness);
            DrawLine(tex, topRight, bottomRight, StrokeColor, LineThickness);
            DrawLine(tex, bottomLeft, leftInner, StrokeColor, LineThickness);
            DrawLine(tex, leftInner, rightInner, StrokeColor, LineThickness);
            DrawLine(tex, rightInner, bottomRight, StrokeColor, LineThickness);
        });
    }

    static Sprite CreateSketch(System.Action<Texture2D> paint)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Clear(tex);
        paint?.Invoke(tex);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
    }

    static void Clear(Texture2D tex)
    {
        Color clear = new Color(0, 0, 0, 0);
        var pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }
        tex.SetPixels(pixels);
    }

    static void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color, int thickness)
    {
        int x0 = Mathf.RoundToInt(start.x);
        int y0 = Mathf.RoundToInt(start.y);
        int x1 = Mathf.RoundToInt(end.x);
        int y1 = Mathf.RoundToInt(end.y);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Plot(tex, x0, y0, color, thickness);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    static void DrawCircle(Texture2D tex, Vector2 center, float radius, Color color, int thickness)
    {
        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);
        int r = Mathf.RoundToInt(radius);
        int t = Mathf.Max(1, thickness);

        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                float dist = Mathf.Sqrt(x * x + y * y);
                if (dist >= r - t && dist <= r + t)
                {
                    Plot(tex, cx + x, cy + y, color, 1);
                }
            }
        }
    }

    static void Plot(Texture2D tex, int x, int y, Color color, int thickness)
    {
        for (int ix = -thickness; ix <= thickness; ix++)
        {
            for (int iy = -thickness; iy <= thickness; iy++)
            {
                int px = Mathf.Clamp(x + ix, 0, tex.width - 1);
                int py = Mathf.Clamp(y + iy, 0, tex.height - 1);
                tex.SetPixel(px, py, color);
            }
        }
    }
}

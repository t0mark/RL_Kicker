using UnityEngine;

public static class TextureUtil
{
    public static Texture2D FromBase64Png(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;

        int comma = s.IndexOf(',');
        if (comma >= 0) s = s.Substring(comma + 1);

        byte[] bytes = System.Convert.FromBase64String(s);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }
}

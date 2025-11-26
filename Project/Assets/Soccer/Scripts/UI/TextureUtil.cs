using UnityEngine;
using UnityEngine.Networking;

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

    public static Texture2D FromUnityWebRequest(UnityWebRequest req)
    {
        if (req?.downloadHandler == null) return null;

        var data = req.downloadHandler.data;
        if (data == null || data.Length == 0) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(data)) return null;

        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }
}

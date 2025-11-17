using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

/// 요청/응답 DTO
[System.Serializable]
public class GenRequest
{
    public string prompt;
    public string team;
    public float  tiling;
    public float  strength;
}

// 서버 응답을 폭넓게 커버하기 위한 통합 DTO
[System.Serializable]
class GenResponse
{
    // URL 계열
    public string image_url;
    public string imageUrl;
    public string url;

    // base64 계열
    public string image_base64;
    public string imageBase64;
}

public class PromptUniformBridge : MonoBehaviour
{
    [Header("Hook to TeamUniformController in scene")]
    public TeamUniformController ctrl;

    [Header("Your server endpoint (POST)")]
    public string endpoint = "http://localhost:8000/generate"; // 서버 주소

    // 기존 엔트리(호환)
    public void GenerateAndApply(string prompt, bool toBlueTeam, Vector2 tiling, float strength = 1f)
    {
        StartCoroutine(Co_GenerateAndApplySafe(prompt, toBlueTeam, tiling, strength, null));
    }

    /// <summary>
    /// 안전 코루틴: 타임아웃/가드/재시도(1회). done == null 가능
    /// </summary>
    public IEnumerator Co_GenerateAndApplySafe(string prompt, bool toBlueTeam, Vector2 tiling, float strength, System.Action<bool> done)
    {
        string ep = (endpoint ?? "").Trim();
        prompt = (prompt ?? "").Trim();

        if (ctrl == null)
        {
            Debug.LogWarning("[UniformBridge] TeamUniformController is null.");
        }

        if (string.IsNullOrEmpty(ep))
        {
            // 서버가 없을 때: 가짜 패턴으로 확인
            Debug.LogWarning("[UniformBridge] endpoint is empty → Fake checker pattern 사용");
            var texFake = FakePatternGenerator.MakeChecker(512, 24);
            Apply(texFake, toBlueTeam, tiling, strength);
            done?.Invoke(true);
            yield break;
        }

        // POST 바디
        var body = JsonUtility.ToJson(new GenRequest
        {
            prompt   = prompt,
            team     = toBlueTeam ? "blue" : "purple",
            tiling   = tiling.x,
            strength = strength
        });

        byte[] bytes = Encoding.UTF8.GetBytes(body);

        bool success = false;

        // 재시도 1회
        for (int attempt = 0; attempt < 2 && !success; attempt++)
        {
            using (var req = new UnityWebRequest(ep, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 60;

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[UniformBridge] POST failed (attempt {attempt}): {req.error}");
                    continue; // 다음 루프에서 재시도
                }

                string json = req.downloadHandler.text ?? "";
                Debug.Log($"[UniformBridge] server json: {json}");

                // ---------- 1단계: 구조화된 JSON 파싱 ----------
                GenResponse resp = null;
                try
                {
                    resp = JsonUtility.FromJson<GenResponse>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UniformBridge] Json parse error: {e.Message}");
                }

                string url = "";
                string b64 = "";

                if (resp != null)
                {
                    url = (resp.image_url ?? resp.imageUrl ?? resp.url ?? "").Trim();
                    b64 = (resp.image_base64 ?? resp.imageBase64 ?? "").Trim();
                }

                // ---------- 2단계: 문자열에서 직접 URL 뽑기(폴백) ----------
                if (string.IsNullOrEmpty(url))
                {
                    int idx = json.IndexOf("http", StringComparison.OrdinalIgnoreCase);
                    int end = (idx >= 0) ? json.IndexOf(".png", idx, StringComparison.OrdinalIgnoreCase) : -1;
                    if (idx >= 0 && end > idx)
                    {
                        url = json.Substring(idx, end - idx + 4);
                        url = url.Trim('\"');
                        Debug.Log($"[UniformBridge] Fallback URL extracted: {url}");
                    }
                }

                Texture2D tex = null;

                // ---------- 3단계: URL로 텍스처 다운 ----------
                if (!string.IsNullOrEmpty(url))
                {
                    using (var getImg = UnityWebRequestTexture.GetTexture(url))
                    {
                        getImg.timeout = 60;
                        yield return getImg.SendWebRequest();

                        if (getImg.result == UnityWebRequest.Result.Success)
                        {
                            tex = DownloadHandlerTexture.GetContent(getImg);
                            Debug.Log("[UniformBridge] Texture downloaded from URL.");
                        }
                        else
                        {
                            Debug.LogWarning($"[UniformBridge] Texture GET failed: {getImg.error} (url={url})");
                        }
                    }
                }

                // ---------- 4단계: base64 폴백 ----------
                if (tex == null && !string.IsNullOrEmpty(b64))
                {
                    tex = TextureUtil.FromBase64Png(b64);
                    if (tex != null)
                    {
                        Debug.Log("[UniformBridge] Texture created from base64.");
                    }
                }

                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    Apply(tex, toBlueTeam, tiling, strength);
                    success = true;
                }
                else
                {
                    Debug.LogWarning("[UniformBridge] No texture decoded from server response.");
                }
            }
        }

        if (!success)
            Debug.LogWarning("[UniformBridge] GenerateAndApply failed after retries.");

        done?.Invoke(success);
    }

    void Apply(Texture2D tex, bool toBlueTeam, Vector2 tiling, float strength)
    {
        if (tex == null)
        {
            Debug.LogWarning("[UniformBridge] Apply called with null texture.");
            return;
        }

        strength = Mathf.Clamp01(strength);

        if (toBlueTeam)
            ctrl.ApplyPatternToBlue(tex, tiling, strength);
        else
            ctrl.ApplyPatternToPurple(tex, tiling, strength);
    }
}

// (테스트 전용) 서버 없이 체크용
public static class FakePatternGenerator
{
    public static Texture2D MakeChecker(int size = 512, int cell = 32)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool c = ((x / cell) + (y / cell)) % 2 == 0;
            var col = c ? new Color(0.9f, 0.9f, 0.9f, 1) : new Color(0.1f, 0.1f, 0.1f, 1);
            tex.SetPixel(x, y, col);
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        return tex;
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class PatternDownloader : MonoBehaviour
{
    [Header("Optional quick test (leave empty to disable)")]
    public string testUrl = "";          // 인스펙터에서 간단 테스트용
    public bool autoRunOnStart = false;  // Start 때 자동 실행 여부
    public bool applyToBlue = true;      // 자동 실행 시 적용 팀
    public Vector2 testTiling = new Vector2(4, 4);
    [Range(0f,1f)] public float testStrength = 1f;

    [Header("Refs")]
    public TeamUniformController ctrl;

    void Start()
    {
        if (autoRunOnStart && !string.IsNullOrWhiteSpace(testUrl))
            StartCoroutine(Co_ApplyFromUrl(testUrl, applyToBlue, testTiling, testStrength));
    }

    [ContextMenu("TEST / ApplyFromUrl(testUrl)")]
    void ContextTest()
    {
        if (!string.IsNullOrWhiteSpace(testUrl))
            StartCoroutine(Co_ApplyFromUrl(testUrl, applyToBlue, testTiling, testStrength));
        else
            Debug.LogWarning("[PatternDownloader] testUrl is empty. Nothing to do.");
    }

    /// <summary>
    /// 기존 호출부(StartCoroutine(d.ApplyFromUrl(...)))와 호환되는 엔트리
    /// </summary>
    public IEnumerator ApplyFromUrl(string url, bool toBlueTeam, Vector2 tiling, float strength = 1f)
    {
        yield return Co_ApplyFromUrl(url, toBlueTeam, tiling, strength);
    }

    private IEnumerator Co_ApplyFromUrl(string rawUrl, bool toBlueTeam, Vector2 tiling, float strength = 1f)
    {
        // --- 입력 가드 ---
        rawUrl = (rawUrl ?? "").Trim();
        if (string.IsNullOrEmpty(rawUrl))
        {
            Debug.LogWarning("[PatternDownloader] URL empty. Skip.");
            yield break;
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Debug.LogWarning($"[PatternDownloader] Invalid URL: '{rawUrl}'. Skip.");
            yield break;
        }

        if (ctrl == null)
        {
            Debug.LogWarning("[PatternDownloader] TeamUniformController not set. Skip.");
            yield break;
        }

        // --- 다운로드 ---
        using (var req = UnityWebRequest.Get(uri.ToString()))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 60;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PatternDownloader] GET failed: {req.error} (url={uri})");
                yield break;
            }

            var tex = TextureUtil.FromUnityWebRequest(req);
            if (tex == null)
            {
                Debug.LogError("[PatternDownloader] Downloaded texture is null.");
                yield break;
            }

            tex.wrapMode = TextureWrapMode.Repeat;

            // --- 적용 ---
            strength = Mathf.Clamp01(strength);
            if (toBlueTeam) ctrl.ApplyPatternToBlue(tex, tiling, strength);
            else            ctrl.ApplyPatternToPurple(tex, tiling, strength);
        }
    }
}

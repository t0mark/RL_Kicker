using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UniformPromptUI : MonoBehaviour
{
    [Header("UI")]
    public InputField inputPrompt;
    public Dropdown teamDropdown;
    public Slider tilingSlider;
    public Slider strengthSlider;
    public Button generateBtn;
    public GameObject loadingSpinner;
    public Text toastText;

    [Header("Bridge")]
    public PromptUniformBridge bridge;

    bool _busy;

    void Awake()
    {
        if (generateBtn != null) generateBtn.onClick.AddListener(OnClickGenerate);
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (toastText) toastText.gameObject.SetActive(false);
    }

    public void OnClickGenerate()
    {
        if (_busy) return;

        var prompt = (inputPrompt ? inputPrompt.text : "").Trim();
        if (string.IsNullOrEmpty(prompt)) { ShowToast("프롬프트를 입력해줘!"); return; }

        bool toBlue = (teamDropdown ? teamDropdown.value == 0 : true);
        float t = Mathf.Clamp(tilingSlider ? tilingSlider.value : 4f, 0.5f, 12f);
        float s = Mathf.Clamp01(strengthSlider ? strengthSlider.value : 1f);

        StartCoroutine(Co_Run(prompt, toBlue, t, s));
    }

    IEnumerator Co_Run(string prompt, bool toBlue, float tiling, float strength)
    {
        _busy = true;
        if (generateBtn) generateBtn.interactable = false;
        if (loadingSpinner) loadingSpinner.SetActive(true);

        bool ok = false;
        yield return bridge.Co_GenerateAndApplySafe(prompt, toBlue, new Vector2(tiling, tiling), strength, success => ok = success);

        if (!ok) ShowToast("생성 실패: 서버/네트워크 확인");
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (generateBtn) generateBtn.interactable = true;
        _busy = false;
    }

    void ShowToast(string msg)
    {
        if (!toastText) return;
        StopAllCoroutines();
        StartCoroutine(CoToast(msg));
    }

    IEnumerator CoToast(string msg)
    {
        toastText.text = msg;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        toastText.gameObject.SetActive(false);
    }
}
using TMPro;
using UnityEngine;

public class ScoreUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI blueScore;
    public TextMeshProUGUI purpleScore;

    public MonoBehaviour scoreSource;

    [Header("Fade Settings")]
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1f;

    void OnEnable()
    {
        SoccerEnvController.OnScoreChanged += OnScoreChanged;
        TryInitCurrentScore();
    }

    void OnDisable()
    {
        SoccerEnvController.OnScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int blue, int purple)
    {
        if (blueScore) blueScore.text = blue.ToString();
        if (purpleScore) purpleScore.text = purple.ToString();
    }

    void TryInitCurrentScore()
    {
        if (!scoreSource)
        {
            scoreSource = FindFirstObjectByType<SoccerEnvController>();
        }

        var src = scoreSource as SoccerEnvController;
        if (src != null)
        {
            OnScoreChanged(src.BlueScore, src.PurpleScore);
        }
        else
        {
            OnScoreChanged(0, 0);
        }
    }

    public void ShowWithFade()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    System.Collections.IEnumerator FadeInRoutine()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, normalized);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}

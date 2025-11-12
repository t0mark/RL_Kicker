using TMPro;
using UnityEngine;

public class ScoreUIManager : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI blueScore;
    public TextMeshProUGUI purpleScore;

    [Header("Optional: 초기 표시용")]
    public MonoBehaviour scoreSource;     // SoccerEnvController 드래그(없으면 자동 탐색 시도)

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
}

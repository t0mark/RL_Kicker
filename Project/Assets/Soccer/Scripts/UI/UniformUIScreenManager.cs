using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles switching between lobby, game, and design screens.
/// Lobby and design pause gameplay, while game mode resumes it.
/// </summary>
public class UniformUIScreenManager : MonoBehaviour
{
    public enum ScreenState
    {
        Lobby,
        Game,
        Design
    }

    [Header("Screens")]
    public GameObject lobbyScreen;
    public GameObject gameScreen;
    public GameObject designScreen;

    [Header("Buttons")]
    public Button playButton;
    public Button designButton;
    public Button backFromGameButton;
    public Button backFromDesignButton;

    [Header("Optional UI")]
    public UniformPromptUI promptUI;

    bool _initialized;
    ScreenState _currentState = ScreenState.Lobby;
    bool _pausedByManager;
    float _cachedTimeScale = 1f;

    void Start()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    void OnDisable()
    {
        if (_pausedByManager)
        {
            Time.timeScale = _cachedTimeScale <= 0f ? 1f : _cachedTimeScale;
            _pausedByManager = false;
        }
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        if (playButton) playButton.onClick.AddListener(() => SwitchState(ScreenState.Game));
        if (designButton) designButton.onClick.AddListener(() => SwitchState(ScreenState.Design));
        if (backFromGameButton) backFromGameButton.onClick.AddListener(() => SwitchState(ScreenState.Lobby));
        if (backFromDesignButton) backFromDesignButton.onClick.AddListener(() => SwitchState(ScreenState.Lobby));

        SwitchState(ScreenState.Lobby);
    }

    public void SwitchState(ScreenState state)
    {
        _currentState = state;

        if (lobbyScreen) lobbyScreen.SetActive(state == ScreenState.Lobby);
        if (gameScreen) gameScreen.SetActive(state == ScreenState.Game);
        if (designScreen) designScreen.SetActive(state == ScreenState.Design);
        if (promptUI) promptUI.gameObject.SetActive(state == ScreenState.Design);

        bool shouldPause = state != ScreenState.Game;
        if (shouldPause)
        {
            if (!_pausedByManager)
            {
                _cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
                _pausedByManager = true;
            }
            Time.timeScale = 0f;
        }
        else
        {
            float resumeScale = _pausedByManager ? _cachedTimeScale : Time.timeScale;
            Time.timeScale = resumeScale <= 0f ? 1f : resumeScale;
            _pausedByManager = false;
        }
    }
}

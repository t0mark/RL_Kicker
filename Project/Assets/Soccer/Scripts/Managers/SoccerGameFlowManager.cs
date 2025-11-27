using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles lobby -> in-game transitions, guarantees a fresh kickoff, and displays
/// a match result overlay once either side reaches the configured target score.
/// </summary>
[DisallowMultipleComponent]
public class SoccerGameFlowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    SoccerEnvController envController;

    [SerializeField]
    UniformUIScreenManager screenManager;

    [SerializeField]
    Canvas rootCanvas;

    [Header("Match Rules")]
    [Min(1)]
    public int pointsToWin = 5;

    CanvasGroup resultOverlayGroup;
    Image resultOverlayDimmer;
    Image resultCard;
    Text resultHeadline;
    Text resultDetails;
    Button replayButton;
    Button lobbyButton;
    bool overlayBuilt;

    bool matchActive;
    float cachedTimeScale = 1f;
    Font overlayFont;

    void Awake()
    {
        if (!rootCanvas)
        {
            rootCanvas = GetComponent<Canvas>();
        }
    }

    void OnEnable()
    {
        UniformUIScreenManager.OnScreenStateChanged += HandleScreenStateChanged;
        SoccerEnvController.OnScoreChanged += HandleScoreChanged;

        EnsureReferences();
        BuildResultOverlay();
        HideResultOverlay();
    }

    void OnDisable()
    {
        UniformUIScreenManager.OnScreenStateChanged -= HandleScreenStateChanged;
        SoccerEnvController.OnScoreChanged -= HandleScoreChanged;
    }

    void EnsureReferences()
    {
        if (!envController)
        {
            envController = FindFirstObjectByType<SoccerEnvController>();
        }

        if (!screenManager)
        {
            screenManager = FindFirstObjectByType<UniformUIScreenManager>();
        }

        if (!rootCanvas)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
    }

    void HandleScreenStateChanged(UniformUIScreenManager.ScreenState state)
    {
        if (state == UniformUIScreenManager.ScreenState.Game)
        {
            BeginMatch();
        }
        else
        {
            matchActive = false;
            HideResultOverlay();
        }
    }

    void HandleScoreChanged(int blue, int purple)
    {
        if (!matchActive)
        {
            return;
        }

        if (pointsToWin <= 0)
        {
            return;
        }

        if (blue >= pointsToWin || purple >= pointsToWin)
        {
            bool playerWon = blue >= pointsToWin;
            CompleteMatch(playerWon, blue, purple);
        }
    }

    void BeginMatch()
    {
        EnsureReferences();
        if (!envController)
        {
            return;
        }

        envController.ResetMatchState();
        matchActive = true;
        HideResultOverlay();
        ResumeGameplay();
    }

    void CompleteMatch(bool playerWon, int blueScore, int purpleScore)
    {
        matchActive = false;
        PauseGameplay();
        ShowResultOverlay(playerWon, blueScore, purpleScore);
    }

    void PauseGameplay()
    {
        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    void ResumeGameplay()
    {
        float resumeScale = cachedTimeScale <= 0f ? 1f : cachedTimeScale;
        Time.timeScale = resumeScale;
    }

    void BuildResultOverlay()
    {
        if (overlayBuilt || rootCanvas == null)
        {
            return;
        }

        // Fullscreen dimmer
        GameObject overlay = new GameObject("MatchResultOverlay", typeof(RectTransform));
        overlay.transform.SetParent(rootCanvas.transform, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        resultOverlayGroup = overlay.AddComponent<CanvasGroup>();
        resultOverlayDimmer = overlay.AddComponent<Image>();
        resultOverlayDimmer.color = new Color(0f, 0.02f, 0.08f, 0.8f);
        resultOverlayDimmer.raycastTarget = true;

        // Card container inspired by sports broadcast result banners
        GameObject card = new GameObject("MatchResultCard", typeof(RectTransform));
        card.transform.SetParent(overlayRect, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620f, 400f);
        cardRect.anchoredPosition = Vector2.zero;

        resultCard = card.AddComponent<Image>();
        Sprite splashSprite = Resources.Load<Sprite>("UI/LobbySplash");
        if (splashSprite != null)
        {
            resultCard.sprite = splashSprite;
            resultCard.type = Image.Type.Sliced;
            resultCard.preserveAspect = false;
        }
        resultCard.color = new Color(0.05f, 0.09f, 0.18f, 0.96f);
        resultCard.raycastTarget = false;
        Shadow dropShadow = card.AddComponent<Shadow>();
        dropShadow.effectColor = new Color(0, 0, 0, 0.6f);
        dropShadow.effectDistance = new Vector2(0f, -6f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 52, 48);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;

        resultHeadline = CreateLabel(card.transform, "ResultHeadline", 60, FontStyle.Bold);        
        resultHeadline.text = string.Empty;

        resultDetails = CreateLabel(card.transform, "ResultDetails", 28, FontStyle.Normal);
        resultDetails.color = new Color(0.82f, 0.89f, 1f, 1f);
        resultDetails.text = string.Empty;

        // Button row
        GameObject buttonRow = new GameObject("ResultButtons", typeof(RectTransform));
        buttonRow.transform.SetParent(card.transform, false);
        HorizontalLayoutGroup rowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 20f;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement rowLayoutElement = buttonRow.AddComponent<LayoutElement>();
        rowLayoutElement.preferredHeight = 80f;

        replayButton = CreateButton(buttonRow.transform, "ReplayButton", "REPLAY");
        replayButton.onClick.AddListener(OnReplayClicked);

        lobbyButton = CreateButton(buttonRow.transform, "BackToLobbyButton", "EXIT");
        lobbyButton.onClick.AddListener(OnLobbyClicked);

        overlayBuilt = true;
    }

    Text CreateLabel(Transform parent, string name, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 0f;
        layoutElement.preferredHeight = fontSize * 1.3f;

        Text label = go.AddComponent<Text>();
        label.font = GetOverlayFont();
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.supportRichText = false;
        label.color = Color.white;
        return label;
    }

    Button CreateButton(Transform parent, string name, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.32f, 0.56f, 1f);
        image.raycastTarget = true;

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.colorMultiplier = 1f;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.46f, 0.78f, 1f);
        colors.pressedColor = new Color(0.09f, 0.19f, 0.36f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        button.colors = colors;

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.preferredHeight = 70f;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(go.transform, false);

        Text lbl = labelObj.AddComponent<Text>();
        lbl.font = GetOverlayFont();
        lbl.text = text;
        lbl.fontSize = 28;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = Color.white;
        lbl.supportRichText = false;

        return button;
    }

    Font GetOverlayFont()
    {
        if (overlayFont == null)
        {
            overlayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (overlayFont == null)
            {
                overlayFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
            }
        }

        return overlayFont;
    }

    void ShowResultOverlay(bool playerWon, int blueScore, int purpleScore)
    {
        if (!overlayBuilt)
        {
            return;
        }

        resultHeadline.text = playerWon ? "VICTORY" : "DEFEAT";
        resultHeadline.color = playerWon
            ? new Color(0.29f, 0.78f, 0.98f, 1f)
            : new Color(0.96f, 0.4f, 0.47f, 1f);

        string scoreLine = $"{blueScore} : {purpleScore}";
        resultDetails.text = playerWon
            ? $"{scoreLine}\n블루 팀이 경기를 지배했습니다!"
            : $"{scoreLine}\n퍼플 팀이 승리를 가져갔습니다.";

        resultCard.color = playerWon
            ? new Color(0.05f, 0.14f, 0.24f, 0.98f)
            : new Color(0.12f, 0.05f, 0.14f, 0.98f);

        resultOverlayDimmer.color = playerWon
            ? new Color(0f, 0.04f, 0.1f, 0.78f)
            : new Color(0.08f, 0f, 0f, 0.78f);

        resultOverlayGroup.alpha = 1f;
        resultOverlayGroup.blocksRaycasts = true;
        resultOverlayGroup.interactable = true;
    }

    void HideResultOverlay()
    {
        if (!overlayBuilt)
        {
            return;
        }

        resultOverlayGroup.alpha = 0f;
        resultOverlayGroup.blocksRaycasts = false;
        resultOverlayGroup.interactable = false;
    }

    void OnReplayClicked()
    {
        HideResultOverlay();
        BeginMatch();
    }

    void OnLobbyClicked()
    {
        HideResultOverlay();
        matchActive = false;
        if (!screenManager)
        {
            screenManager = FindFirstObjectByType<UniformUIScreenManager>();
        }

        if (screenManager != null)
        {
            screenManager.SwitchState(UniformUIScreenManager.ScreenState.Lobby);
        }
    }
}

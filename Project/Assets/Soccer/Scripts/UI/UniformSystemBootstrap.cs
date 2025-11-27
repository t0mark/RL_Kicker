using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game 씬에서 유니폼 생성 시스템을 자동으로 초기화하는 스크립트
/// Canvas GameObject에 부착하면 자동으로 UI와 시스템 컴포넌트를 생성
/// </summary>
[ExecuteAlways]
public class UniformSystemBootstrap : MonoBehaviour
{
    [Header("Team Materials")]
    public Material matBlue;
    public Material matPurple;

    [Header("Server Settings")]
    public string serverEndpoint = "http://localhost:8000/generate";

    [Header("UI Settings")]
    [Tooltip("UI 패널을 자동 생성할지 여부 (false면 수동으로 설정해야 함)")]
    public bool autoCreateUI = true;

    [Header("Team Roots (자동 찾기 시도, 수동 설정 가능)")]
    public Transform[] blueRoots;
    public Transform[] purpleRoots;

    private TeamUniformController uniformController;
    private PromptUniformBridge bridge;
    private UniformPromptUI promptUI;
    private UniformUIScreenManager screenManager;

    private bool initialized = false;

    void OnEnable()
    {
        if (!initialized && Application.isPlaying)
        {
            Debug.Log("[UniformBootstrap] Starting initialization...");
            InitializeUniformSystem();
            initialized = true;
        }
    }

    void Start()
    {
        if (!initialized && Application.isPlaying)
        {
            Debug.Log("[UniformBootstrap] Starting initialization...");
            InitializeUniformSystem();
            initialized = true;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && autoCreateUI)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !initialized)
                {
                    CheckAndCreateUI();
                }
            };
        }
    }

    void CheckAndCreateUI()
    {
        // 이미 UI가 있는지 확인 (Canvas의 모든 자식 검색)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        Transform existingPanel = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "UniformUIScreens")
            {
                existingPanel = child;
                break;
            }
        }

        if (existingPanel == null && autoCreateUI)
        {
            Debug.Log("[UniformBootstrap] Creating UI in Editor mode...");
            CreateUniformUI();
        }
        else if (existingPanel != null)
        {
            Debug.Log("[UniformBootstrap] UI already exists, skipping creation");
        }
    }
#endif

    void InitializeUniformSystem()
    {
        // 1. TeamUniformController 생성 또는 찾기
        uniformController = FindAnyObjectByType<TeamUniformController>();
        if (uniformController == null)
        {
            GameObject ctrlObj = new GameObject("TeamUniformController");
            uniformController = ctrlObj.AddComponent<TeamUniformController>();
            Debug.Log("[UniformBootstrap] TeamUniformController created");
        }

        // 2. 팀 루트 자동 설정 (수동 설정이 없을 경우)
        if (blueRoots == null || blueRoots.Length == 0)
        {
            blueRoots = AutoFindTeamRoots("Blue");
        }
        if (purpleRoots == null || purpleRoots.Length == 0)
        {
            purpleRoots = AutoFindTeamRoots("Purple");
        }

        // 3. TeamUniformController 설정
        SetupUniformController();

        // 4. PromptUniformBridge 생성
        GameObject bridgeObj = new GameObject("PromptUniformBridge");
        bridge = bridgeObj.AddComponent<PromptUniformBridge>();
        bridge.ctrl = uniformController;
        bridge.endpoint = serverEndpoint;
        Debug.Log("[UniformBootstrap] PromptUniformBridge created");

        // 5. UI 생성 (옵션)
        if (autoCreateUI)
        {
            CreateUniformUI();
        }
    }

    void SetupUniformController()
    {
        // blueRoots와 purpleRoots를 List로 변환하여 설정
        var blueList = new System.Collections.Generic.List<Transform>(blueRoots);
        var purpleList = new System.Collections.Generic.List<Transform>(purpleRoots);

        // Reflection을 사용하여 private 필드 설정
        var blueRootsField = typeof(TeamUniformController).GetField("blueRoots",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var purpleRootsField = typeof(TeamUniformController).GetField("purpleRoots",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var matBlueField = typeof(TeamUniformController).GetField("matBlue",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var matPurpleField = typeof(TeamUniformController).GetField("matPurple",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (blueRootsField != null) blueRootsField.SetValue(uniformController, blueList);
        if (purpleRootsField != null) purpleRootsField.SetValue(uniformController, purpleList);
        if (matBlueField != null) matBlueField.SetValue(uniformController, matBlue);
        if (matPurpleField != null) matPurpleField.SetValue(uniformController, matPurple);

        Debug.Log($"[UniformBootstrap] Configured: {blueRoots.Length} blue roots, {purpleRoots.Length} purple roots");
    }

    void CreateUniformUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("[UniformBootstrap] Canvas not found, UI creation skipped");
            return;
        }

        foreach (Transform child in canvas.transform)
        {
            if (child.name == "UniformUIScreens")
            {
                Debug.Log("[UniformBootstrap] UI already exists, skipping");
                return;
            }
        }

        Debug.Log($"[UniformBootstrap] Creating UI on Canvas: {canvas.name}");

        GameObject root = new GameObject("UniformUIScreens");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        screenManager = root.AddComponent<UniformUIScreenManager>();

        // ----- Lobby Screen -----
        GameObject lobbyScreen = new GameObject("LobbyScreen");
        RectTransform lobbyRect = lobbyScreen.AddComponent<RectTransform>();
        lobbyRect.SetParent(root.transform, false);
        lobbyRect.anchorMin = Vector2.zero;
        lobbyRect.anchorMax = Vector2.one;
        lobbyRect.offsetMin = Vector2.zero;
        lobbyRect.offsetMax = Vector2.zero;
        Image lobbyBg = lobbyScreen.AddComponent<Image>();
        lobbyBg.color = new Color(0f, 0f, 0f, 0.7f);

        GameObject lobbyContent = new GameObject("Content");
        RectTransform lobbyContentRect = lobbyContent.AddComponent<RectTransform>();
        lobbyContent.transform.SetParent(lobbyScreen.transform, false);
        lobbyContentRect.anchorMin = new Vector2(0.5f, 0.5f);
        lobbyContentRect.anchorMax = new Vector2(0.5f, 0.5f);
        lobbyContentRect.pivot = new Vector2(0.5f, 0.5f);
        lobbyContentRect.sizeDelta = new Vector2(420, 280);

        VerticalLayoutGroup lobbyLayout = lobbyContent.AddComponent<VerticalLayoutGroup>();
        lobbyLayout.padding = new RectOffset(15, 15, 15, 15);
        lobbyLayout.spacing = 20;
        lobbyLayout.childAlignment = TextAnchor.MiddleCenter;
        lobbyLayout.childControlWidth = true;
        lobbyLayout.childControlHeight = false;
        lobbyLayout.childForceExpandWidth = true;
        lobbyLayout.childForceExpandHeight = false;

        GameObject lobbyTitle = CreateText(lobbyContent.transform, "Uniform Lobby", 28, TextAnchor.MiddleCenter);
        LayoutElement lobbyTitleLayout = lobbyTitle.AddComponent<LayoutElement>();
        lobbyTitleLayout.preferredHeight = 40;

        GameObject lobbyDesc = CreateText(lobbyContent.transform, "시작할 기능을 선택하세요", 16, TextAnchor.MiddleCenter);
        LayoutElement lobbyDescLayout = lobbyDesc.AddComponent<LayoutElement>();
        lobbyDescLayout.preferredHeight = 24;

        GameObject playBtnObj = CreateButton(lobbyContent.transform, "게임 플레이");
        Button playBtn = playBtnObj.GetComponent<Button>();
        LayoutElement playBtnLayout = playBtnObj.AddComponent<LayoutElement>();
        playBtnLayout.preferredHeight = 45;

        GameObject designBtnObj = CreateButton(lobbyContent.transform, "디자인 수정");
        Button designBtn = designBtnObj.GetComponent<Button>();
        LayoutElement designBtnLayout = designBtnObj.AddComponent<LayoutElement>();
        designBtnLayout.preferredHeight = 45;

        // ----- Game Screen -----
        GameObject gameScreen = new GameObject("GameScreen");
        RectTransform gameRect = gameScreen.AddComponent<RectTransform>();
        gameRect.SetParent(root.transform, false);
        gameRect.anchorMin = new Vector2(0, 1);
        gameRect.anchorMax = new Vector2(0, 1);
        gameRect.pivot = new Vector2(0, 1);
        gameRect.anchoredPosition = new Vector2(20, -20);
        gameRect.sizeDelta = new Vector2(260, 120);

        Image gameBg = gameScreen.AddComponent<Image>();
        gameBg.color = new Color(0f, 0f, 0f, 0.5f);

        VerticalLayoutGroup gameLayout = gameScreen.AddComponent<VerticalLayoutGroup>();
        gameLayout.padding = new RectOffset(15, 15, 15, 15);
        gameLayout.spacing = 10;
        gameLayout.childControlWidth = true;
        gameLayout.childControlHeight = false;
        gameLayout.childForceExpandWidth = true;
        gameLayout.childForceExpandHeight = false;

        GameObject gameText = CreateText(gameScreen.transform, "게임 진행 중", 18, TextAnchor.MiddleLeft);
        LayoutElement gameTextLayout = gameText.AddComponent<LayoutElement>();
        gameTextLayout.preferredHeight = 30;

        GameObject gameBackBtnObj = CreateButton(gameScreen.transform, "로비로 돌아가기");
        Button gameBackBtn = gameBackBtnObj.GetComponent<Button>();
        LayoutElement gameBackLayout = gameBackBtnObj.AddComponent<LayoutElement>();
        gameBackLayout.preferredHeight = 40;
        gameScreen.SetActive(false);

        // ----- Design Screen -----
        GameObject designScreen = new GameObject("DesignScreen");
        RectTransform designRect = designScreen.AddComponent<RectTransform>();
        designRect.SetParent(root.transform, false);
        designRect.anchorMin = Vector2.zero;
        designRect.anchorMax = Vector2.one;
        designRect.offsetMin = Vector2.zero;
        designRect.offsetMax = Vector2.zero;
        Image designBg = designScreen.AddComponent<Image>();
        designBg.color = new Color(0f, 0f, 0f, 0.35f);

        GameObject designBackBtnObj = CreateButton(designScreen.transform, "로비로 돌아가기");
        RectTransform designBackRect = designBackBtnObj.GetComponent<RectTransform>();
        designBackRect.anchorMin = new Vector2(1, 1);
        designBackRect.anchorMax = new Vector2(1, 1);
        designBackRect.pivot = new Vector2(1, 1);
        designBackRect.anchoredPosition = new Vector2(-20, -20);
        designBackRect.sizeDelta = new Vector2(200, 40);
        Button designBackBtn = designBackBtnObj.GetComponent<Button>();

        GameObject panel = new GameObject("UniformGeneratorPanel");
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panel.transform.SetParent(designScreen.transform, false);
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -80);
        panelRect.sizeDelta = new Vector2(400, 350);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 10;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        GameObject titleObj = CreateText(panel.transform, "Uniform Generator", 20, TextAnchor.MiddleCenter);
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 30;

        GameObject inputObj = CreateInputField(panel.transform, "Enter prompt...");
        InputField inputPrompt = inputObj.GetComponent<InputField>();
        LayoutElement inputLayout = inputObj.AddComponent<LayoutElement>();
        inputLayout.preferredHeight = 35;

        GameObject dropdownObj = CreateDropdown(panel.transform, new string[] { "Blue Team", "Purple Team" });
        Dropdown teamDropdown = dropdownObj.GetComponent<Dropdown>();
        LayoutElement dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
        dropdownLayout.preferredHeight = 35;

        GameObject tilingObj = CreateSlider(panel.transform, "Tiling", 0.5f, 12f, 4f);
        Slider tilingSlider = tilingObj.GetComponentInChildren<Slider>();
        LayoutElement tilingLayout = tilingObj.AddComponent<LayoutElement>();
        tilingLayout.preferredHeight = 40;

        GameObject strengthObj = CreateSlider(panel.transform, "Strength", 0f, 1f, 1f);
        Slider strengthSlider = strengthObj.GetComponentInChildren<Slider>();
        LayoutElement strengthLayout = strengthObj.AddComponent<LayoutElement>();
        strengthLayout.preferredHeight = 40;

        GameObject btnObj = CreateButton(panel.transform, "Generate Uniform");
        Button generateBtn = btnObj.GetComponent<Button>();
        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 40;

        GameObject spinner = CreateText(panel.transform, "Loading...", 16, TextAnchor.MiddleCenter);
        spinner.SetActive(false);

        GameObject toast = CreateText(panel.transform, "", 14, TextAnchor.MiddleCenter);
        toast.SetActive(false);
        Text toastText = toast.GetComponent<Text>();
        designScreen.SetActive(false);

        if (Application.isPlaying && bridge != null)
        {
            promptUI = panel.AddComponent<UniformPromptUI>();

            SetUIField("inputPrompt", inputPrompt);
            SetUIField("teamDropdown", teamDropdown);
            SetUIField("tilingSlider", tilingSlider);
            SetUIField("strengthSlider", strengthSlider);
            SetUIField("generateBtn", generateBtn);
            SetUIField("loadingSpinner", spinner);
            SetUIField("toastText", toastText);
            SetUIField("bridge", bridge);

            promptUI.Initialize();
        }

        screenManager.lobbyScreen = lobbyScreen;
        screenManager.gameScreen = gameScreen;
        screenManager.designScreen = designScreen;
        screenManager.playButton = playBtn;
        screenManager.designButton = designBtn;
        screenManager.backFromGameButton = gameBackBtn;
        screenManager.backFromDesignButton = designBackBtn;
        screenManager.promptUI = promptUI;

        if (Application.isPlaying)
        {
            screenManager.Initialize();
        }
    }

    void SetUIField(string fieldName, object value)
    {
        var field = typeof(UniformPromptUI).GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(promptUI, value);
        }
    }

    GameObject CreateText(Transform parent, string text, int fontSize, TextAnchor alignment)
    {
        GameObject obj = new GameObject("Text_" + text.Replace(" ", ""));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 30);

        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = Color.white;

        return obj;
    }

    GameObject CreateInputField(Transform parent, string placeholder)
    {
        GameObject obj = new GameObject("InputField");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 35);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        InputField input = obj.AddComponent<InputField>();

        // 텍스트 자식 오브젝트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = Color.white;
        txt.fontSize = 14;
        txt.supportRichText = false;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 6);
        textRect.offsetMax = new Vector2(-10, -6);

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(obj.transform, false);
        Text placeholderTxt = placeholderObj.AddComponent<Text>();
        placeholderTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderTxt.text = placeholder;
        placeholderTxt.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderTxt.fontSize = 14;
        placeholderTxt.fontStyle = FontStyle.Italic;

        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 6);
        placeholderRect.offsetMax = new Vector2(-10, -6);

        input.textComponent = txt;
        input.placeholder = placeholderTxt;

        return obj;
    }

    GameObject CreateDropdown(Transform parent, string[] options)
    {
        GameObject obj = new GameObject("Dropdown");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 35);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Dropdown dropdown = obj.AddComponent<Dropdown>();

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        Text labelTxt = labelObj.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.color = Color.white;
        labelTxt.fontSize = 14;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-25, -6);

        // Arrow (간단한 텍스트로 대체)
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(obj.transform, false);
        Text arrowTxt = arrowObj.AddComponent<Text>();
        arrowTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arrowTxt.text = "▼";
        arrowTxt.color = Color.white;
        arrowTxt.fontSize = 12;
        arrowTxt.alignment = TextAnchor.MiddleCenter;

        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.offsetMin = new Vector2(-20, 0);
        arrowRect.offsetMax = new Vector2(0, 0);

        // Template (드롭다운 리스트)
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(obj.transform, false);
        templateObj.SetActive(false);

        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);

        Image templateImg = templateObj.AddComponent<Image>();
        templateImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // CanvasGroup 추가 (Dropdown의 AlphaFadeList에 필요)
        CanvasGroup templateCanvasGroup = templateObj.AddComponent<CanvasGroup>();

        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        viewportObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);

        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 28);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();

        // Item Background
        GameObject itemBgObj = new GameObject("Item Background");
        itemBgObj.transform.SetParent(itemObj.transform, false);

        RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = Vector2.zero;
        itemBgRect.offsetMax = Vector2.zero;

        Image itemBgImg = itemBgObj.AddComponent<Image>();
        itemBgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // Item Checkmark
        GameObject checkObj = new GameObject("Item Checkmark");
        checkObj.transform.SetParent(itemObj.transform, false);

        RectTransform checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.sizeDelta = new Vector2(20, 20);
        checkRect.anchoredPosition = new Vector2(10, 0);

        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = Color.white;

        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);

        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(20, 1);
        itemLabelRect.offsetMax = new Vector2(-10, -2);

        Text itemLabelTxt = itemLabelObj.AddComponent<Text>();
        itemLabelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemLabelTxt.color = Color.white;
        itemLabelTxt.fontSize = 14;

        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = checkImg;
        itemToggle.isOn = true;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        dropdown.captionText = labelTxt;
        dropdown.itemText = itemLabelTxt;
        dropdown.template = templateRect;

        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(options));

        return obj;
    }

    GameObject CreateSlider(Transform parent, string label, float min, float max, float defaultValue)
    {
        GameObject container = new GameObject("Slider_" + label);
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0, 40);

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 10;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);
        Text labelTxt = labelObj.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.text = label + ":";
        labelTxt.color = Color.white;
        labelTxt.fontSize = 14;
        labelTxt.alignment = TextAnchor.MiddleLeft;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.minWidth = 80;
        labelLayout.flexibleWidth = 0;

        // Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 20);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(10, 0);

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 0.9f, 1f);

        // Handle Slide Area
        GameObject handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.transform.SetParent(sliderObj.transform, false);

        RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        // Handle
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);

        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return container;
    }

    GameObject CreateButton(Transform parent, string text)
    {
        GameObject obj = new GameObject("Button_" + text.Replace(" ", ""));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.8f, 1f);

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text;
        txt.color = Color.white;
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;

        return obj;
    }

    Transform[] AutoFindTeamRoots(string teamName)
    {
        var roots = new System.Collections.Generic.List<Transform>();

        // 1. Players 오브젝트 찾기
        GameObject playersObj = GameObject.Find("Players");
        if (playersObj != null)
        {
            Debug.Log($"[UniformBootstrap] Found Players object, searching children...");

            // Players의 모든 자식 검색
            foreach (Transform child in playersObj.transform)
            {
                if (child.name.Contains(teamName))
                {
                    Debug.Log($"[UniformBootstrap] Found {teamName} player: {child.name}");
                    roots.Add(child);
                }
            }
        }

        // 2. 못 찾았으면 전체 씬에서 AgentSoccer로 검색
        if (roots.Count == 0)
        {
            Debug.Log($"[UniformBootstrap] Players not found, searching entire scene for AgentSoccer...");
            AgentSoccer[] agents = FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None);

            foreach (AgentSoccer agent in agents)
            {
                var teamField = typeof(AgentSoccer).GetField("team",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (teamField != null)
                {
                    int team = (int)teamField.GetValue(agent);
                    bool isBlue = (team == 0);

                    if ((teamName == "Blue" && isBlue) || (teamName == "Purple" && !isBlue))
                    {
                        Debug.Log($"[UniformBootstrap] Found {teamName} agent: {agent.name}");
                        roots.Add(agent.transform);
                    }
                }
            }
        }

        Debug.Log($"[UniformBootstrap] Auto-found {roots.Count} {teamName} team roots");
        return roots.ToArray();
    }
}

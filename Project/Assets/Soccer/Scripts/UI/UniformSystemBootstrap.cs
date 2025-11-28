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
    private Sprite backArrowSprite;
    private Sprite lobbySplashSprite;
    private GameObject lobbyContentRoot;
    private GameObject lobbySplashOverlay;
    private Image lobbyDimOverlay;
    private bool lobbyButtonsShown;

    // Preview stage for the design screen
    private GameObject previewStage;
    private Camera previewCamera;
    private Light previewLight;
    private RenderTexture previewTexture;
    private GameObject previewCharacter;
    private int previewLayer = -1;
    private readonly Vector3 previewStageOrigin = new Vector3(4200f, 120f, 4200f);

    private bool initialized = false;

    void OnEnable()
    {
        if (!initialized && Application.isPlaying)
        {
            Debug.Log("[UniformBootstrap] Starting initialization...");
            InitializeUniformSystem();
            initialized = true;
        }
        else if (initialized && screenManager != null)
        {
            UniformUIScreenManager.OnScreenStateChanged -= HandleScreenStateChanged;
            UniformUIScreenManager.OnScreenStateChanged += HandleScreenStateChanged;
            HandleScreenStateChanged(screenManager.CurrentState);
        }
    }

    void OnDisable()
    {
        UniformUIScreenManager.OnScreenStateChanged -= HandleScreenStateChanged;
        if (!Application.isPlaying)
        {
            TeardownPreviewStage();
        }
    }

    GameObject BuildLobbyScreen(Transform parent, out Button startBtn, out Button designBtn)
    {
        GameObject lobbyScreen = new GameObject("LobbyScreen");
        RectTransform lobbyRect = lobbyScreen.AddComponent<RectTransform>();
        lobbyRect.SetParent(parent, false);
        lobbyRect.anchorMin = Vector2.zero;
        lobbyRect.anchorMax = Vector2.one;
        lobbyRect.offsetMin = Vector2.zero;
        lobbyRect.offsetMax = Vector2.zero;

        // Background splash image
        GameObject bgImageObj = new GameObject("LobbyBackgroundImage");
        RectTransform bgRect = bgImageObj.AddComponent<RectTransform>();
        bgRect.SetParent(lobbyScreen.transform, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgImageObj.AddComponent<Image>();
        if (lobbySplashSprite != null)
        {
            bgImage.sprite = lobbySplashSprite;
            bgImage.preserveAspect = true;
            bgImage.color = Color.white;
        }
        else
        {
            bgImage.color = new Color(0.93f, 0.96f, 1f, 1f);
        }

        // Dim overlay (inactive until splash dismissed)
        GameObject dimObj = new GameObject("LobbyDimmer");
        RectTransform dimRect = dimObj.AddComponent<RectTransform>();
        dimRect.SetParent(lobbyScreen.transform, false);
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        lobbyDimOverlay = dimObj.AddComponent<Image>();
        lobbyDimOverlay.color = new Color(0f, 0f, 0f, 0.35f);
        lobbyDimOverlay.raycastTarget = false;
        dimObj.SetActive(false);

        // Content root (hidden until splash click)
        lobbyContentRoot = new GameObject("LobbyContent");
        RectTransform contentRect = lobbyContentRoot.AddComponent<RectTransform>();
        contentRect.SetParent(lobbyScreen.transform, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        lobbyContentRoot.SetActive(false);

        Transform contentParent = lobbyContentRoot.transform;

        GameObject subtitle = CreateText(contentParent, "Select The Button", 28, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f, 1f));
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.2f, 0.55f);
        subtitleRect.anchorMax = new Vector2(0.8f, 0.68f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        GameObject cardContainer = new GameObject("LobbyCardRow");
        RectTransform cardRect = cardContainer.AddComponent<RectTransform>();
        cardRect.SetParent(contentParent, false);
        cardRect.anchorMin = new Vector2(0.15f, 0.22f);
        cardRect.anchorMax = new Vector2(0.85f, 0.52f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup grid = cardContainer.AddComponent<HorizontalLayoutGroup>();
        grid.spacing = 40;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.childControlWidth = true;
        grid.childControlHeight = true;
        grid.childForceExpandWidth = true;
        grid.childForceExpandHeight = false;

        GameObject startOption = CreateLobbyOption(cardContainer.transform, "빠른 경기", "즉시 플레이를 시작합니다", "START", out startBtn);
        LayoutElement startLayout = startOption.AddComponent<LayoutElement>();
        startLayout.preferredHeight = 200;
        startLayout.flexibleWidth = 1f;

        GameObject characterOption = CreateLobbyOption(cardContainer.transform, "디자인 수정", "캐릭터의 유니폼을 꾸며보세요", "CHARACTER", out designBtn);
        LayoutElement charLayout = characterOption.AddComponent<LayoutElement>();
        charLayout.preferredHeight = 200;
        charLayout.flexibleWidth = 1f;
        // Splash overlay with Touch Screen prompt
        lobbySplashOverlay = new GameObject("LobbySplashOverlay");
        RectTransform overlayRect = lobbySplashOverlay.AddComponent<RectTransform>();
        overlayRect.SetParent(lobbyScreen.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = lobbySplashOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0f);
        Button splashButton = lobbySplashOverlay.AddComponent<Button>();
        splashButton.targetGraphic = overlayImg;
        splashButton.transition = Selectable.Transition.None;
        splashButton.onClick.AddListener(OnLobbySplashClicked);

        GameObject touchText = CreateText(lobbySplashOverlay.transform, "Touch Screen", 32, TextAnchor.MiddleCenter, Color.white);
        RectTransform touchRect = touchText.GetComponent<RectTransform>();
        touchRect.anchorMin = new Vector2(0.3f, 0.08f);
        touchRect.anchorMax = new Vector2(0.7f, 0.16f);
        touchRect.offsetMin = Vector2.zero;
        touchRect.offsetMax = Vector2.zero;

        return lobbyScreen;
    }

    GameObject BuildGameScreen(Transform parent, out Button backBtn)
    {
        GameObject gameScreen = new GameObject("GameScreen");
        RectTransform rect = gameScreen.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -30);
        rect.sizeDelta = Vector2.zero;

        float backSize = 96f;
        GameObject backBtnObj = CreateBackIconButton(gameScreen.transform, backSize);
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = Vector2.zero;
        backRect.sizeDelta = new Vector2(backSize, backSize);
        backBtn = backBtnObj.GetComponent<Button>();
        gameScreen.SetActive(false);

        return gameScreen;
    }

    GameObject BuildDesignScreen(Transform parent, out InputField inputPrompt, out Slider tilingSlider,
        out Slider strengthSlider, out GameObject spinner, out Text toastText, out Button backBtn,
        out Dropdown teamDropdown, out Button generateBtn)
    {
        EnsurePreviewStage();

        GameObject designScreen = new GameObject("DesignScreen");
        RectTransform rect = designScreen.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = designScreen.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.14f, 0.96f);

        GameObject accent = new GameObject("DesignAccent");
        RectTransform accentRect = accent.AddComponent<RectTransform>();
        accentRect.SetParent(designScreen.transform, false);
        accentRect.anchorMin = new Vector2(0, 0);
        accentRect.anchorMax = new Vector2(0.55f, 0.5f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(0.1f, 0.16f, 0.3f, 0.4f);
        accentImg.raycastTarget = false;

        GameObject content = new GameObject("DesignContent");
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.SetParent(designScreen.transform, false);
        contentRect.anchorMin = new Vector2(0.03f, 0.05f);
        contentRect.anchorMax = new Vector2(0.97f, 0.9f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup rowLayout = content.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 18f;
        rowLayout.padding = new RectOffset(18, 18, 18, 18);
        rowLayout.childAlignment = TextAnchor.UpperCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        GameObject previewPanel = new GameObject("PreviewPanel");
        previewPanel.transform.SetParent(content.transform, false);
        LayoutElement previewLayout = previewPanel.AddComponent<LayoutElement>();
        previewLayout.flexibleWidth = 1.6f;
        previewLayout.preferredHeight = 560f;
        Image previewBg = previewPanel.AddComponent<Image>();
        previewBg.color = new Color(0.08f, 0.12f, 0.26f, 0.9f);
        ApplyOutline(previewBg, 1.8f);
        ApplyShadow(previewBg, new Vector2(3.5f, -3.5f), 0.35f);

        VerticalLayoutGroup previewGroup = previewPanel.AddComponent<VerticalLayoutGroup>();
        previewGroup.padding = new RectOffset(18, 18, 18, 18);
        previewGroup.spacing = 10f;
        previewGroup.childAlignment = TextAnchor.UpperCenter;
        previewGroup.childControlWidth = true;
        previewGroup.childForceExpandHeight = true;

        GameObject previewTitle = CreateText(previewPanel.transform, "유니폼 미리보기", 22, TextAnchor.MiddleLeft, new Color(0.82f, 0.89f, 1f, 1f));
        LayoutElement previewTitleLayout = previewTitle.AddComponent<LayoutElement>();
        previewTitleLayout.preferredHeight = 34;

        GameObject previewImageObj = new GameObject("PreviewRender");
        previewImageObj.transform.SetParent(previewPanel.transform, false);
        RawImage previewImage = previewImageObj.AddComponent<RawImage>();
        previewImage.texture = previewTexture;
        previewImage.color = Color.white;
        RectTransform previewImageRect = previewImageObj.GetComponent<RectTransform>();
        previewImageRect.anchorMin = Vector2.zero;
        previewImageRect.anchorMax = Vector2.one;
        previewImageRect.offsetMin = Vector2.zero;
        previewImageRect.offsetMax = Vector2.zero;
        LayoutElement previewImageLayout = previewImageObj.AddComponent<LayoutElement>();
        previewImageLayout.flexibleWidth = 1f;
        previewImageLayout.flexibleHeight = 1f;
        previewImageLayout.preferredHeight = 500f;

        GameObject controlPanel = new GameObject("DesignControls");
        controlPanel.transform.SetParent(content.transform, false);
        LayoutElement controlLayout = controlPanel.AddComponent<LayoutElement>();
        controlLayout.flexibleWidth = 1f;
        Image controlBg = controlPanel.AddComponent<Image>();
        controlBg.color = new Color(0.06f, 0.09f, 0.2f, 0.85f);
        ApplyOutline(controlBg, 1.4f);
        ApplyShadow(controlBg, new Vector2(2.5f, -2.5f), 0.3f);

        VerticalLayoutGroup controlGroup = controlPanel.AddComponent<VerticalLayoutGroup>();
        controlGroup.padding = new RectOffset(20, 20, 22, 20);
        controlGroup.spacing = 10f;
        controlGroup.childAlignment = TextAnchor.UpperLeft;
        controlGroup.childControlWidth = true;
        controlGroup.childForceExpandHeight = false;

        GameObject heading = CreateText(controlPanel.transform, "디자인 실험실", 30, TextAnchor.UpperLeft, Color.white);
        LayoutElement headingLayout = heading.AddComponent<LayoutElement>();
        headingLayout.preferredHeight = 40;
        ApplyShadow(heading.GetComponent<Text>(), new Vector2(1.2f, -1.2f), 0.35f);

        GameObject subHeading = CreateText(controlPanel.transform, "로비와 같은 분위기로 캐릭터를 크게 확인해요.", 16, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 1f, 0.9f));
        LayoutElement subHeadingLayout = subHeading.AddComponent<LayoutElement>();
        subHeadingLayout.preferredHeight = 28;

        GameObject promptLabel = CreateText(controlPanel.transform, "프롬프트", 18, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 1f, 0.95f));
        LayoutElement promptLabelLayout = promptLabel.AddComponent<LayoutElement>();
        promptLabelLayout.preferredHeight = 24;

        GameObject promptRow = new GameObject("PromptRow");
        promptRow.transform.SetParent(controlPanel.transform, false);
        HorizontalLayoutGroup promptRowLayout = promptRow.AddComponent<HorizontalLayoutGroup>();
        promptRowLayout.spacing = 10f;
        promptRowLayout.childAlignment = TextAnchor.MiddleCenter;
        promptRowLayout.childControlWidth = true;
        promptRowLayout.childControlHeight = true;
        promptRowLayout.childForceExpandWidth = true;
        promptRowLayout.childForceExpandHeight = false;

        GameObject inputObj = CreateInputField(promptRow.transform, "팀 유니폼에 어울리는 키워드를 적어주세요");
        inputPrompt = inputObj.GetComponent<InputField>();
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(0, 52);
        LayoutElement inputLayout = inputObj.AddComponent<LayoutElement>();
        inputLayout.flexibleWidth = 1f;
        ApplyOutline(inputObj.GetComponent<Image>(), 1.2f);
        ApplyShadow(inputObj.GetComponent<Image>(), new Vector2(1.5f, -1.5f), 0.2f);

        GameObject generateObj = CreateButton(promptRow.transform, "디자인 생성");
        generateBtn = generateObj.GetComponent<Button>();
        RectTransform generateRect = generateObj.GetComponent<RectTransform>();
        generateRect.sizeDelta = new Vector2(200, 52);
        LayoutElement generateLayout = generateObj.AddComponent<LayoutElement>();
        generateLayout.preferredWidth = 200;
        generateLayout.preferredHeight = 52;
        Image generateImg = generateObj.GetComponent<Image>();
        if (generateImg != null)
        {
            generateImg.color = new Color(0.2f, 0.52f, 0.96f, 1f);
            ApplyShadow(generateImg, new Vector2(2f, -2f), 0.35f);
        }
        Text generateTxt = generateObj.GetComponentInChildren<Text>();
        if (generateTxt != null)
        {
            generateTxt.text = "디자인 생성";
            generateTxt.fontSize = 22;
            generateTxt.color = Color.white;
        }

        GameObject slidersContainer = new GameObject("PatternControls");
        slidersContainer.transform.SetParent(controlPanel.transform, false);
        VerticalLayoutGroup slidersLayout = slidersContainer.AddComponent<VerticalLayoutGroup>();
        slidersLayout.spacing = 10f;
        slidersLayout.childAlignment = TextAnchor.UpperLeft;
        slidersLayout.childControlWidth = true;
        slidersLayout.childForceExpandHeight = false;
        LayoutElement sliderContainerLayout = slidersContainer.AddComponent<LayoutElement>();
        sliderContainerLayout.preferredHeight = 180f;

        GameObject tilingObj = CreateSlider(slidersContainer.transform, "Tiling (패턴 크기)", 0.5f, 12f, 4f);
        tilingSlider = tilingObj.GetComponentInChildren<Slider>();
        GameObject strengthObj = CreateSlider(slidersContainer.transform, "Strength (강도)", 0f, 1f, 1f);
        strengthSlider = strengthObj.GetComponentInChildren<Slider>();

        GameObject hint = CreateText(controlPanel.transform, "프롬프트와 옵션을 정한 뒤 디자인 생성 버튼을 누르면 바로 적용돼요.", 14, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 1f, 0.85f));
        LayoutElement hintLayout = hint.AddComponent<LayoutElement>();
        hintLayout.preferredHeight = 34;

        GameObject spinnerObj = CreateText(controlPanel.transform, "생성 중...", 16, TextAnchor.UpperLeft, new Color(0.76f, 0.86f, 1f, 1f));
        LayoutElement spinnerLayout = spinnerObj.AddComponent<LayoutElement>();
        spinnerLayout.preferredHeight = 24;
        spinnerObj.SetActive(false);
        spinner = spinnerObj;

        GameObject backBtnObj = CreateButton(designScreen.transform, "로비로");
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.04f, 0.92f);
        backRect.anchorMax = new Vector2(0.18f, 0.98f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;
        SetupSketchButton(backBtnObj);
        backBtn = backBtnObj.GetComponent<Button>();

        GameObject toastObj = CreateText(designScreen.transform, "", 16, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.45f, 1f));
        RectTransform toastRect = toastObj.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.3f, 0.02f);
        toastRect.anchorMax = new Vector2(0.7f, 0.07f);
        toastRect.offsetMin = Vector2.zero;
        toastRect.offsetMax = Vector2.zero;
        toastObj.SetActive(false);
        toastText = toastObj.GetComponent<Text>();

        GameObject dropdownObj = CreateDropdown(designScreen.transform, new string[] { "Blue Team", "Purple Team" });
        teamDropdown = dropdownObj.GetComponent<Dropdown>();
        teamDropdown.value = 0;
        RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(1.1f, 1.1f);
        dropdownRect.anchorMax = dropdownRect.anchorMin;
        dropdownRect.sizeDelta = Vector2.zero;
        var cg = dropdownObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        designScreen.SetActive(false);
        return designScreen;
    }

    void CreateSketchColumn(Transform parent, string title, Sprite sprite)
    {
        GameObject column = new GameObject($"{title}Column");
        column.transform.SetParent(parent, false);

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        GameObject label = CreateText(column.transform, title, 24, TextAnchor.MiddleCenter, Color.black);
        LayoutElement labelLayout = label.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 40;

        GameObject card = new GameObject($"{title}Card");
        RectTransform cardRect = card.AddComponent<RectTransform>();
        card.transform.SetParent(column.transform, false);
        cardRect.sizeDelta = new Vector2(240, 260);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.97f, 0.98f, 1f, 0.95f);
        ApplyOutline(cardBg, 1.1f);
        ApplyShadow(cardBg, new Vector2(2, -2), 0.3f);

        GameObject sketch = new GameObject($"{title}Sketch");
        RectTransform sketchRect = sketch.AddComponent<RectTransform>();
        sketch.transform.SetParent(card.transform, false);
        sketchRect.anchorMin = new Vector2(0.1f, 0.1f);
        sketchRect.anchorMax = new Vector2(0.9f, 0.9f);
        sketchRect.offsetMin = Vector2.zero;
        sketchRect.offsetMax = Vector2.zero;

        Image img = sketch.AddComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 1f, 1f);
        img.preserveAspect = true;
    }

    void SetupSketchButton(GameObject buttonObj)
    {
        if (buttonObj == null) return;
        Image img = buttonObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.99f, 0.99f, 1f, 0.95f);
            ApplyOutline(img, 1.5f);
            ApplyShadow(img, new Vector2(2f, -2f), 0.25f);
        }

        Text txt = buttonObj.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.color = new Color(0.1f, 0.16f, 0.34f, 1f);
            txt.fontSize = 28;
        }
    }

    GameObject CreateBackIconButton(Transform parent, float size = 64f)
    {
        GameObject obj = new GameObject("Button_Back");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        Image img = obj.AddComponent<Image>();
        img.color = Color.white;
        img.preserveAspect = true;
        if (backArrowSprite != null)
        {
            img.sprite = backArrowSprite;
        }
        else
        {
            GameObject fallbackObj = CreateText(obj.transform, "←", 28, TextAnchor.MiddleCenter, Color.black);
            RectTransform fallbackRect = fallbackObj.GetComponent<RectTransform>();
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
        }

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;

        ApplyOutline(img, 1f);
        ApplyShadow(img, new Vector2(1.5f, -1.5f), 0.25f);

        return obj;
    }

    void ApplyOutline(Graphic graphic, float distance)
    {
        if (graphic == null) return;
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null)
        {
            outline = graphic.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(distance, -distance);
    }

    void ApplyShadow(Graphic graphic, Vector2 offset, float alpha = 0.45f)
    {
        if (graphic == null) return;
        Shadow shadow = null;
        var shadows = graphic.GetComponents<Shadow>();
        foreach (var s in shadows)
        {
            if (!(s is Outline))
            {
                shadow = s;
                break;
            }
        }

        if (shadow == null)
        {
            shadow = graphic.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        shadow.effectDistance = offset;
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
        else
        {
            // 기존 패널이 남아 있다면 제거하여 새 UI만 노출
            Transform legacyPanel = canvas.transform.Find("UniformGeneratorPanel");
            if (legacyPanel != null)
            {
                if (Application.isPlaying)
                    Destroy(legacyPanel.gameObject);
                else
                    DestroyImmediate(legacyPanel.gameObject);
                Debug.Log("[UniformBootstrap] Legacy UniformGeneratorPanel removed.");
            }
        }

        Transform existingScreens = null;
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "UniformUIScreens")
            {
                existingScreens = child;
                break;
            }
        }
        if (existingScreens != null)
        {
            if (Application.isPlaying)
                Destroy(existingScreens.gameObject);
            else
                DestroyImmediate(existingScreens.gameObject);
            Debug.Log("[UniformBootstrap] Previous UniformUIScreens removed for rebuild.");
        }

        TeardownPreviewStage();

        Debug.Log($"[UniformBootstrap] Creating UI on Canvas: {canvas.name}");

        lobbyButtonsShown = false;
        lobbyContentRoot = null;
        lobbySplashOverlay = null;
        lobbyDimOverlay = null;

        if (backArrowSprite == null)
        {
            backArrowSprite = Resources.Load<Sprite>("UI/BackArrow");
            if (backArrowSprite == null)
            {
                Debug.LogWarning("[UniformBootstrap] BackArrow sprite not found in Resources/UI. Using text fallback.");
            }
        }

        if (lobbySplashSprite == null)
        {
            lobbySplashSprite = Resources.Load<Sprite>("UI/LobbySplash");
            if (lobbySplashSprite == null)
            {
                Debug.LogWarning("[UniformBootstrap] LobbySplash sprite not found in Resources/UI. Using solid color background.");
            }
        }

        GameObject root = new GameObject("UniformUIScreens");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        screenManager = root.AddComponent<UniformUIScreenManager>();

        Button lobbyStartBtn;
        Button lobbyDesignBtn;
        GameObject lobbyScreen = BuildLobbyScreen(root.transform, out lobbyStartBtn, out lobbyDesignBtn);

        Button gameBackBtn;
        GameObject gameScreen = BuildGameScreen(root.transform, out gameBackBtn);

        InputField inputPrompt;
        Slider tilingSlider;
        Slider strengthSlider;
        GameObject spinner;
        Text toastText;
        Button designBackBtn;
        Dropdown teamDropdown;
        Button generateBtn;

        GameObject designScreen = BuildDesignScreen(root.transform, out inputPrompt, out tilingSlider,
            out strengthSlider, out spinner, out toastText, out designBackBtn, out teamDropdown, out generateBtn);

        GameObject promptHost = new GameObject("UniformPromptController");
        promptHost.transform.SetParent(designScreen.transform, false);

        if (Application.isPlaying && bridge != null)
        {
            promptUI = promptHost.AddComponent<UniformPromptUI>();

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
        screenManager.playButton = lobbyStartBtn;
        screenManager.designButton = lobbyDesignBtn;
        screenManager.backFromGameButton = gameBackBtn;
        screenManager.backFromDesignButton = designBackBtn;
        screenManager.promptUI = promptUI;

        UniformUIScreenManager.OnScreenStateChanged -= HandleScreenStateChanged;
        UniformUIScreenManager.OnScreenStateChanged += HandleScreenStateChanged;

        if (Application.isPlaying)
        {
            screenManager.Initialize();
        }

        HandleScreenStateChanged(screenManager.CurrentState);
    }

    void EnsurePreviewStage()
    {
        int layer = ResolvePreviewLayer();

        if (previewStage == null)
        {
            previewStage = new GameObject("UniformPreviewStage");
            previewStage.transform.position = previewStageOrigin;
        }

        if (previewTexture == null)
        {
            previewTexture = new RenderTexture(960, 960, 16, RenderTextureFormat.ARGB32)
            {
                name = "UniformPreviewRT"
            };
        }

        if (previewCamera == null)
        {
            GameObject camObj = new GameObject("UniformPreviewCamera");
            camObj.transform.SetParent(previewStage.transform, false);
            previewCamera = camObj.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.02f, 0.04f, 0.1f, 1f);
            previewCamera.cullingMask = 1 << layer;
            previewCamera.nearClipPlane = 0.05f;
            previewCamera.farClipPlane = 10f;
            previewCamera.fieldOfView = 24f;
            previewCamera.targetTexture = previewTexture;
            camObj.SetActive(false);
        }

        if (previewLight == null)
        {
            GameObject lightObj = new GameObject("PreviewLight");
            lightObj.transform.SetParent(previewStage.transform, false);
            lightObj.transform.localRotation = Quaternion.Euler(30f, 140f, 0f);
            previewLight = lightObj.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.25f;
            previewLight.color = new Color(1f, 0.96f, 0.9f, 1f);
        }

        EnsurePreviewFloor(layer);
        BuildPreviewCharacter();
        PositionPreviewCamera();
        SetLayerRecursively(previewStage, layer);
    }

    void EnsurePreviewFloor(int layer)
    {
        if (previewStage == null) return;

        Transform floor = previewStage.transform.Find("PreviewFloor");
        if (floor == null)
        {
            GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorObj.name = "PreviewFloor";
            floorObj.transform.SetParent(previewStage.transform, false);
            floorObj.transform.localScale = Vector3.one * 0.18f;

            var rend = floorObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial.color = new Color(0.05f, 0.08f, 0.16f, 1f);
            }

            var col = floorObj.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            floor = floorObj.transform;
        }

        SetLayerRecursively(floor.gameObject, layer);
    }

    void BuildPreviewCharacter()
    {
        if (previewStage == null) return;

        if (previewCharacter != null)
        {
            SafeDestroy(previewCharacter);
            previewCharacter = null;
        }

        Transform source = GetPreviewSourceRoot();
        if (source == null)
        {
            Debug.LogWarning("[UniformBootstrap] Preview source character not found.");
            return;
        }

        previewCharacter = Instantiate(source.gameObject, previewStage.transform, false);
        previewCharacter.name = "UniformPreviewCharacter";
        previewCharacter.transform.localPosition = Vector3.zero;
        previewCharacter.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);
        previewCharacter.transform.localScale = source.localScale * 1.6f;

        foreach (var behaviour in previewCharacter.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour is Animator anim)
            {
                anim.speed = 0f;
                continue;
            }

            behaviour.enabled = false;
        }

        foreach (var rb in previewCharacter.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in previewCharacter.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        SetLayerRecursively(previewCharacter, ResolvePreviewLayer());
    }

    void PositionPreviewCamera()
    {
        if (previewCamera == null || previewStage == null) return;

        previewCamera.transform.SetParent(previewStage.transform, false);
        Vector3 focusLocal = new Vector3(0f, 1.1f, 0f);
        float radius = 1.4f;

        if (previewCharacter != null)
        {
            var renderers = previewCharacter.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }

                focusLocal = previewStage.transform.InverseTransformPoint(b.center);
                focusLocal.y = Mathf.Max(1.05f, focusLocal.y);
                radius = Mathf.Max(0.6f, b.extents.magnitude);
            }
        }

        float fovRad = previewCamera.fieldOfView * Mathf.Deg2Rad;
        float distance = radius / Mathf.Tan(fovRad * 0.55f) + 0.35f;
        Vector3 viewDir = new Vector3(0f, 0.12f, -1f).normalized;

        previewCamera.transform.localPosition = focusLocal + viewDir * distance;
        previewCamera.transform.LookAt(previewStage.transform.TransformPoint(focusLocal + new Vector3(0f, 0.2f, 0f)));
    }

    Transform GetPreviewSourceRoot()
    {
        if (blueRoots != null)
        {
            foreach (var t in blueRoots)
            {
                if (t != null) return t;
            }
        }

        if (purpleRoots != null)
        {
            foreach (var t in purpleRoots)
            {
                if (t != null) return t;
            }
        }

        var env = FindFirstObjectByType<SoccerEnvController>();
        if (env != null && env.AgentsList.Count > 0 && env.AgentsList[0].Agent != null)
        {
            return env.AgentsList[0].Agent.transform;
        }

        return null;
    }

    int ResolvePreviewLayer()
    {
        if (previewLayer >= 0) return previewLayer;
        previewLayer = LayerMask.NameToLayer("Preview");
        if (previewLayer < 0) previewLayer = 30;
        return previewLayer;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null || layer < 0) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void TeardownPreviewStage()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }

        SafeDestroy(previewCharacter);
        previewCharacter = null;

        if (previewTexture != null)
        {
            previewTexture.Release();
            SafeDestroy(previewTexture);
            previewTexture = null;
        }

        SafeDestroy(previewStage);
        previewStage = null;
        previewLight = null;
        previewCamera = null;
    }

    void HandleScreenStateChanged(UniformUIScreenManager.ScreenState state)
    {
        bool designActive = state == UniformUIScreenManager.ScreenState.Design;
        if (previewStage != null)
        {
            previewStage.SetActive(designActive);
        }

        if (previewCamera != null)
        {
            previewCamera.gameObject.SetActive(designActive);
            if (designActive)
            {
                previewCamera.targetTexture = previewTexture;
            }
        }
    }

    void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
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

    GameObject CreateText(Transform parent, string text, int fontSize, TextAnchor alignment, Color? overrideColor = null)
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
        txt.color = overrideColor ?? Color.white;

        return obj;
    }

    void OnLobbySplashClicked()
    {
        if (lobbyButtonsShown)
            return;
        lobbyButtonsShown = true;

        if (lobbySplashOverlay != null)
        {
            lobbySplashOverlay.SetActive(false);
        }

        if (lobbyDimOverlay != null)
        {
            lobbyDimOverlay.gameObject.SetActive(true);
        }

        if (lobbyContentRoot != null)
        {
            lobbyContentRoot.SetActive(true);
        }
    }

    GameObject CreateInputField(Transform parent, string placeholder)
    {
        GameObject obj = new GameObject("InputField");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 35);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.18f, 0.34f, 0.95f);

        InputField input = obj.AddComponent<InputField>();

        // 텍스트 자식 오브젝트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.color = new Color(0.92f, 0.96f, 1f, 1f);
        txt.fontSize = 20;
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
        placeholderTxt.color = new Color(0.7f, 0.78f, 1f, 0.85f);
        placeholderTxt.fontSize = 20;
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

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(container.transform, false);
        Text labelTxt = labelObj.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.text = label;
        labelTxt.color = new Color(0.86f, 0.93f, 1f, 1f);
        labelTxt.fontSize = 20;
        labelTxt.alignment = TextAnchor.MiddleLeft;

        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(container.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 30);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.None;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.45f);
        bgRect.anchorMax = new Vector2(1, 0.55f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.14f, 0.26f, 1f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.62f, 0.98f, 1f);

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(sliderObj.transform, false);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26, 45);
        handleRect.anchorMin = new Vector2(0, 0.5f);
        handleRect.anchorMax = new Vector2(0, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.9f, 0.96f, 1f, 1f);

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
        img.color = new Color(0.98f, 0.98f, 1f, 0.95f);

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
        txt.fontSize = 26;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;

        return obj;
    }

    GameObject CreateLobbyOption(Transform parent, string title, string desc, string buttonLabel, out Button button)
    {
        GameObject option = new GameObject("LobbyOption_" + buttonLabel);
        option.transform.SetParent(parent, false);

        Image cardBg = option.AddComponent<Image>();
        cardBg.color = new Color(0.05f, 0.07f, 0.18f, 0.75f);
        ApplyOutline(cardBg, 1.3f);
        ApplyShadow(cardBg, new Vector2(2.5f, -2.5f), 0.25f);

        VerticalLayoutGroup layout = option.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        GameObject header = CreateText(option.transform, title, 28, TextAnchor.MiddleCenter, Color.white);
        Text headerTxt = header.GetComponent<Text>();
        headerTxt.fontStyle = FontStyle.Bold;
        ApplyOutline(headerTxt, 0.5f);

        GameObject descObj = CreateText(option.transform, desc, 16, TextAnchor.MiddleCenter, new Color(0.9f, 0.93f, 1f, 0.95f));
        LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 40;
        ApplyOutline(descObj.GetComponent<Text>(), 0.5f);

        GameObject btnObj = CreateButton(option.transform, buttonLabel);
        SetupSketchButton(btnObj);
        LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 60;
        btnLayout.preferredWidth = 240;

        button = btnObj.GetComponent<Button>();
        return option;
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

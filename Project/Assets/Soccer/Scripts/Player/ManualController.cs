using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(Rigidbody))]
public class ManualController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;
    public float dashMultiplier = 2f;
    public float maxVel = 10f;

    [Header("Rotation")]
    public float turnSpeedDegPerSec = 540f;
    [Tooltip("Turn speed multiplier when dashing")]
    public float dashTurnMultiplier = 2f;

    [Header("UI Settings")]
    public bool showChargeUI = true;
    public Vector3 uiWorldOffset = new Vector3(0f, 2.5f, 0f);
    public Color chargeStartColor = Color.yellow;
    public Color chargeEndColor = Color.red;

    Rigidbody _rb;
    Vector3 _desiredMove;
    Vector3 _lastMoveDirection = Vector3.forward;
    bool _isDashing = false;

    // UI
    Canvas _canvas;
    GameObject _chargeBarContainer;
    Image _chargeBarFill;
    Camera _mainCamera;

    // Player controller for shared behavior
    SoccerPlayerController _playerController;

    public Transform ball => _playerController != null && GameObject.FindGameObjectWithTag("ball") != null
        ? GameObject.FindGameObjectWithTag("ball").transform : null;
    public bool IsDribbling => _playerController != null && _playerController.IsDribbling;
    public bool IsCharging => _playerController != null && _playerController.IsChargingKick;
    public float ChargeProgress => _playerController != null ? _playerController.GetChargeProgress() : 0f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _mainCamera = Camera.main;

        // Initialize or get player controller
        _playerController = GetComponent<SoccerPlayerController>();
        if (_playerController == null)
        {
            _playerController = gameObject.AddComponent<SoccerPlayerController>();
        }

        // Set team if AgentSoccer exists
        var agent = GetComponent<AgentSoccer>();
        if (agent != null)
        {
            _playerController.SetTeam(agent.team);
        }

        // UI creation
        if (showChargeUI)
        {
            CreateChargeUI();
        }
    }

    void OnEnable()
    {
        var bp = GetComponent<BehaviorParameters>();
        if (bp) bp.BehaviorType = BehaviorType.HeuristicOnly;
    }

    void Update()
    {
        float v = 0f, h = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;

        var moveInput = new Vector3(h, 0f, v);
        Vector3 move = Vector3.zero;
        _isDashing = false;

        if (moveInput.sqrMagnitude > 1e-4f)
        {
            Vector3 normalized = moveInput.normalized;
            _lastMoveDirection = normalized;
            move = normalized * moveSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                move *= dashMultiplier;
                _isDashing = true;
            }
        }

        _desiredMove = move;

        // Update dribble
        _playerController.UpdateDribble();

        // Handle kick input (when dribbling or charging)
        if (IsDribbling || IsCharging)
        {
            HandleKickInput();
        }

        // Update UI
        if (showChargeUI && _chargeBarContainer != null)
        {
            UpdateChargeUI();
        }
    }

    void FixedUpdate()
    {
        if (_desiredMove.sqrMagnitude > 0f)
        {
            _rb.AddForce(_desiredMove, ForceMode.VelocityChange);
        }

        var planar = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (planar.magnitude > maxVel)
        {
            planar = planar.normalized * maxVel;
            _rb.linearVelocity = new Vector3(planar.x, _rb.linearVelocity.y, planar.z);
        }

        // Always rotate to face movement direction when moving
        Vector3 planarVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        // Calculate effective turn speed (faster when dashing)
        float effectiveTurnSpeed = turnSpeedDegPerSec;
        if (_isDashing)
        {
            effectiveTurnSpeed *= dashTurnMultiplier;
        }

        // Use velocity direction if moving fast enough, otherwise use last input direction
        if (planarVelocity.magnitude > 0.1f)
        {
            Vector3 faceDir = planarVelocity.normalized;
            var target = Quaternion.LookRotation(faceDir, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, target, effectiveTurnSpeed * Time.fixedDeltaTime));
        }
        else if (_desiredMove.sqrMagnitude > 1e-4f)
        {
            // When starting to move, face the input direction immediately
            Vector3 faceDir = _lastMoveDirection;
            var target = Quaternion.LookRotation(faceDir, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, target, effectiveTurnSpeed * Time.fixedDeltaTime));
        }
    }

    // ===== Kick Input Handling =====

    void HandleKickInput()
    {
        // Hold space to start/maintain charging
        if (Input.GetKey(KeyCode.Space) && !IsCharging)
        {
            _playerController.StartKickCharge();
        }

        // Release space to execute kick
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (IsCharging)
            {
                _playerController.ExecuteChargedKick();
            }
        }
    }

    // ===== UI System =====

    void CreateChargeUI()
    {
        // Find or create Canvas
        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create charge bar background
        _chargeBarContainer = new GameObject("ChargeBarContainer");
        _chargeBarContainer.transform.SetParent(_canvas.transform, false);

        Image bgImage = _chargeBarContainer.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = _chargeBarContainer.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(200f, 20f);

        // Create charge bar fill
        GameObject fillObj = new GameObject("ChargeBarFill");
        fillObj.transform.SetParent(_chargeBarContainer.transform, false);

        _chargeBarFill = fillObj.AddComponent<Image>();
        _chargeBarFill.color = chargeStartColor;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);

        // Initially hidden
        _chargeBarContainer.SetActive(false);
    }

    void UpdateChargeUI()
    {
        if (_mainCamera == null || _chargeBarContainer == null) return;

        // Show UI only when charging
        if (IsCharging && IsDribbling)
        {
            _chargeBarContainer.SetActive(true);

            // Convert world position to screen position
            Vector3 worldPos = transform.position + uiWorldOffset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // Update UI position
            _chargeBarContainer.transform.position = screenPos;

            // Update charge progress (width)
            if (_chargeBarFill != null)
            {
                float progress = ChargeProgress;
                RectTransform fillRect = _chargeBarFill.GetComponent<RectTransform>();
                fillRect.sizeDelta = new Vector2(200f * progress, 0f);
                _chargeBarFill.color = Color.Lerp(chargeStartColor, chargeEndColor, progress);
            }
        }
        else
        {
            _chargeBarContainer.SetActive(false);
        }
    }

    // Debug gizmo (for Scene view)
    void OnDrawGizmos()
    {
        if (IsCharging && IsDribbling)
        {
            Vector3 barPos = transform.position + Vector3.up * 2.5f;
            float barWidth = 1f;
            float barHeight = 0.15f;
            float progress = ChargeProgress;

            // Background
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawCube(barPos, new Vector3(barWidth, barHeight, 0.01f));

            // Charge bar
            Color fillColor = Color.Lerp(Color.yellow, Color.red, progress);
            Gizmos.color = fillColor;
            Vector3 fillPos = barPos - Vector3.right * barWidth * 0.5f * (1f - progress);
            Gizmos.DrawCube(fillPos, new Vector3(barWidth * progress, barHeight, 0.02f));

            // Border
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(barPos, new Vector3(barWidth, barHeight, 0.01f));
        }
    }
}

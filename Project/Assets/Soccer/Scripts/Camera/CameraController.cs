using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Targets")]
    public Transform fallbackTarget;
    public Transform lookAtOverride;

    [Header("Follow")]
    public Vector3 followOffset = new Vector3(0f, 100f, -50f);
    public float followDamping = 8f;
    public float lookDamping = 10f;
    [Tooltip("Speed used when interpolating to a new follow offset at runtime.")]
    public float offsetLerpSpeed = 6f;

    [Header("Limits")]
    public float minY = 30f;
    public float maxY = 300f;

    Transform _current;
    Vector3 _vel;
    Vector3 _smoothedOffset;
    Vector3 _targetOffset;
    bool _offsetInitialized;

    public Vector3 DefaultFollowOffset => followOffset;
    public Vector3 CurrentFollowOffset => _smoothedOffset;

    void Awake()
    {
        EnsureOffsetInitialized();
    }

    void OnEnable()
    {
        PlayerSwitchManager.OnControlledChanged += HandleControlledChanged;
    }

    void OnDisable()
    {
        PlayerSwitchManager.OnControlledChanged -= HandleControlledChanged;
    }

    void Start()
    {
        _current = FindInitialTarget() ?? fallbackTarget;
        SnapToTarget();
    }

    Transform FindInitialTarget()
    {
        var mgr = FindFirstObjectByType<PlayerSwitchManager>();
        return (mgr != null) ? mgr.CurrentControlled : null;
    }

    void HandleControlledChanged(Transform t) => _current = t;

    void LateUpdate()
    {
        if (_current == null) return;

        UpdateFollowOffset();
        Vector3 desired = _current.position + _smoothedOffset;
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref _vel, 1f / Mathf.Max(0.0001f, followDamping)
        );

        var lookT = lookAtOverride != null ? lookAtOverride : _current;
        Vector3 dir = (lookT.position - transform.position).normalized;
        if (dir.sqrMagnitude > 0.0001f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookDamping);
        }
    }

    void SnapToTarget()
    {
        EnsureOffsetInitialized();
        _smoothedOffset = _targetOffset;
        if (_current == null) return;
        Vector3 desired = _current.position + _smoothedOffset;
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        transform.position = desired;

        var lookT = lookAtOverride != null ? lookAtOverride : _current;
        transform.LookAt(lookT.position, Vector3.up);
    }

    public void SetFollowOffset(Vector3 newOffset, bool instant = false)
    {
        EnsureOffsetInitialized();
        _targetOffset = newOffset;
        if (instant)
        {
            _smoothedOffset = newOffset;
        }
    }

    void EnsureOffsetInitialized()
    {
        if (_offsetInitialized)
        {
            return;
        }

        _smoothedOffset = followOffset;
        _targetOffset = followOffset;
        _offsetInitialized = true;
    }

    void UpdateFollowOffset()
    {
        EnsureOffsetInitialized();
        if (offsetLerpSpeed <= 0f)
        {
            _smoothedOffset = _targetOffset;
            return;
        }

        float t = 1f - Mathf.Exp(-offsetLerpSpeed * Time.deltaTime);
        _smoothedOffset = Vector3.Lerp(_smoothedOffset, _targetOffset, t);
    }
}

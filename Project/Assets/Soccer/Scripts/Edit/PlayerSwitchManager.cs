using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

public class PlayerSwitchManager : MonoBehaviour
{
    [Header("Setup")]
    public string blueAgentTag = "blueAgent";
    public Transform ball;
    public float checkInterval = 0.15f;

    [Header("Switching Guard")]
    public float minSwitchCooldown = 0.6f;
    public float hysteresis = 1.5f;

    [Header("Visual Indicator")]
    public bool usePrefabIndicator = false;
    public GameObject controlIndicatorPrefab;
    [Tooltip("Procedural indicator color (used when no prefab is assigned).")]
    public Color indicatorColor = new Color(0.1f, 0.85f, 0.3f, 1f);
    [Tooltip("Radius of the procedural indicator ring.")]
    public float indicatorRadius = 1.8f;
    [Tooltip("Line width of the procedural indicator ring.")]
    public float indicatorLineWidth = 0.1f;
    [Tooltip("Vertical offset applied when attaching the indicator to the player.")]
    public float indicatorYOffset = 0.2f;
    [Range(3, 128)]
    [Tooltip("How smooth the procedural ring should be.")]
    public int indicatorSegments = 32;

    GameObject currentIndicator;
    float _timer, _cooldown;
    Transform _currentControlled;

    GameObject[] _allBlueAgents = Array.Empty<GameObject>();
    LineRenderer _proceduralIndicator;
    Material _indicatorMaterial;

    public static event Action<Transform> OnControlledChanged;
    public Transform CurrentControlled => _currentControlled;

    void Awake()
    {
        CacheAgents();
        TryResolveBallReference();
    }

    void Start()
    {
        EnsureInitialControlledPlayer();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        _cooldown -= Time.deltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;
        if (!TryResolveBallReference()) return;

        CacheAgents();
        if (_allBlueAgents == null || _allBlueAgents.Length == 0) return;

        // 입력이 들어오는 동안은 스위치 보류
        bool userMoving =
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.LeftShift);

        var nearest = FindNearestAgentToBall();
        if (nearest == null) return;
        float bestSqr = SqrDistXZ(nearest, ball);

        // 현재 조종 중인 선수의 거리
        float currentSqr = (_currentControlled != null)
            ? SqrDistXZ(_currentControlled, ball)
            : Mathf.Infinity;

        bool isSame = (nearest == _currentControlled);
        float h2 = hysteresis * hysteresis;
        bool clearlyCloser = (bestSqr + h2) < currentSqr;

        // 스위치 조건: 다른 선수이고, 쿨다운 끝났고, 충분히 더 가깝고, 플레이어가 입력 중이 아님
        if (!isSame && _cooldown <= 0f && clearlyCloser && !userMoving)
        {
            SwitchControlTo(nearest);
        }
    }

    void SwitchControlTo(Transform newTarget)
    {
        if (_allBlueAgents != null)
        {
            foreach (var go in _allBlueAgents)
            {
                var dr = go.GetComponent<DecisionRequester>();
                if (dr) dr.enabled = true;

                var bp = go.GetComponent<BehaviorParameters>();
                if (bp) bp.BehaviorType = BehaviorType.InferenceOnly;

                var drv = go.GetComponent<ManualController>();
                if (drv) drv.enabled = false;

                var ag = go.GetComponent<AgentSoccer>();
                if (ag) ag.manualOverride = false;
            }
        }

        var targetDR = newTarget.GetComponent<DecisionRequester>();
        if (targetDR) targetDR.enabled = false;

        var driver = newTarget.GetComponent<ManualController>();
        if (!driver) driver = newTarget.gameObject.AddComponent<ManualController>();
        driver.enabled = true;

        var tgtAg = newTarget.GetComponent<AgentSoccer>();
        if (tgtAg) tgtAg.manualOverride = true;

        _currentControlled = newTarget;
        OnControlledChanged?.Invoke(_currentControlled);

        if (usePrefabIndicator && controlIndicatorPrefab != null)
        {
            if (currentIndicator == null)
            {
                currentIndicator = Instantiate(controlIndicatorPrefab);
            }
            currentIndicator.transform.SetParent(newTarget, false);
            currentIndicator.transform.localPosition = new Vector3(0, indicatorYOffset, 0);
        }
        else
        {
            AttachProceduralIndicator(newTarget);
        }

        _cooldown = minSwitchCooldown;
    }

    void CacheAgents()
    {
        _allBlueAgents = GameObject.FindGameObjectsWithTag(blueAgentTag);
    }

    bool TryResolveBallReference()
    {
        if (ball != null)
        {
            return true;
        }

        var foundBall = GameObject.FindGameObjectWithTag("ball");
        if (foundBall != null)
        {
            ball = foundBall.transform;
        }

        return ball != null;
    }

    void EnsureInitialControlledPlayer()
    {
        CacheAgents();
        if (!TryResolveBallReference())
        {
            return;
        }

        if (_currentControlled != null)
        {
            return;
        }

        var nearest = FindNearestAgentToBall();
        if (nearest != null)
        {
            SwitchControlTo(nearest);
        }
    }

    Transform FindNearestAgentToBall()
    {
        if (_allBlueAgents == null || _allBlueAgents.Length == 0 || ball == null)
        {
            return null;
        }

        Transform nearest = null;
        float bestSqr = float.MaxValue;

        foreach (var go in _allBlueAgents)
        {
            if (!go || !go.activeInHierarchy) continue;
            float d2 = SqrDistXZ(go.transform, ball);
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                nearest = go.transform;
            }
        }

        return nearest;
    }

    static float SqrDistXZ(Transform tA, Transform tB)
    {
        if (tA == null || tB == null) return float.MaxValue;
        Vector3 d = tA.position - tB.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }

    void AttachProceduralIndicator(Transform target)
    {
        if (target == null)
        {
            return;
        }

        EnsureProceduralIndicator();
        _proceduralIndicator.transform.SetParent(target, false);
        _proceduralIndicator.transform.localPosition = new Vector3(0f, indicatorYOffset, 0f);
        _proceduralIndicator.transform.localRotation = Quaternion.identity;
        _proceduralIndicator.enabled = true;
    }

    void EnsureProceduralIndicator()
    {
        if (_proceduralIndicator != null)
        {
            UpdateIndicatorAppearance();
            return;
        }

        var go = new GameObject("ControlIndicator(LineRenderer)");
        _proceduralIndicator = go.AddComponent<LineRenderer>();
        _proceduralIndicator.loop = true;
        _proceduralIndicator.useWorldSpace = false;
        _proceduralIndicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _proceduralIndicator.receiveShadows = false;
        _proceduralIndicator.alignment = LineAlignment.View;
        _proceduralIndicator.textureMode = LineTextureMode.Stretch;
        _indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
        _proceduralIndicator.material = _indicatorMaterial;
        UpdateIndicatorAppearance();
    }

    void UpdateIndicatorAppearance()
    {
        if (_proceduralIndicator == null)
        {
            return;
        }

        indicatorSegments = Mathf.Clamp(indicatorSegments, 3, 128);
        _proceduralIndicator.positionCount = indicatorSegments;
        _proceduralIndicator.startColor = indicatorColor;
        _proceduralIndicator.endColor = indicatorColor;
        _proceduralIndicator.widthMultiplier = Mathf.Max(0.001f, indicatorLineWidth);
        if (_indicatorMaterial != null)
        {
            _indicatorMaterial.color = indicatorColor;
        }

        var points = new Vector3[indicatorSegments];
        float step = Mathf.PI * 2f / indicatorSegments;
        for (int i = 0; i < indicatorSegments; i++)
        {
            float angle = step * i;
            points[i] = new Vector3(Mathf.Cos(angle) * indicatorRadius, 0f, Mathf.Sin(angle) * indicatorRadius);
        }
        _proceduralIndicator.SetPositions(points);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        UpdateIndicatorAppearance();
    }
#endif

    void OnDestroy()
    {
        if (_indicatorMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_indicatorMaterial);
            }
            else
            {
                DestroyImmediate(_indicatorMaterial);
            }
        }
    }
}

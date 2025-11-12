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
    public GameObject controlIndicatorPrefab;

    GameObject currentIndicator;
    float _timer, _cooldown;
    Transform _currentControlled;

    GameObject[] _allBlueAgents = Array.Empty<GameObject>();

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
        driver.ball = ball;
        driver.enabled = true;

        var tgtAg = newTarget.GetComponent<AgentSoccer>();
        if (tgtAg) tgtAg.manualOverride = true;

        _currentControlled = newTarget;
        OnControlledChanged?.Invoke(_currentControlled);

        if (controlIndicatorPrefab != null)
        {
            if (currentIndicator == null)
                currentIndicator = Instantiate(controlIndicatorPrefab);
            currentIndicator.transform.SetParent(newTarget);
            currentIndicator.transform.localPosition = new Vector3(0, 5f, 0);
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
}

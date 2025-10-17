using System.Linq;
using UnityEngine;

public class AutoKickPass : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform ball;
    public Transform goalTarget;
    public Transform[] teammates;
    public Transform footMarker;

    [Header("Decision")]
    public float maxKickRange = 1.3f;      // 발-공 최대 거리
    public float kickForwardAngle = 55f;   // 골문 정면 각(°)
    public float minShootDistance = 8f;    // 이 거리보다 골 가까우면 슛 가중
    public float passMinAngle = 110f;       // 동료가 시야 각도 안(°)
    public float passMaxDistance = 14f;     // 동료 최대 거리(m)

    public float startDelay = 0.4f;        // ▶ 시작 직후 트리거 금지 시간
    public float decisionCooldown = 0.25f;  // ▶ 연속 결정 쿨타임
    public float minMoveSpeed = 0.03f;     // ▶ 거의 정지면 액션 금지(m/s)

    [Header("Heuristics")]
    public float preferShootWeight = 1.0f;
    public float preferPassWeight  = 1.6f;

    [Header("State Names (Animator와 정확히 일치)")]
    public string kickStateName = "Strike Foward Jog";
    public string passStateName = "Soccer Pass";

    float _cooldown;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Reset()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!ball)
        {
            var b = GameObject.FindWithTag("Ball");
            if (b) ball = b.transform;
        }
        if (!goalTarget)
        {
            var tgt = GameObject.Find("GoalCenter_Purple") ?? GameObject.Find("GoalCenter_Blue");
            if (tgt) goalTarget = tgt.transform;
        }
        if (!footMarker)
        {
            var f = transform.GetComponentsInChildren<Transform>(true)
                             .FirstOrDefault(t => t.name.ToLower().Contains("footmarker"));
            if (f) footMarker = f;
        }
    }

    void Start()
    {
        _cooldown = startDelay;
        if (animator)
        {
            animator.ResetTrigger("Kick");
            animator.ResetTrigger("Pass");
            animator.SetBool("IsDribbling", false);
        }
    }

    void Update()
    {
        if (!animator || !ball || !goalTarget || !footMarker) return;

        if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }

        // ▶ 현재 킥/패스 중이면 새 결정 금지
        var si = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0)) return;
        if (si.IsName(kickStateName) || si.IsName(passStateName)) return;

        // ▶ 거의 정지 상태면 시도 안함(스폰/충돌 직후 튐 방지)
        if (_rb && _rb.linearVelocity.sqrMagnitude < (minMoveSpeed * minMoveSpeed)) return;

        // ▶ 발-공 근접 체크
        float distBall = Vector3.Distance(footMarker.position, ball.position);
        if (distBall > maxKickRange) return;

        // ▶ 방향/거리 계산
        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();

        Vector3 toGoal = goalTarget.position - transform.position;
        toGoal.y = 0; toGoal.Normalize();
        float angleToGoal = Vector3.Angle(fwd, toGoal);
        float distGoal = Vector3.Distance(transform.position, goalTarget.position);

        // ▶ 패스 후보 평가
        Transform bestMate = FindBestTeammate();
        float passScore = 0f;
        if (bestMate)
        {
            Vector3 toMate = bestMate.position - transform.position;
            float distMate = new Vector2(toMate.x, toMate.z).magnitude;

            Vector3 dirMate = toMate; dirMate.y = 0; dirMate.Normalize();
            float angleToMate = Vector3.Angle(fwd, dirMate);

            if (angleToMate <= passMinAngle && distMate <= passMaxDistance)
            {
                passScore = preferPassWeight
                          * ((passMinAngle - angleToMate) / Mathf.Max(passMinAngle, 0.0001f))
                          * Mathf.Clamp01(1f - distMate / Mathf.Max(passMaxDistance, 0.0001f));
            }
        }

        // ▶ 슛 점수 평가
        float shootScore = 0f;
        if (angleToGoal <= kickForwardAngle)
        {
            shootScore = preferShootWeight
                       * ((kickForwardAngle - angleToGoal) / Mathf.Max(kickForwardAngle, 0.0001f))
                       * Mathf.Clamp01(1f - distGoal / Mathf.Max(minShootDistance, 0.0001f));

            if (distGoal <= minShootDistance) shootScore += 0.35f; // 골 근접 보너스
        }

        // ▶ 아무 조건도 만족 못 하면 종료
        if (shootScore <= 0f && passScore <= 0f) return;

        // ▶ 최종 의사결정 + 트리거
        if (shootScore >= passScore)
        {
            animator.ResetTrigger("Pass");
            animator.SetTrigger("Kick");
        }
        else
        {
            animator.ResetTrigger("Kick");
            animator.SetTrigger("Pass");
        }

        _cooldown = decisionCooldown;
    }

    Transform FindBestTeammate()
    {
        if (teammates == null || teammates.Length == 0) return null;
        Transform best = null;
        float bestS = -1f;

        foreach (var t in teammates)
        {
            if (!t) continue;

            Vector3 to = t.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            float ang  = Vector3.Angle(transform.forward, to.normalized);

            float score = Mathf.Clamp01(1f - dist / Mathf.Max(passMaxDistance, 0.0001f))
                        * Mathf.Clamp01((passMinAngle - ang) / Mathf.Max(passMinAngle, 0.0001f));

            if (score > bestS) { bestS = score; best = t; }
        }
        return best;
    }
}
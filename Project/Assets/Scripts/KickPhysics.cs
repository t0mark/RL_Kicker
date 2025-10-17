using UnityEngine;

public class KickPhysics : MonoBehaviour
{
    [Header("Refs")]
    public Transform foot;          // 발등 기준 위치(접촉 판정 중심)
    public Transform goalTarget;    // 슛 방향 타깃(상대 골 중앙 등)
    public Transform teammate;      // 패스 대상(동료)
    public LayerMask ballMask;      // 공 레이어

    [Header("Tuning")]
    public float kickPower = 8f;        // 슛 파워(속도 변화량)
    public float passPower = 6f;        // 패스 파워
    public float lift = 0.2f;           // 위로 드는 비율(로프트)
    public float contactRadius = 0.25f; // 발 주변 공 감지 반경
    public float maxKickDistance = 0.6f;// 실제 임펄스 적용 허용 거리
    public float preDamp = 0.2f;        // 임팩트 전 감속(0~1, 0이면 감속 없음)

    // 발 주변에 있는 공(첫 번째) 찾기
    Rigidbody FindBallAtFoot()
    {
        var hits = Physics.OverlapSphere(foot.position, contactRadius, ballMask);
        if (hits.Length == 0) return null;
        return hits[0].attachedRigidbody;
    }

    // 공에 임펄스(속도 변화) 적용
    void ApplyImpulse(Rigidbody ball, Vector3 dir, float power)
    {
        if (!ball) return;

        // 발과 공이 너무 멀면 안전 차단
        if (Vector3.Distance(foot.position, ball.position) > maxKickDistance) return;

        // 임팩트 전 감속으로 튐/불안정 완화
        ball.linearVelocity *= preDamp;

        // 방향 * 파워 만큼 즉시 속도 변경
        ball.AddForce(dir * power, ForceMode.VelocityChange);
    }

    // 애니메이션 임팩트 타이밍에 호출: 슛(골 타깃 방향)
    public void OnKickContact()
    {
        var rb = FindBallAtFoot(); 
        if (!rb || !goalTarget) return;

        // 골 방향으로, 약간 들어 올리며 정규화
        Vector3 toGoal = (goalTarget.position - rb.position).normalized;
        Vector3 dir = new Vector3(toGoal.x, lift, toGoal.z).normalized;

        ApplyImpulse(rb, dir, kickPower);
    }

    // 애니메이션 임팩트 타이밍에 호출: 패스(동료 방향)
    public void OnPassContact()
    {
        var rb = FindBallAtFoot(); 
        if (!rb || !teammate) return;

        // 동료 방향으로, 슛보다 낮게 띄움
        Vector3 toMate = (teammate.position - rb.position).normalized;
        Vector3 dir = new Vector3(toMate.x, lift * 0.5f, toMate.z).normalized;

        ApplyImpulse(rb, dir, passPower);
    }

#if UNITY_EDITOR
    // 에디터에서 발 주변 감지 반경 가시화
    void OnDrawGizmosSelected()
    {
        if (!foot) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(foot.position, contactRadius);
    }
#endif
}
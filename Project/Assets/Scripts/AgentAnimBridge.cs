using UnityEngine;

public class AgentAnimBridge : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform visualRoot;

    [Header("Tuning")]
    public float targetRunSpeed = 2.8f;
    public float speedDamp = 0.12f;

    Rigidbody rb;
    float smoothedForward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!animator) Debug.LogWarning("AgentAnimBridge: Animator not assigned.");
        if (!visualRoot) visualRoot = transform;
    }

    void Update()
    {
        if (!rb || !animator) return;

        // 1) Rigidbody 속도는 velocity 사용
        Vector3 v = rb.linearVelocity; v.y = 0f;

        // 2) 전방 기준 부호 있는 전진 속도 (뒤로 가면 음수)
        Vector3 fwd = visualRoot.forward; fwd.y = 0f; fwd.Normalize();
        float forward = Vector3.Dot(v, fwd);    // m/s

        // 3) 부드럽게
        smoothedForward = Mathf.Lerp(
            smoothedForward, forward,
            1f - Mathf.Exp(-Time.deltaTime / speedDamp)
        );

        // 4) 최고 달리기 속도로 정규화해서 [-1..1] 범위로
        float forwardNorm = (targetRunSpeed > 0.01f)
            ? Mathf.Clamp(smoothedForward / targetRunSpeed, -1f, 1f)
            : 0f;

        // Blend Tree용 파라미터
        animator.SetFloat("ForwardSpeed", forwardNorm);

        // (선택) 달리기 재생속도 보정
        float runMult = Mathf.Clamp(Mathf.Abs(forwardNorm), 0.6f, 1.2f);
        animator.SetFloat("RunSpeedMult", runMult);

    }
}
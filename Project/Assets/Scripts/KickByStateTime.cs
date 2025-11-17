using UnityEngine;

public class KickByStateTime : MonoBehaviour
{
    public Animator animator;
    public KickPhysics kicker;

    [Header("State Names (Animator 상태 이름과 정확히 일치)")]
    public string kickStateName = "Strike Foward Jog";

    [Header("Trigger time (0~1, 임팩트 시점)")]
    [Range(0f, 1f)] public float kickTime = 0.45f;

    bool kicked;

    void Update()
    {
        if (!animator || !kicker) return;

        var stCur   = animator.GetCurrentAnimatorStateInfo(0);
        var stNext  = animator.GetNextAnimatorStateInfo(0);
        bool inTrans = animator.IsInTransition(0);

        // 전이 중이면 다음 상태가 킥인지 보고, 아니면 현재 상태 기준으로 판단
        bool useNext = inTrans && stNext.IsName(kickStateName);
        var st = useNext ? stNext : stCur;

        // 킥 상태 처리
        if (st.IsName(kickStateName))
        {
            float t = st.normalizedTime % 1f;

            if (!kicked && t >= kickTime)
            {
                kicker.OnKickContact();
                kicked = true;
            }

            // 상태가 거의 끝나거나, 전이 후 다시 처음으로 돌아가면 플래그 리셋
            if ((!useNext && t > 0.98f) || (useNext && stCur.normalizedTime % 1f < 0.1f))
                kicked = false;

            return;
        }

        // 다른 상태면 리셋
        kicked = false;
    }
}
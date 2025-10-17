using UnityEngine;

public class KickByStateTime : MonoBehaviour
{
    public Animator animator;
    public KickPhysics kicker;

    [Header("State Names (Animator 상태 이름과 정확히 일치)")]
    public string kickStateName = "Strike Foward Jog";
    public string passStateName = "Soccer Pass";

    [Header("Trigger time (0~1, 임팩트 시점)")]
    [Range(0f,1f)] public float kickTime = 0.45f;
    [Range(0f,1f)] public float passTime = 0.45f;

    bool kicked, passed;

    void Update()
    {
        if (!animator || !kicker) return;

        var stCur  = animator.GetCurrentAnimatorStateInfo(0);
        var stNext = animator.GetNextAnimatorStateInfo(0);
        bool inTrans = animator.IsInTransition(0);

        // 현재/다음 상태 중 어떤 걸 보고 판단할지 선택
        bool useNext = inTrans && (stNext.IsName(kickStateName) || stNext.IsName(passStateName));
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
            // 상태가 거의 끝나면 플래그 리셋 (다음 진입을 위해)
            if (!useNext && (t > 0.98f) || (useNext && stCur.normalizedTime % 1f < 0.1f))
                kicked = false;
            return;
        }

        // 패스 상태 처리
        if (st.IsName(passStateName))
        {
            float t = st.normalizedTime % 1f;
            if (!passed && t >= passTime)
            {
                kicker.OnPassContact();
                passed = true;
            }
            if (!useNext && (t > 0.98f) || (useNext && stCur.normalizedTime % 1f < 0.1f))
                passed = false;
            return;
        }

        // 다른 상태면 리셋
        kicked = false;
        passed = false;
    }
}

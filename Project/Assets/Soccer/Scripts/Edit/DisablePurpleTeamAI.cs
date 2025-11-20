using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

/// <summary>
/// 퍼플 팀(상대팀)의 AI를 완전히 비활성화하여 정지 상태로 만듭니다.
/// </summary>
public class DisablePurpleTeamAI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInitialize()
    {
        // Scene이 로드된 후 자동으로 이 GameObject를 생성
        GameObject manager = new GameObject("_PurpleAIDisabler");
        manager.AddComponent<DisablePurpleTeamAI>();
        DontDestroyOnLoad(manager);
    }

    void Start()
    {
        // 약간의 딜레이를 주고 실행 (다른 초기화가 끝난 후)
        Invoke(nameof(DisableAllPurpleAgents), 0.2f);
    }

    void DisableAllPurpleAgents()
    {
        // 모든 AgentSoccer 찾기
        var allAgents = FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None);

        int disabledCount = 0;
        foreach (var agent in allAgents)
        {
            // 퍼플 팀만 처리
            if (agent.team == Team.Purple)
            {
                // DecisionRequester 비활성화
                var dr = agent.GetComponent<DecisionRequester>();
                if (dr != null)
                {
                    dr.enabled = false;
                }

                // BehaviorParameters를 HeuristicOnly로 설정
                var bp = agent.GetComponent<BehaviorParameters>();
                if (bp != null)
                {
                    bp.BehaviorType = BehaviorType.HeuristicOnly;
                }

                // manualOverride 활성화 (움직이지 않도록)
                agent.manualOverride = true;

                // Rigidbody 속도 제로
                var rb = agent.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Debug.Log($"[PurpleAI] Disabled {agent.gameObject.name}");
                disabledCount++;
            }
        }

        if (disabledCount > 0)
        {
            Debug.Log($"<color=magenta>[PurpleAI] {disabledCount}개의 퍼플 팀 AI를 완전히 정지시켰습니다.</color>");
        }
    }
}

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

/// <summary>
/// 게임 시작 시 모든 AI를 완전히 비활성화합니다.
/// - 모든 Agent를 Heuristic 모드로 변경
/// - DecisionRequester 비활성화하여 AI 행동 중지
/// - 조작하는 캐릭터만 움직이고 나머지는 정지
/// </summary>
public class DisableModelsOnStart : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        // Scene이 로드될 때 자동으로 이 GameObject를 생성
        GameObject manager = new GameObject("_AIDisabler");
        manager.AddComponent<DisableModelsOnStart>();
        DontDestroyOnLoad(manager);
    }

    void Awake()
    {
        // 모든 BehaviorParameters 찾기
        var allBehaviors = FindObjectsByType<BehaviorParameters>(FindObjectsSortMode.None);

        int changedCount = 0;
        foreach (var behavior in allBehaviors)
        {
            // Heuristic 모드로 전환 (AI 모델 사용 중지)
            if (behavior.BehaviorType != BehaviorType.HeuristicOnly)
            {
                Debug.Log($"[AIDisabler] Changing {behavior.gameObject.name} to HeuristicOnly mode");
                behavior.BehaviorType = BehaviorType.HeuristicOnly;
                changedCount++;
            }

            // DecisionRequester 비활성화 (AI 행동 중지)
            var decisionRequester = behavior.GetComponent<DecisionRequester>();
            if (decisionRequester != null && decisionRequester.enabled)
            {
                decisionRequester.enabled = false;
                Debug.Log($"[AIDisabler] Disabled DecisionRequester on {behavior.gameObject.name}");
            }

            // AgentSoccer의 manualOverride 활성화 (AI 완전 차단)
            var agent = behavior.GetComponent<AgentSoccer>();
            if (agent != null)
            {
                agent.manualOverride = true;
            }
        }

        if (changedCount > 0)
        {
            Debug.Log($"<color=green>[AIDisabler] {changedCount}개의 Agent AI를 비활성화했습니다.</color>");
            Debug.Log("[AIDisabler] 모든 캐릭터가 정지 상태입니다. PlayerSwitchManager가 조작할 캐릭터를 활성화합니다.");
        }
    }
}

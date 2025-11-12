#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SoccerEnvController))]
public class SoccerFiveFormationAuthoring : MonoBehaviour
{
    [Header("Depth (absolute distance from field center on the X axis)")]
    [SerializeField]
    float strikerDepth = 12f;

    [SerializeField]
    float defenderDepth = 20f;

    [SerializeField]
    float goalieDepth = 26f;

    [Header("Lateral spacing on the Z axis")]
    [SerializeField]
    float strikerLateralSpacing = 6f;

    [SerializeField]
    float defenderLateralSpacing = 8f;

    [SerializeField]
    float agentHeight = 0.5f;

    [Tooltip("Re-apply the formation in edit mode when values change.")]
    [SerializeField]
    bool applyInEditMode = true;

    SoccerEnvController m_Controller;

    void Awake()
    {
        m_Controller = GetComponent<SoccerEnvController>();
        m_Controller?.EnsureAgentsList();
    }

    void Start()
    {
        ApplyFormation();
    }

    public void ApplyFormation()
    {
        if (m_Controller == null)
        {
            return;
        }

        m_Controller.EnsureAgentsList();
        var counters = new Dictionary<Team, Dictionary<AgentSoccer.Position, int>>();
        foreach (var player in m_Controller.Players)
        {
            if (player == null || player.Agent == null)
            {
                continue;
            }

            var spawn = ResolveSpawnPosition(player.Agent.team, player.Agent.position, counters);
            player.Agent.initialPos = spawn;
            player.Agent.transform.position = spawn;
        }
    }

    Vector3 ResolveSpawnPosition(
        Team team,
        AgentSoccer.Position position,
        Dictionary<Team, Dictionary<AgentSoccer.Position, int>> counters)
    {
        var slotIndex = GetAndIncrementCounter(team, position, counters);
        var direction = team == Team.Blue ? -1f : 1f;

        var targetDepth = GetDepthForRole(position);
        var lateralSpacing = GetLateralSpacingForRole(position);
        var lateralOffset = ComputeLateralOffset(position, slotIndex, lateralSpacing);

        var halfSize = m_Controller.FieldHalfSize;
        var clampedDepth = Mathf.Clamp(targetDepth, 0f, Mathf.Max(halfSize.x - 1f, 0f));
        var signedDepth = direction * clampedDepth;
        var widthClamp = Mathf.Max(halfSize.y - 1f, 0f);
        var clampedLateral = Mathf.Clamp(lateralOffset, -widthClamp, widthClamp);

        return new Vector3(signedDepth, agentHeight, clampedLateral);
    }

    float GetDepthForRole(AgentSoccer.Position position)
    {
        switch (position)
        {
            case AgentSoccer.Position.Defender:
                return defenderDepth;
            case AgentSoccer.Position.Goalie:
                return goalieDepth;
            default:
                return strikerDepth;
        }
    }

    float GetLateralSpacingForRole(AgentSoccer.Position position)
    {
        switch (position)
        {
            case AgentSoccer.Position.Defender:
                return defenderLateralSpacing;
            case AgentSoccer.Position.Goalie:
                return 0f;
            default:
                return strikerLateralSpacing;
        }
    }

    float ComputeLateralOffset(AgentSoccer.Position position, int slot, float spacing)
    {
        if (position == AgentSoccer.Position.Goalie || spacing <= 0f)
        {
            return 0f;
        }

        switch (slot)
        {
            case 0:
                return -spacing * 0.5f;
            case 1:
                return spacing * 0.5f;
            case 2:
                return -spacing * 1.5f;
            case 3:
                return spacing * 1.5f;
            default:
                return 0f;
        }
    }

    int GetAndIncrementCounter(
        Team team,
        AgentSoccer.Position position,
        Dictionary<Team, Dictionary<AgentSoccer.Position, int>> counters)
    {
        if (!counters.TryGetValue(team, out var teamCounters))
        {
            teamCounters = new Dictionary<AgentSoccer.Position, int>();
            counters.Add(team, teamCounters);
        }

        if (!teamCounters.TryGetValue(position, out var count))
        {
            count = 0;
        }

        teamCounters[position] = count + 1;
        return count;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!applyInEditMode)
        {
            return;
        }

        if (m_Controller == null)
        {
            m_Controller = GetComponent<SoccerEnvController>();
        }

        if (EditorApplication.isPlaying)
        {
            return;
        }

        ApplyFormation();
    }
#endif
}

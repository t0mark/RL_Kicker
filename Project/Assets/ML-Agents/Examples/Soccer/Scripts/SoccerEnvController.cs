using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    [Header("Field Configuration")]
    [Tooltip("Half size of the playable field (x = length, y = width).")]
    [SerializeField]
    Vector2 m_FieldHalfSize = new Vector2(12f, 8f);

    [Tooltip("Random spawn jitter applied to agents on reset (x = length, y = width).")]
    [SerializeField]
    Vector2 m_PlayerSpawnJitter = new Vector2(5f, 0f);

    [Tooltip("Random spawn jitter applied to the ball on reset (x = length, y = width).")]
    [SerializeField]
    Vector2 m_BallSpawnJitter = new Vector2(2.5f, 2.5f);

    [Tooltip("Preferred radius (from own goal) for defenders to hold their shape.")]
    [SerializeField]
    float m_DefensiveShellRadius = 9f;

    [Tooltip("Maximum radius (from own goal) defenders are allowed before receiving penalties.")]
    [SerializeField]
    float m_DefensiveMaxRadius = 14f;

    [Header("Scene References")]
    [SerializeField]
    Transform m_BlueGoal;

    [SerializeField]
    Transform m_PurpleGoal;

    [Tooltip("Automatically collect AgentSoccer instances from children when the list is empty.")]
    [SerializeField]
    bool m_AutoPopulateAgents = true;

    private SoccerSettings m_SoccerSettings;


    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    protected virtual void Start()
    {
        EnsureAgentsList();

        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        // Initialize TeamManager
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        CacheGoalReferences();
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public virtual void ResetBall()
    {
        var randomPosX = Random.Range(-m_BallSpawnJitter.x, m_BallSpawnJitter.x);
        var randomPosZ = Random.Range(-m_BallSpawnJitter.y, m_BallSpawnJitter.y);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

    }

    public virtual void GoalTouched(Team scoredTeam)
    {
        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1);
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1);
        }
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();

    }


    public virtual void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var newStartPos = GetSpawnPosition(item);
            var newRot = GetSpawnRotation(item);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }

    protected virtual Vector3 GetSpawnPosition(PlayerInfo player)
    {
        var randomPosX = Random.Range(-m_PlayerSpawnJitter.x, m_PlayerSpawnJitter.x);
        var randomPosZ = Random.Range(-m_PlayerSpawnJitter.y, m_PlayerSpawnJitter.y);
        return player.Agent.initialPos + new Vector3(randomPosX, 0f, randomPosZ);
    }

    protected virtual Quaternion GetSpawnRotation(PlayerInfo player)
    {
        var rot = player.Agent.rotSign * Random.Range(80.0f, 100.0f);
        return Quaternion.Euler(0, rot, 0);
    }

    void CacheGoalReferences()
    {
        if (m_BlueGoal == null)
        {
            var goal = GameObject.FindGameObjectWithTag("blueGoal");
            if (goal != null)
            {
                m_BlueGoal = goal.transform;
            }
        }
        if (m_PurpleGoal == null)
        {
            var goal = GameObject.FindGameObjectWithTag("purpleGoal");
            if (goal != null)
            {
                m_PurpleGoal = goal.transform;
            }
        }
    }

    public Transform GetGoalTransform(Team team)
    {
        return team == Team.Blue ? m_BlueGoal : m_PurpleGoal;
    }

    public Transform BallTransform => ball != null ? ball.transform : null;

    public Vector2 FieldHalfSize => m_FieldHalfSize;
    public Vector2 PlayerSpawnJitter => m_PlayerSpawnJitter;
    public Vector2 BallSpawnJitter => m_BallSpawnJitter;
    public float DefensiveShellRadius => m_DefensiveShellRadius;
    public float DefensiveMaxRadius => m_DefensiveMaxRadius;
    public IReadOnlyList<PlayerInfo> Players => AgentsList;

    public void SetFieldHalfSize(Vector2 halfSize)
    {
        m_FieldHalfSize = halfSize;
    }

    public void SetPlayerSpawnJitter(Vector2 jitter)
    {
        m_PlayerSpawnJitter = jitter;
    }

    public void SetBallSpawnJitter(Vector2 jitter)
    {
        m_BallSpawnJitter = jitter;
    }

    public void SetDefensiveRadii(float shellRadius, float maxRadius)
    {
        m_DefensiveShellRadius = shellRadius;
        m_DefensiveMaxRadius = Mathf.Max(shellRadius, maxRadius);
    }

    public void EnsureAgentsList()
    {
        if (!m_AutoPopulateAgents)
        {
            return;
        }

        AgentsList.RemoveAll(info => info == null || info.Agent == null);
        PopulateAgentsFromChildren();
    }

    void PopulateAgentsFromChildren()
    {
        if (!m_AutoPopulateAgents)
        {
            return;
        }

        var foundAgents = GetComponentsInChildren<AgentSoccer>(true);
        foreach (var agent in foundAgents)
        {
            var alreadyRegistered = AgentsList.Exists(info => info.Agent == agent);
            if (alreadyRegistered)
            {
                continue;
            }

            AgentsList.Add(new PlayerInfo
            {
                Agent = agent
            });
        }
    }
}

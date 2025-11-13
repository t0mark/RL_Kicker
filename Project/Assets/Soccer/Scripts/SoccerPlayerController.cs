using UnityEngine;

/// <summary>
/// 축구 선수의 공통 행동(이동, 드리블, 킥, 태클)을 관리하는 컴포넌트
/// AI 에이전트와 수동 조작 모두에서 사용됨
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SoccerPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float m_LateralSpeed = 0.3f;
    [SerializeField] float m_ForwardSpeed = 1.0f;

    [Header("Dribble Settings")]
    [SerializeField] float dribbleRange = 2f;
    [SerializeField] float dribbleDistance = 2.5f;
    [SerializeField] float dribbleFollowSpeed = 15f;
    [SerializeField] float dribbleHeight = 0.5f;

    [Header("Kick Settings")]
    [SerializeField] float minKickPower = 16f;
    [SerializeField] float maxKickPower = 50f;
    [SerializeField] float kickChargeTime = 0.5f;
    [SerializeField] float kickCooldown = 0.5f;

    [Header("Tackle Settings")]
    [SerializeField] float tackleForce = 8f;

    // Components
    Rigidbody m_Rb;
    Transform m_Ball;
    Rigidbody m_BallRb;

    // Dribble state
    bool m_IsDribbling = false;
    float m_LastKickTime = -999f;
    float m_KickChargeStartTime = -999f;
    bool m_IsChargingKick = false;

    // Team info
    Team m_Team;

    // Properties
    public bool IsDribbling => m_IsDribbling;
    public bool IsChargingKick => m_IsChargingKick;
    public float LateralSpeed => m_LateralSpeed;
    public float ForwardSpeed => m_ForwardSpeed;
    public Rigidbody Rb => m_Rb;

    void Awake()
    {
        m_Rb = GetComponent<Rigidbody>();
        m_Rb.maxAngularVelocity = 500;

        // Find ball
        var ballObj = GameObject.FindGameObjectWithTag("ball");
        if (ballObj != null)
        {
            m_Ball = ballObj.transform;
            m_BallRb = ballObj.GetComponent<Rigidbody>();
        }

        // Get team info
        var agent = GetComponent<AgentSoccer>();
        if (agent != null)
        {
            m_Team = agent.team;
        }
    }

    void FixedUpdate()
    {
        if (m_IsDribbling && m_Ball != null)
        {
            UpdateDribblePosition();
        }

        // Auto-execute kick when max charge time reached
        if (m_IsChargingKick && Time.time - m_KickChargeStartTime >= kickChargeTime)
        {
            ExecuteChargedKick();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Handle tackle when dribbling
        if (m_IsDribbling)
        {
            var otherAgent = collision.gameObject.GetComponent<AgentSoccer>();
            if (otherAgent != null && otherAgent.team != m_Team)
            {
                Vector3 tackleDirection = (m_Ball.position - collision.transform.position).normalized;
                tackleDirection.y = 0.3f;
                ReleaseBall(tackleDirection * tackleForce);
            }
        }
    }

    /// <summary>
    /// Set movement speeds based on position
    /// </summary>
    public void SetSpeeds(float lateral, float forward)
    {
        m_LateralSpeed = lateral;
        m_ForwardSpeed = forward;
    }

    /// <summary>
    /// Set team for tackle detection
    /// </summary>
    public void SetTeam(Team team)
    {
        m_Team = team;
    }

    /// <summary>
    /// Apply movement to the player
    /// </summary>
    public void Move(Vector3 direction, float runSpeed)
    {
        m_Rb.AddForce(direction * runSpeed, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Rotate the player
    /// </summary>
    public void Rotate(Vector3 rotateDir, float rotateSpeed = 100f)
    {
        transform.Rotate(rotateDir, Time.deltaTime * rotateSpeed);
    }

    /// <summary>
    /// Update dribble system - call this every frame
    /// </summary>
    public void UpdateDribble()
    {
        if (m_Ball == null) return;

        // Don't change dribble state while charging
        if (m_IsChargingKick) return;

        // Check if we should start dribbling
        if (!m_IsDribbling)
        {
            // Don't start dribbling right after kicking
            if (Time.time - m_LastKickTime < kickCooldown)
            {
                return;
            }

            float distanceToBall = Vector3.Distance(transform.position, m_Ball.position);
            if (distanceToBall <= dribbleRange)
            {
                // Check if another player is already dribbling
                if (!IsAnyoneElseDribbling())
                {
                    StartDribble();
                }
            }
        }
    }

    /// <summary>
    /// Start charging a kick (only works while dribbling)
    /// </summary>
    public void StartKickCharge()
    {
        if (!m_IsDribbling) return;
        if (Time.time - m_LastKickTime < kickCooldown) return;
        if (m_IsChargingKick) return;

        m_IsChargingKick = true;
        m_KickChargeStartTime = Time.time;
    }

    /// <summary>
    /// Execute the charged kick
    /// </summary>
    public void ExecuteChargedKick()
    {
        if (!m_IsChargingKick) return;

        if (!m_IsDribbling)
        {
            CancelKickCharge();
            return;
        }

        float chargeTime = Mathf.Clamp(Time.time - m_KickChargeStartTime, 0f, kickChargeTime);
        float chargeRatio = chargeTime / kickChargeTime;
        float power = Mathf.Lerp(minKickPower, maxKickPower, chargeRatio);

        m_LastKickTime = Time.time;

        // Kick in the direction the character is facing
        // Add player's velocity to the kick for more distance when moving
        Vector3 kickDirection = transform.forward;
        Vector3 playerVelocity = m_Rb != null ? m_Rb.linearVelocity : Vector3.zero;

        ReleaseBall(kickDirection * power, playerVelocity);
    }

    /// <summary>
    /// Cancel kick charging
    /// </summary>
    public void CancelKickCharge()
    {
        m_IsChargingKick = false;
        m_KickChargeStartTime = -999f;
    }

    /// <summary>
    /// Get current charge progress (0-1)
    /// </summary>
    public float GetChargeProgress()
    {
        if (!m_IsChargingKick) return 0f;
        float chargeTime = Time.time - m_KickChargeStartTime;
        return Mathf.Clamp01(chargeTime / kickChargeTime);
    }

    /// <summary>
    /// Force release the ball (for tackles or manual control)
    /// </summary>
    public void ForceReleaseBall(Vector3 force)
    {
        ReleaseBall(force);
    }

    void StartDribble()
    {
        m_IsDribbling = true;

        if (m_BallRb != null)
        {
            m_BallRb.useGravity = false;
            m_BallRb.linearVelocity = Vector3.zero;
            m_BallRb.angularVelocity = Vector3.zero;

            // Disable collision between ball and player during dribble
            Collider ballCollider = m_Ball.GetComponent<Collider>();
            Collider playerCollider = m_Rb.GetComponent<Collider>();
            if (ballCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, playerCollider, true);
            }
        }
    }

    void UpdateDribblePosition()
    {
        // Always use the direction the character is facing for responsive dribbling
        // This ensures the ball follows direction changes immediately
        Vector3 movementDirection = transform.forward;

        // Calculate player speed for dynamic follow speed
        float playerSpeed = m_Rb.linearVelocity.magnitude;
        float dynamicFollowSpeed = dribbleFollowSpeed + (playerSpeed * 0.5f);

        // Calculate target position for the ball
        Vector3 targetPosition = transform.position + movementDirection * dribbleDistance;
        targetPosition.y = dribbleHeight;

        // Smoothly move the ball to target position
        Vector3 newPosition = Vector3.Lerp(
            m_Ball.position,
            targetPosition,
            Time.fixedDeltaTime * dynamicFollowSpeed
        );

        m_BallRb.MovePosition(newPosition);
        m_BallRb.linearVelocity = Vector3.zero;
        m_BallRb.angularVelocity = Vector3.zero;
    }

    void ReleaseBall(Vector3 force, Vector3 velocityOffset = default)
    {
        CancelKickCharge();
        m_IsDribbling = false;

        if (m_BallRb != null)
        {
            m_BallRb.useGravity = true;

            // Directly set the ball's velocity = kick force + player velocity
            // This ensures the player's movement is properly transferred to the ball
            m_BallRb.linearVelocity = force + velocityOffset;

            // Re-enable collision between ball and player after release
            Collider ballCollider = m_Ball.GetComponent<Collider>();
            Collider playerCollider = m_Rb.GetComponent<Collider>();
            if (ballCollider != null && playerCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, playerCollider, false);
            }
        }
    }

    bool IsAnyoneElseDribbling()
    {
        // Check all ManualControllers
        var allControllers = FindObjectsByType<ManualController>(FindObjectsSortMode.None);
        foreach (var controller in allControllers)
        {
            if (controller.gameObject != gameObject)
            {
                var otherPlayerController = controller.GetComponent<SoccerPlayerController>();
                if (otherPlayerController != null && otherPlayerController.m_IsDribbling)
                {
                    return true;
                }
            }
        }

        // Check all AgentSoccers
        var allAgents = FindObjectsByType<AgentSoccer>(FindObjectsSortMode.None);
        foreach (var agent in allAgents)
        {
            if (agent.gameObject != gameObject)
            {
                var otherPlayerController = agent.GetComponent<SoccerPlayerController>();
                if (otherPlayerController != null && otherPlayerController.m_IsDribbling)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

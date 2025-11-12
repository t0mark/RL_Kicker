using UnityEngine;

/// <summary>
/// 드리블 시스템을 관리하는 컴포넌트
/// 플레이어가 공 근처에 있으면 자동으로 드리블 상태가 되며,
/// 상대에게 태클당하거나 킥을 하면 공이 떨어집니다.
/// </summary>
public class DribbleController : MonoBehaviour
{
    [Header("Dribble Settings")]
    [Tooltip("드리블을 시작할 수 있는 최대 거리")]
    public float dribbleRange = 2f;

    [Tooltip("드리블 중 공의 거리 (이동 방향 기준)")]
    public float dribbleDistance = 1f;

    [Tooltip("공이 플레이어를 따라가는 속도")]
    public float dribbleFollowSpeed = 15f;

    [Tooltip("이동 속도가 이 값보다 작으면 forward 방향 사용")]
    public float minMovementSpeed = 0.1f;

    [Tooltip("드리블 중 공의 높이")]
    public float dribbleHeight = 0.5f;

    [Header("Kick Settings")]
    [Tooltip("킥 파워")]
    public float kickPower = 15f;

    [Tooltip("킥 쿨다운 시간")]
    public float kickCooldown = 0.5f;

    [Header("Tackle Settings")]
    [Tooltip("상대에게 태클당했을 때 공이 튕겨나가는 힘")]
    public float tackleForce = 8f;

    [Header("References")]
    public Transform ball;
    public Rigidbody ballRb;

    // 드리블 상태
    private bool _isDribbling = false;
    private float _lastKickTime = -999f;
    private Team _playerTeam;

    // 컴포넌트 참조
    private AgentSoccer _agentSoccer;
    private ManualController _manualController;
    private Rigidbody _rb;

    public bool IsDribbling => _isDribbling;

    void Awake()
    {
        _agentSoccer = GetComponent<AgentSoccer>();
        _manualController = GetComponent<ManualController>();
        _rb = GetComponent<Rigidbody>();

        // 팀 설정
        if (_agentSoccer != null)
        {
            _playerTeam = _agentSoccer.team;
        }
    }

    void Start()
    {
        // 공 자동 찾기
        if (ball == null)
        {
            var ballObj = GameObject.FindGameObjectWithTag("ball");
            if (ballObj != null)
            {
                ball = ballObj.transform;
                ballRb = ballObj.GetComponent<Rigidbody>();
            }
        }
    }

    void Update()
    {
        if (ball == null || ballRb == null) return;

        // 드리블 시작 체크
        if (!_isDribbling)
        {
            CheckStartDribble();
        }
        else
        {
            // 수동 컨트롤러에서 킥 입력 체크 (스페이스바)
            if (_manualController != null && Input.GetKeyDown(KeyCode.Space))
            {
                Kick();
            }
        }
    }

    void FixedUpdate()
    {
        if (_isDribbling && ball != null)
        {
            UpdateDribblePosition();
        }
    }

    /// <summary>
    /// 공이 드리블 범위 안에 있고, 다른 플레이어가 드리블 중이 아니면 드리블 시작
    /// </summary>
    void CheckStartDribble()
    {
        float distanceToBall = Vector3.Distance(transform.position, ball.position);

        if (distanceToBall <= dribbleRange)
        {
            // 다른 플레이어가 드리블 중인지 체크
            var allDribblers = FindObjectsByType<DribbleController>(FindObjectsSortMode.None);
            bool otherPlayerDribbling = false;

            foreach (var dribbler in allDribblers)
            {
                if (dribbler != this && dribbler.IsDribbling)
                {
                    otherPlayerDribbling = true;
                    break;
                }
            }

            if (!otherPlayerDribbling)
            {
                StartDribble();
            }
        }
    }

    /// <summary>
    /// 드리블 시작
    /// </summary>
    void StartDribble()
    {
        _isDribbling = true;

        // 공의 물리 설정 변경 (드리블 중에는 중력 감소)
        if (ballRb != null)
        {
            ballRb.useGravity = false;
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"{gameObject.name} started dribbling");
    }

    /// <summary>
    /// 드리블 중 공의 위치 업데이트
    /// </summary>
    void UpdateDribblePosition()
    {
        // 이동 방향 계산 (실제 velocity 사용)
        Vector3 movementDirection = Vector3.zero;

        if (_rb != null)
        {
            // XZ 평면에서의 이동 속도
            Vector3 planarVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            if (planarVelocity.magnitude > minMovementSpeed)
            {
                // 이동 중이면 이동 방향 사용
                movementDirection = planarVelocity.normalized;
            }
            else
            {
                // 정지 중이거나 느리게 움직이면 바라보는 방향 사용
                movementDirection = transform.forward;
            }
        }
        else
        {
            // Rigidbody가 없으면 바라보는 방향 사용
            movementDirection = transform.forward;
        }

        // 이동 방향으로 공의 목표 위치 계산
        Vector3 targetPosition = transform.position + movementDirection * dribbleDistance;
        targetPosition.y = dribbleHeight;

        Vector3 newPosition = Vector3.Lerp(
            ball.position,
            targetPosition,
            Time.fixedDeltaTime * dribbleFollowSpeed
        );

        ballRb.MovePosition(newPosition);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// 킥 실행 (공을 이동 방향으로 차기)
    /// </summary>
    public void Kick()
    {
        if (!_isDribbling) return;
        if (Time.time - _lastKickTime < kickCooldown) return;

        _lastKickTime = Time.time;

        // 이동 방향으로 킥
        Vector3 kickDirection = GetMovementDirection();
        ReleaseBall(kickDirection * kickPower);

        Debug.Log($"{gameObject.name} kicked the ball");
    }

    /// <summary>
    /// AI 에이전트를 위한 킥 메서드 (킥 파워 조절 가능)
    /// </summary>
    public void KickWithPower(float power)
    {
        if (!_isDribbling) return;
        if (Time.time - _lastKickTime < kickCooldown) return;

        _lastKickTime = Time.time;

        // 이동 방향으로 킥
        Vector3 kickDirection = GetMovementDirection();
        ReleaseBall(kickDirection * power);
    }

    /// <summary>
    /// 현재 이동 방향 계산 (velocity 또는 forward)
    /// </summary>
    Vector3 GetMovementDirection()
    {
        if (_rb != null)
        {
            Vector3 planarVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            if (planarVelocity.magnitude > minMovementSpeed)
            {
                return planarVelocity.normalized;
            }
        }

        return transform.forward;
    }

    /// <summary>
    /// 공을 놓고 힘을 가함
    /// </summary>
    void ReleaseBall(Vector3 force)
    {
        _isDribbling = false;

        if (ballRb != null)
        {
            ballRb.useGravity = true;
            ballRb.AddForce(force, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// 상대 플레이어와 충돌 시 호출 (태클)
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (!_isDribbling) return;

        // 상대 팀 플레이어와 충돌했는지 체크
        var otherAgent = collision.gameObject.GetComponent<AgentSoccer>();
        if (otherAgent != null && otherAgent.team != _playerTeam)
        {
            // 태클당함 - 공을 랜덤한 방향으로 튕겨냄
            Vector3 tackleDirection = (ball.position - collision.transform.position).normalized;
            tackleDirection.y = 0.3f; // 약간 위로

            ReleaseBall(tackleDirection * tackleForce);

            Debug.Log($"{gameObject.name} was tackled by {collision.gameObject.name}");
        }
    }

    /// <summary>
    /// 드리블을 강제로 중단 (외부에서 호출 가능)
    /// </summary>
    public void StopDribble()
    {
        if (!_isDribbling) return;

        _isDribbling = false;

        if (ballRb != null)
        {
            ballRb.useGravity = true;
        }
    }

    // 디버그용 기즈모
    void OnDrawGizmosSelected()
    {
        // 드리블 시작 범위 (노란색/녹색)
        Gizmos.color = _isDribbling ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dribbleRange);

        if (_isDribbling && ball != null)
        {
            // 공의 목표 위치 (청록색)
            Gizmos.color = Color.cyan;
            Vector3 movementDir = GetMovementDirection();
            Vector3 targetPos = transform.position + movementDir * dribbleDistance;
            targetPos.y = dribbleHeight;
            Gizmos.DrawLine(ball.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.2f);

            // 이동 방향 화살표 (마젠타)
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, movementDir * dribbleDistance);
        }
    }
}

using UnityEngine;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(Rigidbody))]
public class ManualController : MonoBehaviour
{
    [Header("Refs")]
    public Transform ball;

    [Header("Move")]
    public float moveSpeed = 10f;
    public float dashMultiplier = 2f;
    public float maxVel = 10f;

    [Header("Auto Face Ball")]
    public bool autoFaceBall = true;
    public float turnSpeedDegPerSec = 540f;

    [Header("Dribble")]
    [Tooltip("드리블 중일 때 자동으로 볼을 바라보기를 비활성화할지 여부")]
    public bool disableAutoFaceWhenDribbling = true;

    Rigidbody _rb;
    Vector3 _desiredMove;
    DribbleController _dribbleController;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _dribbleController = GetComponent<DribbleController>();
    }

    void OnEnable()
    {
        var bp = GetComponent<BehaviorParameters>();
        if (bp) bp.BehaviorType = BehaviorType.InferenceOnly;
    }

    void Update()
    {
        float v = 0f, h = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;

        var move = new Vector3(h, 0f, v);
        if (move.sqrMagnitude > 1e-4f)
        {
            move = move.normalized * moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) move *= dashMultiplier;
        }
        else move = Vector3.zero;

        _desiredMove = move;
    }

    void FixedUpdate()
    {
        if (_desiredMove.sqrMagnitude > 0f)
        {
            _rb.AddForce(_desiredMove, ForceMode.VelocityChange);
        }

        var planar = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (planar.magnitude > maxVel)
        {
            planar = planar.normalized * maxVel;
            _rb.linearVelocity = new Vector3(planar.x, _rb.linearVelocity.y, planar.z);
        }

        // 볼 자동 바라보기 (드리블 중이 아닐 때만)
        bool shouldAutoFace = autoFaceBall && ball;
        if (disableAutoFaceWhenDribbling && _dribbleController != null && _dribbleController.IsDribbling)
        {
            shouldAutoFace = false;
        }

        if (shouldAutoFace)
        {
            var to = ball.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 1e-4f)
            {
                var target = Quaternion.LookRotation(to.normalized, Vector3.up);
                _rb.MoveRotation(Quaternion.RotateTowards(
                    _rb.rotation, target, turnSpeedDegPerSec * Time.fixedDeltaTime));
            }
        }
    }

    /// <summary>
    /// 수동 컨트롤러로 공을 차기 위한 헬퍼 메서드
    /// Space키로 호출됨 (DribbleController의 Update에서 처리)
    /// </summary>
    public void TriggerKick()
    {
        if (_dribbleController != null)
        {
            _dribbleController.Kick();
        }
    }
}

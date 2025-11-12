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

    Rigidbody _rb;
    Vector3 _desiredMove;

    void Awake() => _rb = GetComponent<Rigidbody>();

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

        // 볼 자동 바라보기
        if (autoFaceBall && ball)
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
}

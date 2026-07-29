using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("移动属性")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField, Min(1f)] private float _maxTurnSpeed = 180f;
    [SerializeField, Min(0f)] private float _moveDirectionDeadZone = 0.1f;

    [Header("群体状态")]
    [SerializeField, Min(0.1f)] private float _surroundRadius = 5f;
    [SerializeField, Min(0.05f)] private float _surroundPointReachedDistance = 0.5f;

    [Header("分离力（Boids）")]
    [SerializeField, Min(0.1f)] private float _separationRadius = 1.5f;
    [SerializeField, Range(0f, 1f)] private float _separationWeight = 0.3f;

    [Header("RVO 局部避障")]
    [SerializeField, Range(0f, 0.5f)] private float _maxRvoBlendWeight = 0.5f;
    [SerializeField, Min(1)] private int _maxComfortableNeighborCount = 5;
    [SerializeField, Min(0.1f)] private float _rvoTimeHorizon = 1.5f;
    [SerializeField, Min(0f)] private float _rvoAgentPadding = 0.05f;
    [SerializeField] private float _obstacleCheckDistance = 2f;
    [SerializeField] private LayerMask _obstacleLayerMask;

    [Header("逻辑代理尺寸")]
    [SerializeField, Min(0.05f)] private float _agentRadius = 0.3f;
    [SerializeField, Min(0.1f)] private float _agentHeight = 1.7f;
    [SerializeField] private Vector3 _agentCenter = new Vector3(0f, 0.85f, 0f);

    private Enemy _enemy;
    private Vector3 _lastMoveDirection;

    public Vector3 Velocity { get; private set; }
    public float MoveSpeed => _enemy != null ? _enemy.Buff.GetMoveSpeed(_moveSpeed) : _moveSpeed;
    public float SurroundRadius => Mathf.Max(_surroundRadius, _enemy != null ? _enemy.AttackRange : 0f);
    public float SurroundPointReachedDistance => _surroundPointReachedDistance;
    public float SeparationRadius => _separationRadius;
    public float SeparationWeight => _separationWeight;
    public float MaxRvoBlendWeight => _maxRvoBlendWeight;
    public int MaxComfortableNeighborCount => _maxComfortableNeighborCount;
    public float RvoTimeHorizon => _rvoTimeHorizon;
    public float RvoAgentPadding => _rvoAgentPadding;
    public float ObstacleCheckDistance => _obstacleCheckDistance;
    public float ColliderRadius => _agentRadius;
    public Vector3 ColliderCenter => _agentCenter;
    public float ColliderHeight => _agentHeight;
    public LayerMask ObstacleLayerMask => _obstacleLayerMask;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _lastMoveDirection = transform.forward;

        // 兼容尚未清理的旧预制体：移动完全由 Transform 驱动，
        // CharacterController 永久禁用，避免进入物理场景更新。
        CharacterController legacyController = GetComponent<CharacterController>();
        if (legacyController != null)
            legacyController.enabled = false;
    }

    public void Move(Vector3 direction, float speedMultiplier = 1f)
    {
        direction.y = 0f;
        float inputMagnitude = direction.magnitude;
        if (inputMagnitude < 0.01f)
        {
            Stop();
            return;
        }

        float magnitude = Mathf.Clamp01(inputMagnitude) * Mathf.Max(0f, speedMultiplier);
        Vector3 velocity = direction / inputMagnitude * MoveSpeed * magnitude;
        MoveVelocity(velocity);
    }

    public void MoveVelocity(Vector3 velocity)
    {
        velocity.y = 0f;
        float speed = Mathf.Min(velocity.magnitude, MoveSpeed);
        if (speed < _moveDirectionDeadZone)
        {
            Stop();
            return;
        }

        Vector3 desiredDirection = velocity / velocity.magnitude;
        Vector3 currentDirection = _lastMoveDirection;
        currentDirection.y = 0f;
        if (currentDirection.sqrMagnitude < 0.0001f)
            currentDirection = transform.forward;

        float maxRadiansDelta = _maxTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 limitedDirection = Vector3.RotateTowards(
            currentDirection.normalized,
            desiredDirection,
            maxRadiansDelta,
            0f);
        limitedDirection.y = 0f;
        limitedDirection.Normalize();
        velocity = limitedDirection * speed;

        Vector3 position = transform.position + velocity * Time.deltaTime;
        position.y = 0f;
        transform.position = position;

        Velocity = velocity;
        _lastMoveDirection = limitedDirection;
        FaceDirection(velocity);
    }

    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
        float maxDegreesDelta = _maxTurnSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesDelta);
    }

    public void FaceTarget(Transform target)
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        FaceDirection(dir);
    }

    public void FaceTargetImmediate(Transform target)
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
        _lastMoveDirection = transform.forward;
    }

    public void Stop()
    {
        Velocity = Vector3.zero;
    }

    public void ResetNavigationVelocity()
    {
        Velocity = Vector3.zero;
        _lastMoveDirection = transform.forward;
    }

    public void SnapToGround()
    {
        ClampYToZero();
    }

    public void BeginKeepGrounded()
    {
        ClampYToZero();
    }

    public void EndKeepGrounded()
    {
    }

    private void ClampYToZero()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
    }

    public void DisableCollision()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    public void EnableCollision()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            // 旧资源上的 CharacterController 只用于兼容迁移，绝不重新启用。
            if (colliders[i] is CharacterController) continue;
            colliders[i].enabled = true;
        }
    }
}

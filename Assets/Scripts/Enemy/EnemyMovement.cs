using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("移动属性")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("分离力（Boids）")]
    [SerializeField] private float _separationRadius = 2.5f;
    [SerializeField] private float _separationForce = 4f;
    [SerializeField] private float _hardPushDistance = 1.0f;
    [SerializeField] private float _hardPushForce = 6f;

    [Header("简单避障")]
    [SerializeField] private float _obstacleCheckDistance = 2f;
    [SerializeField] private float _obstacleAvoidForce = 5f;
    [SerializeField] private LayerMask _obstacleLayerMask;

    [Header("贴地")]
    [SerializeField] private LayerMask _groundLayerMask = 1 << 6;
    [SerializeField] private float _groundRaycastUpDistance = 3f;
    [SerializeField] private float _groundRaycastDownDistance = 10f;
    [SerializeField] private float _footGroundPadding = 0.02f;

    private CharacterController _characterController;
    private Enemy _enemy;
    private Transform[] _footAnchors;
    private bool _missingCharacterControllerLogged;
    private bool _keepGrounded;

    public Vector3 Velocity { get; private set; }
    public float MoveSpeed => _enemy != null ? _enemy.Buff.GetMoveSpeed(_moveSpeed) : _moveSpeed;
    public float SeparationRadius => _separationRadius;
    public float SeparationForce => _separationForce;
    public float HardPushDistance => _hardPushDistance;
    public float HardPushForce => _hardPushForce;
    public float ObstacleCheckDistance => _obstacleCheckDistance;
    public float ObstacleAvoidForce => _obstacleAvoidForce;
    public float ColliderRadius => _characterController != null ? _characterController.radius : 0.3f;
    public Vector3 ColliderCenter => _characterController != null ? _characterController.center : Vector3.up * 0.4f;
    public float ColliderHeight => _characterController != null ? _characterController.height : 1.7f;
    public LayerMask ObstacleLayerMask => _obstacleLayerMask;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _enemy = GetComponent<Enemy>();
        _footAnchors = FindFootAnchors();
    }

    private void LateUpdate()
    {
        if (_keepGrounded)
            KeepFeetAboveGround();
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

        if (_characterController != null && _characterController.enabled)
        {
            Vector3 motion = velocity;
            motion.y = _characterController.isGrounded ? -1f : -4f;
            _characterController.Move(motion * Time.deltaTime);
        }
        else
        {
            if (!_missingCharacterControllerLogged)
            {
                Debug.LogError($"[EnemyMovement] {name} 缺少 CharacterController，已停止 Transform 直移以避免穿墙", this);
                _missingCharacterControllerLogged = true;
            }

            Stop();
            return;
        }

        Velocity = velocity;
        FaceDirection(velocity);
    }

    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
        float maxDegreesDelta = _rotationSpeed * 60f * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesDelta);
    }

    public void FaceTarget(Transform target)
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        FaceDirection(dir);
    }

    public void Stop()
    {
        Velocity = Vector3.zero;
    }

    public void SnapToGround()
    {
        if (!TryGetGroundY(transform.position, out float groundY))
            return;

        Vector3 position = transform.position;
        position.y = groundY;
        transform.position = position;
    }

    public void BeginKeepGrounded()
    {
        _keepGrounded = true;
        SnapToGround();
        KeepFeetAboveGround();
    }

    public void EndKeepGrounded()
    {
        _keepGrounded = false;
    }

    private void KeepFeetAboveGround()
    {
        if (_footAnchors == null || _footAnchors.Length == 0)
            _footAnchors = FindFootAnchors();

        if (_footAnchors == null || _footAnchors.Length == 0)
        {
            SnapToGround();
            return;
        }

        float maxLift = 0f;
        for (int i = 0; i < _footAnchors.Length; i++)
        {
            Transform foot = _footAnchors[i];
            if (foot == null || !TryGetGroundY(foot.position, out float groundY))
                continue;

            float targetY = groundY + Mathf.Max(0f, _footGroundPadding);
            if (foot.position.y < targetY)
                maxLift = Mathf.Max(maxLift, targetY - foot.position.y);
        }

        if (maxLift <= 0f)
            return;

        Vector3 position = transform.position;
        position.y += maxLift;
        transform.position = position;
    }

    private Transform[] FindFootAnchors()
    {
        Transform leftFoot = null;
        Transform rightFoot = null;
        Transform leftToe = null;
        Transform rightToe = null;
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            string childName = child.name;
            if (childName.Contains("LeftToe"))
                leftToe = child;
            else if (childName.Contains("RightToe"))
                rightToe = child;
            else if (childName.Contains("LeftFoot"))
                leftFoot = child;
            else if (childName.Contains("RightFoot"))
                rightFoot = child;
        }

        if (leftToe != null || rightToe != null)
            return new[] { leftToe != null ? leftToe : leftFoot, rightToe != null ? rightToe : rightFoot };

        if (leftFoot != null || rightFoot != null)
            return new[] { leftFoot, rightFoot };

        return null;
    }

    private bool TryGetGroundY(Vector3 position, out float groundY)
    {
        groundY = position.y;
        if (_groundLayerMask == 0)
            return false;

        Vector3 origin = position + Vector3.up * Mathf.Max(0f, _groundRaycastUpDistance);
        float distance = Mathf.Max(0.1f, _groundRaycastUpDistance + _groundRaycastDownDistance);
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, _groundLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        groundY = hit.point.y;
        return true;
    }

    public void DisableCollision()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    public void EnableCollision()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }
    }
}

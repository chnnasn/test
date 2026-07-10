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

    private CharacterController _characterController;
    private bool _missingCharacterControllerLogged;

    public Vector3 Velocity { get; private set; }
    public float MoveSpeed => _moveSpeed;
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
        Vector3 velocity = direction / inputMagnitude * _moveSpeed * magnitude;

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

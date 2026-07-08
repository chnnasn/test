using UnityEngine;

/// <summary>
/// 弹线特效：沿射击方向飞行，到达最大距离后自动销毁
/// </summary>
public class BulletTrail : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _traveledDistance;
    private float _maxDistance;
    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    public void Init(Vector3 origin, Vector3 direction, float speed, float maxDistance)
    {
        transform.position = origin;
        _direction = direction.normalized;
        _speed = speed;
        _maxDistance = maxDistance;
        _traveledDistance = 0f;

        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }

        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void Update()
    {
        float step = _speed * Time.deltaTime;
        transform.position += _direction * step;
        _traveledDistance += step;

        if (_traveledDistance >= _maxDistance)
            Destroy(gameObject);
    }
}

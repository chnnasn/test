using System.Collections;
using UnityEngine;

/// <summary>
/// 高能射线：持续射击激光束，对选中目标持续造成伤害
/// - 光束持续 _duration 秒，每隔 _damageInterval 秒造成一次伤害
/// - 目标由 AutoCombatController 索敌保证，开火必中
/// </summary>
public class HighEnergyRayProjectile : SkillProjectileBase
{
    [Header("击中特效")]
    [SerializeField] private Transform _hitEffect;

    [Header("保底")]
    [SerializeField] private float _maxLifetime = 15f;

    private Transform _firePoint;
    private float _duration;
    private float _damageInterval;

    private LineRenderer _lineRenderer;
    private EnemyController _targetEnemy;
    private Coroutine _rayCoroutine;

    // ==================== 初始化 ====================

    public void SetFirePoint(Transform firePoint) => _firePoint = firePoint;

    public override void Init(SkillEffectData cfg, Vector3 firePos, Vector3 direction, Transform target)
    {
        base.Init(cfg, firePos, direction, target);

        _duration       = cfg.GetFloat("持续射击时间(秒)");
        _damageInterval = cfg.GetFloat("攻击间隔(秒)");

        if (target != null)
            _targetEnemy = target.GetComponent<EnemyController>();

        _lineRenderer = GetComponentInChildren<LineRenderer>();
        if (_lineRenderer == null)
        {
            Debug.LogError("[高能射线] 缺少 LineRenderer");
            Destroy(gameObject);
            return;
        }
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;

        if (_hitEffect != null)
            _hitEffect.gameObject.SetActive(false);

        _rayCoroutine = StartCoroutine(RayLoop());
        StartCoroutine(DestroyAfterSeconds(_maxLifetime));
    }

    // ==================== 每帧更新光束 ====================

    private void Update()
    {
        if (_lineRenderer == null) return;

        Vector3 startPos = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 endPos;

        if (_targetEnemy != null && !_targetEnemy.IsDead)
        {
            endPos = _targetEnemy.transform.position + Vector3.up * 1f;
        }
        else
        {
            endPos = startPos + transform.forward * 20f;
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);

        if (_hitEffect != null)
        {
            if (_targetEnemy != null && !_targetEnemy.IsDead)
            {
                _hitEffect.gameObject.SetActive(true);
                _hitEffect.position = endPos;
                _hitEffect.rotation = Quaternion.LookRotation((startPos - endPos).normalized);
            }
            else
            {
                _hitEffect.gameObject.SetActive(false);
            }
        }
    }

    // ==================== 主协程 ====================

    private IEnumerator RayLoop()
    {
        _lineRenderer.enabled = true;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            if (_targetEnemy == null || _targetEnemy.IsDead)
                break;

            _targetEnemy.TakeDamage(_damage, _targetEnemy.transform.position + Vector3.up * 1f);
            Debug.Log($"[高能射线] 命中 {_targetEnemy.name} 伤害:{_damage} 已持续:{elapsed:F1}s");

            yield return new WaitForSeconds(_damageInterval);
            elapsed += _damageInterval;
        }

        _lineRenderer.enabled = false;
        if (_hitEffect != null) _hitEffect.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    // ==================== 保底 ====================

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_rayCoroutine != null) StopCoroutine(_rayCoroutine);
        if (_hitEffect != null) _hitEffect.gameObject.SetActive(false);
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 聚焦激光枪：激光束闪烁攻击选中目标
/// - Coroutine 控制闪烁节奏（亮→扣血→灭→换目标→亮…）
/// - Update 每帧更新 LineRenderer 起终点（跟随目标移动）
/// - 目标由 AutoCombatController 索敌保证，开火必中
/// </summary>
public class LaserGunProjectile : SkillProjectileBase
{
    [Header("击中特效")]
    [SerializeField] private Transform _hitEffect;

    [Header("保底")]
    [SerializeField] private float _maxLifetime = 15f;

    private Transform _firePoint;

    private LineRenderer _lineRenderer;
    private EnemyController _targetEnemy;
    private int _flashCount;
    private float _flashOnDuration;
    private float _flashOffDuration;
    private bool _isBeamVisible;
    private Coroutine _flashCoroutine;

    // ==================== 初始化 ====================

    public void SetFirePoint(Transform firePoint)
    {
        _firePoint = firePoint;
    }

    public override void Init(SkillEffectData cfg, Vector3 firePos, Vector3 direction, Transform target)
    {
        base.Init(cfg, firePos, direction, target);

        _flashCount       = cfg.GetInt("闪烁次数");
        _flashOnDuration  = cfg.GetFloat("亮持续(秒)");
        _flashOffDuration = cfg.GetFloat("灭持续(秒)");

        if (target != null)
            _targetEnemy = target.GetComponent<EnemyController>();

        _lineRenderer = GetComponentInChildren<LineRenderer>();
        if (_lineRenderer == null)
        {
            Debug.LogError("[激光枪] 预制体及其子结点上缺少 LineRenderer 组件");
            Destroy(gameObject);
            return;
        }
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;

        if (_hitEffect != null)
            _hitEffect.gameObject.SetActive(false);

        _flashCoroutine = StartCoroutine(FlashLoop());
        StartCoroutine(DestroyAfterSeconds(_maxLifetime));
    }

    // ==================== 每帧更新光束 ====================

    private void Update()
    {
        if (_lineRenderer == null) return;

        if (_isBeamVisible)
        {
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
                _hitEffect.position = endPos;
                _hitEffect.rotation = Quaternion.LookRotation((startPos - endPos).normalized);
            }
        }
    }

    // ==================== 闪烁协程 ====================

    private IEnumerator FlashLoop()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            if (_targetEnemy == null || _targetEnemy.IsDead)
            {
                Debug.Log($"[激光枪] 目标无效，提前结束（已闪烁{i}次）");
                break;
            }

            // ON：亮灯 + 扣血
            ShowBeam();
            _targetEnemy.TakeDamage(_damage, _targetEnemy.transform.position + Vector3.up * 1f);
            Debug.Log($"[激光枪] 第{i + 1}次闪烁 命中 {_targetEnemy.name} 伤害:{_damage}");

            yield return new WaitForSeconds(_flashOnDuration);

            // OFF：灭灯
            HideBeam();

            if (i >= _flashCount - 1)
                break;

            yield return new WaitForSeconds(_flashOffDuration);
        }

        Destroy(gameObject);
    }

    // ==================== 光束显隐 ====================

    private void ShowBeam()
    {
        _isBeamVisible = true;
        if (_lineRenderer != null)
            _lineRenderer.enabled = true;
        if (_hitEffect != null)
            _hitEffect.gameObject.SetActive(true);
    }

    private void HideBeam()
    {
        _isBeamVisible = false;
        if (_lineRenderer != null)
            _lineRenderer.enabled = false;
        if (_hitEffect != null)
            _hitEffect.gameObject.SetActive(false);
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
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        if (_hitEffect != null)
            _hitEffect.gameObject.SetActive(false);
    }
}

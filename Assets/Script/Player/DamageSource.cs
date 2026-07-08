using UnityEngine;

/// <summary>
/// 伤害来源方向指示器管理器：用对象池管理多个 DamageIndicator 实例
/// </summary>
public class DamageSource : MonoBehaviour
{
    [Header("指示器预制体（挂有 DamageIndicator 组件）")]
    [SerializeField] private DamageIndicator _indicatorPrefab;
    [SerializeField] private float _showDuration = 1.5f;
    [SerializeField] private float _fadeOutTime = 0.5f;
    [SerializeField] private int _poolInitCount = 5;

    private ObjectPool _pool;

    private void Awake()
    {
        if (_indicatorPrefab != null)
            _pool = new ObjectPool(_indicatorPrefab.gameObject, _poolInitCount);
    }

    public void Bind(PlayerMove player)
    {
        player.OnDamageReceived += OnDamageReceived;
    }

    private void OnDamageReceived(float angle)
    {
        if (_pool == null) return;

        var obj = _pool.Get();
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.zero;

        var indicator = obj.GetComponent<DamageIndicator>();
        if (indicator != null)
            indicator.Show(angle, _showDuration, _fadeOutTime, Recycle);
    }

    private void Recycle(DamageIndicator indicator)
    {
        _pool.Recycle(indicator.gameObject);
    }
}

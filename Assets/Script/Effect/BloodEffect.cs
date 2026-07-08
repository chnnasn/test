using UnityEngine;

/// <summary>
/// 溅血特效：播放一段时间后自动回收到对象池
/// </summary>
public class BloodEffect : MonoBehaviour
{
    [Header("生命周期")]
    [SerializeField] private float _duration = 1.5f;

    private ObjectPool _pool;
    private float _timer;

    /// <summary>初始化，绑定所属对象池</summary>
    public void Init(ObjectPool pool)
    {
        _pool = pool;
        _timer = _duration;
    }

    private void OnEnable()
    {
        _timer = _duration;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            if (_pool != null)
                _pool.Recycle(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}

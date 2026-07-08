using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss怪物血条（World Space Canvas，跟随怪物头顶，自动近大远小）
/// 预制体根节点需挂 Canvas（RenderMode=WorldSpace），血条为子节点
/// </summary>
public class EnemyHpBar : MonoBehaviour
{
    [Header("血条")]
    [SerializeField] private Slider _slider;

    [Header("面朝相机")]
    [SerializeField] private bool _faceCamera = true;

    private EnemyController _enemy;
    private Transform _rootTransform;
    private Camera _camera;

    private void Awake()
    {
        _rootTransform = transform;
        _camera = Camera.main;
    }

    /// <summary>
    /// 绑定目标怪物
    /// </summary>
    public void Bind(EnemyController enemy)
    {
        gameObject.SetActive(true);
        _enemy = enemy;
        if (_slider != null && enemy.Data != null)
        {
            _slider.maxValue = enemy.Data.MaxHp;
            _slider.value = enemy.CurrentHp;
        }
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (_enemy == null || _enemy.IsDead || !_enemy.gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        // 更新血条值
        if (_slider != null)
            _slider.value = _enemy.CurrentHp;

        // 面朝相机（Billboard）
        if (_faceCamera)
            _rootTransform.forward = _camera.transform.forward;
    }

    private void OnDestroy()
    {
        _enemy = null;
    }
}

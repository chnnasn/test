using UnityEngine;

/// <summary>
/// 疾跑加速区域触发器
/// 挂载在路径上的触发点（需 Collider + 设为 Trigger）
/// tag="runA" → 玩家进入时开始加速，倍率可配
/// tag="runB" → 玩家进入时恢复默认速度
/// 触发后自动禁用碰撞体，防止二次触发
/// </summary>
[RequireComponent(typeof(Collider))]
public class RunZoneTrigger : MonoBehaviour
{
    [Header("加速倍率（仅 runA 生效，2.0 = 两倍速）")]
    [SerializeField] private float _speedMultiplier = 2f;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;

        // 确保有 Rigidbody，否则 CharacterController 无法触发 OnTriggerEnter
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // tag 检查
        if (!CompareTag("runA") && !CompareTag("runB"))
            Debug.LogWarning($"[RunZone] {gameObject.name} 的 tag 应为 runA 或 runB，当前: {tag}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMove player = other.GetComponent<PlayerMove>();
        if (player == null) return;

        if (CompareTag("runA"))
        {
            player.SetSpeedMultiplier(_speedMultiplier);
            Debug.Log($"[RunZone] 进入加速区，倍率: {_speedMultiplier}x");
        }
        else if (CompareTag("runB"))
        {
            player.SetSpeedMultiplier(1f);
            Debug.Log("[RunZone] 离开加速区，速度恢复");
        }

        // 防止二次触发
        _collider.enabled = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 选中时在 Scene 视图中绘制触发范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = CompareTag("runA")
            ? new Color(1f, 0.5f, 0f, 0.3f)   // 橙色半透明 = 加速起点
            : new Color(0f, 0.7f, 1f, 0.3f);   // 蓝色半透明 = 加速终点

        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        else if (col is CapsuleCollider capsule)
            Gizmos.DrawCube(capsule.center, new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f));
    }

    private void Reset()
    {
        // 自动设置默认值
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 3f, 3f);
        }
    }
#endif
}

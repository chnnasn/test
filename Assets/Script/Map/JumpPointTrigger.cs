using UnityEngine;

/// <summary>
/// 跳跃触发点
/// 挂载在路径上的触发点（需 Collider + 设为 Trigger + tag="jumpPoint"）
/// 角色进入时调用 PlayerMove.Jump()，委托 Movement 组件执行跳跃物理
/// 触发后自动禁用碰撞体，防止二次触发
/// </summary>
[RequireComponent(typeof(Collider))]
public class JumpPointTrigger : MonoBehaviour
{
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (!CompareTag("jumpPoint"))
            Debug.LogWarning($"[JumpPoint] {gameObject.name} 的 tag 应为 jumpPoint，当前: {tag}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMove player = other.GetComponent<PlayerMove>();
        if (player == null) return;

        player.Jump();
        Debug.Log($"[JumpPoint] 触发跳跃");

        _collider.enabled = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.3f);
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
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 3f, 3f);
        }
    }
#endif
}

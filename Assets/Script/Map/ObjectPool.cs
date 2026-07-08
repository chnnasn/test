using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池
/// 缓存 GameObject 实例，避免频繁 Instantiate / Destroy 带来的性能开销
/// </summary>
public class ObjectPool
{
    private Queue<GameObject> _pool = new Queue<GameObject>();  // 空闲对象队列
    private GameObject _prefab;                                  // 预制体引用
    private Transform _parent;                                   // 对象池父节点（统一管理Hierarchy）

    /// <summary>
    /// 构造函数，初始化时预创建 initCount 个对象放入池中
    /// </summary>
    public ObjectPool(GameObject prefab, int initCount = 3)
    {
        _prefab = prefab;
        _parent = new GameObject($"Pool_{prefab.name}").transform;
        for (int i = 0; i < initCount; i++) Recycle(CreateNew());
    }

    /// <summary>
    /// 从对象池中取一个可用对象
    /// 池中有空闲对象则复用，否则新建一个
    /// </summary>
    /// <param name="activate">是否立即激活对象，默认true。设为false可在设置位置后再手动激活，避免视觉闪烁</param>
    public GameObject Get(bool activate = true)
    {
        var obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        if (activate) obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 将用完的对象回收到池中（设为非激活状态）
    /// </summary>
    public void Recycle(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// 创建新实例（内部方法，设为非激活状态，挂到池父节点下）
    /// </summary>
    private GameObject CreateNew()
    {
        var obj = Object.Instantiate(_prefab, _parent);
        obj.SetActive(false);
        return obj;
    }
}

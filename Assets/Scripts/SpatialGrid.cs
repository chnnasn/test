using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空间哈希分桶。每帧调用 RebuildAll() 重建网格，然后通过 QueryNeighbors 查询。
/// 无论场景有多少敌人都是 O(1) 邻居查询。
/// </summary>
public static class SpatialGrid
{
    private const float CELL_SIZE = 4f;
    private static readonly Dictionary<int, List<Enemy>> _cells = new Dictionary<int, List<Enemy>>(128);
    private static readonly HashSet<Enemy> _allEnemies = new HashSet<Enemy>(256);
    private static readonly HashSet<int> _activeKeys = new HashSet<int>(64);

    /// <summary>
    /// 注册敌人（OnEnable 调用）
    /// </summary>
    public static void Register(Enemy enemy)
    {
        _allEnemies.Add(enemy);
    }

    /// <summary>
    /// 注销敌人（OnDisable / OnDestroy 调用）
    /// </summary>
    public static void Unregister(Enemy enemy)
    {
        _allEnemies.Remove(enemy);
    }

    /// <summary>
    /// 每帧开始时调用：清空全部格子，重新哈希所有敌人
    /// </summary>
    public static void RebuildAll()
    {
        // 清空所有活跃格子
        foreach (int key in _activeKeys)
        {
            _cells[key].Clear();
        }
        _activeKeys.Clear();

        // 重新哈希每个敌人到对应格子
        foreach (Enemy enemy in _allEnemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            int key = Hash(enemy.transform.position);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = new List<Enemy>(8);
                _cells[key] = list;
                _activeKeys.Add(key);
            }
            list.Add(enemy);
        }
    }

    /// <summary>
    /// 查询指定位置半径内的所有敌人
    /// </summary>
    public static int QueryNeighbors(Vector3 center, float radius, Enemy self, List<Enemy> outList)
    {
        outList.Clear();

        int cx = Mathf.FloorToInt(center.x / CELL_SIZE);
        int cz = Mathf.FloorToInt(center.z / CELL_SIZE);
        int range = Mathf.CeilToInt(radius / CELL_SIZE);

        float sqrR = radius * radius;

        for (int x = cx - range; x <= cx + range; x++)
        {
            for (int z = cz - range; z <= cz + range; z++)
            {
                int key = (x << 16) ^ (z & 0xFFFF);
                if (!_cells.TryGetValue(key, out var cellList)) continue;

                for (int i = 0; i < cellList.Count; i++)
                {
                    Enemy other = cellList[i];
                    if (other == self || other == null || !other.IsAlive) continue;

                    float dx = center.x - other.transform.position.x;
                    float dz = center.z - other.transform.position.z;
                    if (dx * dx + dz * dz < sqrR)
                        outList.Add(other);
                }
            }
        }

        return outList.Count;
    }

    private static int Hash(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / CELL_SIZE);
        int z = Mathf.FloorToInt(pos.z / CELL_SIZE);
        return (x << 16) ^ (z & 0xFFFF);
    }
}

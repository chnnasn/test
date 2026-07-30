using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空间哈希分桶。通过 RebuildAll() 增量同步敌人所在格子，然后通过 QueryNeighbors 查询。
/// 无论场景有多少敌人都是 O(1) 邻居查询。
/// </summary>
public static class SpatialGrid
{
    private const float CELL_SIZE = 4f;
    private static readonly Dictionary<int, List<Enemy>> _cells = new Dictionary<int, List<Enemy>>(128);
    private static readonly List<Enemy> _allEnemies = new List<Enemy>(256);
    private static readonly Dictionary<Enemy, int> _enemyIndices = new Dictionary<Enemy, int>(256);
    private static readonly Dictionary<Enemy, int> _enemyCells = new Dictionary<Enemy, int>(256);

    /// <summary>
    /// 注册敌人（OnEnable 调用）
    /// </summary>
    public static void Register(Enemy enemy)
    {
        if (enemy == null || _enemyIndices.ContainsKey(enemy)) return;

        int index = _allEnemies.Count;
        _allEnemies.Add(enemy);
        _enemyIndices[enemy] = index;

        int key = Hash(enemy.transform.position);
        _enemyCells[enemy] = key;
        AddToCell(key, enemy);
    }

    /// <summary>
    /// 注销敌人（OnDisable / OnDestroy 调用）
    /// </summary>
    public static void Unregister(Enemy enemy)
    {
        if (enemy == null || !_enemyIndices.TryGetValue(enemy, out int index)) return;
        RemoveAt(index);
    }

    /// <summary>
    /// 每帧开始时调用：增量同步敌人所在格子，仅在跨格时移动列表。
    /// </summary>
    public static void RebuildAll()
    {
        for (int i = 0; i < _allEnemies.Count; i++)
        {
            Enemy enemy = _allEnemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                RemoveAt(i);
                i--;
                continue;
            }

            int newKey = Hash(enemy.transform.position);
            if (!_enemyCells.TryGetValue(enemy, out int oldKey))
            {
                _enemyCells[enemy] = newKey;
                AddToCell(newKey, enemy);
                continue;
            }

            if (oldKey == newKey) continue;

            RemoveFromCell(oldKey, enemy);
            AddToCell(newKey, enemy);
            _enemyCells[enemy] = newKey;
        }
    }

    /// <summary>
    /// 查询指定位置半径内的所有敌人
    /// </summary>
    public static int QueryNeighbors(Vector3 center, float radius, Enemy self, List<Enemy> outList)
    {
        return QueryNeighbors(center, radius, self, outList, false, 0);
    }

    /// <summary>
    /// 查询同一逻辑批次内的邻居。用于局部 Boids/RVO，避免不同玩家的敌群互相改变速度。
    /// </summary>
    public static int QueryNeighbors(
        Vector3 center,
        float radius,
        Enemy self,
        List<Enemy> outList,
        int batchId)
    {
        return QueryNeighbors(center, radius, self, outList, true, batchId);
    }

    private static int QueryNeighbors(
        Vector3 center,
        float radius,
        Enemy self,
        List<Enemy> outList,
        bool filterByBatch,
        int batchId)
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
                    if (filterByBatch && other.CrowdBatchId != batchId) continue;

                    float dx = center.x - other.transform.position.x;
                    float dz = center.z - other.transform.position.z;
                    if (dx * dx + dz * dz < sqrR)
                        outList.Add(other);
                }
            }
        }

        return outList.Count;
    }

    public static void Clear()
    {
        _cells.Clear();
        _allEnemies.Clear();
        _enemyIndices.Clear();
        _enemyCells.Clear();
    }

    private static void AddToCell(int key, Enemy enemy)
    {
        if (!_cells.TryGetValue(key, out var list))
        {
            list = new List<Enemy>(8);
            _cells[key] = list;
        }

        list.Add(enemy);
    }

    private static void RemoveFromCell(int key, Enemy enemy)
    {
        if (!_cells.TryGetValue(key, out var list)) return;
        list.Remove(enemy);
    }

    private static void RemoveAt(int index)
    {
        Enemy enemy = _allEnemies[index];

        if (enemy != null && _enemyCells.TryGetValue(enemy, out int key))
        {
            RemoveFromCell(key, enemy);
            _enemyCells.Remove(enemy);
        }

        int lastIndex = _allEnemies.Count - 1;
        Enemy lastEnemy = _allEnemies[lastIndex];

        if (index != lastIndex)
        {
            _allEnemies[index] = lastEnemy;
            if (lastEnemy != null)
                _enemyIndices[lastEnemy] = index;
        }

        _allEnemies.RemoveAt(lastIndex);

        if (enemy != null)
            _enemyIndices.Remove(enemy);
    }

    private static int Hash(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / CELL_SIZE);
        int z = Mathf.FloorToInt(pos.z / CELL_SIZE);
        return (x << 16) ^ (z & 0xFFFF);
    }
}

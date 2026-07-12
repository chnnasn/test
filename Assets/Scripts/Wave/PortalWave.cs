using UnityEngine;

[CreateAssetMenu(fileName = "PortalWave", menuName = "ScriptableObjects/PortalWave", order = 1)]
public class PortalWave : ScriptableObject
{
    [Tooltip("当前波次会启用的传送门预制体。")]
    public SpawnPortal[] spawnPortals;

    [Tooltip("当前波次总共生成多少只敌人。")]
    [Min(0)]
    public int totalEnemyCount = 10;

    [Tooltip("当前波次分几轮生成。")]
    [Min(1)]
    public int waveNumber = 1;

    [Tooltip("每一轮之间等待多久。")]
    [Min(0f)]
    public float timeBetweenEnemyWaves = 1.0f;

    [Tooltip("每一轮每个传送门生成多少只敌人。按顺序使用数组配置，超过数组长度后使用最后一个值。")]
    public int[] enemyCountPerRound = { 5 };

    public int GetPortalEnemyCount(int portalIndex)
    {
        int portalCount = spawnPortals != null ? spawnPortals.Length : 0;
        if (portalCount <= 0 || totalEnemyCount <= 0)
            return 0;

        int baseCount = totalEnemyCount / portalCount;
        int remainder = totalEnemyCount % portalCount;
        return baseCount + (portalIndex < remainder ? 1 : 0);
    }

    public int GetEnemyCountPerRound(int roundIndex)
    {
        if (enemyCountPerRound == null || enemyCountPerRound.Length == 0)
            return 1;

        int index = Mathf.Clamp(roundIndex, 0, enemyCountPerRound.Length - 1);
        return Mathf.Max(1, enemyCountPerRound[index]);
    }
}

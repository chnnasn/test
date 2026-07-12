using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPortal : MonoBehaviour
{
    [SerializeField] private GameObject[] _enemies;
    [SerializeField] private int[] _enemySpawnWeight;
    private float enemySpawnDuration = 0.5f;
    public bool isLastPortal;

    private Func<GameObject, Vector3, Quaternion, Enemy> _spawnEnemy;
    private Func<Vector3> _getSpawnPosition;
    private Action _onPortalFinished;
    private int _totalEnemyCount;
    private int _waveNumber;
    private float _timeBetweenEnemyWaves;
    private int[] _enemyCountPerRound;

    public void CollectPrewarmEnemies(Dictionary<GameObject, int> counts, int maxEnemySpawnCount)
    {
        if (counts == null || _enemies == null) return;

        int count = Mathf.Max(0, maxEnemySpawnCount);
        if (count <= 0) return;

        for (int i = 0; i < _enemies.Length; i++)
        {
            GameObject enemyPrefab = _enemies[i];
            if (enemyPrefab == null) continue;

            if (!counts.ContainsKey(enemyPrefab))
                counts[enemyPrefab] = 0;
            counts[enemyPrefab] += count;
        }
    }

    public void Init(Func<GameObject, Vector3, Quaternion, Enemy> spawnEnemy, Action onPortalFinished, int totalEnemyCount, int waveNumber, float timeBetweenEnemyWaves, int[] enemyCountPerRound, Func<Vector3> getSpawnPosition = null)
    {
        _spawnEnemy = spawnEnemy;
        _getSpawnPosition = getSpawnPosition;
        _onPortalFinished = onPortalFinished;
        _totalEnemyCount = Mathf.Max(0, totalEnemyCount);
        _waveNumber = Mathf.Max(1, waveNumber);
        _timeBetweenEnemyWaves = Mathf.Max(0f, timeBetweenEnemyWaves);
        _enemyCountPerRound = enemyCountPerRound;
    }

    void Start()
    {
        StartCoroutine(SpawningCoroutine());

    }
    

    private IEnumerator SpawningCoroutine()
    {
        int enemyNumberLeft = _totalEnemyCount;
        int roundIndex = 0;
        yield return new WaitForSeconds(1.5f);
        while (enemyNumberLeft > 0 && roundIndex < _waveNumber)
        {
            int currentRoundCount = Mathf.Min(GetEnemyCountPerRound(roundIndex), enemyNumberLeft);
            roundIndex++;
            enemyNumberLeft -= currentRoundCount;

            while (currentRoundCount > 0)
            {
                int i = GetEnemyNumber();
                currentRoundCount--;
                Vector3 spawnPosition = GetCurrentRoundSpawnPosition();
                _spawnEnemy?.Invoke(_enemies[i], spawnPosition, Quaternion.identity);
                yield return new WaitForSeconds(enemySpawnDuration);
            }
            if (enemyNumberLeft > 0)
                yield return new WaitForSeconds(_timeBetweenEnemyWaves);
        }
        _onPortalFinished?.Invoke();
        Destroy(gameObject);
    }

    private int GetEnemyCountPerRound(int roundIndex)
    {
        if (_enemyCountPerRound == null || _enemyCountPerRound.Length == 0)
            return 1;

        int index = Mathf.Clamp(roundIndex, 0, _enemyCountPerRound.Length - 1);
        return Mathf.Max(1, _enemyCountPerRound[index]);
    }

    private Vector3 GetCurrentRoundSpawnPosition()
    {
        return _getSpawnPosition != null ? _getSpawnPosition.Invoke() : transform.position;
    }

    private int GetEnemyNumber()
    {
        int totalWeight = GetTotalWeight();
        float rnd = UnityEngine.Random.Range(0, 1.0f) * totalWeight;
        for (int i = 1; i < _enemySpawnWeight.Length; i++)
        {
            if (rnd < _enemySpawnWeight[i - 1] + _enemySpawnWeight[i] && rnd > _enemySpawnWeight[i - 1])
            {
                return i;
            }
        }
        return 0;
    }

    private int GetTotalWeight()
    {
        int totalWeight = 0;
        for (int i = 0; i < _enemySpawnWeight.Length; i++)
        {
            totalWeight += _enemySpawnWeight[i];
        }
        return totalWeight;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPortal : MonoBehaviour
{
    [SerializeField] private GameObject[] _enemies;
    [SerializeField] private int[] _enemySpawnWeight;
    [SerializeField] private int _averageEnemyPerWave;
    [SerializeField] private float _timeBetweenEnemyWaves;
    [SerializeField] private int _waveNumber;
    private float enemySpawnDuration = 0.5f;
    public bool isLastPortal;
    
    void Start()
    {
        StartCoroutine(SpawningCoroutine());

    }
    

    private IEnumerator SpawningCoroutine()
    {
        int waveNumberLeft = _waveNumber;
        yield return new WaitForSeconds(1.5f);
        while (waveNumberLeft > 0)
        {
            waveNumberLeft--;
            int rnd = Random.Range(_averageEnemyPerWave - 1, _averageEnemyPerWave + 2);
            while (rnd > 0)
            {
                int i = GetEnemyNumber();
                int totalWeight = GetTotalWeight();
                rnd--;
                Instantiate(_enemies[i], transform.position, Quaternion.identity);
                yield return new WaitForSeconds(enemySpawnDuration);
            }
            if (waveNumberLeft == 0)
            {
                /*GetComponentInChildren<SpriteRenderer>().enabled = false;
                PortalManager.instance.StartCountdown(_timeBetweenEnemyWaves / 2,_waveNumber);
                yield return new WaitForSeconds(_timeBetweenEnemyWaves / 2);
                PortalManager.instance.SpawnNextWave();
                if (isLastPortal)
                {
                    GameManager.instance.checkForWinCondition = true;
                }*/
            }
            yield return new WaitForSeconds(_timeBetweenEnemyWaves);
        }
        Destroy(gameObject);
    }

    private int GetEnemyNumber()
    {
        int totalWeight = GetTotalWeight();
        float rnd = Random.Range(0, 1.0f) * totalWeight;
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

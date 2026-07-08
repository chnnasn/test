using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalManager : MonoBehaviour
{
    [SerializeField] private PortalWave[] _portalWaves;
    [SerializeField] private SpawnPoint[] _spawnPoints;
    int currentWave;
    private bool _canSpawnWaves = true;
    
    void Start()
    {
        _spawnPoints = GetComponentsInChildren<SpawnPoint>();
        currentWave = 0;
    }

    public void SpawnNextWave()
    {
        if (!_canSpawnWaves)
            return;

        StartCoroutine(canSpawnWavesCoroutine());
        
        //todo：判断玩家是否存活
        if (true)
        {
            if (currentWave < _portalWaves.Length)
            {
                ResetSpawnPoints();
                int portalNumber = _portalWaves[currentWave].spawnPortals.Length;
                while (portalNumber > 0)
                {
                    int rnd = Random.Range(0, _spawnPoints.Length);
                    if (!_spawnPoints[rnd].busy)
                    {
                        Instantiate(_portalWaves[currentWave].spawnPortals[portalNumber - 1], _spawnPoints[rnd].transform.position, Quaternion.identity);
                        _spawnPoints[rnd].busy = true;
                        portalNumber--;
                    }
                }
            }
            currentWave++;
        }
    }

    IEnumerator canSpawnWavesCoroutine()
    {
        _canSpawnWaves = false;
        yield return new WaitForSeconds(8.0f);
        _canSpawnWaves = true;
    }

    private void ResetSpawnPoints()
    {
        foreach (SpawnPoint spawnPoint in _spawnPoints)
        {
            spawnPoint.busy = false;
        }
    }
}

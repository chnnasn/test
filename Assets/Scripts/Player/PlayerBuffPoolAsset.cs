using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffPool", menuName = "ScriptableObjects/Player Buff Pool", order = 5)]
public class PlayerBuffPoolAsset : ScriptableObject
{
    [SerializeField] private PlayerBuffAsset[] _buffs;

    public PlayerBuffAsset[] Buffs => _buffs;

    public PlayerBuffAsset[] GetRandomDifferentBuffs(int count, IReadOnlyCollection<PlayerBuffAsset> excludedBuffs = null)
    {
        if (_buffs == null || _buffs.Length == 0 || count <= 0)
            return new PlayerBuffAsset[0];

        List<PlayerBuffAsset> candidates = new List<PlayerBuffAsset>(_buffs.Length);
        foreach (PlayerBuffAsset buff in _buffs)
        {
            if (buff == null) continue;
            if (excludedBuffs != null && excludedBuffs.Contains(buff)) continue;

            candidates.Add(buff);
        }

        if (candidates.Count == 0)
            return new PlayerBuffAsset[0];

        int resultCount = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < resultCount; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);
            PlayerBuffAsset temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
        }

        PlayerBuffAsset[] result = new PlayerBuffAsset[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            result[i] = candidates[i];
        }

        return result;
    }
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffPool", menuName = "ScriptableObjects/Player Buff Pool", order = 5)]
public class PlayerBuffPoolAsset : ScriptableObject
{
    [SerializeField] private PlayerBuffAsset[] _buffs;

    public PlayerBuffAsset[] Buffs => _buffs;

    public PlayerBuffAsset[] GetRandomDifferentBuffs(
        int count, 
        ICollection<PlayerBuffAsset> excludedBuffs = null, 
        PlayerBuff playerBuff = null)
    {
        if (_buffs == null || _buffs.Length == 0 || count <= 0)
            return new PlayerBuffAsset[0];

        // 使用 HashSet 去重：防止池子里拖入了重复的 ScriptableObject
        var candidatesSet = new HashSet<PlayerBuffAsset>();
        foreach (PlayerBuffAsset buff in _buffs)
        {
            if (buff == null) continue;
            if (excludedBuffs != null && excludedBuffs.Contains(buff)) continue;
            if (!CanDrawBuff(buff, playerBuff)) continue;
            candidatesSet.Add(buff);
        }

        if (candidatesSet.Count == 0)
            return new PlayerBuffAsset[0];

        List<PlayerBuffAsset> candidates = new List<PlayerBuffAsset>(candidatesSet);

        int resultCount = Mathf.Min(count, candidates.Count);
        // Fisher-Yates 洗牌
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

    private bool CanDrawBuff(PlayerBuffAsset buff, PlayerBuff playerBuff)
    {
        if (playerBuff == null) return true;

        return buff.Kind switch
        {
            PlayerBuffKind.DroneSkillPower => playerBuff.IsSkillUnlocked(PlayerSkillKind.Drone),
            PlayerBuffKind.IceBombSkillPower => playerBuff.IsSkillUnlocked(PlayerSkillKind.IceBomb),
            PlayerBuffKind.Gambling => false,
            _ => true
        };
    }
}

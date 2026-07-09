using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBuffPool", menuName = "ScriptableObjects/Player Buff Pool", order = 5)]
public class PlayerBuffPoolAsset : ScriptableObject
{
    [SerializeField] private PlayerBuffAsset[] _buffs;

    public PlayerBuffAsset[] Buffs => _buffs;

    public PlayerBuffAsset[] GetRandomDifferentBuffs(int count)
    {
        if (_buffs == null || _buffs.Length == 0 || count <= 0)
            return new PlayerBuffAsset[0];

        int resultCount = Mathf.Min(count, _buffs.Length);
        PlayerBuffAsset[] candidates = new PlayerBuffAsset[_buffs.Length];
        _buffs.CopyTo(candidates, 0);

        for (int i = 0; i < resultCount; i++)
        {
            int randomIndex = Random.Range(i, candidates.Length);
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

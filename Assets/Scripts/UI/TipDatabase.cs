using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TipDatabase", menuName = "ScriptableObjects/Tip Database", order = 6)]
public class TipDatabase : ScriptableObject
{
    [TextArea(2, 4)]
    [SerializeField] private string[] _tips;

    public string[] Tips => _tips;

    public string GetRandomTip()
    {
        if (_tips == null || _tips.Length == 0)
            return string.Empty;

        return _tips[Random.Range(0, _tips.Length)];
    }
}

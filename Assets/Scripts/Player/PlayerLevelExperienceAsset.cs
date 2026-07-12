using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLevelExperience", menuName = "ScriptableObjects/Player Level Experience", order = 3)]
public class PlayerLevelExperienceAsset : ScriptableObject
{
    [SerializeField] private float[] _levelExperienceRequirements;

    public float[] LevelExperienceRequirements => _levelExperienceRequirements;

    public float GetRequiredExperience(int levelIndex)
    {
        if (_levelExperienceRequirements == null || _levelExperienceRequirements.Length == 0 || levelIndex < 0)
            return 0f;

        int index = Mathf.Clamp(levelIndex, 0, _levelExperienceRequirements.Length - 1);
        return _levelExperienceRequirements[index];
    }
}

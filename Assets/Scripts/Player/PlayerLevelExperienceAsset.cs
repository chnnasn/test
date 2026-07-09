using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLevelExperience", menuName = "ScriptableObjects/Player Level Experience", order = 3)]
public class PlayerLevelExperienceAsset : ScriptableObject
{
    [SerializeField] private float[] _levelExperienceRequirements;

    public float[] LevelExperienceRequirements => _levelExperienceRequirements;

    public float GetRequiredExperience(int levelIndex)
    {
        if (_levelExperienceRequirements == null || levelIndex < 0 || levelIndex >= _levelExperienceRequirements.Length)
            return 0f;

        return _levelExperienceRequirements[levelIndex];
    }
}

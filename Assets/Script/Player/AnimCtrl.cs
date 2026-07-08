using UnityEngine;

/// <summary>
/// 枪械动画控制器（纯动画触发 + 武器显隐）
/// </summary>
public class AnimCtrl : MonoBehaviour
{
    [Header("动画组件")]
    [SerializeField] private Animator _armAnimator;
    [SerializeField] private Animator _gunAnimator;

    [Header("武器模型")]
    [SerializeField] private GameObject _rangedWeaponObj;

    private int _fireHash = Animator.StringToHash("rangedAtk");
    private int _reloadHash = Animator.StringToHash("rangedReload");

    private void Start()
    {
        if (_rangedWeaponObj != null) _rangedWeaponObj.SetActive(true);
    }

    // ==================== 攻击 ====================

    public void PlayRangedFire()
    {
        _armAnimator.SetTrigger(_fireHash);
        _gunAnimator.SetTrigger(_fireHash);
    }

    public void PlayRangedReload()
    {
        _armAnimator.SetTrigger(_reloadHash);
        _gunAnimator.SetTrigger(_reloadHash);
    }
}

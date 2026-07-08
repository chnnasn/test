using UnityEngine;

/// <summary>
/// UI 根节点单例，挂载到场景中的 UIRoot GameObject 上。
/// 统一管理所有动态 UI 的父容器引用。
/// </summary>
public class UIRoot : MonoSingleton<UIRoot>
{
    [Header("技能冷却UI容器")]
    public Transform SkillCooldownParent;

    [Header("Boss血条容器")]
    public Transform BossHpBarParent;

    [Header("游戏结束面板")]
    public GameOverPanel GameOverPanel;

    [Header("弹夹UI")]
    public AmmoUI AmmoUI;
}

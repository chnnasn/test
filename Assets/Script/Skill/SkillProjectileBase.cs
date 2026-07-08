using UnityEngine;

/// <summary>
/// 技能抛射体基类——所有技能（火箭弹、液氮桶、激光等）继承此类
/// 子类覆写 Init() 时必须调用 base.Init()，再读取各自的类型特定参数
/// </summary>
public abstract class SkillProjectileBase : MonoBehaviour
{
    protected float _damage;

    /// <summary>
    /// 初始化。子类覆写时必须先调用 base.Init()
    /// </summary>
    public virtual void Init(SkillEffectData cfg, Vector3 firePos, Vector3 direction, Transform target)
    {
        _damage = cfg.Damage;
        transform.position = firePos;
    }
}

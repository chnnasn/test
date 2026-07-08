using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能控制器：管理已获得技能的冷却计时，冷却结束后自动发射
/// 不关心具体技能类型——通过 SkillConfig 的 PrefabPath 加载预制体，调用 SkillProjectileBase.Init()
/// </summary>
public class SkillController : MonoBehaviour
{
    [Header("发射点")]
    [SerializeField] private Transform _firePoint;          // 通用技能发射点
    [SerializeField] private Transform _rayFirePoint;       // 高能射线专用发射点

    [Header("冷却UI")]
    [SerializeField] private string _cooldownUIPath = "Prefab/UIPrefab/SkillCooldownUI";

    private AutoCombatController _autoCombat;
    private Dictionary<int, float> _cooldownTimers = new Dictionary<int, float>();
    private Dictionary<int, GameObject> _uiInstances = new Dictionary<int, GameObject>();

    /// <summary>冷却状态变化事件 (skillId, remaining, total)</summary>
    public event Action<int, float, float> OnCooldownChanged;

    public int ActiveSkillCount => _cooldownTimers.Count;

    private Transform UIParent => UIRoot.Instance != null ? UIRoot.Instance.SkillCooldownParent : null;

    private void Awake()
    {
        _autoCombat = GetComponent<AutoCombatController>();
    }

    private void Start()
    {
        RefreshAcquiredSkills();
    }

    private void Update()
    {
        if (_autoCombat == null) return;

        foreach (var skillId in new List<int>(_cooldownTimers.Keys))
        {
            float remaining = _cooldownTimers[skillId] - Time.deltaTime;

            if (remaining <= 0)
            {
                if (_autoCombat.HasTarget)
                {
                    FireSkill(skillId);
                    remaining = SkillConfig.Get(skillId).Cooldown;
                }
                else
                {
                    remaining = 0f;
                }
            }

            _cooldownTimers[skillId] = remaining;
            var cfg = SkillConfig.Get(skillId);
            OnCooldownChanged?.Invoke(skillId, remaining, cfg.Cooldown);
        }
    }

    public void RefreshAcquiredSkills()
    {
        var poolData = SkillConfigReader.Instance?.PoolData;
        if (poolData == null)
        {
            Debug.LogWarning("[SkillCtrl] RefreshAcquiredSkills 跳过：SkillConfigReader 数据为空");
            return;
        }

        foreach (var node in poolData.allSkillNodes)
        {
            if (node.state != SkillNodeState.Acquired) continue;
            if (_cooldownTimers.ContainsKey(node.skillId)) continue;

            if (!SkillConfig.Loaded)
            {
                var assets = Resources.LoadAll<TextAsset>("Config/SkillConfig");
                if (assets.Length > 0)
                {
                    var texts = new List<string>();
                    foreach (var a in assets) texts.Add(a.text);
                    SkillConfig.LoadConfig(texts);
                }
            }

            if (!SkillConfig.Contains(node.skillId))
            {
                Debug.LogWarning($"[SkillCtrl] 技能id={node.skillId} 在 SkillConfig/ 目录中未配置，跳过");
                continue;
            }

            var cfg = SkillConfig.Get(node.skillId);
            _cooldownTimers[node.skillId] = 0f;

            // 动态生成冷却UI
            if (string.IsNullOrEmpty(_cooldownUIPath))
            {
                Debug.LogWarning("[SkillCtrl] _cooldownUIPath 为空，跳过UI生成");
            }
            else if (UIParent == null)
            {
                Debug.LogWarning("[SkillCtrl] UIParent 为空，请检查 UIRoot 是否挂载且 SkillCooldownParent 已赋值");
            }
            else
            {
                var uiObj = ResourceManager.Instance.LoadPrefabAndInstantiate(_cooldownUIPath, UIParent);
                if (uiObj == null)
                {
                    Debug.LogError($"[SkillCtrl] 冷却UI预制体加载失败，路径: Resources/{_cooldownUIPath}");
                }
                else
                {
                    var ui = uiObj.GetComponent<SkillCooldownUI>();
                    if (ui == null)
                    {
                        Debug.LogError("[SkillCtrl] 预制体上缺少 SkillCooldownUI 组件");
                    }
                    else
                    {
                        ui.Bind(this, node.skillId, cfg);
                        _uiInstances[node.skillId] = uiObj;
                        Debug.Log($"[SkillCtrl] 冷却UI已生成: {cfg.SkillName}");
                    }
                }
            }

            Debug.Log($"[SkillCtrl] 技能就绪 {cfg.SkillName}(id={node.skillId}) 冷却:{cfg.Cooldown}s 伤害:{cfg.Damage}");
        }
    }

    private void FireSkill(int skillId)
    {
        var cfg = SkillConfig.Get(skillId);
        if (string.IsNullOrEmpty(cfg.PrefabPath))
        {
            Debug.LogError($"[SkillCtrl] 技能 {cfg.SkillName} 未配置预制体路径");
            return;
        }

        Vector3 firePos = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1.5f;
        Transform target = _autoCombat.CurrentTarget;
        Vector3 direction = target != null ? (target.position - firePos).normalized : transform.forward;

        // 加载预制体
        var obj = ResourceManager.Instance.LoadPrefabAndInstantiate(cfg.PrefabPath, null);
        if (obj == null)
        {
            Debug.LogError($"[SkillCtrl] 预制体加载失败: Resources/{cfg.PrefabPath}");
            return;
        }
        obj.transform.position = firePos;
        obj.transform.rotation = Quaternion.LookRotation(direction);
        var proj = obj.GetComponent<SkillProjectileBase>();
        if (proj == null)
        {
            Debug.LogError($"[SkillCtrl] 预制体 {cfg.PrefabPath} 缺少 SkillProjectileBase 组件");
            Destroy(obj);
            return;
        }

        proj.Init(cfg, firePos, direction, target);

        // 激光/射线类技能需要持续读取发射点位置
        if (proj is LaserGunProjectile laser)
            laser.SetFirePoint(_firePoint);
        else if (proj is HighEnergyRayProjectile ray)
            ray.SetFirePoint(_rayFirePoint != null ? _rayFirePoint : _firePoint);

        Debug.Log($"[SkillCtrl] 发射 {cfg.SkillName} 伤害:{cfg.Damage}");
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能配置加载器。
/// - Init() 检查 CSV 文件，缺失时给出提示
/// - Reload(callback) 解析 CSV 生成 SkillPoolData，通过回调返回解析成败
/// - PoolData 对外暴露纯数据，业务逻辑由 SkillPoolSelect 提供
/// </summary>
public class SkillConfigReader : MonoBehaviour
{
    public static SkillConfigReader Instance { get; private set; }

    [Header("CSV 配置文件")]
    public List<TextAsset> csvFiles = new List<TextAsset>();

    // 纯数据。加载完成后通过此对象读取所有技能信息。
    public SkillPoolData PoolData { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Init();
    }

    /// <summary>
    /// 初始化：检查 CSV 文件并执行加载。
    /// 若未配置 CSV 文件则输出提示并返回 false。
    /// </summary>
    public bool Init(Action<bool> onComplete = null)
    {
        if (csvFiles == null || csvFiles.Count == 0)
        {
            Debug.LogWarning("[SkillConfigReader] 未配置 CSV 文件，请在 Inspector 中拖入技能配表");
            onComplete?.Invoke(false);
            return false;
        }

        Reload(onComplete);
        return true;
    }

    /// <summary>
    /// 重新解析 CSV 并生成 SkillPoolData。
    /// 通过 onComplete 回调返回解析结果（true = 成功，false = 失败）。
    /// </summary>
    public void Reload(Action<bool> onComplete = null)
    {
        if (csvFiles == null || csvFiles.Count == 0)
        {
            Debug.LogWarning("[SkillConfigReader] Reload 失败：未配置 CSV 文件");
            onComplete?.Invoke(false);
            return;
        }

        PoolData = new SkillPoolData();
        int count = PoolData.LoadFromCSV(csvFiles);

        if (count < 0)
        {
            Debug.LogError("[SkillConfigReader] 解析失败：CSV 文件列表为空");
            onComplete?.Invoke(false);
            return;
        }

        // 数据就绪后，通知 SkillPoolSelect 刷新初始状态（根技能变为可获取）
        if (SkillPoolSelect.Instance != null)
            SkillPoolSelect.Instance.RefreshAllStates();

        if (count == 0)
        {
            Debug.LogWarning("[SkillConfigReader] 解析完成，但未加载到任何技能");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.Log($"[SkillConfigReader] 解析成功，共加载 {count} 个技能");
            onComplete?.Invoke(true);
        }
    }
}

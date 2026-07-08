using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏结束面板。角色死亡或抵达终点时调用。
/// Show(true) = 胜利，Show(false) = 失败。
/// </summary>
public class GameOverPanel : MonoSingleton<GameOverPanel>
{
    [Header("面板根节点")]
    [SerializeField] private GameObject _panel;

    [Header("结束显示")]
    [SerializeField] private GameObject _winObject;
    [SerializeField] private GameObject _loseObject;

    [Header("按钮")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private TMP_Text _restartButtonText;
    [SerializeField] private string _restartText = "继续游戏";
    [SerializeField] private Button _quitButton;
    [SerializeField] private TMP_Text _quitButtonText;
    [SerializeField] private string _quitText = "退出游戏";

    private void Start()
    {
        if (_panel != null)
            _panel.SetActive(false);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestart);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuit);

        // 初始化按钮文字
        if (_restartButtonText != null)
            _restartButtonText.text = _restartText;
        if (_quitButtonText != null)
            _quitButtonText.text = _quitText;
    }

    /// <summary>
    /// 显示游戏结束面板
    /// </summary>
    /// <param name="isWin">true=胜利，false=失败</param>
    public void Show(bool isWin)
    {
        if (_panel == null) return;

        if (_winObject != null)
            _winObject.SetActive(isWin);
        if (_loseObject != null)
            _loseObject.SetActive(!isWin);

        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnRestart()
    {
        Time.timeScale = 1f;
        _panel.SetActive(false);

        // 清理所有技能冷却UI实例（UIRoot DontDestroyOnLoad，子节点会残留）
        var cooldownParent = UIRoot.Instance != null ? UIRoot.Instance.SkillCooldownParent : null;
        if (cooldownParent != null)
        {
            for (int i = cooldownParent.childCount - 1; i >= 0; i--)
                Destroy(cooldownParent.GetChild(i).gameObject);
        }

        // 重置技能状态（SkillConfigReader 是 DontDestroyOnLoad，PoolData 会跨场景存活）
        SkillPoolSelect.Instance?.ResetAllStates();
        // 隐藏技能选择面板（如果之前是打开状态）
        if (SkillPoolSelect.Instance != null && SkillPoolSelect.Instance.skillSelectPanel != null)
            SkillPoolSelect.Instance.skillSelectPanel.SetActive(false);

        // 重置经验等级
        ExpManager.Instance?.Reset();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

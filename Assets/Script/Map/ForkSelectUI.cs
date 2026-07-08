using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 岔路选择UI面板
/// 到达岔路口时显示，玩家点击按钮选择前进路线
/// </summary>
public class ForkSelectUI : MonoSingleton<ForkSelectUI>
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button[] _routeButtons;

    private void Start()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    /// <summary>
    /// 显示岔路选择面板，自动从 LevelFlow 获取可选路线
    /// </summary>
    public void Show()
    {
        var flow = FindObjectOfType<LevelFlow>();
        if (flow == null) return;

        var room = flow.GetCurrentRoom();
        if (room == null || !room.IsFork) return;

        int[] nextIds = room.NextId;

        for (int i = 0; i < _routeButtons.Length; i++)
        {
            if (i < nextIds.Length)
            {
                _routeButtons[i].gameObject.SetActive(true);
                SetButtonText(_routeButtons[i], $"room {nextIds[i]}");
                int index = i;
                _routeButtons[i].onClick.RemoveAllListeners();
                _routeButtons[i].onClick.AddListener(() => OnRouteSelected(index));
            }
            else
            {
                _routeButtons[i].gameObject.SetActive(false);
            }
        }

        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnRouteSelected(int index)
    {
        _panel.SetActive(false);
        Time.timeScale = 1f;
        FindObjectOfType<LevelFlow>()?.SelectFork(index);
    }

    private void SetButtonText(Button btn, string text)
    {
        //var tmp = btn.GetComponentInChildren<TMP_Text>();
        //if (tmp != null)
        //{
        //    tmp.text = text;
        //    return;
        //}
    }
}

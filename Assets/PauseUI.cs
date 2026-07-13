using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    public Text DescText;

    private void OnEnable()
    {
        RefreshTip();
    }

    private void RefreshTip()
    {
        if (DescText == null) return;

        string tip = GameManager.Instance != null ? GameManager.Instance.GetRandomTip() : string.Empty;
        DescText.text = string.IsNullOrEmpty(tip) ? string.Empty : $"小Tip：\n{tip}";
    }

    public void ResumeGame()
    {
        EventManager.Instance.SetGameResume();
        gameObject.SetActive(false);
    }

    public void quit()
    {
     Application.Quit();   
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Settle : MonoBehaviour
{
    
    public Text Tittle;

    public void SetTittle(string text)
    {
        Tittle.text = text;
        if (text == "胜利")
        {
            Tittle.gameObject.GetComponent<Text>().color = Color.green;
        }
        else
        {
            Tittle.gameObject.GetComponent<Text>().color = Color.red;
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ReStart()
    {
        EventManager.Instance.SetGameResume();
        EventManager.Instance.TriggerBeforeDemoRestart();
        LoadingSceneManager.SetRestartOnLoad();
        SceneManager.LoadScene("Loading");
    }

}

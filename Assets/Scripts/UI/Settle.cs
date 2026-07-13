using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    
}

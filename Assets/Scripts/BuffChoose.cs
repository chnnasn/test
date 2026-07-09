using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffChoose : MonoBehaviour
{

    private int _index;

    public void SetIndex(int index)
    {
        _index  = index;
        
    }

    public void ChooseBuff()
    {
        EventManager.Instance.SetBuffIndex(_index);
    }
}
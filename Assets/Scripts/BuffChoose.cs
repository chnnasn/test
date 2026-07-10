using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuffChoose : MonoBehaviour, IPointerClickHandler
{
    private Dictionary<GameObject, int> _childIndexMap;
    private HashSet<GameObject> _validTargets;

    [SerializeField]private Text[] _texts;
    private string[] _buffs;

    private int _index = -1;
    private bool _canChoose;

    private void OnEnable()
    {
        _index = -1;
        _canChoose = true;
    }

    private void Start()
    {
        IniCache();
    }

    private void IniCache()
    {
        _childIndexMap = new Dictionary<GameObject, int>();
        _validTargets = new HashSet<GameObject>();

        for (int i = 0; i < transform.childCount-1; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            _childIndexMap[child] = i;
            _validTargets.Add(child);
        }
    }

    public void SetBuffs(string[] buffs)
    {
        _buffs = buffs;
        _index = -1;
        _canChoose = true;
        RefreshTexts();
    }

    public void SetCanChoose(bool canChoose)
    {
        _canChoose = canChoose;
    }

    private void RefreshTexts()
    {
        if (_texts == null) return;

        for (int i = 0; i < _texts.Length; i++)
        {
            if (_texts[i] == null) continue;
            _texts[i].text = _buffs != null && i < _buffs.Length ? _buffs[i] : string.Empty;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_canChoose) return;

        GameObject clickedObject = GetValidTarget(eventData.pointerCurrentRaycast.gameObject);
        if (clickedObject == null)
        {
            clickedObject = GetValidTarget(eventData.pointerPressRaycast.gameObject);
        }
        if (clickedObject == null)
        {
            clickedObject = GetValidTarget(eventData.pointerPress);
        }

        if (clickedObject == null) return;

        _index = _childIndexMap[clickedObject];
    }

    private GameObject GetValidTarget(GameObject target)
    {
        if (target == null) return null;

        Transform current = target.transform;
        while (current != null)
        {
            if (_validTargets.Contains(current.gameObject))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        return null;
    }

    public void ChooseBuff()
    {
        if (!_canChoose || _index == -1) return;

        _canChoose = false;
        EventManager.Instance.SetBuffIndex(_index);
        _buffs = null;
        _index = -1;
    }
}

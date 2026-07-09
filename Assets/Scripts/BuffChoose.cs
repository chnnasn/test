using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuffChoose : MonoBehaviour, IPointerClickHandler
{
    private Dictionary<GameObject, int> _childIndexMap;
    private HashSet<GameObject> _validTargets;

    private int _index;

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

    public void OnPointerClick(PointerEventData eventData)
    {
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
        EventManager.Instance.SetBuffIndex(_index);
    }
}
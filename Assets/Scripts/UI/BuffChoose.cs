using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuffChoose : MonoBehaviour, IPointerClickHandler
{
    private Dictionary<GameObject, int> _childIndexMap;
    private Dictionary<GameObject, Vector3> _originalScaleMap;
    private Dictionary<Image, Color> _originalColorMap;
    private HashSet<GameObject> _validTargets;
    private GameObject _selectedTarget;

    [SerializeField]private Text[] _texts;
    private string[] _buffs;

    private int _index = -1;
    private bool _canChoose;

    [Header("点击反馈")]
    [SerializeField] private float _clickScale = 1.1f;
    [SerializeField] private float _clickDuration = 0.15f;
    [SerializeField] private Color _clickColor = new Color(1f, 0.9f, 0.6f, 1f);

    private void OnEnable()
    {
        _index = -1;
        _selectedTarget = null;
        _canChoose = true;
    }

    private void Start()
    {
        IniCache();
    }

    private void IniCache()
    {
        _childIndexMap = new Dictionary<GameObject, int>();
        _originalScaleMap = new Dictionary<GameObject, Vector3>();
        _originalColorMap = new Dictionary<Image, Color>();
        _validTargets = new HashSet<GameObject>();

        for (int i = 0; i < transform.childCount-1; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            _childIndexMap[child] = i;
            _originalScaleMap[child] = child.transform.localScale;
            _validTargets.Add(child);

            foreach (Image image in child.GetComponentsInChildren<Image>(true))
            {
                _originalColorMap[image] = image.color;
            }
        }
    }

    public void SetBuffs(string[] buffs)
    {
        _buffs = buffs;
        _index = -1;
        ResetSelectedTarget();
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
        PlayClickEffect(clickedObject);
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

    private void PlayClickEffect(GameObject target)
    {
        if (_selectedTarget == target) return;

        ResetSelectedTarget();

        _selectedTarget = target;
        Transform targetTransform = target.transform;
        Vector3 originalScale = GetOriginalScale(target);

        targetTransform.DOKill();
        targetTransform.DOScale(originalScale * _clickScale, _clickDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        foreach (Image image in target.GetComponentsInChildren<Image>(true))
        {
            image.DOKill();
            image.DOColor(_clickColor, _clickDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    private void ResetSelectedTarget()
    {
        if (_selectedTarget == null) return;

        Transform selectedTransform = _selectedTarget.transform;
        selectedTransform.DOKill();
        selectedTransform.DOScale(GetOriginalScale(_selectedTarget), _clickDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        foreach (Image image in _selectedTarget.GetComponentsInChildren<Image>(true))
        {
            image.DOKill();
            image.DOColor(GetOriginalColor(image), _clickDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
        }

        _selectedTarget = null;
    }

    private Vector3 GetOriginalScale(GameObject target)
    {
        if (_originalScaleMap != null && _originalScaleMap.TryGetValue(target, out Vector3 originalScale))
        {
            return originalScale;
        }

        return target.transform.localScale;
    }

    private Color GetOriginalColor(Image image)
    {
        if (_originalColorMap != null && _originalColorMap.TryGetValue(image, out Color originalColor))
        {
            return originalColor;
        }

        return image.color;
    }

    public void ChooseBuff()
    {
        if (!_canChoose || _index == -1) return;

        ResetSelectedTarget();
        _canChoose = false;
        EventManager.Instance.SetBuffIndex(_index);
        _buffs = null;
        _index = -1;
    }
}

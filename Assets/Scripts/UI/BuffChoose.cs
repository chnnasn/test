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

    [SerializeField]private Transform[] _texts;
    private string[] _buffs;
    private string[] _buffDescs;

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

        if (_texts == null) return;

        for (int i = 0; i < _texts.Length; i++)
        {
            if (_texts[i] == null) continue;

            GameObject child = _texts[i].gameObject;
            _childIndexMap[child] = i;
            _originalScaleMap[child] = child.transform.localScale;
            _validTargets.Add(child);

            foreach (Image image in child.GetComponentsInChildren<Image>(true))
            {
                _originalColorMap[image] = image.color;
            }
        }
    }

    public void SetBuffs(string[] buffs, string[] descs)
    {
        _buffs = buffs;
        _buffDescs = descs;
        _index = -1;
        ResetSelectedTarget();
        _canChoose = true;
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (_texts == null) return;

        for (int i = 0; i < _texts.Length; i++)
        {
            if (_texts[i] == null) continue;

            Transform parent = _texts[i];
            if (parent.childCount < 1) continue;

            Transform textRoot = parent.GetChild(0);
            if (textRoot.childCount < 2) continue;

            Text nameText = textRoot.GetChild(0).GetComponent<Text>();
            Text descText = textRoot.GetChild(1).GetComponent<Text>();

            if (nameText != null)
                nameText.text = _buffs != null && i < _buffs.Length ? _buffs[i] : string.Empty;

            if (descText != null)
                descText.text = _buffDescs != null && i < _buffDescs.Length ? _buffDescs[i] : string.Empty;
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
        
        Image image = target.transform.GetChild(0).GetComponentInChildren<Image>();
        
        image.DOKill();
        image.DOColor(_clickColor, _clickDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
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
        _buffDescs = null;
        _index = -1;
    }

    /// <summary>
    /// 外部触发赌博 Buff 的接口，不会被池子抽到，通过其他方式调用。
    /// </summary>
    public void TriggerGambling()
    {
        EventManager.Instance.SetRequestGambling();
    }
}

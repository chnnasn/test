using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class JoyStick : ScrollRect
{
    protected float Radius = 0;
    Vector2 diraction;
    Canvas canvas;
    RectTransform rectTransform;

    private int? activePointerId = null;
    private Character _character;

    public Vector2 Direction { get; private set; }

    protected override void Start()
    {
        base.Start();
        rectTransform = gameObject.GetComponent<RectTransform>();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        Radius = (transform as RectTransform).sizeDelta.x * 0.5f;
        content = transform.GetChild(0).gameObject.GetComponent<RectTransform>();
        
        _character = FindFirstObjectByType<Character>();
    }

    void Update()
    {
        if (_character == null) return;

        Vector2 input = activePointerId.HasValue
            ? Vector2.ClampMagnitude(diraction / Radius, 1f)
            : Vector2.zero;

        Direction = input.normalized;
        _character.SetExternalMoveInput(input);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (activePointerId == null)
        {
            activePointerId = eventData.pointerId;
            base.OnBeginDrag(eventData);
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (activePointerId == eventData.pointerId)
        {
            base.OnDrag(eventData);
            var contentPosition = this.content.anchoredPosition;

            if (contentPosition.magnitude > Radius)
            {
                contentPosition = contentPosition.normalized * Radius;
                SetContentAnchoredPosition(contentPosition);
            }

            diraction = (Vector2)(content.localPosition - Vector3.zero);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (activePointerId == eventData.pointerId)
        {
            base.OnEndDrag(eventData);
            activePointerId = null;
        }
    }
}

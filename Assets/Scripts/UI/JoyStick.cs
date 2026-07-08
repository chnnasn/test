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
    private Vector2 _dragOrigin;
    private Vector2 _pendingDelta;

    public Vector2 Direction { get; private set; }

    [SerializeField, Range(0.5f, 1f)] private float _runThreshold = 0.9f;
    [SerializeField, Range(1f, 4f)] private float _sensitivity = 2f;

    protected override void Start()
    {
        base.Start();

        // 彻底禁用 ScrollRect 的自动滚动，全部自己处理
        horizontal = false;
        vertical = false;
        inertia = false;
        movementType = MovementType.Clamped;

        rectTransform = gameObject.GetComponent<RectTransform>();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        Radius = (transform as RectTransform).sizeDelta.x * 0.5f;
        content = transform.GetChild(0).gameObject.GetComponent<RectTransform>();
        content.anchoredPosition = Vector2.zero;

        // 通过 GameManager 间接获取 Character，不直接 FindFirstObjectByType
        if (GameManager.Instance != null)
            _character = GameManager.Instance.GetCharacter();
    }

    void Update()
    {
        if (_character == null) return;

        Vector2 input = activePointerId.HasValue
            ? Vector2.ClampMagnitude(diraction / Radius, 1f)
            : Vector2.zero;

        Direction = input.normalized;

        // 摇杆推到阈值以上 + 向前 → 自动奔跑；松开则停止
        _character.SetExternalRunning(input.magnitude >= _runThreshold && input.y > 0f);

        _character.SetExternalMoveInput(input);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (activePointerId == null)
        {
            activePointerId = eventData.pointerId;
            _dragOrigin = eventData.position;
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId) return;

        Vector2 delta = (eventData.position - _dragOrigin) * _sensitivity / canvas.scaleFactor;
        delta = Vector2.ClampMagnitude(delta, Radius);
        _pendingDelta = delta;
        diraction = delta;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId) return;

        activePointerId = null;
        _pendingDelta = Vector2.zero;
        diraction = Vector2.zero;
    }

    /// <summary>
    /// LateUpdate 中应用位置，确保覆盖 ScrollRect 内部的位置修正
    /// </summary>
    void LateUpdate()
    {
        // 每帧强制应用，覆盖 ScrollRect 内部位置修正
        content.anchoredPosition = _pendingDelta;
    }
}

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

    public Vector2 Direction { get; private set; }

    [SerializeField, Range(0.5f, 1f)] private float _runThreshold = 0.9f;
    [SerializeField, Range(1f, 4f)] private float _sensitivity = 2f;
    [SerializeField, Range(5f, 30f)] private float _returnSpeed = 15f;

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

        // 松开后平滑回中
        if (!activePointerId.HasValue)
            diraction = Vector2.Lerp(diraction, Vector2.zero, _returnSpeed * Time.deltaTime);

        Vector2 input = Vector2.ClampMagnitude(diraction / Radius, 1f);
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

        diraction = Vector2.ClampMagnitude(
            (eventData.position - _dragOrigin) * _sensitivity / canvas.scaleFactor,
            Radius);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId) return;
        // 只解绑手指，diraction 由 Update 平滑回弹到零
        activePointerId = null;
    }

    void LateUpdate()
    {
        // diraction 就是手柄位置，LateUpdate 覆盖 ScrollRect 内部修正
        content.anchoredPosition = diraction;
    }
}

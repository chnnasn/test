using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ScreenTouch : MonoBehaviour
{
	#region FIELDS SERIALIZED

	[Header("灵敏度")]
	[Tooltip("水平（Yaw）旋转灵敏度。与鼠标 Pointer/delta 的 ScaleVector2 处理器叠加。")]
	[SerializeField]
	private float sensitivityX = 0.1f;

	[Tooltip("垂直（Pitch）旋转灵敏度。与鼠标 Pointer/delta 的 ScaleVector2 处理器叠加。")]
	[SerializeField]
	private float sensitivityY = 0.1f;

	[Tooltip("是否反转垂直方向。")]
	[SerializeField]
	private bool invertY = false;

	#endregion

	#region FIELDS

	/// <summary>
	/// 当前用于视角旋转的手指。
	/// </summary>
	private Finger lookFinger = null;

	/// <summary>
	/// 上一帧用于计算视角旋转的触摸位置。
	/// </summary>
	private Vector2 lastLookPosition;

	/// <summary>
	/// 是否已经记录上一帧触摸位置。
	/// </summary>
	private bool hasLastLookPosition;

	/// <summary>
	/// UI 射线检测缓存，避免触摸检测时每次 new PointerEventData/List。
	/// </summary>
	private EventSystem cachedEventSystem;
	private PointerEventData cachedPointerEventData;
	private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(8);

	/// <summary>
	/// 记录哪些手指曾在 UI 上按下，永久排除它们成为视角控制手指。
	/// </summary>
	private readonly HashSet<int> uiFingerIndices = new HashSet<int>();

	/// <summary>
	/// 预分配缓冲区，用于清理已结束的手指索引，避免 GC。
	/// </summary>
	private readonly List<int> _fingerRemoveBuffer = new List<int>(4);

	#endregion

	#region UNITY

	private void OnEnable()
	{
		EnhancedTouchSupport.Enable();
	}

	private void OnDisable()
	{
		ResetLookFinger();
		EnhancedTouchSupport.Disable();
	}

	private void Update()
	{
		if (EventManager.Instance.GetCharacter() == null) return;
		HandleTouchLook();
	}

	#endregion

	#region FUNCTIONS

	private void HandleTouchLook()
	{
		if (Touch.activeTouches.Count == 0)
		{
			ResetLookFinger();
			return;
		}

		// 清理已结束的手指索引（手动迭代，避免 RemoveWhere 委托产生 GC）
		_fingerRemoveBuffer.Clear();
		foreach (int idx in uiFingerIndices)
		{
			bool stillActive = false;
			var fingers = Touch.activeFingers;
			for (int i = 0; i < fingers.Count; i++)
			{
				var f = fingers[i];
				if (f.index == idx && f.currentTouch.valid &&
				    f.currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Ended &&
				    f.currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
				{
					stillActive = true;
					break;
				}


				if (f.index == idx && f.currentTouch.valid &&
				    f.currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Ended &&
				    f.currentTouch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
				{
					stillActive = true;
					break;
				}
			}
			if (!stillActive)
				_fingerRemoveBuffer.Add(idx);
		}
		for (int i = 0; i < _fingerRemoveBuffer.Count; i++)
			uiFingerIndices.Remove(_fingerRemoveBuffer[i]);

		if (lookFinger != null)
		{
			var touch = lookFinger.currentTouch;
			if (!touch.valid
			    || touch.phase == UnityEngine.InputSystem.TouchPhase.Ended
			    || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
			{
				ResetLookFinger();
			}
			else
			{
				Vector2 currentPosition = touch.screenPosition;
				if (hasLastLookPosition)
					ApplyLookDelta(currentPosition - lastLookPosition);

				lastLookPosition = currentPosition;
				hasLastLookPosition = true;
			}
		}
		else
		{
			foreach (var finger in Touch.activeFingers)
			{
				var touch = finger.currentTouch;

				// Began 阶段：标记"非 shoot 按钮"的 UI 手指，永久排除
				if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
				{
					if (IsBlockingUI(touch.screenPosition))
					{
						uiFingerIndices.Add(finger.index);
						continue;
					}
				}

				// 排除已在 UI 上按下的手指
				if (uiFingerIndices.Contains(finger.index))
					continue;

				if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began
				    && touch.valid)
				{
					lookFinger = finger;
					lastLookPosition = touch.screenPosition;
					hasLastLookPosition = true;
					break;
				}
			}
		}
	}

	private void ApplyLookDelta(Vector2 delta)
	{
		if (GameManager.Instance == null) return;

		float x = delta.x * sensitivityX;
		float y = delta.y * sensitivityY * (invertY ? -1.0f : 1.0f);
		EventManager.Instance.GetCharacter()?.OnLook(new Vector2(x, y));
	}

	private void ResetLookFinger()
	{
		lookFinger = null;
		hasLastLookPosition = false;

		// OnDisable 时 GameManager 可能已被销毁，判空保护
		if (GameManager.Instance == null) return;
			EventManager.Instance.GetCharacter()?.OnLook(Vector2.zero);
	}

	/// <summary>
	/// 判断屏幕位置是否在需要屏蔽视角控制的 UI 上。
	/// shoot 按钮不屏蔽（允许按住开火的同时拖动瞄准），
	/// 其他 UI（摇杆、换弹、Buff 等）全部屏蔽。
	/// </summary>
	private bool IsBlockingUI(Vector2 screenPos)
	{
		EventSystem eventSystem = EventSystem.current;
		if (eventSystem == null) return false;

		if (cachedEventSystem != eventSystem || cachedPointerEventData == null)
		{
			cachedEventSystem = eventSystem;
			cachedPointerEventData = new PointerEventData(eventSystem);
		}

		cachedPointerEventData.position = screenPos;
		uiRaycastResults.Clear();
		eventSystem.RaycastAll(cachedPointerEventData, uiRaycastResults);

		if (uiRaycastResults.Count == 0)
			return false;   // 没有命中任何 UI → 不屏蔽

		// 命中 UI，检查最上层是否是 shoot 按钮
		GameObject topObject = uiRaycastResults[0].gameObject;
		ButtonClick btn = topObject.GetComponentInParent<ButtonClick>();
		if (btn != null && btn.Type == ButtonType.shoot)
			return false;   // shoot 按钮 → 不屏蔽，允许视角控制

		return true;        // 其他 UI → 屏蔽
	}

	#endregion
}

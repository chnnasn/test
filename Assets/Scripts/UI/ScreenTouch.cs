using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// 移动端触摸拖动控制摄像机视角旋转（基于 Input System EnhancedTouch）。
/// 按住屏幕任意位置拖动 → 使用 EnhancedTouch 的帧增量 → 调用 Character.OnLook(Vector2)，
/// 与鼠标输入走完全相同的 axisLook → CameraLook 管线。
/// 仅当触摸在 UI 上时不处理，交由 UI 事件系统。
/// </summary>
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

	private void Start()
	{
		if (GameManager.Instance == null || GameManager.Instance.GetCharacter() == null)
		{
			Debug.LogWarning("[ScreenTouch] 未找到 Character 组件。");
			return;
		}

		GameManager.Instance.GetCharacter().SetCursorLocked(true);
	}

	private void Update()
	{
		if (GameManager.Instance == null || GameManager.Instance.GetCharacter() == null) return;
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
				if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began
				    && touch.valid
				    && !IsPointerOverUI(touch.screenPosition))
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
		GameManager.Instance.GetCharacter()?.OnLook(new Vector2(x, y));
	}

	private void ResetLookFinger()
	{
		lookFinger = null;
		hasLastLookPosition = false;

		// OnDisable 时 GameManager 可能已被销毁，判空保护
		if (GameManager.Instance == null) return;
		GameManager.Instance.GetCharacter()?.OnLook(Vector2.zero);
	}

	/// <summary>
	/// 判断屏幕位置是否在 UI 上。触碰到 UI 时不处理视角旋转。
	/// </summary>
	private bool IsPointerOverUI(Vector2 screenPos)
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
		return uiRaycastResults.Count > 0;
	}

	#endregion
}

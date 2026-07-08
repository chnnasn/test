using InfimaGames.LowPolyShooterPack;
using UnityEngine;

/// <summary>
/// 移动端触摸拖动控制摄像机视角旋转。
/// 屏幕右半区域单指拖动 → 旋转视角（水平Yaw + 垂直Pitch）。
/// 与左侧 JoyStick 移动摇杆自然分区，互不干扰。
/// </summary>
public class ScreenTouch : MonoBehaviour
{
    #region FIELDS SERIALIZED

    [Header("灵敏度")]
    [Tooltip("水平（Yaw）旋转灵敏度。")]
    [SerializeField]
    private float sensitivityX = 2.0f;

    [Tooltip("垂直（Pitch）旋转灵敏度。")]
    [SerializeField]
    private float sensitivityY = 2.0f;

    [Tooltip("是否反转垂直方向。")]
    [SerializeField]
    private bool invertY = false;

    [Header("俯仰限制")]
    [Tooltip("垂直旋转最小角度（向下看）。")]
    [SerializeField]
    private float pitchMin = -80f;

    [Tooltip("垂直旋转最大角度（向上看）。")]
    [SerializeField]
    private float pitchMax = 80f;

    [Header("分区")]
    [Tooltip("是否启用屏幕左右分区。右侧用于视角旋转，左侧留给摇杆。")]
    [SerializeField]
    private bool splitScreen = true;

    [Tooltip("屏幕分区的比例，0.5 表示从屏幕正中间分割。")]
    [SerializeField]
    [Range(0.1f, 0.9f)]
    private float splitRatio = 0.5f;

    [Header("引用（可选，留空则自动查找）")]
    [Tooltip("玩家 Transform，用于水平旋转（Yaw）。留空则自动从场景中查找 Character。")]
    [SerializeField]
    private Transform playerTransform;

    [Tooltip("摄像机 Transform，用于垂直旋转（Pitch）。留空则使用 Camera.main。")]
    [SerializeField]
    private Transform cameraTransform;

    #endregion

    #region FIELDS

    /// <summary>
    /// 当前用于视角旋转的手指ID。为null表示没有手指在控制视角。
    /// </summary>
    private int? lookFingerId = null;

    /// <summary>
    /// 上一帧的触摸位置，用于计算增量。
    /// </summary>
    private Vector2 lastTouchPosition;

    /// <summary>
    /// 当前累积的俯仰角（度）。缓存数值避免从欧拉角反算时的万向节问题。
    /// </summary>
    private float currentPitch;

    #endregion

    #region UNITY

    private void Start()
    {
        ResolveReferences();
        CacheInitialPitch();
    }

    private void Update()
    {
        HandleTouchLook();
    }

    #endregion

    #region FUNCTIONS

    /// <summary>
    /// 自动查找并绑定必要的 Transform 引用。
    /// </summary>
    private void ResolveReferences()
    {
        // 玩家 Transform：优先使用序列化字段，否则从场景中查找 Character 组件
        if (playerTransform == null)
        {
            var character = FindFirstObjectByType<Character>();
            if (character != null)
                playerTransform = character.transform;
        }

        // 摄像机 Transform：优先使用序列化字段，否则使用 Camera.main
        if (cameraTransform == null)
        {
            var mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        if (playerTransform == null)
            Debug.LogWarning("[ScreenTouch] 未找到玩家 Transform，Yaw 旋转将不会生效。");
        if (cameraTransform == null)
            Debug.LogWarning("[ScreenTouch] 未找到摄像机 Transform，Pitch 旋转将不会生效。");
    }

    /// <summary>
    /// 从摄像机当前旋转中缓存初始俯仰角。
    /// </summary>
    private void CacheInitialPitch()
    {
        if (cameraTransform != null)
        {
            // 使用 localEulerAngles 而非 eulerAngles，因为 Pitch 是相机本地旋转
            float angle = cameraTransform.localEulerAngles.x;
            // 将 0~360 角度转换为 -180~180
            if (angle > 180f) angle -= 360f;
            currentPitch = Mathf.Clamp(angle, pitchMin, pitchMax);
        }
    }

    /// <summary>
    /// 主触摸处理逻辑。遍历所有触摸点，找到属于视角控制的触摸并应用旋转。
    /// </summary>
    private void HandleTouchLook()
    {
        // 没有任何触摸时，重置视角手指ID
        if (Input.touchCount == 0)
        {
            lookFingerId = null;
            return;
        }

        // 遍历所有触摸点
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.touches[i];
            bool isLookSide = IsLookSide(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // 触摸开始且位于视角区域、且当前没有其他手指在控制视角
                    if (isLookSide && lookFingerId == null)
                    {
                        lookFingerId = touch.fingerId;
                        lastTouchPosition = touch.position;
                    }
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    // 当前手指正在控制视角时，计算增量并旋转
                    if (touch.fingerId == lookFingerId)
                    {
                        Vector2 delta = touch.deltaPosition;
                        if (delta.sqrMagnitude > 0.001f)
                        {
                            ApplyLookRotation(delta);
                        }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // 手指抬起或取消，释放视角控制权
                    if (touch.fingerId == lookFingerId)
                        lookFingerId = null;
                    break;
            }
        }
    }

    /// <summary>
    /// 判断触摸位置是否属于视角控制区域（屏幕右侧）。
    /// </summary>
    private bool IsLookSide(Vector2 screenPos)
    {
        if (!splitScreen) return true;
        return screenPos.x > Screen.width * splitRatio;
    }

    /// <summary>
    /// 应用视角旋转。
    /// 水平拖动 → Yaw（旋转玩家）
    /// 垂直拖动 → Pitch（倾斜摄像机）
    /// </summary>
    private void ApplyLookRotation(Vector2 delta)
    {
        // 应用灵敏度。注意：Touch.deltaPosition 已经是帧间增量，
        // 此处不使用 Time.deltaTime 以避免帧率不同导致的灵敏度不一致。
        float yawDelta = delta.x * sensitivityX;
        float pitchDelta = delta.y * sensitivityY;

        if (invertY)
            pitchDelta = -pitchDelta;

        // 水平旋转：旋转玩家 Transform（Yaw），摄像机如果是其子物体将自然跟随
        if (playerTransform != null)
            playerTransform.Rotate(Vector3.up, yawDelta, Space.World);

        // 垂直旋转：更新累积俯仰角并应用到摄像机本地旋转
        if (cameraTransform != null)
        {
            currentPitch -= pitchDelta;
            currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

            Vector3 localEuler = cameraTransform.localEulerAngles;
            cameraTransform.localEulerAngles = new Vector3(currentPitch, localEuler.y, localEuler.z);
        }
    }

    #endregion
}

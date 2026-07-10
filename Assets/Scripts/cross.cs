using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class Cross : MonoBehaviour
{
	#region 引用

	[Header("准星部件")]
	[SerializeField] private CanvasGroup crosshairCanvasGroup;
	[SerializeField] private RectTransform centerDot;
	[SerializeField] private RectTransform topPart;
	[SerializeField] private RectTransform bottomPart;
	[SerializeField] private RectTransform leftPart;
	[SerializeField] private RectTransform rightPart;

	#endregion

	#region 参数

	[Header("散布精度缩放")]
	[Tooltip("将武器散布值转换为UI像素偏移的缩放系数。视觉 spread = 武器 spread × 此系数")]
	[SerializeField] private float spreadVisualScale = 200f;

	[Header("跑步")]
	[Tooltip("跑步时额外散布占武器散布的比例")]
	[SerializeField, Range(0, 1)] private float runSpreadRatio = 0.4f;
	[SerializeField] private float runAlpha = 0.35f;         // 跑步时透明度

	[Header("静止")]
	[SerializeField] private float defaultAlpha = 1f;        // 静止时透明度

	[Header("射击散布")]
	[Tooltip("射击散布随时间累积的曲线")]
	[SerializeField] private AnimationCurve fireSpreadCurve = AnimationCurve.Linear(0, 0, 1, 1);
	[Tooltip("达到最大散布所需时间（秒）")]
	[SerializeField] private float fireBuildUpTime = 1.2f;
	[Tooltip("松手后恢复到默认的时间（秒）")]
	[SerializeField] private float fireRecoveryTime = 0.5f;

	[Header("平滑")]
	[SerializeField, Range(0.01f, 0.5f)] private float smoothTime = 0.08f;

	#endregion

	#region 运行时状态

	// ---- 通过 GenericProperty 从 Character 监听的状态 ----
	private bool _isAiming;
	private bool _isRunning;
	private bool _isFiring;

	// ---- 武器散布参数（从 Character 的武器动态读取） ----
	private float _currentWeaponSpread;

	// ---- 插值中间量 ----
	private float _currentExtraSpread;
	private float _currentAlpha;
	private float _spreadVelocity;
	private float _alphaVelocity;

	// ---- 射击计时 ----
	private float _fireAccumulatedTime;

	// ---- 各部件默认基准位置（prefab 中设计好的静止位置） ----
	private Vector3 _topBasePos, _bottomBasePos, _leftBasePos, _rightBasePos;

	// ---- Character 引用 ----
	private Character _character;

	#endregion

	#region 公共接口

	/// <summary>
	/// 手动刷新武器散布参数（武器切换时由 Character 通知调用）。
	/// </summary>
	public void RefreshWeaponSpread()
	{
		if (_character != null)
			_currentWeaponSpread = _character.GetCurrentWeaponSpread();
	}

	#endregion

	#region Unity 生命周期

	private void Start()
	{
		CacheDefaultPositions();

		// 查找 Character 组件
		_character = GameManager.Instance.GetCharacter();
		if (_character != null)
		{
			// 订阅 GenericProperty 状态变化
			_character.IsAimingProp.OnValueChanged += OnAimingChanged;
			_character.IsRunningProp.OnValueChanged += OnRunningChanged;
			_character.IsFiringProp.OnValueChanged += OnFiringChanged;

			// 读取初始武器散布
			RefreshWeaponSpread();
		}

		_currentExtraSpread = 0f;
		_currentAlpha = defaultAlpha;
	}

	private void OnDestroy()
	{
		// 解绑 GenericProperty 回调
		if (_character != null)
		{
			_character.IsAimingProp.OnValueChanged -= OnAimingChanged;
			_character.IsRunningProp.OnValueChanged -= OnRunningChanged;
			_character.IsFiringProp.OnValueChanged -= OnFiringChanged;
		}
	}

	private void Update()
	{
		// 每帧动态读取武器散布值（武器切换时自动更新）
		if (_character != null)
			_currentWeaponSpread = _character.GetCurrentWeaponSpread();

		ComputeTargets(out float targetSpread, out float targetAlpha);
		SmoothValues(targetSpread, targetAlpha);
		ApplyToParts();
	}

	#endregion

	#region GenericProperty 回调 —— 由 Character 自动触发

	private void OnAimingChanged(bool aiming) => _isAiming = aiming;
	private void OnRunningChanged(bool running) => _isRunning = running;
	private void OnFiringChanged(bool firing) => _isFiring = firing;

	#endregion

	#region 核心逻辑

	/// <summary>
	/// 缓存各部件预制体中的默认 localPosition，
	/// 后续以 transform.right（自身局部 X 轴）追加 spread 增量。
	/// </summary>
	private void CacheDefaultPositions()
	{
		if (topPart)    _topBasePos    = topPart.localPosition;
		if (bottomPart) _bottomBasePos = bottomPart.localPosition;
		if (leftPart)   _leftBasePos   = leftPart.localPosition;
		if (rightPart)  _rightBasePos  = rightPart.localPosition;
	}

	/// <summary>
	/// 根据当前状态与武器散布参数，计算目标散布增量和透明度。
	/// 优先级：瞄准 > 跑步 > 射击 > 静止（默认）
	/// </summary>
	private void ComputeTargets(out float targetSpread, out float targetAlpha)
	{
		// 动态计算当前武器的视觉散布值
		float maxFireSpread = _currentWeaponSpread * spreadVisualScale;
		float runExtraSpread = maxFireSpread * runSpreadRatio;

		// ---------- 瞄准（最高优先级） ----------
		if (_isAiming)
		{
			targetSpread = 0f;
			targetAlpha  = 0f;
			_fireAccumulatedTime = 0f;   // 进入瞄准时重置射击累积
			return;
		}

		// ---------- 跑步 ----------
		if (_isRunning)
		{
			targetSpread = runExtraSpread;
			targetAlpha  = runAlpha;
			_fireAccumulatedTime = 0f;
			return;
		}

		// ---------- 射击 / 静止 ----------
		if (_isFiring)
		{
			// 持续射击：逐步累积时间
			_fireAccumulatedTime += Time.deltaTime;
			float t = Mathf.Clamp01(_fireAccumulatedTime / fireBuildUpTime);
			float spreadFromFire = fireSpreadCurve.Evaluate(t) * maxFireSpread;
			targetSpread = spreadFromFire;
			targetAlpha  = defaultAlpha;
		}
		else
		{
			// 非射击状态：快速回退射击累积
			if (_fireAccumulatedTime > 0f)
			{
				float recoverySpeed = fireBuildUpTime / fireRecoveryTime;
				_fireAccumulatedTime -= Time.deltaTime * recoverySpeed;
				_fireAccumulatedTime = Mathf.Max(0f, _fireAccumulatedTime);
			}

			float t = Mathf.Clamp01(_fireAccumulatedTime / fireBuildUpTime);
			float residualSpread = fireSpreadCurve.Evaluate(t) * maxFireSpread;
			targetSpread = residualSpread;
			targetAlpha  = defaultAlpha;
		}
	}

	/// <summary>
	/// SmoothDamp 平滑过渡 spread 和 alpha。
	/// </summary>
	private void SmoothValues(float targetSpread, float targetAlpha)
	{
		_currentExtraSpread = Mathf.SmoothDamp(
			_currentExtraSpread, targetSpread,
			ref _spreadVelocity, smoothTime);

		_currentAlpha = Mathf.SmoothDamp(
			_currentAlpha, targetAlpha,
			ref _alphaVelocity, smoothTime);
	}

	/// <summary>
	/// 将计算结果应用到每个准星部件。
	/// 每个部件沿自身局部 X 轴（transform.right）移动，
	/// X 正方向 = 远离中心。
	/// </summary>
	private void ApplyToParts()
	{
		float spread = _currentExtraSpread;

		// 部件沿自身局部 X 轴移动 —— transform.right 即本地 X+ 方向
		if (topPart)    topPart.localPosition    = _topBasePos    + topPart.right    * spread;
		if (bottomPart) bottomPart.localPosition = _bottomBasePos + bottomPart.right * spread;
		if (leftPart)   leftPart.localPosition   = _leftBasePos   + leftPart.right   * spread;
		if (rightPart)  rightPart.localPosition  = _rightBasePos  + rightPart.right  * spread;

		// 透明度
		if (crosshairCanvasGroup)
			crosshairCanvasGroup.alpha = _currentAlpha;

		// 中心点：当 alpha ≈ 0 时也跟随隐藏
		if (centerDot)
			centerDot.gameObject.SetActive(_currentAlpha > 0.01f);
	}

	#endregion
}

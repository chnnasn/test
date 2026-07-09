using UnityEngine;

/// <summary>
/// 准星系统 —— 通过 EventManager → UIManager 调度控制。
/// 
/// 状态行为：
///   瞄准(ADS)   → 完全隐藏（用瞄具不需要准星）
///   跑步        → 放大 + 半透明（模拟真实枪械晃动）
///   静止站立     → 默认大小，完全可见
///   连续射击     → 逐步放大（AnimationCurve 控制散布扩散）
///
/// 所有准星部件（上下左右）只沿自身 X 轴移动，
/// X 轴正方向 = 该部件指向中心的反方向（即远离中心）。
/// 过渡使用 Mathf.SmoothDamp（Lerp + Spring）平滑处理。
/// </summary>
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

    [Header("跑步")]
    [SerializeField] private float runExtraSpread = 30f;    // 跑步时额外散布增量
    [SerializeField] private float runAlpha = 0.35f;         // 跑步时透明度

    [Header("静止")]
    [SerializeField] private float defaultAlpha = 1f;        // 静止时透明度

    [Header("射击散布")]
    [SerializeField] private AnimationCurve fireSpreadCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float maxFireSpread = 80f;      // 最大射击散布增量
    [SerializeField] private float fireBuildUpTime = 1.2f;   // 达到最大散布所需时间
    [SerializeField] private float fireRecoveryTime = 0.5f;  // 松手后恢复到默认的时间

    [Header("平滑")]
    [SerializeField, Range(0.01f, 0.5f)] private float smoothTime = 0.08f;

    #endregion

    #region 运行时状态

    // ---- 输入状态（由 UIManager 设置） ----
    private bool _isAiming;
    private bool _isRunning;
    private bool _isFiring;

    // ---- 插值中间量 ----
    private float _currentExtraSpread;
    private float _currentAlpha;
    private float _spreadVelocity;
    private float _alphaVelocity;

    // ---- 射击计时 ----
    private float _fireAccumulatedTime;

    // ---- 各部件默认基准位置（prefab 中设计好的静止位置） ----
    private Vector3 _topBasePos, _bottomBasePos, _leftBasePos, _rightBasePos;

    #endregion

    #region 公共接口 —— 由 UIManager 调用

    public void SetAiming(bool aiming) => _isAiming = aiming;
    public void SetRunning(bool running) => _isRunning = running;
    public void SetFiring(bool firing) => _isFiring = firing;

    #endregion

    #region Unity 生命周期

    private void Start()
    {
        CacheDefaultPositions();
        _currentExtraSpread = 0f;
        _currentAlpha = defaultAlpha;
    }

    private void Update()
    {
        ComputeTargets(out float targetSpread, out float targetAlpha);
        SmoothValues(targetSpread, targetAlpha);
        ApplyToParts();
    }

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
    /// 根据当前状态计算机目标散布增量和透明度。
    /// 优先级：瞄准 > 跑步 > 射击 > 静止（默认）
    /// </summary>
    private void ComputeTargets(out float targetSpread, out float targetAlpha)
    {
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

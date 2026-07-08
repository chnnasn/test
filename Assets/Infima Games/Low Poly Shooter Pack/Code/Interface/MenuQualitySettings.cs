//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 画质设置菜单。提供六档画质切换（极低/低/中/高/极高/超高）、后期处理开关控制、
    /// 以及重新开始和退出游戏功能。通过鼠标锁定状态自动显示/隐藏菜单，并伴随动画过渡。
    /// </summary>
    public class MenuQualitySettings : Element
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("用于播放动画的画布对象。")]
        [SerializeField]
        private GameObject animatedCanvas;

        [Tooltip("显示此菜单时播放的动画剪辑。")]
        [SerializeField]
        private AnimationClip animationShow;

        [Tooltip("隐藏此菜单时播放的动画剪辑。")]
        [SerializeField]
        private AnimationClip animationHide;

        #endregion

        #region FIELDS

        /// <summary>
        /// 动画组件引用。
        /// </summary>
        private Animation animationComponent;
        /// <summary>
        /// 如果为true，表示菜单已启用并正在正常显示。
        /// </summary>
        private bool menuIsEnabled;

        /// <summary>
        /// 主后期处理Volume（全局后期处理效果）。
        /// </summary>
        private PostProcessVolume postProcessingVolume;
        /// <summary>
        /// 瞄准镜后期处理Volume（瞄准时的后期处理效果）。
        /// </summary>
        private PostProcessVolume postProcessingVolumeScope;

        /// <summary>
        /// 景深效果设置引用。
        /// </summary>
        private DepthOfField depthOfField;

        #endregion

        #region UNITY

        private void Start()
        {
            //启动时隐藏菜单（透明度设为0）。
            animatedCanvas.GetComponent<CanvasGroup>().alpha = 0;
            //获取画布的动画组件。
            animationComponent = animatedCanvas.GetComponent<Animation>();

            //在场景中查找并引用后期处理Volume。
            postProcessingVolume = GameObject.Find("Post Processing Volume")?.GetComponent<PostProcessVolume>();
            postProcessingVolumeScope = GameObject.Find("Post Processing Volume Scope")?.GetComponent<PostProcessVolume>();

            //从主后期处理Volume中获取景深设置。
            if(postProcessingVolume != null)
                postProcessingVolume.profile.TryGetSettings(out depthOfField);
        }

        /// <summary>
        /// 每帧检测鼠标锁定状态变化，自动显示或隐藏菜单。
        /// 鼠标解锁（暂停菜单打开）时显示画质设置菜单并启用景深效果；
        /// 鼠标锁定（游戏中）时隐藏菜单并禁用景深效果。
        /// </summary>
        protected override void Tick()
        {
            //根据鼠标锁定状态切换菜单的显示/隐藏。
            bool cursorLocked = characterBehaviour.IsCursorLocked();
            switch (cursorLocked)
            {
                //鼠标锁定且菜单当前可见 → 隐藏菜单。
                case true when menuIsEnabled:
                    Hide();
                    break;
                //鼠标解锁且菜单当前不可见 → 显示菜单。
                case false when !menuIsEnabled:
                    Show();
                    break;
            }
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 通过播放动画来显示菜单，同时启用景深效果。
        /// </summary>
        private void Show()
        {
            //标记菜单为已启用。
            menuIsEnabled = true;

            //设置并播放显示动画。
            animationComponent.clip = animationShow;
            animationComponent.Play();

            //启用景深效果（菜单显示时模糊背景）。
            if(depthOfField != null)
                depthOfField.active = true;
        }
        /// <summary>
        /// 通过播放动画来隐藏菜单，同时禁用景深效果。
        /// </summary>
        private void Hide()
        {
            //标记菜单为已禁用。
            menuIsEnabled = false;

            //设置并播放隐藏动画。
            animationComponent.clip = animationHide;
            animationComponent.Play();

            //禁用景深效果（恢复清晰画面）。
            if(depthOfField != null)
                depthOfField.active = false;
        }

        /// <summary>
        /// 设置后期处理Volume是否启用。
        /// </summary>
        /// <param name="value">true启用，false禁用。</param>
        private void SetPostProcessingState(bool value = true)
        {
            //切换主Volume和瞄准镜Volume的启用状态。
            if(postProcessingVolume != null)
                postProcessingVolume.enabled = value;
            if(postProcessingVolumeScope != null)
                postProcessingVolumeScope.enabled = value;
        }

        /// <summary>
        /// 设置画质为极低（QualityLevel 0），并禁用后期处理以提高性能。
        /// </summary>
        public void SetQualityVeryLow()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(0);
            //禁用后期处理。
            SetPostProcessingState(false);
        }
        /// <summary>
        /// 设置画质为低（QualityLevel 1），并禁用后期处理以提高性能。
        /// </summary>
        public void SetQualityLow()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(1);
            //禁用后期处理。
            SetPostProcessingState(false);
        }

        /// <summary>
        /// 设置画质为中（QualityLevel 2），并启用后期处理。
        /// </summary>
        public void SetQualityMedium()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(2);
            //启用后期处理。
            SetPostProcessingState();
        }
        /// <summary>
        /// 设置画质为高（QualityLevel 3），并启用后期处理。
        /// </summary>
        public void SetQualityHigh()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(3);
            //启用后期处理。
            SetPostProcessingState();
        }

        /// <summary>
        /// 设置画质为极高（QualityLevel 4），并启用后期处理。
        /// </summary>
        public void SetQualityVeryHigh()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(4);
            //启用后期处理。
            SetPostProcessingState();
        }
        /// <summary>
        /// 设置画质为超高（QualityLevel 5），并启用后期处理。
        /// </summary>
        public void SetQualityUltra()
        {
            //设置画质等级。
            QualitySettings.SetQualityLevel(5);
            //启用后期处理。
            SetPostProcessingState();
        }

        /// <summary>
        /// 重新开始当前关卡。在编辑器中通过EditorSceneManager加载，在构建版本中通过SceneManager加载。
        /// </summary>
        public void Restart()
        {
            //获取当前活动场景的路径。
            string sceneToLoad = SceneManager.GetActiveScene().path;

            #if UNITY_EDITOR
            //编辑器模式下异步重新加载场景。
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
            #else
            //构建版本中异步重新加载场景。
            SceneManager.LoadSceneAsync(sceneToLoad, new LoadSceneParameters(LoadSceneMode.Single));
            #endif
        }
        /// <summary>
        /// 退出游戏应用程序。
        /// </summary>
        public void Quit()
        {
            //退出应用。
            Application.Quit();
        }

        #endregion
    }
}
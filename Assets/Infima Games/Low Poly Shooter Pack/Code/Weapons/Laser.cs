//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 激光配件类型枚举。用于区分激光配件的功能：激光瞄准器或手电筒。
    /// </summary>
    public enum LaserType { Lasersight, Flashlight }

    /// <summary>
    /// 激光配件类。代表武器上的激光/手电配件，支持切换开关、射线追踪显示。
    /// 激光光束会在Update中实时检测射程并调整光束长度。支持在奔跑/瞄准时自动关闭。
    /// </summary>
    public class Laser : LaserBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("激光配件精灵图标，显示在玩家的界面中。")]
        [SerializeField]
        private Sprite sprite;

        [Tooltip("激光类型：激光瞄准器或手电筒。")]
        [SerializeField]
        private LaserType laserType;

        [Tooltip("勾选时，激光瞄准器将在游戏开始时处于激活状态。")]
        [SerializeField]
        private bool active = true;

        [Tooltip("勾选时，激光将在角色奔跑时自动关闭。")]
        [SerializeField]
        private bool turnOffWhileRunning = true;

        [Tooltip("勾选时，激光将在角色瞄准时自动关闭。")]
        [SerializeField]
        private bool turnOffWhileAiming = true;

        [Title(label: "音频")]

        [Tooltip("切换激光开关时播放的音频片段。")]
        [SerializeField]
        private AudioClip toggleClip;

        [Tooltip("切换音频的音频设置。")]
        [SerializeField]
        private AudioSettings toggleAudioSettings;

        [Title(label: "展开设置")]

        [Tooltip("激光的Transform变换组件。")]
        [SerializeField]
        private Transform laserTransform;

        [ShowIf("laserType", LaserType.Lasersight)]
        [Tooltip("决定激光光束的粗细程度。")]
        [SerializeField]
        private float beamThickness = 1.2f;

        [ShowIf("laserType", LaserType.Lasersight)]
        [Tooltip("激光光束追踪的最大距离。")]
        [SerializeField]
        private float beamMaxDistance = 500.0f;

        #endregion

        #region FIELDS

        /// <summary>
        /// 光束父级Transform。激光光束的实际缩放对象，用于根据射线检测结果调整光束长度。
        /// </summary>
        private Transform beamParent;

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取激光的精灵图标。
        /// </summary>
        public override Sprite GetSprite() => sprite;
        /// <summary>
        /// 获取奔跑时是否自动关闭激光。
        /// </summary>
        public override bool GetTurnOffWhileRunning() => turnOffWhileRunning;
        /// <summary>
        /// 获取瞄准时是否自动关闭激光。
        /// </summary>
        public override bool GetTurnOffWhileAiming() => turnOffWhileAiming;

        #endregion

        #region METHODS

        /// <summary>
        /// 切换激光的开关状态。反转active状态后重新应用，并播放切换音效。
        /// </summary>
        public override void Toggle()
        {
            //切换激活状态。
            active = !active;

            //激活/禁用激光GameObject。
            Reapply();

            //播放切换激光的音效！
            if(toggleClip != null)
                ServiceLocator.Current.Get<IAudioManagerService>().PlayOneShot(toggleClip, toggleAudioSettings);
        }
        /// <summary>
        /// 根据当前的active状态重新应用激光的显示/隐藏。
        /// 与Toggle不同，此方法不会改变active状态，仅刷新GameObject的激活状态。
        /// </summary>
        public override void Reapply()
        {
            //根据active状态激活/禁用激光GameObject。
            if(laserTransform != null)
                laserTransform.gameObject.SetActive(active);
        }
        /// <summary>
        /// 强制隐藏激光。
        /// </summary>
        public override void Hide()
        {
            //禁用激光GameObject。
            if(laserTransform != null)
                laserTransform.gameObject.SetActive(false);
        }

        #endregion

        #region UNITY

        /// <summary>
        /// Unity Awake生命周期。缓存光束父级Transform。
        /// </summary>
        private void Awake()
        {
            //安全检查：如果激光Transform为空则跳过。
            if (laserTransform == null)
                return;

            //缓存光束父级Transform，用于后续调整光束长度。
            beamParent = laserTransform.parent;
        }
        /// <summary>
        /// Unity Update生命周期。每帧通过射线检测计算激光光束的目标长度，并动态调整光束缩放。
        /// 如果射线没有命中任何物体，则使用最大距离作为默认缩放值。
        /// </summary>
        private void Update()
        {
            //安全检查：如果激光Transform为空则跳过。
            if (laserTransform == null)
                return;

            //目标缩放值。如果射线未命中任何物体，则使用默认的最大距离。
            float targetScale = beamMaxDistance;

            //从光束起点向前发射射线检测命中点。
            if (Physics.Raycast(new Ray(laserTransform.position, beamParent.forward), out RaycastHit hit, beamMaxDistance))
                targetScale = hit.distance * 5.0f;

            //缩放光束使其正好到达命中位置。X和Y为光束粗细，Z为光束长度。
            beamParent.localScale = new Vector3(beamThickness, beamThickness, targetScale);
        }

        #endregion
    }
}
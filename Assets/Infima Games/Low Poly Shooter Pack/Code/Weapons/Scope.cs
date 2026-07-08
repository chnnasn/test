//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 瞄准镜配件类。定义武器瞄准镜的倍率、瞄准偏移、视野缩放、准镜材质切换等功能。
    /// 在瞄准时(OnAim)恢复默认材质以看清瞄准镜，停止瞄准时(OnAimStop)使用遮挡材质隐藏镜内视野。
    /// 这种材质切换机制用于模拟真实瞄准时眼睛靠近瞄准镜才能看清的效果。
    /// </summary>
    public class Scope : ScopeBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "倍率")]

        [Tooltip("通过此瞄准镜瞄准时，鼠标灵敏度的乘数。值越小，瞄准时灵敏度越低。")]
        [SerializeField]
        private float multiplierMouseSensitivity = 0.8f;

        [Tooltip("通过此瞄准镜瞄准时，武器散布的乘数。值越小，瞄准时精度越高。")]
        [SerializeField]
        private float multiplierSpread = 0.1f;

        [Title(label: "界面")]

        [Tooltip("瞄准镜在角色界面上显示的精灵图标。")]
        [SerializeField]
        private Sprite sprite;

        [Title(label: "晃动")]

        [Tooltip("通过此瞄准镜瞄准时，武器晃动的乘数。")]
        [SerializeField]
        private float swayMultiplier = 1.0f;

        [Title(label: "瞄准偏移")]

        [Tooltip("瞄准时武器骨骼的位置偏移量。用于调整武器在瞄准状态下的位置。")]
        [SerializeField]
        private Vector3 offsetAimingLocation;

        [Tooltip("瞄准时武器骨骼的旋转偏移量。用于调整武器在瞄准状态下的角度。")]
        [SerializeField]
        private Vector3 offsetAimingRotation;

        [Title(label: "视野缩放")]

        [Tooltip("瞄准时摄像机视野的乘数。值越小，瞄准时画面越放大。")]
        [SerializeField]
        private float fieldOfViewMultiplierAim = 0.9f;

        [Tooltip("瞄准时武器专用摄像机的视野乘数。")]
        [SerializeField]
        private float fieldOfViewMultiplierAimWeapon = 0.7f;

        [Title(label: "材质")]

        [Tooltip("瞄准镜材质的索引。该索引对应的材质在不瞄准时会被隐藏。")]
        [SerializeField]
        private int materialIndex = 3;

        [Tooltip("用于在不瞄准时遮挡瞄准镜视野的材质。")]
        [SerializeField]
        private Material materialHidden;

        #endregion

        #region FIELDS

        /// <summary>
        /// 瞄准镜的MeshRenderer组件缓存。
        /// </summary>
        private MeshRenderer meshRenderer;
        /// <summary>
        /// 默认的瞄准镜材质。缓存以便随时重新应用，因为在运行时材质通常会被替换。
        /// </summary>
        private Material materialDefault;

        #endregion

        #region UNITY

        /// <summary>
        /// Unity Awake生命周期。缓存MeshRenderer组件和默认材质。
        /// </summary>
        private void Awake()
        {
            //缓存瞄准镜的MeshRenderer组件。
            meshRenderer = GetComponentInChildren<MeshRenderer>();

            //确保材质索引在有效范围内。
            if (!HasMaterialIndex())
                return;

            //缓存默认材质以便后续恢复。
            materialDefault = meshRenderer.materials[materialIndex];
        }
        /// <summary>
        /// Unity Start生命周期。初始化为停止瞄准状态（隐藏瞄准镜视野）。
        /// </summary>
        private void Start()
        {
            //从默认状态（不瞄准）开始。
            OnAimStop();
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 获取鼠标灵敏度乘数。
        /// </summary>
        public override float GetMultiplierMouseSensitivity() => multiplierMouseSensitivity;
        /// <summary>
        /// 获取散布乘数。
        /// </summary>
        public override float GetMultiplierSpread() => multiplierSpread;

        /// <summary>
        /// 获取瞄准时的位置偏移量。
        /// </summary>
        public override Vector3 GetOffsetAimingLocation() => offsetAimingLocation;
        /// <summary>
        /// 获取瞄准时的旋转偏移量。
        /// </summary>
        public override Vector3 GetOffsetAimingRotation() => offsetAimingRotation;

        /// <summary>
        /// 获取瞄准时摄像机视野乘数。
        /// </summary>
        public override float GetFieldOfViewMultiplierAim() => fieldOfViewMultiplierAim;
        /// <summary>
        /// 获取瞄准时武器摄像机视野乘数。
        /// </summary>
        public override float GetFieldOfViewMultiplierAimWeapon() => fieldOfViewMultiplierAimWeapon;

        /// <summary>
        /// 获取瞄准镜的精灵图标。
        /// </summary>
        public override Sprite GetSprite() => sprite;
        /// <summary>
        /// 获取武器晃动乘数。
        /// </summary>
        public override float GetSwayMultiplier() => swayMultiplier;

        /// <summary>
        /// 检查MeshRenderer是否有指定索引的材质。防止材质索引越界。
        /// </summary>
        /// <returns>如果索引在有效范围内则返回true</returns>
        private bool HasMaterialIndex()
        {
            //空检查。
            if (meshRenderer == null)
                return false;

            //确保材质索引在有效范围内（非负且小于材质数组长度）。
            return materialIndex < meshRenderer.materials.Length && materialIndex >= 0;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// 瞄准事件。将指定索引处的材质恢复为默认透明材质，使玩家能看到瞄准镜内部。
        /// </summary>
        public override void OnAim()
        {
            //确保材质索引在有效范围内。
            if (!HasMaterialIndex())
                return;

            //获取当前材质数组。
            Material[] materials = meshRenderer.materials;
            //恢复为默认材质。
            materials[materialIndex] = materialDefault;
            //更新材质数组到MeshRenderer。
            meshRenderer.materials = materials;
        }
        /// <summary>
        /// 停止瞄准事件。将指定索引处的材质替换为遮挡材质，隐藏瞄准镜内部视野。
        /// </summary>
        public override void OnAimStop()
        {
            //确保材质索引在有效范围内。
            if (!HasMaterialIndex())
                return;

            //获取当前材质数组。
            Material[] materials = meshRenderer.materials;
            //替换为遮挡材质以隐藏瞄准镜视野。
            materials[materialIndex] = materialHidden;
            //更新材质数组到MeshRenderer。
            meshRenderer.materials = materials;
        }

        #endregion
    }
}
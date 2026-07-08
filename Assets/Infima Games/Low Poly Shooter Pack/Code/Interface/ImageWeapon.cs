//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using UnityEngine.UI;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 武器图标组件。负责根据当前装备的武器及其配件（枪身、握把、激光、消音器、弹匣、瞄准镜），
    /// 为对应的Image组件分配正确的Sprite。每帧检测配件变化并更新图标显示。
    /// </summary>
    public class ImageWeapon : Element
    {
        #region FIELDS SERIALIZED

        [Title(label: "颜色")]

        [Tooltip("应用于所有武器图标Image的颜色。")]
        [SerializeField]
        private Color imageColor = Color.white;

        [Title(label: "设置")]

        [Tooltip("武器枪身图标。")]
        [SerializeField]
        private Image imageWeaponBody;

        [Tooltip("武器握把图标。")]
        [SerializeField]
        private Image imageWeaponGrip;

        [Tooltip("武器激光指示器图标。")]
        [SerializeField]
        private Image imageWeaponLaser;

        [Tooltip("武器消音器/枪口图标。")]
        [SerializeField]
        private Image imageWeaponMuzzle;

        [Tooltip("武器弹匣图标。")]
        [SerializeField]
        private Image imageWeaponMagazine;

        [Tooltip("武器瞄准镜图标。")]
        [SerializeField]
        private Image imageWeaponScope;

        [Tooltip("武器默认瞄准镜图标。")]
        [SerializeField]
        private Image imageWeaponScopeDefault;

        #endregion

        #region FIELDS

        /// <summary>
        /// 武器配件管理器。用于获取当前装备的各配件行为组件。
        /// </summary>
        private WeaponAttachmentManagerBehaviour attachmentManagerBehaviour;

        #endregion

        #region METHODS

        /// <summary>
        /// 每帧更新：为当前武器的所有配件图标分配正确的Sprite。
        /// 对于每个配件（默认镜、瞄准镜、弹匣、激光、握把、枪口），
        /// 先从配件管理器获取对应的配件行为组件，再获取其Sprite并赋值。
        /// 如果配件不存在或与默认配件相同，则隐藏对应的图标。
        /// </summary>
        protected override void Tick()
        {
            //统一设置所有Image的颜色和透明度。
            Color toAssign = imageColor;
            foreach (Image image in GetComponents<Image>())
                image.color = toAssign;

            //获取配件管理器。
            attachmentManagerBehaviour = equippedWeaponBehaviour.GetAttachmentManager();
            //更新武器枪身Sprite。
            imageWeaponBody.sprite = equippedWeaponBehaviour.GetSpriteBody();

            //用于临时存储当前配件Sprite的变量。
            Sprite sprite = default;

            //获取默认瞄准镜配件并设置图标。
            ScopeBehaviour scopeDefaultBehaviour = attachmentManagerBehaviour.GetEquippedScopeDefault();
            //获取Sprite。
            if (scopeDefaultBehaviour != null)
                sprite = scopeDefaultBehaviour.GetSprite();
            //分配Sprite（如果默认镜不存在则隐藏图标）。
            AssignSprite(imageWeaponScopeDefault, sprite, scopeDefaultBehaviour == null);

            //获取当前装备的瞄准镜配件并设置图标。
            ScopeBehaviour scopeBehaviour = attachmentManagerBehaviour.GetEquippedScope();
            //获取Sprite。
            if (scopeBehaviour != null)
                sprite = scopeBehaviour.GetSprite();
            //分配Sprite（如果瞄准镜不存在或与默认镜相同则隐藏图标）。
            AssignSprite(imageWeaponScope, sprite, scopeBehaviour == null || scopeBehaviour == scopeDefaultBehaviour);

            //获取当前装备的弹匣配件并设置图标。
            MagazineBehaviour magazineBehaviour = attachmentManagerBehaviour.GetEquippedMagazine();
            //获取Sprite。
            if (magazineBehaviour != null)
                sprite = magazineBehaviour.GetSprite();
            //分配Sprite（如果弹匣不存在则隐藏图标）。
            AssignSprite(imageWeaponMagazine, sprite, magazineBehaviour == null);

            //获取当前装备的激光指示器配件并设置图标。
            LaserBehaviour laserBehaviour = attachmentManagerBehaviour.GetEquippedLaser();
            //获取Sprite。
            if (laserBehaviour != null)
                sprite = laserBehaviour.GetSprite();
            //分配Sprite（如果激光不存在则隐藏图标）。
            AssignSprite(imageWeaponLaser, sprite, laserBehaviour == null);

            //获取当前装备的握把配件并设置图标。
            GripBehaviour gripBehaviour = attachmentManagerBehaviour.GetEquippedGrip();
            //获取Sprite。
            if (gripBehaviour != null)
                sprite = gripBehaviour.GetSprite();
            //分配Sprite（如果握把不存在则隐藏图标）。
            AssignSprite(imageWeaponGrip, sprite, gripBehaviour == null);

            //获取当前装备的枪口/消音器配件并设置图标。
            MuzzleBehaviour muzzleBehaviour = attachmentManagerBehaviour.GetEquippedMuzzle();
            //获取Sprite。
            if (muzzleBehaviour != null)
                sprite = muzzleBehaviour.GetSprite();
            //分配Sprite（如果枪口不存在则隐藏图标）。
            AssignSprite(imageWeaponMuzzle, sprite, muzzleBehaviour == null);
        }

        /// <summary>
        /// 为Image组件分配Sprite并控制其可见性。
        /// </summary>
        /// <param name="image">目标Image组件。</param>
        /// <param name="sprite">要分配的Sprite。</param>
        /// <param name="forceHide">是否强制隐藏（即使有Sprite也不显示）。</param>
        private static void AssignSprite(Image image, Sprite sprite, bool forceHide = false)
        {
            //更新Sprite。
            image.sprite = sprite;
            //如果Sprite为null或需要强制隐藏，则禁用Image。
            image.enabled = sprite != null && !forceHide;
        }

        #endregion
    }
}
//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 枪口配件类。定义武器枪口的开火点、火焰粒子特效和枪口闪光灯效果。
    /// 在Awake中实例化粒子系统和灯光预制体，Effect()方法在每次开火时被调用来播放特效。
    /// </summary>
    public class Muzzle : MuzzleBehaviour
    {
        #region FIELDS SERIALIZED

        [Title(label: "设置")]

        [Tooltip("枪口顶端的插槽Transform。通常用作子弹的发射点。")]
        [SerializeField]
        private Transform socket;

        [Tooltip("枪口在玩家界面上显示的精灵图标。")]
        [SerializeField]
        private Sprite sprite;

        [Tooltip("通过此枪口开火时播放的音频片段。")]
        [SerializeField]
        private AudioClip audioClipFire;

        [Title(label: "粒子效果")]

        [Tooltip("开火时生成的粒子特效预制体。")]
        [SerializeField]
        private GameObject prefabFlashParticles;

        [Tooltip("每次开火时发射的粒子数量。")]
        [SerializeField]
        private int flashParticlesCount = 5;

        [Title(label: "枪口闪光灯")]

        [Tooltip("枪口闪光预制体。开火时使用的一个小型灯光效果。")]
        [SerializeField]
        private GameObject prefabFlashLight;

        [Tooltip("闪光灯保持激活的时间。超过此时长后自动禁用。")]
        [SerializeField]
        private float flashLightDuration;

        [Tooltip("灯光在枪口上的本地偏移量。")]
        [SerializeField]
        private Vector3 flashLightOffset;

        #endregion

        #region FIELDS

        /// <summary>
        /// 实例化的粒子系统引用。
        /// </summary>
        private ParticleSystem particles;
        /// <summary>
        /// 实例化的灯光组件引用。
        /// </summary>
        private Light flashLight;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Unity Awake生命周期。实例化枪口火焰粒子系统和闪光灯预制体，并缓存引用。
        /// 粒子系统和灯光都被实例化为枪口插槽的子物体，初始时灯光处于禁用状态。
        /// </summary>
        private void Awake()
        {
            //空检查：确保粒子预制体存在。
            if(prefabFlashParticles != null)
            {
                //在枪口插槽位置实例化粒子预制体。
                GameObject spawnedParticlesPrefab = Instantiate(prefabFlashParticles, socket);
                //重置本地位置。
                spawnedParticlesPrefab.transform.localPosition = default;
                //重置本地旋转。
                spawnedParticlesPrefab.transform.localEulerAngles = default;

                //获取ParticleSystem组件引用并缓存。
                particles = spawnedParticlesPrefab.GetComponent<ParticleSystem>();
            }

            //空检查：确保闪光灯预制体存在。
            if (prefabFlashLight)
            {
                //在枪口插槽位置实例化闪光灯预制体。
                GameObject spawnedFlashLightPrefab = Instantiate(prefabFlashLight, socket);
                //应用本地位置偏移量。
                spawnedFlashLightPrefab.transform.localPosition = flashLightOffset;
                //重置本地旋转。
                spawnedFlashLightPrefab.transform.localEulerAngles = default;

                //获取Light组件引用并缓存。
                flashLight = spawnedFlashLightPrefab.GetComponent<Light>();
                //初始时禁用灯光。
                flashLight.enabled = false;
            }
        }

        #endregion

        #region GETTERS

        /// <summary>
        /// 播放枪口火焰特效。发射粒子并激活枪口闪光灯，闪光灯在持续指定时间后自动关闭。
        /// </summary>
        public override void Effect()
        {
            //尝试从枪口发射火焰粒子！
            if(particles != null)
                particles.Emit(flashParticlesCount);

            //确保有可用的闪光灯！
            if (flashLight != null)
            {
                //启用灯光。
                flashLight.enabled = true;
                //在指定秒数后禁用灯光。
                StartCoroutine(nameof(DisableLight));
            }
        }

        /// <summary>
        /// 获取枪口插槽Transform（子弹发射点）。
        /// </summary>
        public override Transform GetSocket() => socket;

        /// <summary>
        /// 获取枪口的精灵图标。
        /// </summary>
        public override Sprite GetSprite() => sprite;
        /// <summary>
        /// 获取开火音频片段。
        /// </summary>
        public override AudioClip GetAudioClipFire() => audioClipFire;

        /// <summary>
        /// 获取开火粒子系统。
        /// </summary>
        public override ParticleSystem GetParticlesFire() => particles;
        /// <summary>
        /// 获取每次开火的粒子发射数量。
        /// </summary>
        public override int GetParticlesFireCount() => flashParticlesCount;

        /// <summary>
        /// 获取枪口闪光灯Light组件。
        /// </summary>
        public override Light GetFlashLight() => flashLight;
        /// <summary>
        /// 获取闪光灯保持激活的持续时间。
        /// </summary>
        public override float GetFlashLightDuration() => flashLightDuration;

        #endregion

        #region METHODS

        /// <summary>
        /// 协程：在等待闪灯持续时间后禁用闪光灯。
        /// </summary>
        private IEnumerator DisableLight()
        {
            //等待指定的闪灯持续时间。
            yield return new WaitForSeconds(flashLightDuration);
            //禁用闪光灯。
            flashLight.enabled = false;
        }

        #endregion
    }
}
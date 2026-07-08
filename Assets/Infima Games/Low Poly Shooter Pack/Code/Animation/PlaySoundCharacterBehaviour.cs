//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 角色武器音效播放状态行为。是一个辅助StateMachineBehaviour，用于在动画状态机中
    /// 根据需要播放特定的武器或角色音效。相比PlaySoundBehaviour，此类能根据当前装备的武器
    /// 和音效类型动态选择正确的音频片段。
    /// </summary>
    public class PlaySoundCharacterBehaviour : StateMachineBehaviour
    {
        /// <summary>
        /// 武器音效类型枚举。定义了各种武器动作对应的音效类别。
        /// </summary>
        private enum SoundType
        {
            //角色动作音效。
            GrenadeThrow, Melee,
            //收枪/拔枪音效。
            Holster, Unholster,
            //普通换弹音效。
            Reload, ReloadEmpty,
            //分段换弹音效（适用于霰弹枪等逐发装填武器）。
            ReloadOpen, ReloadInsert, ReloadClose,
            //射击音效。
            Fire, FireEmpty,
            //拉栓音效。
            BoltAction
        }

        #region FIELDS SERIALIZED

        [Title(label: "Setup")]

        [Tooltip("音效播放的延迟时间（秒）。设为0则立即播放。")]
        [SerializeField]
        private float delay;

        [Tooltip("要播放的武器音效类型。根据此枚举从武器或角色获取对应的音频片段。")]
        [SerializeField]
        private SoundType soundType;

        [Title(label: "Audio Settings")]

        [Tooltip("音频播放设置（音量、空间混合等）。")]
        [SerializeField]
        private AudioSettings audioSettings = new AudioSettings(1.0f, 0.0f, true);

        #endregion

        #region FIELDS

        /// <summary>
        /// 玩家角色引用。用于获取角色级别的音效（如手雷、近战音效）。
        /// </summary>
        private CharacterBehaviour playerCharacter;

        /// <summary>
        /// 玩家背包引用。用于获取当前装备武器及武器级别的音效。
        /// </summary>
        private InventoryBehaviour playerInventory;

        /// <summary>
        /// 音频管理服务接口。处理游戏中所有音效的播放。
        /// </summary>
        private IAudioManagerService audioManagerService;

        #endregion

        #region UNITY

        /// <summary>
        /// 进入动画状态时调用。根据配置的音效类型，从角色或武器获取对应的音频片段并播放。
        /// 支持延迟播放，可用于模拟动画关键帧触发的音效。
        /// </summary>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //获取角色组件。使用??=延迟初始化，首次访问时从服务定位器获取。
            playerCharacter ??= ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();

            //获取背包组件。
            playerInventory ??= playerCharacter.GetInventory();

            //尝试获取当前装备武器的WeaponBehaviour组件。
            if (!(playerInventory.GetEquipped() is { } weaponBehaviour))
                return;

            //获取音频管理服务。
            audioManagerService ??= ServiceLocator.Current.Get<IAudioManagerService>();

            #region 根据音效类型选择正确的音频片段

            //使用switch表达式根据音效类型从不同来源获取音频片段。
            AudioClip clip = soundType switch
            {
                //手雷投掷音效 -> 从角色获取（角色级别的通用音效）。
                SoundType.GrenadeThrow => playerCharacter.GetAudioClipsGrenadeThrow().GetRandom(),
                //近战攻击音效 -> 从角色获取。
                SoundType.Melee => playerCharacter.GetAudioClipsMelee().GetRandom(),

                //收枪音效 -> 从当前装备武器获取。
                SoundType.Holster => weaponBehaviour.GetAudioClipHolster(),
                //拔枪音效 -> 从当前装备武器获取。
                SoundType.Unholster => weaponBehaviour.GetAudioClipUnholster(),

                //普通换弹音效 -> 从武器获取。
                SoundType.Reload => weaponBehaviour.GetAudioClipReload(),
                //空仓换弹音效 -> 从武器获取（包含更多操作步骤的音效）。
                SoundType.ReloadEmpty => weaponBehaviour.GetAudioClipReloadEmpty(),

                //打开弹仓音效 -> 从武器获取（逐发装填第一步）。
                SoundType.ReloadOpen => weaponBehaviour.GetAudioClipReloadOpen(),
                //插入弹药音效 -> 从武器获取（逐发装填第二步）。
                SoundType.ReloadInsert => weaponBehaviour.GetAudioClipReloadInsert(),
                //关闭弹仓音效 -> 从武器获取（逐发装填第三步）。
                SoundType.ReloadClose => weaponBehaviour.GetAudioClipReloadClose(),

                //正常射击音效 -> 从武器获取。
                SoundType.Fire => weaponBehaviour.GetAudioClipFire(),
                //空仓射击音效 -> 从武器获取（击锤空击声）。
                SoundType.FireEmpty => weaponBehaviour.GetAudioClipFireEmpty(),

                //拉栓音效 -> 从武器获取。
                SoundType.BoltAction => weaponBehaviour.GetAudioClipBoltAction(),

                //未匹配时返回空（不播放任何音效）。
                _ => default
            };

            #endregion

            //根据配置的延迟时间播放音效。如果delay为0，则立即播放。
            audioManagerService.PlayOneShotDelayed(clip, audioSettings, delay);
        }

        #endregion
    }
}
//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 引导启动器。
    /// 在场景加载前通过RuntimeInitializeOnLoadMethod自动执行，
    /// 负责初始化ServiceLocator并注册所有核心服务。
    /// </summary>
    public static class Bootstraper
    {
        /// <summary>
        /// 初始化入口。
        /// 通过[RuntimeInitializeOnLoadMethod]特性在场景加载前自动调用。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            //初始化默认的服务定位器实例。
            ServiceLocator.Initialize();

            //注册游戏模式服务。
            ServiceLocator.Current.Register<IGameModeService>(new GameModeService());

            #region Sound Manager Service

            //创建SoundManager的GameObject，并添加AudioManagerService组件。
            var soundManagerObject = new GameObject("Sound Manager");
            var soundManagerService = soundManagerObject.AddComponent<AudioManagerService>();

            //确保SoundManager在场景切换时不被销毁，使其成为全局单例。
            Object.DontDestroyOnLoad(soundManagerObject);

            //在服务定位器中注册音频管理服务。
            ServiceLocator.Current.Register<IAudioManagerService>(soundManagerService);

            #endregion
        }
    }
}
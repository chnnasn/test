//Copyright 2022, Infima Games. All Rights Reserved.

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 游戏模式服务接口。
    /// 继承自IGameService，通过ServiceLocator注册和获取。
    /// </summary>
    public interface IGameModeService : IGameService
    {
        /// <summary>
        /// 返回当前场景中的玩家角色。
        /// </summary>
        CharacterBehaviour GetPlayerCharacter();
    }
}
//Copyright 2022, Infima Games. All Rights Reserved.

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 游戏模式服务。
    /// 负责管理和提供玩家角色的引用，使用懒加载模式首次访问时查找场景中的CharacterBehaviour。
    /// </summary>
    public class GameModeService : IGameModeService
    {
        #region FIELDS

        /// <summary>
        /// 玩家角色引用（懒加载缓存）。
        /// </summary>
        private CharacterBehaviour playerCharacter;

        #endregion

        #region FUNCTIONS

        public CharacterBehaviour GetPlayerCharacter()
        {
            //如果玩家角色引用为空，则在场景中查找（懒加载），之后缓存使用。
            if (playerCharacter == null)
                playerCharacter = UnityEngine.Object.FindObjectOfType<CharacterBehaviour>();

            //返回玩家角色。
            return playerCharacter;
        }

        #endregion
    }
}
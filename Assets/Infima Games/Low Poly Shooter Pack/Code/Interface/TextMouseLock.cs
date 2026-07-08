//Copyright 2022, Infima Games. All Rights Reserved.

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 鼠标锁定状态文本。通过更新界面中的文本来提示开发者当前鼠标是否被锁定。
    /// 主要用于开发调试，在正式版本中通常隐藏。
    /// </summary>
    public class TextMouseLock : ElementText
    {
        #region METHODS

        /// <summary>
        /// 每帧更新：根据鼠标锁定状态显示"Cursor Locked"或"Cursor Unlocked"。
        /// </summary>
        protected override void Tick()
        {
            //根据鼠标是否锁定更新文本内容。
            textMesh.text = "Cursor " + (characterBehaviour.IsCursorLocked() ? "Locked" : "Unlocked");
        }

        #endregion
    }
}
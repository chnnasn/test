//Copyright 2022, Infima Games. All Rights Reserved.

using System.Globalization;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// 总弹药文本。显示当前武器的总备弹数量（不含弹匣中的弹药）。
    /// </summary>
    public class TextAmmunitionTotal : ElementText
    {
        #region METHODS

        /// <summary>
        /// 每帧更新：从当前装备武器获取总备弹数量并更新文本显示。
        /// </summary>
        protected override void Tick()
        {
            //获取总备弹数量。
            float ammunitionTotal = equippedWeaponBehaviour.GetAmmunitionTotal();

            //更新文本显示（使用InvariantCulture确保数字格式一致）。
            textMesh.text = ammunitionTotal.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 武器静态工具类。提供配件选择和激活的通用扩展方法。
    /// </summary>
    public static class UtilitiesWeapons
    {
        /// <summary>
        /// 从配件数组中选中指定索引的组件并激活它，同时禁用数组中所有其他组件。
        /// 这是武器附件管理器的核心工具方法：先遍历数组禁用全部，再单独激活目标索引的那个。
        /// </summary>
        /// <typeparam name="T">需要是MonoBehaviour的子类</typeparam>
        /// <param name="array">配件数组</param>
        /// <param name="index">要激活的配件索引</param>
        /// <returns>激活的组件引用。如果数组为空或索引越界则返回null。</returns>
        public static T SelectAndSetActive<T>(this T[] array, int index) where T : MonoBehaviour
        {
            //确保数组中存在对象！如果没有，可能会发生错误或崩溃。
            if (!array.IsValid())
                return null;

            //先禁用所有对象。这样我们就不需要手动逐个去禁用。
            array.ForEach(obj => obj.gameObject.SetActive(false));

            //索引有效性检查。
            if (!array.IsValidIndex(index))
                return null;

            //激活目标索引的组件。
            T behaviour = array[index];
            if(behaviour != null)
                behaviour.gameObject.SetActive(true);

            //返回激活的组件引用。
            return behaviour;
        }
    }
}
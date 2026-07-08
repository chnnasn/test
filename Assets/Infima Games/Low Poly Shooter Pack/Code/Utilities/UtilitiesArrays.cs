//Copyright 2022, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 数组工具扩展类。
    /// 为T[]数组提供了三个实用的扩展方法：索引有效性检查、数组有效性验证和随机元素获取。
    /// </summary>
    public static class UtilitiesArrays
    {
        /// <summary>
        /// 判断数组在指定索引处是否有效（索引在[0, Length)范围内）。
        /// </summary>
        public static bool IsValidIndex<T>(this T[] array, int index) => array.Length > index && index >= 0;
        /// <summary>
        /// 判断数组是否有效（非null且长度大于0）。
        /// </summary>
        public static bool IsValid<T>(this T[] array) => !array.Equals(null) && array.Length > 0;
        /// <summary>
        /// 从数组中随机获取一个元素。
        /// 使用UnityEngine.Random.Range生成随机索引。
        /// </summary>
        public static T GetRandom<T>(this T[] array) => array[Random.Range(0, array.Length)];
    }
}
//Copyright 2022, Infima Games. All Rights Reserved.
//实现参考自: https://medium.com/medialesson/simple-service-locator-for-your-unity-project-40e317aad307

using System;
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// 简单服务定位器，用于管理<see cref="IGameService"/>实例的注册和获取。
    /// 使用静态单例模式（Current属性），内部以Dictionary&lt;string, IGameService&gt;存储服务。
    /// 支持服务的注册（Register）、获取（Get）和注销（Unregister），并包含重复注册检测和错误日志。
    /// </summary>
    public class ServiceLocator
    {
        /// <summary>
        /// 当前已注册的服务字典，以类型名为键。
        /// </summary>
        private readonly Dictionary<string, IGameService> services = new Dictionary<string, IGameService>();

        /// <summary>
        /// 全局静态服务定位器单例。
        /// </summary>
        public static ServiceLocator Current { get; private set; }

        /// <summary>
        /// 初始化服务定位器，创建新的实例。
        /// </summary>
        public static void Initialize() { Current = new ServiceLocator(); }

        /// <summary>
        /// 获取指定类型的服务实例。
        /// 如果服务未注册，会记录致命错误并抛出InvalidOperationException。
        /// </summary>
        /// <typeparam name="T">要查找的服务类型（必须实现IGameService）。</typeparam>
        /// <returns>服务实例。</returns>
        public T Get<T>() where T : IGameService
        {
            string key = typeof(T).Name;
            if (!services.ContainsKey(key))
            {
                //服务未注册，记录错误并抛出异常。
                Log.kill($"{key} not registered with {GetType().Name}");
                throw new InvalidOperationException();
            }

            return (T)services[key];
        }

        /// <summary>
        /// 向当前服务定位器注册服务实例。
        /// 如果该类型已注册，则记录错误并跳过（防止覆盖已有注册）。
        /// </summary>
        /// <typeparam name="T">服务类型。</typeparam>
        /// <param name="service">服务实例。</param>
        public void Register<T>(T service) where T : IGameService
        {
            string key = typeof(T).Name;
            if (services.ContainsKey(key))
            {
                //该类型的服务已经注册，记录错误并返回，防止重复注册。
                Log.kill($"Attempted to register service of type {key} which is already registered with the {GetType().Name}.");
                return;
            }

            //添加服务到字典。
            services.Add(key, service);
        }

        /// <summary>
        /// 从当前服务定位器注销指定类型的服务。
        /// 如果该类型未注册，则记录错误并跳过。
        /// </summary>
        /// <typeparam name="T">服务类型。</typeparam>
        public void Unregister<T>() where T : IGameService
        {
            string key = typeof(T).Name;
            if (!services.ContainsKey(key))
            {
                //该类型的服务尚未注册，记录错误并返回。
                Log.kill($"Attempted to unregister service of type {key} which is not registered with the {GetType().Name}.");
                return;
            }

            services.Remove(key);
        }
    }
}
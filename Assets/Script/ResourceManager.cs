using System;
using UnityEngine;

/// <summary>
/// 封装同步/异步Resources加载, 后续换成AB加载
/// </summary>
public class ResourceManager : MonoSingleton<ResourceManager>
{
    /// <summary>
    /// 同步加载Prefab, 不实例化
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public GameObject LoadPrefab(string path)
    {
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[ResMgr] Prefab加载失败: {path}");
            return null;
        }
        return prefab;
    }

    /// <summary>
    /// 同步加载并实例化
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public GameObject LoadPrefabAndInstantiate(string path, Transform parent = null)
    {
        var prefab = LoadPrefab(path);
        if (prefab == null) return null;
        var obj = Instantiate(prefab, parent);
        obj.name = prefab.name;
        return obj;
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="onLoaded"></param>
    public void LoadPrefabAsync(string path, Action<GameObject> onLoaded)
    {
        var req = Resources.LoadAsync<GameObject>(path);
        req.completed += _ =>
        {
            if (req.asset == null)
            {
                Debug.LogError($"[ResMgr] Prefab异步加载失败: {path}");
                onLoaded?.Invoke(null);
                return;
            }
            onLoaded?.Invoke(req.asset as GameObject);
        };
    }

    /// <summary>
    /// 异步加载并实例化
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parent"></param>
    /// <param name="onLoaded"></param>
    public void LoadPrefabAndInstantiateAsync(string path, Transform parent, Action<GameObject> onLoaded)
    {
        LoadPrefabAsync(path, prefab =>
        {
            if (prefab == null) { onLoaded?.Invoke(null); return; }
            var obj = Instantiate(prefab, parent);
            obj.name = prefab.name;
            onLoaded?.Invoke(obj);
        });
    }

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public TextAsset LoadTextAsset(string path)
    {
        var asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"[ResMgr] TextAsset加载失败: {path}");
            return null;
        }
        return asset;
    }

    /// <summary>
    /// 同步加载并返回字符串内容
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public string LoadText(string path)
    {
        return LoadTextAsset(path)?.text;
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="onLoaded"></param>
    public void LoadTextAssetAsync(string path, Action<TextAsset> onLoaded)
    {
        var req = Resources.LoadAsync<TextAsset>(path);
        req.completed += _ =>
        {
            if (req.asset == null)
            {
                Debug.LogError($"[ResMgr] TextAsset异步加载失败: {path}");
                onLoaded?.Invoke(null);
                return;
            }
            onLoaded?.Invoke(req.asset as TextAsset);
        };
    }

    /// <summary>
    /// 异步加载并返回文本内容
    /// </summary>
    /// <param name="path"></param>
    /// <param name="onLoaded"></param>
    public void LoadTextAsync(string path, Action<string> onLoaded)
    {
        LoadTextAssetAsync(path, asset => onLoaded?.Invoke(asset?.text));
    }
}

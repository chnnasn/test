using UnityEngine;

public abstract class LazySingleton<T> : MonoBehaviour where T : LazySingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _isShuttingDown;
    public static T Instance
    {
        get
        {
            if (!Application.isPlaying)
                return _instance;
            if (_instance != null)
                return _instance;
            lock (_lock)
            {
                if (_instance != null)
                    return _instance;

                _instance = FindObjectOfType<T>() as T;
                if (_instance != null)
                {
                    _isShuttingDown = false;
                    return _instance;
                }

                if (_isShuttingDown)
                    return null;

                GameObject go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
            }
            return _instance;
        }
    }
    public static bool TryGetExistingInstance(out T instance)
    {
        instance = _instance;
        if (instance != null)
            return true;
        if (!Application.isPlaying)
            return false;
        instance = FindObjectOfType<T>() as T;
        _instance = instance;
        if (instance != null)
        {
            _isShuttingDown = false;
            return true;
        }
        return false;
    }
    protected  virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = (T)this;
            _isShuttingDown = false;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    protected virtual void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

[DefaultExecutionOrder(-100)]
public class GameManager : LazySingleton<GameManager>
{
    [Header("Flow Field 设置")]
    [SerializeField] private FlowFieldAsset _flowFieldAsset;

    [Header("Tip 设置")]
    [SerializeField] private TipDatabase _tipDatabase;

    private Character _character;
    private bool _flowFieldInitialized;
    private Text _frameText;
    private float _fpsTimer;
    private int _fpsFrameCount;

    public Action<float> AttackAction;

    public string GetRandomTip()
    {
        return _tipDatabase != null ? _tipDatabase.GetRandomTip() : string.Empty;
    }

    private void Start()
    {
        Application.targetFrameRate = 120;
        EventManager.Instance.GamePause += OnGamePause;
        EventManager.Instance.GameResume += OnGameResume;
        InitCharacter();
        InitFrameText();

        if (_flowFieldInitialized && _character != null)
        {
            FlowField.SetTarget(_character.transform.position);
        }
    }

    private void OnDestroy()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
        {
            eventManager.GamePause -= OnGamePause;
            eventManager.GameResume -= OnGameResume;
        }
    }

    private void Update()
    {
        SpatialGrid.RebuildAll();
        UpdateFrameRate();

        if (_character == null)
        {
            InitCharacter();
        }

        if (_flowFieldInitialized && _character != null)
        {
            FlowField.SetTarget(_character.transform.position);
        }
    }

    private void InitFlowField()
    {
        if (_flowFieldInitialized) return;

        if (_flowFieldAsset == null)
        {
            Debug.LogError("[GameManager] FlowFieldAsset 未设置，请先创建并 Bake 流场资产");
            return;
        }

        FlowField.Initialize(_flowFieldAsset);
        _flowFieldInitialized = FlowField.IsInitialized;
    }

    private void InitCharacter()
    {
        if (_character != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _character = playerObj.GetComponent<Character>();

        if (_character != null)
            InitFlowField();
    }

    private void InitFrameText()
    {
        if (_frameText != null) return;

        GameObject frameObj = GameObject.Find("Frame");
        if (frameObj != null)
            _frameText = frameObj.GetComponent<Text>();
    }

    private void UpdateFrameRate()
    {
        if (_frameText == null)
        {
            InitFrameText();
            if (_frameText == null) return;
        }

        _fpsTimer += Time.unscaledDeltaTime;
        _fpsFrameCount++;

        if (_fpsTimer < 0.25f) return;

        float fps = _fpsFrameCount / _fpsTimer;
        _frameText.text = $"FPS: {fps:0}";
        _fpsTimer = 0f;
        _fpsFrameCount = 0;
    }

    public bool IsFlowFieldReady => _flowFieldInitialized;

    public bool WarmupFlowField()
    {
        InitFlowField();
        return _flowFieldInitialized;
    }
    
    private void OnGamePause()
    {
        Time.timeScale = 0f;
    }

    private void OnGameResume()
    {
        Time.timeScale = 1f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_flowFieldAsset != null)
        {
            // 编辑模式下预览 Bake 资产，运行时显示完整流场方向
            if (Application.isPlaying && FlowField.IsInitialized && _character != null)
            {
                FlowField.DrawGridOutline(_flowFieldAsset.WorldMin, _flowFieldAsset.WorldMax,
                    _flowFieldAsset.CellSize, _flowFieldAsset.Width, _flowFieldAsset.Height);
                FlowField.DrawGizmos(_character.transform.position);
            }
            else
            {
                FlowField.DrawAssetPreview(_flowFieldAsset);
            }

            FlowField.DrawLegend(_flowFieldAsset.WorldMin);
        }
        else
        {
            FlowField.DrawLegend(Vector3.zero);
        }
    }
#endif
}

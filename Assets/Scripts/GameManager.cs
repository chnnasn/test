using System;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

[DefaultExecutionOrder(-100)]
public class GameManager : LazySingleton<GameManager>
{
    [Header("Flow Field 设置")]
    [SerializeField] private FlowFieldAsset _flowFieldAsset;

    private Character _character;
    private bool _flowFieldInitialized;

    public Action<float> AttackAction;

    private void Start()
    {
        InitCharacter();

        if (_flowFieldInitialized && _character != null)
        {
            FlowField.SetTarget(_character.transform.position);
        }
    }

    private void Update()
    {
        SpatialGrid.RebuildAll();

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

    public GameObject GetPlayer()
    {
        InitCharacter();
        return _character != null ? _character.gameObject : null;
    }

    public Character GetCharacter()
    {
        InitCharacter();
        return _character;
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

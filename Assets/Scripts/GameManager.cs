using System;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

[DefaultExecutionOrder(-100)]
public class GameManager : LazySingleton<GameManager>
{
    [Header("Flow Field 设置")]
    [SerializeField] private Vector3 _worldMin = new Vector3(-50, 0, -50);
    [SerializeField] private Vector3 _worldMax = new Vector3(50, 0, 50);
    [SerializeField] private LayerMask _obstacleMask;

    private Character _character;
    private bool _characterInitialized;
    private bool _flowFieldInitialized;

    public Action<float> AttackAction;

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
        _flowFieldInitialized = true;
        FlowField.Initialize(_worldMin, _worldMax, _obstacleMask);
    }

    private void InitCharacter()
    {
        if (_characterInitialized) return;
        _characterInitialized = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _character = playerObj.GetComponent<Character>();

        if (_character != null)
            InitFlowField();
    }

    public GameObject GetPlayer()
    {
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
        // 始终绘制网格边界框（编辑模式下也能看到范围）
        FlowField.DrawGridOutline(_worldMin, _worldMax);

        // 运行时绘制完整的流场方向 + 障碍物
        if (Application.isPlaying && FlowField.IsInitialized && _character != null)
        {
            FlowField.DrawGizmos(_character.transform.position);
        }

        // 图例
        FlowField.DrawLegend(_worldMin);
    }
#endif
}

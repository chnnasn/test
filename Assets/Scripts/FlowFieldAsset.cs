using UnityEngine;

[CreateAssetMenu(fileName = "FlowFieldAsset", menuName = "ScriptableObjects/FlowField Asset", order = 2)]
public class FlowFieldAsset : ScriptableObject
{
    [Header("Bake 设置")]
    [SerializeField] private Vector3 _worldMin = new Vector3(-50, 0, -50);
    [SerializeField] private Vector3 _worldMax = new Vector3(50, 0, 50);
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Bake 结果")]
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private bool[] _blockedCells;
    [SerializeField] private int _blockedCount;
    [SerializeField] private string _bakedSceneName;
    [SerializeField] private string _bakedTime;

    public Vector3 WorldMin => _worldMin;
    public Vector3 WorldMax => _worldMax;
    public float CellSize => _cellSize;
    public LayerMask ObstacleMask => _obstacleMask;
    public int Width => _width;
    public int Height => _height;
    public bool[] BlockedCells => _blockedCells;
    public int BlockedCount => _blockedCount;
    public string BakedSceneName => _bakedSceneName;
    public string BakedTime => _bakedTime;

    public bool IsValid => _cellSize > 0f && _width > 0 && _height > 0 &&
                           _blockedCells != null && _blockedCells.Length == _width * _height;

    public Vector3 CellToWorld(int x, int y)
    {
        float wx = _worldMin.x + (x + 0.5f) * _cellSize;
        float wz = _worldMin.z + (y + 0.5f) * _cellSize;
        return new Vector3(wx, 0, wz);
    }

    public bool IsBlocked(int x, int y)
    {
        if (!IsValid || x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return false;
        }

        return _blockedCells[y * _width + x];
    }

    public void SetBakeData(Vector3 worldMin, Vector3 worldMax, float cellSize, LayerMask obstacleMask,
        int width, int height, bool[] blockedCells, int blockedCount, string bakedSceneName, string bakedTime)
    {
        _worldMin = worldMin;
        _worldMax = worldMax;
        _cellSize = Mathf.Max(0.01f, cellSize);
        _obstacleMask = obstacleMask;
        _width = width;
        _height = height;
        _blockedCells = blockedCells;
        _blockedCount = blockedCount;
        _bakedSceneName = bakedSceneName;
        _bakedTime = bakedTime;
    }
}

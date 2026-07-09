using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(FlowFieldAsset))]
public class FlowFieldBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FlowFieldAsset asset = (FlowFieldAsset)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bake 信息", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("有效", asset.IsValid ? "是" : "否");
        EditorGUILayout.LabelField("尺寸", $"{asset.Width} x {asset.Height}");
        EditorGUILayout.LabelField("障碍格", asset.BlockedCount.ToString());
        EditorGUILayout.LabelField("场景", asset.BakedSceneName);
        EditorGUILayout.LabelField("时间", asset.BakedTime);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake From Current Scene"))
        {
            Bake(asset);
        }
    }

    private static void Bake(FlowFieldAsset asset)
    {
        if (asset.CellSize <= 0f)
        {
            Debug.LogError("[FlowFieldBaker] Bake 失败：Cell Size 必须大于 0");
            return;
        }

        Vector3 worldMin = asset.WorldMin;
        Vector3 worldMax = asset.WorldMax;
        float cellSize = asset.CellSize;
        int width = Mathf.CeilToInt((worldMax.x - worldMin.x) / cellSize);
        int height = Mathf.CeilToInt((worldMax.z - worldMin.z) / cellSize);

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("[FlowFieldBaker] Bake 失败：World Min/Max 范围无效");
            return;
        }

        bool[] blockedCells = new bool[width * height];
        int blockedCount = 0;
        Vector3 halfExtents = new Vector3(
            cellSize * 0.45f + asset.AgentRadius,
            1f,
            cellSize * 0.45f + asset.AgentRadius);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                Vector3 cellCenter = new Vector3(
                    worldMin.x + (x + 0.5f) * cellSize,
                    0,
                    worldMin.z + (y + 0.5f) * cellSize);

                bool blocked = Physics.CheckBox(cellCenter, halfExtents, Quaternion.identity, asset.ObstacleMask);
                blockedCells[idx] = blocked;
                if (blocked)
                {
                    blockedCount++;
                }
            }
        }

        string sceneName = EditorSceneManager.GetActiveScene().name;
        string bakedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        asset.SetBakeData(worldMin, worldMax, cellSize, asset.ObstacleMask,
            width, height, blockedCells, blockedCount, sceneName, bakedTime);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        Debug.Log($"[FlowFieldBaker] Bake 完成: {width}x{height} 格, 障碍 {blockedCount} 格, 资产 {asset.name}");
    }
}

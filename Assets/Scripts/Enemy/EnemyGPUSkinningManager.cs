using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPU 骨骼蒙皮管理器。
/// 配置不全时自动回退原始 SkinnedMeshRenderer，确保零影响。
/// 
/// 需要配置：
///   1. Skinning Compute → 拖入 EnemyGPUSkinning.compute（需放在 Resources/ 目录下或直接拖引用）
///   2. GPU Skinning Material Template → 用 GPUSkinningLit.shader 创建材质后拖入
/// </summary>
[DefaultExecutionOrder(100)]
public class EnemyGPUSkinningManager : MonoBehaviour
{
    [SerializeField] private ComputeShader _skinningCompute;
    [SerializeField] private Material _gpuSkinningMaterialTemplate;
    [SerializeField] private int _maxTotalInstances = 128;

    private static EnemyGPUSkinningManager _instance;
    private bool _isReady;
    private static bool _warnedNotReady;
    private static bool _warnedMaterialNull;
    private static bool _warnedComputeNull;

    /// <summary>
    /// 获取已就绪的 GPU Skinning 管理器实例。
    /// 如果组件不在场景中或配置不全，返回 false，Enemy 将使用原始 SkinnedMeshRenderer。
    /// </summary>
    public static bool TryGetInstance(out EnemyGPUSkinningManager instance)
    {
        if (_instance != null && _instance._isReady)
        {
            instance = _instance;
            return true;
        }

        _instance = FindObjectOfType<EnemyGPUSkinningManager>();
        if (_instance == null)
        {
            if (!_warnedNotReady)
            {
                Debug.Log("[GPUSkinning] 未找到 EnemyGPUSkinningManager 组件，使用原始 SkinnedMeshRenderer。");
                _warnedNotReady = true;
            }
            instance = null;
            return false;
        }

        instance = _instance;
        return _instance._isReady;
    }

    // ──────────────────────────────
    //  Mesh 类型数据
    // ──────────────────────────────

    private sealed class MeshSkinningData
    {
        public Mesh SharedMesh;
        public Matrix4x4[] BindPoses;
        public int Kernel;
        public int VertexCount, IndexCount, BonesPerInstance;

        public ComputeBuffer RestPositions;
        public ComputeBuffer RestNormals;
        public ComputeBuffer UVs;
        public ComputeBuffer BoneWeights;
        public ComputeBuffer BoneIndices;

        public ComputeBuffer BoneMatrices;
        public ComputeBuffer SkinnedPositions;
        public ComputeBuffer SkinnedNormals;
        public ComputeBuffer SkinnedUVs;

        public ComputeBuffer DrawArgs;
        public Material InstanceMaterial;
        public uint[] DrawArgsData;

        public int MaxInstances;
        public int ActiveCount;
        public Enemy[] Instances;
        public Transform[][] InstanceBones;
        public Bounds WorldBounds;
    }

    private readonly Dictionary<Mesh, MeshSkinningData> _meshData = new Dictionary<Mesh, MeshSkinningData>();
    private readonly Dictionary<Enemy, MeshSkinningData> _instanceData = new Dictionary<Enemy, MeshSkinningData>();
    private readonly Dictionary<Enemy, int> _instanceSlots = new Dictionary<Enemy, int>();
    private Matrix4x4[] _boneMatrixUpload;

    // ──────────────────────────────
    //  生命周期
    // ──────────────────────────────

    private void Awake()
    {
        // 检查配置完整性
        if (_skinningCompute == null)
        {
            _skinningCompute = Resources.Load<ComputeShader>("EnemyGPUSkinning");
            if (_skinningCompute == null && !_warnedComputeNull)
            {
                Debug.LogError("[GPUSkinning] 未找到 ComputeShader！请把 EnemyGPUSkinning.compute 放入 Resources/ 目录，或在 Inspector 中直接拖入。已回退原始渲染。");
                _warnedComputeNull = true;
            }
        }

        if (_gpuSkinningMaterialTemplate == null && !_warnedMaterialNull)
        {
            Debug.LogError("[GPUSkinning] 未设置 GPU Skinning Material Template！请在 Inspector 中拖入 GPUSkinningLit.shader 创建的材质。已回退原始渲染。");
            _warnedMaterialNull = true;
        }

        _isReady = _skinningCompute != null && _gpuSkinningMaterialTemplate != null;

        if (_isReady)
            EventManager.Instance.BeforeDemoRestart += OnBeforeDemoRestart;
    }

    private void OnBeforeDemoRestart()
    {
        foreach (var data in _meshData.Values)
        {
            for (int i = 0; i < data.Instances.Length; i++)
            {
                data.Instances[i] = null;
                data.InstanceBones[i] = null;
            }
            data.ActiveCount = 0;
        }
        _instanceData.Clear();
        _instanceSlots.Clear();
    }

    private void OnDestroy()
    {
        if (EventManager.TryGetExistingInstance(out EventManager em))
            em.BeforeDemoRestart -= OnBeforeDemoRestart;

        _instance = null;
        foreach (var data in _meshData.Values)
            ReleaseMeshData(data);
        _instanceData.Clear();
        _instanceSlots.Clear();
        _meshData.Clear();
    }

    // ──────────────────────────────
    //  实例注册 / 注销
    // ──────────────────────────────

    public void Register(Enemy enemy)
    {
        if (!_isReady || enemy?.AnimatorController == null) return;
        if (_instanceData.ContainsKey(enemy)) return;

        SkinnedMeshRenderer smr = enemy.CachedSkinnedRenderer;
        if (smr?.sharedMesh == null) return;

        Mesh mesh = smr.sharedMesh;
        if (!_meshData.TryGetValue(mesh, out var data))
        {
            data = CreateMeshData(mesh, smr);
            if (data == null) return;
            _meshData[mesh] = data;
        }

        if (data.ActiveCount >= data.MaxInstances)
        {
            Debug.LogWarning($"[GPUSkinning] Mesh '{mesh.name}' 实例槽已满 (max={data.MaxInstances})");
            return;
        }

        int slot = data.ActiveCount++;
        data.Instances[slot] = enemy;
        data.InstanceBones[slot] = smr.bones;
        _instanceData.Add(enemy, data);
        _instanceSlots.Add(enemy, slot);
    }

    public void Unregister(Enemy enemy)
    {
        if (!_isReady || enemy == null || !_instanceData.TryGetValue(enemy, out var data)) return;

        int slot = _instanceSlots[enemy];
        int lastSlot = data.ActiveCount - 1;
        if (slot != lastSlot)
        {
            Enemy movedEnemy = data.Instances[lastSlot];
            data.Instances[slot] = movedEnemy;
            data.InstanceBones[slot] = data.InstanceBones[lastSlot];
            _instanceSlots[movedEnemy] = slot;
        }

        data.Instances[lastSlot] = null;
        data.InstanceBones[lastSlot] = null;
        data.ActiveCount = lastSlot;
        _instanceData.Remove(enemy);
        _instanceSlots.Remove(enemy);
    }

    // ──────────────────────────────
    //  预热（Loading 阶段调用）
    // ──────────────────────────────

    public void PrewarmEnemyType(GameObject enemyPrefab)
    {
        if (!_isReady || enemyPrefab == null) return;

        SkinnedMeshRenderer smr = enemyPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr?.sharedMesh == null) return;

        Mesh mesh = smr.sharedMesh;
        if (_meshData.ContainsKey(mesh)) return;

        MeshSkinningData data = CreateMeshData(mesh, smr);
        if (data != null)
            _meshData[mesh] = data;
    }

    // ──────────────────────────────
    //  每帧主循环
    // ──────────────────────────────

    private void LateUpdate()
    {
        if (!_isReady) return;

        foreach (var kvp in _meshData)
        {
            MeshSkinningData data = kvp.Value;
            if (data.ActiveCount == 0) continue;

            int instanceCount = data.ActiveCount;
            if (instanceCount == 0) continue;

            // 1. 收集骨骼矩阵
            int bonesPerInst = data.BonesPerInstance;
            int totalBones = instanceCount * bonesPerInst;
            if (_boneMatrixUpload == null || _boneMatrixUpload.Length < totalBones)
                _boneMatrixUpload = new Matrix4x4[totalBones];

            for (int i = 0; i < instanceCount; i++)
            {
                Transform[] bones = data.InstanceBones[i];
                int count = Mathf.Min(bones.Length, bonesPerInst);
                int off = i * bonesPerInst;
                for (int b = 0; b < count; b++)
                    _boneMatrixUpload[off + b] = (bones[b] != null) ? bones[b].localToWorldMatrix * data.BindPoses[b] : Matrix4x4.identity;
                for (int b = count; b < bonesPerInst; b++)
                    _boneMatrixUpload[off + b] = Matrix4x4.identity;
            }

            // 2. 上传骨骼矩阵
            data.BoneMatrices.SetData(_boneMatrixUpload, 0, 0, totalBones);

            // 3. ComputeShader dispatch
            int kernel = data.Kernel;
            _skinningCompute.SetBuffer(kernel, "_RestPositions", data.RestPositions);
            _skinningCompute.SetBuffer(kernel, "_RestNormals", data.RestNormals);
            _skinningCompute.SetBuffer(kernel, "_UVs", data.UVs);
            _skinningCompute.SetBuffer(kernel, "_BoneWeights", data.BoneWeights);
            _skinningCompute.SetBuffer(kernel, "_BoneIndices", data.BoneIndices);
            _skinningCompute.SetBuffer(kernel, "_BoneMatrices", data.BoneMatrices);
            _skinningCompute.SetBuffer(kernel, "_SkinnedPositions", data.SkinnedPositions);
            _skinningCompute.SetBuffer(kernel, "_SkinnedNormals", data.SkinnedNormals);
            _skinningCompute.SetBuffer(kernel, "_SkinnedUVs", data.SkinnedUVs);
            _skinningCompute.SetInt("_BonesPerInstance", bonesPerInst);
            _skinningCompute.SetInt("_VertexCount", data.VertexCount);
            _skinningCompute.SetInt("_InstanceCount", instanceCount);

            int totalWork = data.VertexCount * instanceCount;
            _skinningCompute.Dispatch(kernel, Mathf.CeilToInt(totalWork / 64f), 1, 1);

            // 5. 设置材质
            Material mat = data.InstanceMaterial;
            mat.SetBuffer("_SkinnedPositions", data.SkinnedPositions);
            mat.SetBuffer("_SkinnedNormals", data.SkinnedNormals);
            mat.SetBuffer("_SkinnedUVs", data.SkinnedUVs);
            mat.SetInt("_VertexCount", data.VertexCount);

            // 6. 更新 Draw Args
            data.DrawArgsData[0] = (uint)data.IndexCount;
            data.DrawArgsData[1] = (uint)instanceCount;
            data.DrawArgsData[2] = 0;
            data.DrawArgsData[3] = 0;
            data.DrawArgsData[4] = 0;
            data.DrawArgs.SetData(data.DrawArgsData);

            // 7. 绘制（关闭阴影 — 133 个敌人投阴影 GPU 成本极高，留给玩家/场景主光源）
            Graphics.DrawMeshInstancedIndirect(
                data.SharedMesh, 0, mat, data.WorldBounds,
                data.DrawArgs, 0, null,
                UnityEngine.Rendering.ShadowCastingMode.Off, false
            );
        }
    }

    // ──────────────────────────────
    //  Mesh 初始化
    // ──────────────────────────────

    private MeshSkinningData CreateMeshData(Mesh mesh, SkinnedMeshRenderer smr)
    {
        int vc = mesh.vertexCount;
        Matrix4x4[] bindPoses = mesh.bindposes;
        int bones = bindPoses.Length;
        if (vc == 0 || bones == 0) return null;

        Vector3[] pos = mesh.vertices;
        Vector3[] nrm = mesh.normals;
        Vector2[] uv = mesh.uv;
        BoneWeight[] bw = mesh.boneWeights;

        float[] w = new float[vc * 4];
        uint[] idx = new uint[vc * 4];
        for (int i = 0; i < vc; i++)
        {
            w[i * 4 + 0] = bw[i].weight0; w[i * 4 + 1] = bw[i].weight1;
            w[i * 4 + 2] = bw[i].weight2; w[i * 4 + 3] = bw[i].weight3;
            idx[i * 4 + 0] = (uint)bw[i].boneIndex0; idx[i * 4 + 1] = (uint)bw[i].boneIndex1;
            idx[i * 4 + 2] = (uint)bw[i].boneIndex2; idx[i * 4 + 3] = (uint)bw[i].boneIndex3;
        }

        int maxI = Mathf.Max(1, _maxTotalInstances / 3);
        var data = new MeshSkinningData
        {
            SharedMesh = mesh,
            BindPoses = bindPoses,
            Kernel = _skinningCompute.FindKernel("CSSkinning"),
            VertexCount = vc,
            IndexCount = (int)mesh.GetIndexCount(0),
            BonesPerInstance = bones,
            MaxInstances = maxI,
            Instances = new Enemy[maxI],
            InstanceBones = new Transform[maxI][],
            WorldBounds = new Bounds(Vector3.zero, new Vector3(200f, 20f, 200f)),

            RestPositions = new ComputeBuffer(vc, 12),
            RestNormals = new ComputeBuffer(vc, 12),
            UVs = new ComputeBuffer(vc, 8),
            BoneWeights = new ComputeBuffer(vc, 16),
            BoneIndices = new ComputeBuffer(vc, 16),
            BoneMatrices = new ComputeBuffer(bones * maxI, 64),
            SkinnedPositions = new ComputeBuffer(vc * maxI, 12),
            SkinnedNormals = new ComputeBuffer(vc * maxI, 12),
            SkinnedUVs = new ComputeBuffer(vc * maxI, 8),
            DrawArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments),
            DrawArgsData = new uint[5],
            InstanceMaterial = new Material(_gpuSkinningMaterialTemplate),
        };

        data.RestPositions.SetData(pos);
        data.RestNormals.SetData(nrm);
        data.UVs.SetData(uv);
        data.BoneWeights.SetData(w);
        data.BoneIndices.SetData(idx);

        if (smr.sharedMaterial != null)
            data.InstanceMaterial.CopyPropertiesFromMaterial(smr.sharedMaterial);

        return data;
    }

    private static void ReleaseMeshData(MeshSkinningData d)
    {
        d.RestPositions?.Release(); d.RestNormals?.Release(); d.UVs?.Release();
        d.BoneWeights?.Release(); d.BoneIndices?.Release(); d.BoneMatrices?.Release();
        d.SkinnedPositions?.Release(); d.SkinnedNormals?.Release(); d.SkinnedUVs?.Release();
        d.DrawArgs?.Release();
        if (d.InstanceMaterial != null) Destroy(d.InstanceMaterial);
    }
}

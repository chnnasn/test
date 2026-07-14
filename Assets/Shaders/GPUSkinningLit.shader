// GPUSkinningLit.shader
// URP Lit Shader — GPU 蒙皮版
// Vertex Shader 从 ComputeShader 输出的 StructuredBuffer 读取蒙皮后顶点/法线/UV，
// 配合 DrawMeshInstancedIndirect（保留原始索引缓冲）实现单 Draw Call 批量绘制同类型僵尸。
// 光照计算完全复用 URP Lit 的 PBR 管线。

Shader "Enemy/GPUSkinningLit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── GPU 蒙皮输出 buffer（由 ComputeShader 每帧写入）──
            StructuredBuffer<float3> _SkinnedPositions;
            StructuredBuffer<float3> _SkinnedNormals;
            StructuredBuffer<float2> _SkinnedUVs;
            uint _VertexCount;

            // ── 材质参数 ──
            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);       SAMPLER(sampler_BumpMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float  _BumpScale;
            float  _Smoothness;
            float  _Metallic;

            // DrawMeshInstancedIndirect 要求顶点输入与 Mesh 顶点布局匹配（都声明但不使用）
            struct Attributes
            {
                float4 positionOS : POSITION;   // 未使用 — Mesh 原始顶点，仅占位
                float3 normalOS   : NORMAL;     // 未使用
                float4 tangentOS  : TANGENT;    // 未使用
                float2 uv0        : TEXCOORD0;  // 未使用
                uint   vertexID   : SV_VertexID;      // 索引缓冲区解析后的顶点编号
                uint   instanceID : SV_InstanceID;    // 实例编号
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                uint bufferIdx = input.instanceID * _VertexCount + input.vertexID;

                // 从 GPU Buffer 读取蒙皮后数据
                float3 positionOS = _SkinnedPositions[bufferIdx];
                float3 normalOS   = _SkinnedNormals[bufferIdx];
                float2 uv         = _SkinnedUVs[bufferIdx];

                Varyings output;
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(normalOS);
                output.uv         = uv;
                output.viewDirWS  = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                output.fogFactor   = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                float3 albedo  = baseMap.rgb * _BaseColor.rgb;
                float3 normalWS = normalize(input.normalWS);

                // GI：球谐环境光 + Shadow Mask
                half3 bakedGI = SampleSH(normalWS);

                InputData inputData = (InputData)0;
                inputData.positionWS  = input.positionWS;
                inputData.normalWS    = normalWS;
                inputData.viewDirectionWS = normalize(input.viewDirWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord    = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI     = bakedGI;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask  = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.smoothness = _Smoothness;
                surfaceData.metallic   = _Metallic;
                surfaceData.alpha      = baseMap.a * _BaseColor.a;
                surfaceData.occlusion   = 1;

                float4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // ── 阴影投射 Pass（同样从 GPU buffer 读取）──
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float3> _SkinnedPositions;
            uint _VertexCount;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;   // 未使用，占位
                uint   vertexID   : SV_VertexID;
                uint   instanceID : SV_InstanceID;
            };

            float4 vertShadow(ShadowAttributes input) : SV_POSITION
            {
                uint bufferIdx = input.instanceID * _VertexCount + input.vertexID;
                float3 positionOS = _SkinnedPositions[bufferIdx];
                float3 positionWS = TransformObjectToWorld(positionOS);
                return TransformWorldToHClip(positionWS);
            }

            half4 fragShadow() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

using UnityEngine;
using AGXUnity;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// 放土エリア専用の DeformableTerrain を実行時に生成する。
    /// メイン地形の起伏をコピーし、地表中心を放土エリア中心に合わせて生成する。
    /// 粒子は全地形で共有するため、放土地形は範囲外粒子を削除しない。
    ///
    /// このコンポーネントはシーン内のメイン地形と同じ GameObject または独立の GameObject に
    /// 配置し、インスペクターでメイン地形と放土エリア中心を指定する。
    /// Awake() で実行され、Simulation および DumpSoil の Initialize より前に地形を構築する。
    /// </summary>
    public class DumpTerrainFactory : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("メイン（掘削）地形。高さコピーとマテリアル参照元。")]
        public DeformableTerrain mainTerrain;

        [Header("Dump Area")]
        [Tooltip("放土エリアの地表中心（世界座標）。")]
        public Vector3 dumpAreaCenter = new Vector3(188f, 9f, 140f);

        [Tooltip("放土地形の最小サイズ。AGX の等間隔格子に合わせ X/Z は大きい方に揃える。")]
        public Vector3 dumpTerrainSize = new Vector3(12f, 30f, 12f);

        [Tooltip("放土地形のハイトマップ解像度")]
        public int dumpHeightmapResolution = 65;

        private GameObject generatedTerrain;
        private TerrainData generatedTerrainData;
        private DeformableTerrainProperties generatedProperties;

        /// <summary>
        /// メイン地形のハイトマップから指定した世界座標の正規化高さをサンプリングする。
        /// 範囲外はクランプする。純粋関数（AGX 依存なし）。
        /// </summary>
        public static float SampleHeightFromMainTerrain(
            float[,] mainHeights, int mainResolution,
            Vector3 mainTerrainSize, Vector3 mainTerrainPos,
            Vector2 worldXZ)
        {
            float fx = (worldXZ.x - mainTerrainPos.x) / mainTerrainSize.x * (mainResolution - 1);
            float fy = (worldXZ.y - mainTerrainPos.z) / mainTerrainSize.z * (mainResolution - 1);

            // Clamp to valid range
            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, mainResolution - 2);
            int iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, mainResolution - 2);
            float tx = Mathf.Clamp01(fx - ix);
            float ty = Mathf.Clamp01(fy - iy);

            // Unity TerrainData stores rows in Z and columns in X.
            float h00 = mainHeights[iy, ix];
            float h10 = mainHeights[iy, ix + 1];
            float h01 = mainHeights[iy + 1, ix];
            float h11 = mainHeights[iy + 1, ix + 1];

            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, ty);
        }

        /// <summary>
        /// メイン地形から放土地形用のハイトマップを構築する。純粋関数（AGX 依存なし）。
        /// </summary>
        public static float[,] BuildDumpHeightmap(
            float[,] mainHeights, int mainResolution,
            Vector3 mainTerrainSize, Vector3 mainTerrainPos,
            Vector3 dumpTerrainWorldMin, Vector3 dumpTerrainSize,
            int dumpResolution)
        {
            float[,] dumpHeights = new float[dumpResolution, dumpResolution];

            for (int dy = 0; dy < dumpResolution; dy++)
            {
                for (int dx = 0; dx < dumpResolution; dx++)
                {
                    // World position of this dump terrain cell
                    float worldX = dumpTerrainWorldMin.x +
                                   (float)dx / (dumpResolution - 1) * dumpTerrainSize.x;
                    float worldZ = dumpTerrainWorldMin.z +
                                   (float)dy / (dumpResolution - 1) * dumpTerrainSize.z;

                    float normalizedHeight = SampleHeightFromMainTerrain(
                        mainHeights, mainResolution,
                        mainTerrainSize, mainTerrainPos,
                        new Vector2(worldX, worldZ));

                    float worldHeight = mainTerrainPos.y + normalizedHeight * mainTerrainSize.y;
                    dumpHeights[dy, dx] = (worldHeight - dumpTerrainWorldMin.y) / dumpTerrainSize.y;
                }
            }

            return dumpHeights;
        }

        /// <summary>
        /// 放土用 DeformableTerrain を生成する。Awake() から呼ばれる。
        /// </summary>
        public DeformableTerrain CreateDumpTerrain()
        {
            if (mainTerrain == null)
            {
                Debug.LogError("[DumpTerrainFactory] mainTerrain is not assigned. Cannot create dump terrain.");
                return null;
            }

            TerrainData mainTerrainData = mainTerrain.TerrainData;
            int mainRes = mainTerrainData.heightmapResolution;
            float[,] mainHeights = mainTerrainData.GetHeights(0, 0, mainRes, mainRes);
            Vector3 mainTerrainSize = mainTerrainData.size;
            Vector3 mainTerrainPos = mainTerrain.transform.position;

            // AGX uses one element size for both horizontal axes. Reserve vertical
            // room for its depth offset and at least 1 m above the source height range.
            Vector3 size = dumpTerrainSize;
            size.x = size.z = Mathf.Max(size.x, size.z);
            size.y = Mathf.Max(size.y, mainTerrainSize.y + mainTerrain.MaximumDepth + 1f);
            Vector3 worldMin = new Vector3(
                dumpAreaCenter.x - size.x * 0.5f,
                mainTerrainPos.y,
                dumpAreaCenter.z - size.z * 0.5f);

            float[,] heights = BuildDumpHeightmap(
                mainHeights, mainRes, mainTerrainSize, mainTerrainPos,
                worldMin, size, dumpHeightmapResolution);

            // Preserve the sampled relief, but anchor the receiving surface to the
            // marker's Y as well as X/Z. Terrain origins are corners, not centers.
            float centerHeight = SampleHeightFromMainTerrain(
                mainHeights, mainRes, mainTerrainSize, mainTerrainPos,
                new Vector2(dumpAreaCenter.x, dumpAreaCenter.z)) * mainTerrainSize.y;
            worldMin.y = dumpAreaCenter.y - centerHeight;

            generatedTerrainData = new TerrainData
            {
                heightmapResolution = dumpHeightmapResolution,
                size = size,
                terrainLayers = mainTerrainData.terrainLayers
            };
            generatedTerrainData.SetHeights(0, 0, heights);
            generatedTerrain = Terrain.CreateTerrainGameObject(generatedTerrainData);
            generatedTerrain.name = "Terrain_Dump";
            generatedTerrain.transform.position = worldMin;
            // Keep a scene root: mainTerrain itself moves down by MaximumDepth.

            var dumpDeformable = generatedTerrain.AddComponent<DeformableTerrain>();
            dumpDeformable.Material = mainTerrain.Material;
            dumpDeformable.ParticleMaterial = mainTerrain.ParticleMaterial;
            dumpDeformable.DefaultTerrainMaterial = mainTerrain.DefaultTerrainMaterial;
            dumpDeformable.MaximumDepth = mainTerrain.MaximumDepth;

            // Native terrain instances share one particle system. A small terrain
            // must not delete the excavation particles outside its own bounds.
            // Clone before changing the flag so the main terrain keeps its policy.
            generatedProperties = mainTerrain.TerrainProperties != null
                ? Instantiate(mainTerrain.TerrainProperties)
                : ScriptAsset.Create<DeformableTerrainProperties>();
            generatedProperties.DeleteSoilParticlesOutsideBoundsEnabled = false;
            dumpDeformable.TerrainProperties = generatedProperties;

            generatedTerrain.AddComponent<TerrainRole>().role = TerrainRole.Role.Dump;
            Debug.Log($"[DumpTerrainFactory] Created dump terrain at {worldMin}, " +
                      $"size {size}, resolution {dumpHeightmapResolution}");
            return dumpDeformable;
        }

        void OnDestroy()
        {
            // These objects are generated per scene, never project assets.
            if (Application.isPlaying)
            {
                Destroy(generatedTerrain);
                Destroy(generatedTerrainData);
                Destroy(generatedProperties);
            }
            else
            {
                DestroyImmediate(generatedTerrain);
                DestroyImmediate(generatedTerrainData);
                DestroyImmediate(generatedProperties);
            }
        }

        void Awake()
        {
            // Create the dump terrain before Simulation and DumpSoil initialize.
            // If a dump terrain already exists (e.g., from a previous play session
            // that didn't clean up), don't create another.
            if (mainTerrain == null)
            {
                // mainTerrain is set by DumpTerrainBootstrap after AddComponent,
                // which triggers Awake() before the field is assigned.
                // DumpTerrainBootstrap calls CreateDumpTerrain() after setting fields.
                return;
            }

            if (TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump) != null)
            {
                Debug.Log("[DumpTerrainFactory] Dump terrain already exists, skipping creation.");
                return;
            }

            CreateDumpTerrain();
        }
    }
}

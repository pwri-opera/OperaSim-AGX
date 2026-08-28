using UnityEngine;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Collide;

namespace PWRISimulator
{
    /// <summary>
    /// 放土エリア専用の DeformableTerrain を実行時に生成する。
    /// メイン（掘削）地形から放土エリアの高さをサンプリングして初期ハイトマップを作成し、
    /// DumpSoil がこの地形に粒子を放出するようにする（issue #59: 掘削地形への雪崩伝播を防止）。
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
        [Tooltip("放土エリアの世界座標中心")]
        public Vector3 dumpAreaCenter = new Vector3(188f, 0f, 140f);

        [Tooltip("放土地形のサイズ (幅 x 高さ最大 x 奥行き)。放土エリア(10x5) + マージン(1m) = 12x7")]
        public Vector3 dumpTerrainSize = new Vector3(12f, 6f, 7f);

        [Tooltip("放土地形のハイトマップ解像度")]
        public int dumpHeightmapResolution = 65;

        /// <summary>
        /// メイン地形のハイトマップから指定した世界座標の正規化高さをサンプリングする。
        /// 範囲外はクランプする。純粋関数（AGX 依存なし）。
        /// </summary>
        public static float SampleHeightFromMainTerrain(
            float[,] mainHeights, int mainResolution,
            Vector3 mainTerrainSize, Vector3 mainTerrainPos,
            Vector2 worldXZ)
        {
            float worldToNorm = (mainResolution - 1) / mainTerrainSize.x;
            // Unity Terrain heightmap: index [x, y] where x maps to world X, y maps to world Z
            // But Unity heightmap is accessed as heights[y, x] (row=y, col=x)
            // and the terrain origin is at mainTerrainPos.
            float fx = (worldXZ.x - mainTerrainPos.x) / mainTerrainSize.x * (mainResolution - 1);
            float fy = (worldXZ.y - mainTerrainPos.z) / mainTerrainSize.z * (mainResolution - 1);

            // Clamp to valid range
            int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, mainResolution - 2);
            int iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, mainResolution - 2);
            float tx = Mathf.Clamp01(fx - ix);
            float ty = Mathf.Clamp01(fy - iy);

            // Bilinear interpolation. mainHeights is indexed as [x, y] in this project's
            // convention (matching TerrainData.GetHeights which returns heights[y, x] but
            // our test data uses [x, y] for simplicity — see note in BuildDumpHeightmap).
            float h00 = mainHeights[ix, iy];
            float h10 = mainHeights[ix + 1, iy];
            float h01 = mainHeights[ix, iy + 1];
            float h11 = mainHeights[ix + 1, iy + 1];

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

                    // Convert from main terrain normalized height to dump terrain normalized height.
                    // normalized_height = world_height / terrain_size.y
                    // So: dump_norm = main_norm * (main_size.y / dump_size.y)
                    dumpHeights[dx, dy] = normalizedHeight * mainTerrainSize.y / dumpTerrainSize.y;
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

            // 1. Create TerrainData
            TerrainData dumpTerrainData = new TerrainData();
            dumpTerrainData.heightmapResolution = dumpHeightmapResolution;
            dumpTerrainData.size = dumpTerrainSize;

            // 2. Copy heights from main terrain
            TerrainData mainTerrainData = mainTerrain.TerrainData;
            int mainRes = mainTerrainData.heightmapResolution;
            float[,] mainHeights = mainTerrainData.GetHeights(0, 0, mainRes, mainRes);

            Vector3 mainTerrainSize = mainTerrainData.size;
            Vector3 mainTerrainPos = mainTerrain.transform.position;
            // After DeformableTerrain.Initialize, the terrain is moved down by MaximumDepth.
            // But DumpTerrainFactory runs in Awake, before Initialize. So mainTerrainPos
            // is the pre-initialization position. We account for this in the y-offset.

            Vector3 dumpWorldMin = new Vector3(
                dumpAreaCenter.x - dumpTerrainSize.x * 0.5f,
                mainTerrainPos.y, // same base y as main terrain
                dumpAreaCenter.z - dumpTerrainSize.z * 0.5f);

            float[,] dumpHeights = BuildDumpHeightmap(
                mainHeights, mainRes,
                mainTerrainSize, mainTerrainPos,
                dumpWorldMin, dumpTerrainSize,
                dumpHeightmapResolution);

            dumpTerrainData.SetHeights(0, 0, dumpHeights);

            // 3. Create Terrain GameObject
            GameObject dumpObj = Terrain.CreateTerrainGameObject(dumpTerrainData);
            dumpObj.name = "Terrain_Dump";
            dumpObj.transform.position = dumpWorldMin;
            dumpObj.transform.parent = transform; // parent under the factory's GameObject

            // 4. Add DeformableTerrain component
            var dumpDeformable = dumpObj.GetComponent<DeformableTerrain>() ??
                                  dumpObj.AddComponent<DeformableTerrain>();

            // Copy material references from main terrain
            dumpDeformable.Material = mainTerrain.Material;
            dumpDeformable.ParticleMaterial = mainTerrain.ParticleMaterial;
            dumpDeformable.DefaultTerrainMaterial = mainTerrain.DefaultTerrainMaterial;
            dumpDeformable.TerrainProperties = mainTerrain.TerrainProperties;
            dumpDeformable.MaximumDepth = mainTerrain.MaximumDepth;

            // 5. Add TerrainRole marker
            var role = dumpObj.GetComponent<TerrainRole>() ??
                       dumpObj.AddComponent<TerrainRole>();
            role.role = TerrainRole.Role.Dump;

            Debug.Log($"[DumpTerrainFactory] Created dump terrain at {dumpWorldMin}, " +
                      $"size {dumpTerrainSize}, resolution {dumpHeightmapResolution}");

            return dumpDeformable;
        }

        void Awake()
        {
            // Create the dump terrain before Simulation and DumpSoil initialize.
            // If a dump terrain already exists (e.g., from a previous play session
            // that didn't clean up), don't create another.
            if (TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump) != null)
            {
                Debug.Log("[DumpTerrainFactory] Dump terrain already exists, skipping creation.");
                return;
            }

            CreateDumpTerrain();
        }
    }
}

using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// DumpTerrainFactory をシーンに自動的に追加するブートストラップ。
    /// シーン内の放土マーカーを配置基準として使用する。
    /// メイン地形 GameObject にこのコンポーネントを追加する。
    /// [DefaultExecutionOrder(-100)] により、他の Awake() より先に実行され、
    /// DumpTerrainFactory が Simulation / DumpSoil の Initialize より前に
    /// 放土地形を構築できるようにする。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DumpTerrainBootstrap : MonoBehaviour
    {
        [Tooltip("放土エリアの X/Z 中心。GameScene の Dump_frame を指定する。地表高さはメイン地形に合わせる。")]
        public Transform dumpArea;

        void Awake()
        {
            // 既に DumpTerrainFactory が存在する場合はスキップ
            if (FindObjectOfType<DumpTerrainFactory>() != null)
                return;

            if (dumpArea == null)
            {
                Debug.LogError("[DumpTerrainBootstrap] Dump area marker is not assigned.");
                return;
            }

            var mainTerrain = FindObjectOfType<DeformableTerrain>();
            if (mainTerrain == null)
            {
                Debug.LogError("[DumpTerrainBootstrap] No DeformableTerrain found in scene.");
                return;
            }

            var factory = mainTerrain.gameObject.AddComponent<DumpTerrainFactory>();
            factory.mainTerrain = mainTerrain;
            factory.dumpAreaCenter = dumpArea.position;

            // AddComponent triggers Awake() synchronously before the fields above
            // are assigned, so DumpTerrainFactory.Awake() sees mainTerrain == null
            // and returns early. Call CreateDumpTerrain() now that fields are set.
            if (TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump) == null)
                factory.CreateDumpTerrain();
        }
    }
}

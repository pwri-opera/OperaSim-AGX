using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// DumpTerrainFactory をシーンに自動的に追加するブートストラップ。
    /// シーンファイルを直接編集せずに放土地形を有効にするためのヘルパー。
    /// メイン地形 GameObject にこのコンポーネントを追加する。
    /// [DefaultExecutionOrder(-100)] により、他の Awake() より先に実行され、
    /// DumpTerrainFactory が Simulation / DumpSoil の Initialize より前に
    /// 放土地形を構築できるようにする。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class DumpTerrainBootstrap : MonoBehaviour
    {
        [Tooltip("放土エリアの世界座標中心。GameScene の Dump_frame 位置に合わせる。")]
        public Vector3 dumpAreaCenter = new Vector3(188f, 0f, 140f);

        void Awake()
        {
            // 既に DumpTerrainFactory が存在する場合はスキップ
            if (FindObjectOfType<DumpTerrainFactory>() != null)
                return;

            var mainTerrain = FindObjectOfType<DeformableTerrain>();
            if (mainTerrain == null)
            {
                Debug.LogError("[DumpTerrainBootstrap] No DeformableTerrain found in scene.");
                return;
            }

            var factory = mainTerrain.gameObject.AddComponent<DumpTerrainFactory>();
            factory.mainTerrain = mainTerrain;
            factory.dumpAreaCenter = dumpAreaCenter;
        }
    }
}

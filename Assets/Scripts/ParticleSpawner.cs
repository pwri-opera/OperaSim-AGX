using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// SpawnメソッドまたはInspectorのSpawnボタン経由で、指示した形状体積の中に粒子を一瞬に作成させるスクリプト。
    /// 参考：agx.ParticleSystem.spawnParticlesInGeometry
    /// </summary>
    public class ParticleSpawner : ScriptComponent
    {
        [Tooltip("Margin around the particles being spawned.")]
        [Range(0.0f, 1.0f)]
        public double margin = 0.0;
        [Tooltip("The factor of the distance that is used to randomize particle positions")]
        [Range(0.0f, 1.0f)]
        public double jitterFactor = 0.0;
        [Tooltip("Use Hexagonal-Close-Packing (HCP) instead of regular carthesian grid packing.")]
        public bool hcpPacking = false;
        [Tooltip("Automically spawn once when start playing.")]
        public bool autoSpawnOnStart = false;

        [Header("Overrides (auto-assigned on Play)")]

        [Tooltip("The shape to spawn particlesin. If None, auto-assigned to the Shape on this game object.")]
        public Shape spawnVolumeShape;
        public DeformableTerrain terrain;

        public bool overrideRadius = false;

        [ConditionalHide(nameof(overrideRadius), hideCompletely = true)]
        public double radius = 0.1;

        readonly Dictionary<double, agx.ParticleEmitter.DistributionTable> distributionTables =
            new Dictionary<double, agx.Emitter.DistributionTable>();

        protected override bool Initialize()
        {
            if (terrain == null)
                terrain = FindObjectOfType<DeformableTerrain>();

            if (spawnVolumeShape == null)
                spawnVolumeShape = GetComponent<Shape>();

            if (autoSpawnOnStart)
                Spawn();

            return base.Initialize();
        }

        public void Spawn()
        {
            if (terrain?.GetInitialized<DeformableTerrain>() == null || spawnVolumeShape?.GetInitialized<Shape>() == null)
                return;

            agxTerrain.SoilSimulationInterface soilSim = terrain.Native?.getSoilSimulationInterface();
            agx.GranularBodySystem granularSystem = soilSim?.getGranularBodySystem();

            // ユーザ設定次第、Terrainの粒子の半径か指示された半径を使用
            radius = overrideRadius ? radius : terrain.Native.getParticleNominalRadius();
            
            // 半径を使用するDistributionTableを取得するが、まだ作っていない場合は作成して保存
            agx.ParticleEmitter.DistributionTable distTable;
            if (distributionTables.ContainsKey(radius))
            {
                distTable = distributionTables[radius];
            }
            else
            {
                distTable = new agx.ParticleEmitter.DistributionTable();
                distTable.addModel(new agx.ParticleEmitter.DistributionModel(
                    radius, terrain.Native.getMaterial(agxTerrain.Terrain.MaterialType.PARTICLE), 1));
                distributionTables[radius] = distTable;
            }

            // marginとradiusを使って、粒子同士の原点距離を計算
            agx.Vec3 spacing = new agx.Vec3((radius + margin) * 2.0);

            // 粒子がSpawnゾーンのGeometryと衝突しないようにする
            granularSystem.setEnableCollisions(spawnVolumeShape.NativeGeometry, false);

            // 粒子を生成する
            agxData.EntityRange particles = hcpPacking ?
                granularSystem.spawnParticlesInGeometryHCP(
                    spawnVolumeShape.NativeGeometry, distTable, spacing, jitterFactor) :
                granularSystem.spawnParticlesInGeometry(
                    spawnVolumeShape.NativeGeometry, distTable, spacing, jitterFactor);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ParticleSpawner))]
    public class ParticleSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトGUIを表示
            base.OnInspectorGUI();

            // Spawnボタンを追加
            if(GUILayout.Button("Spawn"))
            {
                (target as ParticleSpawner).Spawn();
            }
        }
    }
#endif
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using PWRISimulator.ROS;

namespace PWRISimulator
{
    /// <summary>
    /// リセット処理
    /// </summary>
    public class StageReset : MonoBehaviour
    {
        private const string fileName = "StartTerrain";

        // AgxDynamicsの内蔵のTerrainオブジェクト。
        private agxTerrain.Terrain terrainNative;

        private DeformableTerrain terrain;

        private GameObject shovelObj;
        private string shovelName = SpawnObject.zx200_objName;
        private Vector3 shovelPos;
        private Quaternion shovelQut;

        // リセット時にダンプトラックを初期位置に復元するための保存データ
        private struct DumpTruckSaveData
        {
            public string name;
            public Vector3 position;
            public Quaternion rotation;
        }
        private List<DumpTruckSaveData> savedDumpTrucks = new List<DumpTruckSaveData>();

        // Start is called before the first frame update
        void Start()
        {
            // 初期地形を保存
            var saveScript = new saveScript();
            saveScript.SerializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));

            // ショベルカーの位置を保存
            shovelObj = Zx200ObjectUtility.FindZx200Object();
            if (shovelObj != null)
            {
                shovelName = shovelObj.name;
                shovelPos = shovelObj.transform.position;
                shovelQut = shovelObj.transform.rotation;
            }

            // ダンプトラックの初期位置を保存
            SaveDumpTruckPositions();
        }

        /// <summary>
        /// 現在配置されているダンプトラックの位置・姿勢を保存する。
        /// Dump_ObjListに加えて、FindObjectsOfTypeでリスト外のトラックも検索する。
        /// </summary>
        void SaveDumpTruckPositions()
        {
            savedDumpTrucks.Clear();

            // Dump_ObjListから保存
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < GlobalVariables.Dump_ObjList.Count; i++)
            {
                GameObject dumpObj = GlobalVariables.Dump_ObjList[i];
                if (dumpObj == null || seen.Contains(dumpObj))
                    continue;
                seen.Add(dumpObj);
                savedDumpTrucks.Add(new DumpTruckSaveData
                {
                    name = dumpObj.name,
                    position = dumpObj.transform.position,
                    rotation = dumpObj.transform.rotation
                });
            }

            // FindObjectsOfTypeでリスト外のトラックも検索して保存
            foreach (var dumpInput in FindObjectsOfType<DumpTruckInput>(true))
            {
                var dumpRoot = dumpInput.transform.root.gameObject;
                if (TerrainSaveUtility.IsSavedDumpTruckRootName(dumpRoot.name) && !seen.Contains(dumpRoot) && dumpRoot.activeSelf)
                {
                    seen.Add(dumpRoot);
                    savedDumpTrucks.Add(new DumpTruckSaveData
                    {
                        name = dumpRoot.name,
                        position = dumpRoot.transform.position,
                        rotation = dumpRoot.transform.rotation
                    });
                }
            }

            UnityEngine.Debug.Log("StageReset: Saved " + savedDumpTrucks.Count + " dump truck(s) for reset.");
        }

        /// <summary>
        /// 保存した位置・姿勢でダンプトラックを再配置する。
        /// </summary>
        void RespawnDumpTrucks()
        {
            var ic120obj = new ic120obj();
            foreach (var saved in savedDumpTrucks)
            {
                // IDを名前から抽出
                int lastUnderscore = saved.name.LastIndexOf("_");
                int spawnID = 0;
                if (lastUnderscore >= 0 && int.TryParse(saved.name.Substring(lastUnderscore + 1), out int parsed))
                    spawnID = parsed;

                ic120obj.Spawn_ic120(saved.position, saved.rotation, spawnID, SpawnObject.ic120_path);
                GlobalVariables.ic120Counter++;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // リセットが実行された場合
            if (GlobalVariables.SelectMode == 2)
            {
                // 泥濘エリアのカウントリセット
                GlobalVariables.countMat.Clear();

                // AGX地形取得
                if (terrain == null)
                {
                    terrain = FindObjectOfType<DeformableTerrain>();
                }

                // 土壌粒子モデルを削除（降順で削除してインデックスを安定させる）
                var soilSim = terrain.Native?.getSoilSimulationInterface();
                if (soilSim != null)
                {
                    var soilParticles = soilSim.getSoilParticles();
                    for (int i = (int)soilParticles.size() - 1; i >= 0; i--)
                    {
                        var particle = soilParticles.at((uint)i);
                        soilSim.removeSoilParticle(particle);
                        particle.ReturnToPool();
                    }
                }


                // 保存した初期地形を読込
                var loadScript = new loadScript();
                loadScript.DeserializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));

                // ハイトマップのリセット
                terrain.ResetHeights();

                // 地形スコアリングのリセット
                TerrainScore.Reset();


                // ショベルカーを削除（SetActive(false)でAGXコールバックを解除してからDestroy）
                shovelObj = Zx200ObjectUtility.FindZx200Object();
                if (shovelObj != null)
                {
                    shovelName = shovelObj.name;
                    shovelObj.SetActive(false);
                    UnityEngine.Object.Destroy(shovelObj);
                }


                UnityEngine.Debug.Log("Dump_IDList.Count: " + GlobalVariables.Dump_IDList.Count);
                UnityEngine.Debug.Log("Dump_ObjList.Count: " + GlobalVariables.Dump_ObjList.Count);

                // 削除対象のダンプトラックを収集（Dump_ObjList + FindObjectsOfType）
                var dumpObjectsToDestroy = new HashSet<GameObject>();
                for (int i = 0; i < GlobalVariables.Dump_ObjList.Count; i++)
                {
                    if (GlobalVariables.Dump_ObjList[i] != null)
                        dumpObjectsToDestroy.Add(GlobalVariables.Dump_ObjList[i]);
                }
                foreach (var dumpInput in FindObjectsOfType<DumpTruckInput>(true))
                {
                    var dumpRoot = dumpInput.transform.root.gameObject;
                    if (TerrainSaveUtility.IsSavedDumpTruckRootName(dumpRoot.name))
                        dumpObjectsToDestroy.Add(dumpRoot);
                }

                // ダンプトラック削除（SetActive(false)でAGXコールバックを解除してからDestroy）
                foreach (GameObject dumpObj in dumpObjectsToDestroy)
                {
                    if (dumpObj != null)
                    {
                        dumpObj.SetActive(false);
                        UnityEngine.Object.Destroy(dumpObj);
                        GameObject objMassBody = GameObject.Find(dumpObj.name + "_SoilMassBody");
                        if (objMassBody != null)
                        {
                            objMassBody.SetActive(false);
                            UnityEngine.Object.Destroy(objMassBody);
                        }
                        GameObject objMassJoint = GameObject.Find(dumpObj.name + "_SoilMassJoint");
                        if (objMassJoint != null)
                        {
                            objMassJoint.SetActive(false);
                            UnityEngine.Object.Destroy(objMassJoint);
                        }
                    }
                }



                // 保持しているダンプトラックオブジェクトリストのクリア
                GlobalVariables.Dump_IDList.Clear();
                GlobalVariables.Dump_ObjList.Clear();

                // カウンターのクリア
                GlobalVariables.CameraCounter = 0;
                GlobalVariables.ic120Counter = 0;


                // ショベルカー再配置
                GameObject zx200_prefab = Resources.Load<GameObject>(SpawnObject.zx200_path);
                shovelObj = (GameObject)UnityEngine.Object.Instantiate(zx200_prefab, shovelPos, shovelQut);
                shovelObj.name = shovelName;

                StartCoroutine(Zx200ObjectUtility.AttachShovelToTerrainWhenInitialized(terrain, shovelObj));


                // ショベルカー
                var cameraObj = shovelObj.transform.Find("base_link/track_link/CameraStr").gameObject;
                cameraObj.SetActive(false);


                // ダンプトラック再配置
                RespawnDumpTrucks();


                //GlobalVariables.ForceCameraChange = true;
                CameraChanger.Reset();


                // フラグを下ろす
                GlobalVariables.SelectMode = -1;
            }
        }
    }
}

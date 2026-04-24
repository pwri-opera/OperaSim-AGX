using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;

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

                // 土壌粒子モデルを削除
                var soilSim = terrain.Native?.getSoilSimulationInterface();
                var soilParticles = soilSim.getSoilParticles();

                for (uint i = 0; i < soilParticles.size(); i++)
                {
                    soilSim.removeSoilParticle(soilParticles.at(i));
                }


                // 保存した初期地形を読込
                var loadScript = new loadScript();
                loadScript.DeserializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));

                // ハイトマップのリセット
                terrain.ResetHeights();

                // 地形スコアリングのリセット
                TerrainScore.Reset();


                // ショベルカーを削除
                shovelObj = Zx200ObjectUtility.FindZx200Object();
                if (shovelObj != null)
                {
                    shovelName = shovelObj.name;
                    UnityEngine.Object.Destroy(shovelObj);
                }


                UnityEngine.Debug.Log("Dump_IDList.Count: " + GlobalVariables.Dump_IDList.Count);
                UnityEngine.Debug.Log("Dump_ObjList.Count: " + GlobalVariables.Dump_ObjList.Count);

                // ダンプトラック削除
                for (int i = 0; i < GlobalVariables.Dump_ObjList.Count; i++)
                {
                    UnityEngine.Debug.Log("ID: " + GlobalVariables.Dump_IDList[i]);

                    GameObject dumpObj = GlobalVariables.Dump_ObjList[i];

                    if (dumpObj != null)
                    {
                        // 削除
                        Destroy(dumpObj);
                        GameObject objMassBody = GameObject.Find(dumpObj.name + "_SoilMassBody");
                        if (objMassBody != null) Destroy(objMassBody);
                        GameObject objMassJoint = GameObject.Find(dumpObj.name + "_SoilMassJoint");
                        if (objMassJoint != null) Destroy(objMassJoint);
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


                //GlobalVariables.ForceCameraChange = true;
                CameraChanger.Reset();


                // フラグを下ろす
                GlobalVariables.SelectMode = -1;
            }
        }
    }
}

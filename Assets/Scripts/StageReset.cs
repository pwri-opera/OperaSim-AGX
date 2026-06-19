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
    /// ���Z�b�g����
    /// </summary>
    public class StageReset : MonoBehaviour
    {
        private const string fileName = "StartTerrain";

        // AgxDynamics�̓�����Terrain�I�u�W�F�N�g�B
        private agxTerrain.Terrain terrainNative;

        private DeformableTerrain terrain;

        private GameObject shovelObj;
        private Vector3 shovelPos;
        private Quaternion shovelQut;

        // Start is called before the first frame update
        void Start()
        {
            // �����n�`��ۑ�
            var saveScript = gameObject.AddComponent<saveScript>();
            saveScript.SerializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));
            Destroy(saveScript);

            // �V���x���J�[�̈ʒu��ۑ�
            shovelObj = GameObject.Find(SpawnObject.zx200_objName);
            if (shovelObj != null)
            {
                shovelPos = shovelObj.transform.position;
                shovelQut = shovelObj.transform.rotation;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // ���Z�b�g�����s���ꂽ�ꍇ
            if (GlobalVariables.SelectMode == 2)
            {
                // �D�^�G���A�̃J�E���g���Z�b�g
                GlobalVariables.countMat.Clear();

                // AGX�n�`�擾
                if (terrain == null)
                {
                    terrain = FindObjectOfType<DeformableTerrain>();
                }

                // �y�뗱�q���f�����폜
                var soilSim = terrain.Native?.getSoilSimulationInterface();
                var soilParticles = soilSim.getSoilParticles();

                for (uint i = 0; i < soilParticles.size(); i++)
                {
                    soilSim.removeSoilParticle(soilParticles.at(i));
                }


                // �ۑ����������n�`��Ǎ�
                var loadScript = gameObject.AddComponent<loadScript>();
                loadScript.DeserializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));
                Destroy(loadScript);

                // �n�C�g�}�b�v�̃��Z�b�g
                terrain.ResetHeights();

                // �n�`�X�R�A�����O�̃��Z�b�g
                TerrainScore.Reset();


                // �V���x���J�[���폜
                shovelObj = GameObject.Find(SpawnObject.zx200_objName);
                if (shovelObj != null)
                {
                    UnityEngine.Object.Destroy(shovelObj);
                }


                UnityEngine.Debug.Log("Dump_IDList.Count: " + GlobalVariables.Dump_IDList.Count);
                UnityEngine.Debug.Log("Dump_ObjList.Count: " + GlobalVariables.Dump_ObjList.Count);

                // �_���v�g���b�N�폜
                for (int i = 0; i < GlobalVariables.Dump_ObjList.Count; i++)
                {
                    UnityEngine.Debug.Log("ID: " + GlobalVariables.Dump_IDList[i]);

                    GameObject dumpObj = GlobalVariables.Dump_ObjList[i];

                    if (dumpObj != null)
                    {
                        // �폜
                        Destroy(dumpObj);
                        GameObject objMassBody = GameObject.Find(dumpObj.name + "_SoilMassBody");
                        if (objMassBody != null) Destroy(objMassBody);
                        GameObject objMassJoint = GameObject.Find(dumpObj.name + "_SoilMassJoint");
                        if (objMassJoint != null) Destroy(objMassJoint);
                    }
                }



                // �ێ����Ă���_���v�g���b�N�I�u�W�F�N�g���X�g�̃N���A
                GlobalVariables.Dump_IDList.Clear();
                GlobalVariables.Dump_ObjList.Clear();

                // �J�E���^�[�̃N���A
                GlobalVariables.CameraCounter = 0;
                GlobalVariables.ic120Counter = 0;


                // �V���x���J�[�Ĕz�u
                GameObject zx200_prefab = Resources.Load<GameObject>(SpawnObject.zx200_path);
                shovelObj = (GameObject)UnityEngine.Object.Instantiate(zx200_prefab, shovelPos, shovelQut);
                shovelObj.name = SpawnObject.zx200_objName;


                // �V���x���J�[
                Transform obj = shovelObj.transform.Find("base_link/body_link/CameraStr")
                    ?? shovelObj.transform.Find("base_link/track_link/CameraStr");
                if (obj != null)                {
                    obj.gameObject.SetActive(false);
                }

                //GlobalVariables.ForceCameraChange = true;
                CameraChanger.Reset();


                // �V���x���J�[�@��ݒ�
                var shovel = FindObjectOfType<DeformableTerrainShovel>();
                terrain.Native.add(shovel.GetInitialized<DeformableTerrainShovel>()?.Native);


                // �t���O�����낷
                GlobalVariables.SelectMode = -1;
            }
        }
    }
}

using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using PWRISimulator.ROS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
# endif

using Debug = UnityEngine.Debug;


namespace PWRISimulator
{
    /// <summary>
    /// �n�`�̃��[�h����
    /// </summary>
    public class loadScript : MonoBehaviour
    {
        private const string setDirName = "setting";
        private const string simDirName = "simulation";
        private const string fileName = "SaveTerrain";

        public bool completedFlag;

        private DeformableTerrain terrain;


        public void DeserializeTerrain(string path)
        {
            double time = Time.realtimeSinceStartup;

            // �o�C�i���t�@�C���ǂݍ���
            StreamReader reader = new StreamReader(path + ".ter");
            string jsonString = reader.ReadToEnd();

            // �n�`�f�[�^�̔z��ň���
            saveScript.SaveData save = new saveScript.SaveData();
            save = JsonUtility.FromJson<saveScript.SaveData>(jsonString);

            //Debug.Log(save.list.Length);


            foreach (saveScript.SerializedTerrain st in save.list)
            {
                // �����̃I�u�W�F�N�g������
                GameObject obj = GameObject.Find(st.name);

                if (obj != null)
                {
                    // �����̃I�u�W�F�N�g�����݂�����擾
                    TerrainData terrainData = obj.GetComponent<Terrain>().terrainData;

                    // �n�`�ǂݍ���
                    terrainData.SetAlphamaps(0, 0, ConvertFromFlat(st.alphas, terrainData.alphamapResolution, terrainData.alphamapResolution, terrainData.alphamapLayers));
                    TerrainSaveUtility.ApplySerializedHeights(obj, ConvertFromFlat(st.heights, terrainData.heightmapResolution, terrainData.heightmapResolution));
                }
            }

            Debug.Log("COMPLETED IN " + ((Time.realtimeSinceStartup - time)) + " " + jsonString.Length);

            // ���������t���O
            completedFlag = true;
        }


        private float[] ConvertToFlat(float[,] arr)
        {
            int len = arr.GetLength(0) * arr.GetLength(1);
            float[] ret = new float[len];
            int index = 0;
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int i2 = 0; i2 < arr.GetLength(1); i2++)
                {
                    ret[index] = arr[i, i2];
                    index++;
                }
            }
            return ret;
        }

        private float[,] ConvertFromFlat(float[] arr, int len1, int len2)
        {
            float[,] ret = new float[len1, len2];
            int index = 0;
            for (int i = 0; i < len1; i++)
            {
                for (int i2 = 0; i2 < len2; i2++)
                {
                    ret[i, i2] = arr[index];
                    index++;
                }
            }
            return ret;
        }

        private float[] ConvertToFlat(float[,,] arr)
        {
            int len = arr.GetLength(0) * arr.GetLength(1) * arr.GetLength(2); ;
            float[] ret = new float[len];
            int index = 0;
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int i2 = 0; i2 < arr.GetLength(1); i2++)
                {
                    for (int i3 = 0; i3 < arr.GetLength(2); i3++)
                    {
                        ret[index] = arr[i, i2, i3];
                        index++;
                    }
                }
            }
            return ret;
        }

        private float[,,] ConvertFromFlat(float[] arr, int len1, int len2, int len3)
        {
            float[,,] ret = new float[len1, len2, len3];
            int index = 0;
            for (int i = 0; i < len1; i++)
            {
                for (int i2 = 0; i2 < len2; i2++)
                {
                    for (int i3 = 0; i3 < len3; i3++)
                    {
                        ret[i, i2, i3] = arr[index];
                        index++;
                    }
                }
            }
            return ret;
        }


        // �{�^���������ꂽ�ꍇ�ɌĂяo��
        public void OnClick()
        {
            // ���������t���O
            completedFlag = false;

            // �ۑ��f�B���N�g��
            var dirPath = "";
            if (GlobalVariables.ActionMode == 3)
            {
                dirPath = Path.Combine(GlobalVariables.BACKUP_FOLDER, simDirName);
            }
            else
            {
                dirPath = Path.Combine(GlobalVariables.BACKUP_FOLDER, setDirName);
            }

            // �ۑ��f�B���N�g�����Ȃ���Γǂݍ��߂Ȃ��̂ŏI��
            if (!Directory.Exists(dirPath))
            {
                completedFlag = true;
                return;
            }


            //----------
            // �ۑ��n�̃t�@�C���ǂݍ���
            //----------
            // �p��
            StreamReader rd_ms = new StreamReader(Path.Combine(dirPath, "MachinesJoints"));
            string str_ms = rd_ms.ReadToEnd();
            rd_ms.Close();

            saveScript.SaveMachines json_ms = JsonUtility.FromJson<saveScript.SaveMachines>(str_ms);

            GlobalVariables.saveMachines = json_ms;

            GlobalVariables.MachineCameraSliders.Clear();
            if (json_ms.cameraSliders != null)
            {
                foreach (var s in json_ms.cameraSliders)
                {
                    if (s != null && !string.IsNullOrEmpty(s.machineName))
                    {
                        GlobalVariables.MachineCameraSliders[s.machineName] = s;
                    }
                }
            }


            // �ύ�
            StreamReader rd_ds = new StreamReader(Path.Combine(dirPath, "DumpSoil"));
            string str_ds = rd_ds.ReadToEnd();
            rd_ds.Close();

            saveScript.SaveDumpSoil json_ds = JsonUtility.FromJson<saveScript.SaveDumpSoil>(str_ds);

            GlobalVariables.saveDumpSoil = json_ds;


            // �y�뗱�q���f��
            StreamReader rd = new StreamReader(Path.Combine(dirPath, "SoilParticles"));
            string str_p = rd.ReadToEnd();
            rd.Close();

            saveScript.SaveParticles json = JsonUtility.FromJson<saveScript.SaveParticles>(str_p);

            GlobalVariables.saveParticles = json;


            //----------
            // �n�`�ēǂݍ���
            //----------
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

            // �n�`��Ǎ�
            DeserializeTerrain(Path.Combine(dirPath, fileName));

            // �n�`�X�R�A�����O�̃��Z�b�g
            TerrainScore.Reset();


            //----------
            // �d�@�ēǂݍ���
            //----------
            // �V���x���J�[�폜
            GameObject _shovelObj = Zx200ObjectUtility.FindZx200Object();
            if (_shovelObj != null)
            {
                UnityEngine.Object.Destroy(_shovelObj);
            }

            // �ʒu
            Vector3 _pos = new Vector3((float)json_ms.data[0].p.x, (float)json_ms.data[0].p.y, (float)json_ms.data[0].p.z);
            Debug.Log(_pos);

            // ��]
            Quaternion _qut = new Quaternion((float)json_ms.data[0].q.x, (float)json_ms.data[0].q.y, (float)json_ms.data[0].q.z, (float)json_ms.data[0].q.w);
            Debug.Log(_qut);

            // �V���x���J�[�Ĕz�u
            GameObject zx200_prefab = Resources.Load<GameObject>(SpawnObject.zx200_path);
            GameObject shovelObj = (GameObject)UnityEngine.Object.Instantiate(zx200_prefab, _pos, _qut);
            shovelObj.name = string.IsNullOrEmpty(json_ms.data[0].name) ? SpawnObject.zx200_objName : json_ms.data[0].name;
            shovelObj.SetActive(json_ms.data[0].active);

            StartCoroutine(Zx200ObjectUtility.AttachShovelToTerrainWhenInitialized(terrain, shovelObj));


            Debug.Log("Dump_IDList.Count: " + GlobalVariables.Dump_IDList.Count);
            Debug.Log("Dump_ObjList.Count: " + GlobalVariables.Dump_ObjList.Count);


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

            // �_���v�g���b�N�폜
            foreach (GameObject dumpObj in dumpObjectsToDestroy)
            {
                if (dumpObj != null)
                {
                    // �폜
                    dumpObj.SetActive(false);
                    Destroy(dumpObj);
                    GameObject objMassBody = GameObject.Find(dumpObj.name + "_SoilMassBody");
                    if (objMassBody != null) Destroy(objMassBody);
                    GameObject objMassJoint = GameObject.Find(dumpObj.name + "_SoilMassJoint");
                    if (objMassJoint != null) Destroy(objMassJoint);
                }
            }

            foreach (var dumpInput in FindObjectsOfType<DumpTruckInput>(true))
            {
                var dumpRoot = dumpInput.transform.root.gameObject;
                if (TerrainSaveUtility.IsSavedDumpTruckRootName(dumpRoot.name))
                {
                    dumpRoot.SetActive(false);
                }
            }

            // �ێ����Ă���_���v�g���b�N�I�u�W�F�N�g���X�g�̃N���A
            GlobalVariables.Dump_IDList.Clear();
            GlobalVariables.Dump_ObjList.Clear();

            GlobalVariables.ic120Counter = 0;


            // �_���v�g���b�N�Ɋւ�����ǂݍ���
            for (int i = 1; i < json_ms.data.Length; i++)
            {
                // �Ĕz�u
                GameObject ic120_prefab = Resources.Load<GameObject>("Prefabs/ic120_prefVar");
                GameObject ic120obj = (GameObject)UnityEngine.Object.Instantiate(ic120_prefab, json_ms.data[i].p, json_ms.data[i].q);
                ic120obj.name = json_ms.data[i].name;

                // ID��ێ�
                GlobalVariables.Dump_IDList.Add(json_ms.data[i].id);
                // �I�u�W�F�N�g��ێ�
                GlobalVariables.Dump_ObjList.Add(ic120obj);

                GlobalVariables.ic120Counter += 1;
            }


            //----------
            // �J�����ēǂݍ���
            //----------
            // ���ݔz�u����Ă���J�������폜
            List<Camera> cameras = new List<Camera>();
            cameras.AddRange(FindObjectsOfType<Camera>(true));

            for (int i = 0; i < cameras.Count; i++)
            {
                //Debug.Log(cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"));

                // �ǉ������J�����̂ݍ폜
                if (cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"))
                {
                    UnityEngine.Object.Destroy(cameras[i].gameObject.transform.root.gameObject);
                }
            }

            // �J�����̃J�E���g���Z�b�g
            GlobalVariables.CameraCounter = 0;


            // �J�����ʒu�ǂݍ���
            for (int i = 0; i < json_ms.camera.Length; i++)
            {
                if (json_ms.camera[i].name.Contains("Camera_"))
                {
                    // �ǉ������J����
                    var cameraObj = new cameraObj();
                    cameraObj.Spawn_Camera(json_ms.camera[i].p, json_ms.camera[i].q, Int32.Parse(json_ms.camera[i].id), SpawnObject.camera_path);

                    var obj = GameObject.Find(json_ms.camera[i].name + "/CameraStr");
                    if (obj != null)
                    {
                        obj.SetActive(json_ms.camera[i].active);
                    }

                    GlobalVariables.CameraCounter += 1;

                }
                else if (json_ms.camera[i].name.Contains("MainCameraStr"))
                {
                    // ���C���J����
                    var obj = GameObject.Find(json_ms.camera[i].name);
                    if (obj != null)
                    {
                        obj.transform.position = json_ms.camera[i].p;
                        obj.transform.rotation = json_ms.camera[i].q;
                        obj.SetActive(json_ms.camera[i].active);
                    }
                }
                else
                {
                    // �d�@�̃J����
                    GameObject obj = null;
                    if (json_ms.camera[i].name.Contains("zx200"))
                    {
                        // �V���x���J�[
                        var shovelCameraStr = shovelObj.transform.Find("base_link/body_link/CameraStr")
                            ?? shovelObj.transform.Find("base_link/track_link/CameraStr");
                        obj = shovelCameraStr != null ? shovelCameraStr.gameObject : null;
                    }
                    else
                    {
                        // �_���v�g���b�N
                        for (int j = 0; j < GlobalVariables.Dump_ObjList.Count; j++)
                        {
                            if (GlobalVariables.Dump_ObjList[j].name == json_ms.camera[i].name)
                            {
                                obj = GlobalVariables.Dump_ObjList[j].transform.Find("base_link/track_link/CameraStr").gameObject;
                                break;
                            }
                        }
                        //var p_obj = GameObject.Find(json_ms.camera[i].name);
                        //obj = p_obj.transform.Find("base_link/track_link/CameraStr").gameObject;
                    }

                    if (obj != null)
                    {
                        Debug.Log(json_ms.camera[i].name + ", " + json_ms.camera[i].active);

                        obj.transform.position = json_ms.camera[i].p;
                        obj.transform.rotation = json_ms.camera[i].q;
                        obj.SetActive(json_ms.camera[i].active);
                    }
                }
            }



            // �D�^�G���A�̃J�E���g�s��Ǎ�
            using (StreamReader sr = new StreamReader(Path.Combine(dirPath, "MudAreaMatrix")))
            {
                string content = sr.ReadToEnd();
                string[] strAry = content.Split(',');

                int rows = GlobalVariables.countMat.RowCount;
                int cols = GlobalVariables.countMat.ColumnCount;

                Debug.Log("rows: " + rows + ", cols: " + cols);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        GlobalVariables.countMat[i, j] = double.Parse(strAry[(rows * i - 1) + (j + 1)]);
                    }
                }
            }


            // Debug
            //using (StreamWriter sw = new StreamWriter(Path.Combine(dirPath, "debug_MudAreaMatrix")))
            //{
            //    //sw.Write(GlobalVariables.countMat);

            //    int rows = GlobalVariables.countMat.RowCount;
            //    int cols = GlobalVariables.countMat.ColumnCount;

            //    Debug.Log("rows: " + rows + ", cols: " + cols);

            //    for (int i = 0; i < rows; i++)
            //    {
            //        string line = "";
            //        for (int j = 0; j < cols; j++)
            //        {
            //            line = line + GlobalVariables.countMat[i, j].ToString() + ",";
            //        }
            //        sw.WriteLine(line);
            //    }
            //}


            // �J�E���g���Z�b�g
            GlobalVariables.SetupJointDumpCount = 0;

            // �t���O��؂�ւ��ē���J�n
            GlobalVariables.SetupJointFlag = true;
            GlobalVariables.SetupJointDumpFlag = true;


            //DeserializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));
        }
    }
}
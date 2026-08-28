using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using PWRISimulator.ROS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
# if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
# endif

using Debug = UnityEngine.Debug;


namespace PWRISimulator
{
    /// <summary>
    /// �n�`�̃Z�[�u����
    /// </summary>
    public class saveScript : MonoBehaviour
    {
        private const string setDirName = "setting";
        private const string simDirName = "simulation";
        private const string fileName = "SaveTerrain";

        public bool completedFlag;

        private DeformableTerrain terrain;


        [System.Serializable]
        public class SaveData
        {
            public SerializedTerrain[] list;
        }

        [System.Serializable]
        public class SerializedTerrain
        {
            public float[] heights;
            public float[] alphas;
            public string name; //add 
        }


        [System.Serializable]
        public class SaveParticles
        {
            public Particles[] data;
        }

        [System.Serializable]
        public class Particles
        {
            public agxVec3 position;
            public agxVec3 velocity;
            public double radius;
            public double mass;
        }

        [System.Serializable]
        public class agxVec3
        {
            public double x;
            public double y;
            public double z;
        }

        // �_���v�g���b�N�̐ύ�
        [System.Serializable]
        public class SaveDumpSoil
        {
            public TransDumpSoil[] data;
        }

        [System.Serializable]
        public class TransDumpSoil
        {
            public string id;
            public double mass;
        }

        // �d�@�̎p��(���ԂƃX�R�A���ǉ�)
        [System.Serializable]
        public class SaveMachines
        {
            public float time;
            public int score;
            public objProperties[] data;
            public objProperties[] camera;
            public MachineCameraSliderState[] cameraSliders;
        }

        [System.Serializable]
        public class MachineCameraSliderState
        {
            public string machineName;
            public float horizontalAngle;
            public float verticalAngle;
            public float upDownPos;
            public float frontRearPos;
            public float leftRightPos;
        }

        [System.Serializable]
        public class objJoint
        {
            public double swing_joint;
            public double boom_joint;
            public double arm_joint;
            public double bucket_joint;
            public double right_track;
            public double left_track;
            public double dump_joint;
        }

        [System.Serializable]
        public class objProperties
        {
            public string name;
            public string id;
            public Vector3 p;
            public Quaternion q;
            public objJoint joint;
            public bool active;
        }


        public void SerializeTerrain(string path)
        {
            double time = Time.realtimeSinceStartup;

            SaveData save = new SaveData();
            save.list = new SerializedTerrain[Terrain.activeTerrains.Length];

            int i = 0;

            // �e���C���f�[�^���擾
            foreach (Terrain _terrain in Terrain.activeTerrains)
            {
                TerrainData terrainData = _terrain.terrainData;

                SerializedTerrain st = new SerializedTerrain();
                st.heights = ConvertToFlat(TerrainSaveUtility.GetSerializableHeights(_terrain));
                st.alphas = ConvertToFlat(terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution));
                st.name = _terrain.name;

                save.list[i] = st;

                i++;
            }

            string json = JsonUtility.ToJson(save);

            StreamWriter writer = new StreamWriter(path + ".ter", false);
            writer.Write(json);
            writer.Close();

            Debug.Log("SaveScript COMPLETED IN " + ((Time.realtimeSinceStartup - time)) + " " + json.Length);


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

            // �ۑ��f�B���N�g�����Ȃ���΍쐬
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }



            // ���ԂƃX�R�A��ێ�
            var myTime = CountdownTimer.timeRemaining;
            var myScore = GlobalVariables.score;


            // �n�`���q���f�����擾
            if (terrain == null)
            {
                terrain = FindObjectOfType<DeformableTerrain>();
            }

            var soilSim = terrain.Native.getSoilSimulationInterface();
            var soilParticles = soilSim.getSoilParticles();

            SaveParticles save = new SaveParticles();
            save.data = new Particles[soilParticles.size()];

            if ((int)soilParticles.size() > 0)
            {
                for (uint i = 0; i < soilParticles.size(); i++)
                {
                    //Debug.Log("Index: " + i);
                    save.data[i] = new Particles();

                    // agx::Physics::GranularBodyPtr ����ʒu��a���擾
                    var pos = soilParticles.at(i).getPosition();
                    //Debug.Log("Position: " + pos[0] + ", " + pos[1] + ", " + pos[2]);
                    save.data[i].position = new agxVec3();
                    save.data[i].position.x = pos[0];
                    save.data[i].position.y = pos[1];
                    save.data[i].position.z = pos[2];

                    var vel = soilParticles.at(i).getVelocity();
                    //Debug.Log("Velocity: " + vel[0] + ", " + vel[1] + ", " + vel[2]);
                    save.data[i].velocity = new agxVec3();
                    save.data[i].velocity.x = vel[0];
                    save.data[i].velocity.y = vel[1];
                    save.data[i].velocity.z = vel[2];

                    var rad = soilParticles.at(i).getRadius();
                    //Debug.Log("Radius: " + rad);
                    save.data[i].radius = rad;

                    var mas = soilParticles.at(i).getMass();
                    //Debug.Log("Mass: " + mas);
                    save.data[i].mass = mas;
                }
            }

            // �n�`���q���f����ۑ�
            if (save.data != null)
            {
                string json = JsonUtility.ToJson(save, true);
                StreamWriter sw = new StreamWriter(Path.Combine(dirPath, "SoilParticles"));
                sw.Write(json);
                sw.Flush();
                sw.Close();
            }


            // �n�`��ۑ�
            SerializeTerrain(Path.Combine(dirPath, fileName));


            // �d�@�̎p���ێ�
            SaveMachines sm = new SaveMachines();
            int num = (int)GlobalVariables.Dump_ObjList.Count + 1;
            sm.data = new objProperties[num];
            sm.data[0] = new objProperties();


            // ���ԂƃX�R�A
            sm.time = myTime;
            sm.score = myScore;


            // �J�����z�u�ۑ�
            List<Camera> cameras = new List<Camera>();
            cameras.AddRange(FindObjectsOfType<Camera>(true));

            var tmpList = new List<objProperties>();

            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i].gameObject.name.Contains("MainCameraStr") ||
                    cameras[i].gameObject.name.Contains("Str") &&
                    (cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_") ||
                    cameras[i].gameObject.transform.root.gameObject.name.Contains("zx200") ||
                    cameras[i].gameObject.transform.root.gameObject.name.Contains("ic120")))
                {
                    var tmp = new objProperties();
                    tmp.name = cameras[i].gameObject.transform.root.gameObject.name;

                    if (cameras[i].gameObject.transform.root.gameObject.name.Contains("Camera_"))
                    {
                        string text = cameras[i].gameObject.transform.root.gameObject.name;
                        int index = text.LastIndexOf("_");
                        tmp.id = text.Substring(index + 1);

                        tmp.p = cameras[i].gameObject.transform.root.gameObject.transform.position;
                        tmp.q = cameras[i].gameObject.transform.root.gameObject.transform.rotation;

                    }
                    else if (cameras[i].gameObject.name.Contains("MainCameraStr"))
                    {
                        tmp.p = cameras[i].gameObject.transform.position;
                        tmp.q = cameras[i].gameObject.transform.rotation;
                    }
                    else
                    {
                        tmp.p = cameras[i].gameObject.transform.position;
                        tmp.q = cameras[i].gameObject.transform.rotation;
                    }

                    tmp.active = cameras[i].gameObject.activeInHierarchy;

                    tmpList.Add(tmp);
                }
            }

            sm.camera = new objProperties[tmpList.Count];
            for (int i = 0; i < tmpList.Count; i++)
            {
                sm.camera[i] = new objProperties();
                sm.camera[i] = tmpList[i];
            }

            sm.cameraSliders = new MachineCameraSliderState[GlobalVariables.MachineCameraSliders.Count];
            int sliderIdx = 0;
            foreach (var kvp in GlobalVariables.MachineCameraSliders)
            {
                sm.cameraSliders[sliderIdx++] = kvp.Value;
            }


            // �V���x���J�[�̎p���Ɣz�u���擾
            GameObject shovelObj = Zx200ObjectUtility.FindZx200Object();

            var shovelInput = shovelObj.GetComponent<ExcavatorInput>();
            var shovelJoint = shovelInput.joints;

            sm.data[0].name = shovelObj.name;
            sm.data[0].id = shovelObj.name;
            sm.data[0].p = shovelObj.transform.position;
            sm.data[0].q = shovelObj.transform.rotation;
            sm.data[0].active = shovelObj.activeInHierarchy;

            sm.data[0].joint = new objJoint();

            sm.data[0].joint.swing_joint = shovelJoint.swing.JointCurrentPosition;
            sm.data[0].joint.boom_joint = shovelJoint.boomTilt.JointCurrentPosition;
            sm.data[0].joint.arm_joint = shovelJoint.armTilt.JointCurrentPosition;
            sm.data[0].joint.bucket_joint = shovelJoint.bucketTilt.JointCurrentPosition;
            sm.data[0].joint.right_track = shovelJoint.rightSprocket.JointCurrentPosition;
            sm.data[0].joint.left_track = shovelJoint.leftSprocket.JointCurrentPosition;


            Debug.Log("Dump_IDList.Count: " + GlobalVariables.Dump_IDList.Count);
            Debug.Log("Dump_ObjList.Count: " + GlobalVariables.Dump_ObjList.Count);


            // �_���v�g���b�N�̎p���Ɣz�u���擾
            if ((int)GlobalVariables.Dump_ObjList.Count > 0)
            {
                SaveDumpSoil sd = new SaveDumpSoil();
                sd.data = new TransDumpSoil[GlobalVariables.Dump_ObjList.Count];

                for (int i = 0; i < GlobalVariables.Dump_ObjList.Count; i++)
                {
                    sd.data[i] = new TransDumpSoil();

                    var id = GlobalVariables.Dump_IDList[i];
                    GameObject dumpObj = GlobalVariables.Dump_ObjList[i];

                    var ds = dumpObj.GetComponentInChildren<DumpSoil>();
                    Debug.Log("id: " + id + ", mass: " + ds.soilMass);

                    sd.data[i].id = id;
                    sd.data[i].mass = ds.soilMass;

                    // -----------------
                    var dumpInput = dumpObj.GetComponent<DumpTruckInput>();
                    var dumpJoint = dumpInput.joints;

                    sm.data[i + 1] = new objProperties();
                    sm.data[i + 1].joint = new objJoint();

                    sm.data[i + 1].name = dumpObj.name;
                    sm.data[i + 1].id = id;
                    sm.data[i + 1].p = TerrainSaveUtility.GetMachineRootPosition(dumpObj);
                    sm.data[i + 1].q = TerrainSaveUtility.GetMachineRootRotation(dumpObj);
                    sm.data[i + 1].active = dumpObj.activeInHierarchy;

                    sm.data[i + 1].joint.right_track = dumpJoint.rightSprocket.CurrentPosition;
                    sm.data[i + 1].joint.left_track = dumpJoint.leftSprocket.CurrentPosition;
                    sm.data[i + 1].joint.dump_joint = dumpJoint.dump_joint.CurrentPosition;
                }

                // �_���v�g���b�N�̐ύڕۑ�
                if (sd.data != null)
                {
                    string json_sd = JsonUtility.ToJson(sd, true);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(dirPath, "DumpSoil")))
                    {
                        sw.Write(json_sd);
                    }
                }
            }


            // �d�@�̎p����ۑ�
            if (sm.data != null)
            {
                string json_sm = JsonUtility.ToJson(sm, true);
                using (StreamWriter sw = new StreamWriter(Path.Combine(dirPath, "MachinesJoints")))
                {
                    sw.Write(json_sm);
                }
            }

            // �D�^�G���A�̃J�E���g�s���ۑ�
            using (StreamWriter sw = new StreamWriter(Path.Combine(dirPath, "MudAreaMatrix")))
            {
                //sw.Write(GlobalVariables.countMat);

                int rows = GlobalVariables.countMat.RowCount;
                int cols = GlobalVariables.countMat.ColumnCount;

                Debug.Log("rows: " + rows + ", cols: " + cols);

                for (int i = 0; i < rows; i++)
                {
                    string line = "";
                    for (int j = 0; j < cols; j++)
                    {
                        line = line + GlobalVariables.countMat[i, j].ToString() + ",";
                    }
                    sw.WriteLine(line);
                }
            }


            //SerializeTerrain(Path.Combine(GlobalVariables.BACKUP_FOLDER, fileName));
        }
    }
}
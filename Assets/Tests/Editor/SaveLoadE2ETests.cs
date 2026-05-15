using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// End-to-end editor-mode tests for the save/load system.
    ///
    /// Tests are isolated: each test that modifies GlobalVariables or the
    /// filesystem restores state in TearDown.  Temporary files are written to
    /// a per-test directory under Application.temporaryCachePath and cleaned up
    /// after each test so the source tree is never modified.
    ///
    /// Because Unity's compilation model does not allow a dedicated test
    /// assembly to directly reference Assembly-CSharp types, these tests run
    /// inside Assembly-CSharp-Editor (the editor-special folder convention) and
    /// use reflection only where direct access to internal/private members is
    /// needed.  For public types and static helpers the direct API is used.
    /// </summary>
    public class SaveLoadE2ETests
    {
        private string _tempDir;

        // Cached reflection handles reused across tests.
        private Type _saveScriptType;
        private Type _settingSaveLoadManagerType;
        private Type _globalVariablesType;

        // ------------------------------------------------------------------ //
        // Lifecycle                                                            //
        // ------------------------------------------------------------------ //

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _tempDir = Path.Combine(
                Application.temporaryCachePath,
                "SaveLoadE2ETests",
                TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(_tempDir);

            _saveScriptType            = Type.GetType("PWRISimulator.saveScript, Assembly-CSharp");
            _settingSaveLoadManagerType = Type.GetType("PWRISimulator.SettingSaveLoadManager, Assembly-CSharp");
            _globalVariablesType       = Type.GetType("PWRISimulator.GlobalVariables, Assembly-CSharp");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ------------------------------------------------------------------ //
        // SaveData / SerializedTerrain JSON structure                         //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that saveScript.SaveData can be round-tripped through
        /// JsonUtility with its list, heights, alphas, and name fields intact.
        /// </summary>
        [Test]
        public void TerrainSaveData_JsonRoundTrip_PreservesAllFields()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            // Build an instance of the inner SaveData type via reflection.
            Type saveDataType = _saveScriptType.GetNestedType("SaveData");
            Type serializedTerrainType = _saveScriptType.GetNestedType("SerializedTerrain");
            Assert.That(saveDataType, Is.Not.Null, "saveScript.SaveData inner type not found.");
            Assert.That(serializedTerrainType, Is.Not.Null, "saveScript.SerializedTerrain inner type not found.");

            // Create a SerializedTerrain with known values.
            object st = Activator.CreateInstance(serializedTerrainType);
            float[] heights = { 0.1f, 0.2f, 0.3f, 0.4f };
            float[] alphas  = { 1.0f, 0.0f };
            string terrainName = "Terrain_Test";
            serializedTerrainType.GetField("heights").SetValue(st, heights);
            serializedTerrainType.GetField("alphas").SetValue(st, alphas);
            serializedTerrainType.GetField("name").SetValue(st, terrainName);

            // Wrap in a SaveData array.
            var listArray = Array.CreateInstance(serializedTerrainType, 1);
            listArray.SetValue(st, 0);
            object saveData = Activator.CreateInstance(saveDataType);
            saveDataType.GetField("list").SetValue(saveData, listArray);

            // Serialize to JSON.
            string json = JsonUtility.ToJson(saveData);

            Assert.That(json, Is.Not.Empty, "Serialized JSON should not be empty.");
            Assert.That(json, Does.Contain("\"list\""), "JSON must contain 'list' key.");
            Assert.That(json, Does.Contain("\"heights\""), "JSON must contain 'heights' key.");
            Assert.That(json, Does.Contain("\"alphas\""), "JSON must contain 'alphas' key.");
            Assert.That(json, Does.Contain("\"name\""), "JSON must contain 'name' key.");
            Assert.That(json, Does.Contain("Terrain_Test"), "JSON must contain the terrain name.");

            // Deserialize back.
            object restored = JsonUtility.FromJson(json, saveDataType);
            Assert.That(restored, Is.Not.Null, "Deserialized SaveData must not be null.");

            var restoredList = (Array)saveDataType.GetField("list").GetValue(restored);
            Assert.That(restoredList, Is.Not.Null.And.Not.Empty, "Restored list must not be empty.");

            object restoredSt = restoredList.GetValue(0);
            string restoredName = (string)serializedTerrainType.GetField("name").GetValue(restoredSt);
            float[] restoredHeights = (float[])serializedTerrainType.GetField("heights").GetValue(restoredSt);
            float[] restoredAlphas  = (float[])serializedTerrainType.GetField("alphas").GetValue(restoredSt);

            Assert.That(restoredName, Is.EqualTo(terrainName), "Terrain name must survive round-trip.");
            Assert.That(restoredHeights, Is.EqualTo(heights).Within(1e-6f), "Heights must survive round-trip.");
            Assert.That(restoredAlphas, Is.EqualTo(alphas).Within(1e-6f), "Alphas must survive round-trip.");
        }

        /// <summary>
        /// Verifies that a SaveData written to a .ter file can be read back with
        /// the same content.
        /// </summary>
        [Test]
        public void TerrainSaveData_WrittenToFile_CanBeReadBackAsIdenticalJson()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            Type saveDataType = _saveScriptType.GetNestedType("SaveData");
            Type serializedTerrainType = _saveScriptType.GetNestedType("SerializedTerrain");

            object st = Activator.CreateInstance(serializedTerrainType);
            serializedTerrainType.GetField("heights").SetValue(st, new float[] { 0.5f, 0.6f });
            serializedTerrainType.GetField("alphas").SetValue(st, new float[] { 1.0f });
            serializedTerrainType.GetField("name").SetValue(st, "FileTerrain");

            var listArray = Array.CreateInstance(serializedTerrainType, 1);
            listArray.SetValue(st, 0);
            object saveData = Activator.CreateInstance(saveDataType);
            saveDataType.GetField("list").SetValue(saveData, listArray);

            string json = JsonUtility.ToJson(saveData);
            string filePath = Path.Combine(_tempDir, "test.ter");
            File.WriteAllText(filePath, json);

            Assert.That(File.Exists(filePath), Is.True, ".ter file must be created.");

            string readBack = File.ReadAllText(filePath);
            Assert.That(readBack, Is.EqualTo(json), "File content must match original JSON.");
        }

        // ------------------------------------------------------------------ //
        // SaveMachines / objProperties / objJoint JSON structure              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that the machines JSON data structure (SaveMachines with
        /// objProperties and objJoint) round-trips cleanly through JsonUtility.
        /// </summary>
        [Test]
        public void MachinesData_JsonRoundTrip_PreservesPositionJointsAndId()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            Type saveMachinesType  = _saveScriptType.GetNestedType("SaveMachines");
            Type objPropertiesType = _saveScriptType.GetNestedType("objProperties");
            Type objJointType      = _saveScriptType.GetNestedType("objJoint");

            Assert.That(saveMachinesType,  Is.Not.Null, "saveScript.SaveMachines inner type not found.");
            Assert.That(objPropertiesType, Is.Not.Null, "saveScript.objProperties inner type not found.");
            Assert.That(objJointType,      Is.Not.Null, "saveScript.objJoint inner type not found.");

            // Build a joint.
            object joint = Activator.CreateInstance(objJointType);
            objJointType.GetField("swing_joint").SetValue(joint, 1.1);
            objJointType.GetField("boom_joint").SetValue(joint, 2.2);
            objJointType.GetField("arm_joint").SetValue(joint, 3.3);
            objJointType.GetField("bucket_joint").SetValue(joint, 4.4);

            // Build an objProperties.
            object props = Activator.CreateInstance(objPropertiesType);
            objPropertiesType.GetField("name").SetValue(props, "zx200_0");
            objPropertiesType.GetField("id").SetValue(props, "machine-01");
            objPropertiesType.GetField("p").SetValue(props, new Vector3(1f, 2f, 3f));
            objPropertiesType.GetField("q").SetValue(props, Quaternion.Euler(0f, 45f, 0f));
            objPropertiesType.GetField("joint").SetValue(props, joint);
            objPropertiesType.GetField("active").SetValue(props, true);

            // Build a SaveMachines.
            var dataArray = Array.CreateInstance(objPropertiesType, 1);
            dataArray.SetValue(props, 0);
            object saveMachines = Activator.CreateInstance(saveMachinesType);
            saveMachinesType.GetField("time").SetValue(saveMachines, 120.5f);
            saveMachinesType.GetField("score").SetValue(saveMachines, 50);
            saveMachinesType.GetField("data").SetValue(saveMachines, dataArray);
            saveMachinesType.GetField("camera").SetValue(saveMachines, Array.CreateInstance(objPropertiesType, 0));

            string json = JsonUtility.ToJson(saveMachines);
            Assert.That(json, Does.Contain("\"time\""), "JSON must contain 'time'.");
            Assert.That(json, Does.Contain("\"score\""), "JSON must contain 'score'.");
            Assert.That(json, Does.Contain("\"data\""), "JSON must contain 'data'.");
            Assert.That(json, Does.Contain("zx200_0"), "JSON must contain machine name.");
            Assert.That(json, Does.Contain("machine-01"), "JSON must contain machine id.");
            Assert.That(json, Does.Contain("\"swing_joint\""), "JSON must contain swing_joint.");
            Assert.That(json, Does.Contain("\"boom_joint\""), "JSON must contain boom_joint.");

            // Round-trip.
            object restored = JsonUtility.FromJson(json, saveMachinesType);
            Assert.That(restored, Is.Not.Null);

            float restoredTime = (float)saveMachinesType.GetField("time").GetValue(restored);
            int restoredScore  = (int)saveMachinesType.GetField("score").GetValue(restored);
            var restoredData   = (Array)saveMachinesType.GetField("data").GetValue(restored);

            Assert.That(restoredTime,  Is.EqualTo(120.5f).Within(1e-4f), "time must survive round-trip.");
            Assert.That(restoredScore, Is.EqualTo(50), "score must survive round-trip.");
            Assert.That(restoredData,  Is.Not.Null.And.Not.Empty, "data must survive round-trip.");

            object restoredProps = restoredData.GetValue(0);
            string restoredName  = (string)objPropertiesType.GetField("name").GetValue(restoredProps);
            Vector3 restoredPos  = (Vector3)objPropertiesType.GetField("p").GetValue(restoredProps);
            bool restoredActive  = (bool)objPropertiesType.GetField("active").GetValue(restoredProps);

            Assert.That(restoredName,   Is.EqualTo("zx200_0"), "Machine name must survive round-trip.");
            Assert.That(restoredPos.x,  Is.EqualTo(1f).Within(1e-4f), "Position x must survive round-trip.");
            Assert.That(restoredPos.y,  Is.EqualTo(2f).Within(1e-4f), "Position y must survive round-trip.");
            Assert.That(restoredPos.z,  Is.EqualTo(3f).Within(1e-4f), "Position z must survive round-trip.");
            Assert.That(restoredActive, Is.True, "active flag must survive round-trip.");

            object restoredJoint = objPropertiesType.GetField("joint").GetValue(restoredProps);
            double restoredBoom  = (double)objJointType.GetField("boom_joint").GetValue(restoredJoint);
            Assert.That(restoredBoom, Is.EqualTo(2.2).Within(1e-6), "boom_joint must survive round-trip.");
        }

        /// <summary>
        /// Verifies that SaveMachines data written to a file under a temp path
        /// can be read back and the core structure is intact.
        /// </summary>
        [Test]
        public void MachinesData_WrittenToFile_CanBeReadBackByJsonUtility()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            Type saveMachinesType  = _saveScriptType.GetNestedType("SaveMachines");
            Type objPropertiesType = _saveScriptType.GetNestedType("objProperties");

            var emptyData = Array.CreateInstance(objPropertiesType, 0);
            object saveMachines = Activator.CreateInstance(saveMachinesType);
            saveMachinesType.GetField("time").SetValue(saveMachines, 0.0f);
            saveMachinesType.GetField("score").SetValue(saveMachines, 0);
            saveMachinesType.GetField("data").SetValue(saveMachines, emptyData);
            saveMachinesType.GetField("camera").SetValue(saveMachines, emptyData);

            string json = JsonUtility.ToJson(saveMachines);
            string filePath = Path.Combine(_tempDir, "MachinesJoints.json");
            File.WriteAllText(filePath, json);

            Assert.That(File.Exists(filePath), Is.True, "MachinesJoints file must exist.");

            string readJson = File.ReadAllText(filePath);
            object restored = JsonUtility.FromJson(readJson, saveMachinesType);
            Assert.That(restored, Is.Not.Null, "Restored SaveMachines must not be null.");
        }

        // ------------------------------------------------------------------ //
        // SaveParticles JSON structure                                         //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that the soil particle data structure (SaveParticles with
        /// Particles and agxVec3) round-trips cleanly through JsonUtility.
        /// </summary>
        [Test]
        public void SoilParticles_JsonRoundTrip_PreservesPositionVelocityRadiusMass()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            Type saveParticlesType = _saveScriptType.GetNestedType("SaveParticles");
            Type particlesType     = _saveScriptType.GetNestedType("Particles");
            Type agxVec3Type       = _saveScriptType.GetNestedType("agxVec3");

            Assert.That(saveParticlesType, Is.Not.Null, "saveScript.SaveParticles inner type not found.");
            Assert.That(particlesType,     Is.Not.Null, "saveScript.Particles inner type not found.");
            Assert.That(agxVec3Type,       Is.Not.Null, "saveScript.agxVec3 inner type not found.");

            // Build a particle.
            object pos = Activator.CreateInstance(agxVec3Type);
            agxVec3Type.GetField("x").SetValue(pos, 1.0);
            agxVec3Type.GetField("y").SetValue(pos, 2.0);
            agxVec3Type.GetField("z").SetValue(pos, 3.0);

            object vel = Activator.CreateInstance(agxVec3Type);
            agxVec3Type.GetField("x").SetValue(vel, 0.1);
            agxVec3Type.GetField("y").SetValue(vel, 0.2);
            agxVec3Type.GetField("z").SetValue(vel, 0.3);

            object particle = Activator.CreateInstance(particlesType);
            particlesType.GetField("position").SetValue(particle, pos);
            particlesType.GetField("velocity").SetValue(particle, vel);
            particlesType.GetField("radius").SetValue(particle, 0.05);
            particlesType.GetField("mass").SetValue(particle, 0.01);

            var dataArray = Array.CreateInstance(particlesType, 1);
            dataArray.SetValue(particle, 0);
            object saveParticles = Activator.CreateInstance(saveParticlesType);
            saveParticlesType.GetField("data").SetValue(saveParticles, dataArray);

            string json = JsonUtility.ToJson(saveParticles);
            Assert.That(json, Does.Contain("\"position\""), "JSON must contain 'position'.");
            Assert.That(json, Does.Contain("\"velocity\""), "JSON must contain 'velocity'.");
            Assert.That(json, Does.Contain("\"radius\""), "JSON must contain 'radius'.");
            Assert.That(json, Does.Contain("\"mass\""), "JSON must contain 'mass'.");

            object restored = JsonUtility.FromJson(json, saveParticlesType);
            Assert.That(restored, Is.Not.Null, "Restored SaveParticles must not be null.");

            var restoredData = (Array)saveParticlesType.GetField("data").GetValue(restored);
            Assert.That(restoredData.Length, Is.EqualTo(1), "Must have exactly one particle.");

            object rp       = restoredData.GetValue(0);
            double radius   = (double)particlesType.GetField("radius").GetValue(rp);
            double mass     = (double)particlesType.GetField("mass").GetValue(rp);
            object rpos     = particlesType.GetField("position").GetValue(rp);
            double rx       = (double)agxVec3Type.GetField("x").GetValue(rpos);

            Assert.That(radius, Is.EqualTo(0.05).Within(1e-9), "radius must survive round-trip.");
            Assert.That(mass,   Is.EqualTo(0.01).Within(1e-9), "mass must survive round-trip.");
            Assert.That(rx,     Is.EqualTo(1.0).Within(1e-9), "position.x must survive round-trip.");
        }

        /// <summary>
        /// Verifies that an empty SaveParticles (zero particles) serializes to
        /// valid JSON and back without error.
        /// </summary>
        [Test]
        public void SoilParticles_EmptyDataset_SerializesAndDeserializesWithoutError()
        {
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript type not found in Assembly-CSharp.");

            Type saveParticlesType = _saveScriptType.GetNestedType("SaveParticles");
            Type particlesType     = _saveScriptType.GetNestedType("Particles");

            var emptyData = Array.CreateInstance(particlesType, 0);
            object saveParticles = Activator.CreateInstance(saveParticlesType);
            saveParticlesType.GetField("data").SetValue(saveParticles, emptyData);

            string json = JsonUtility.ToJson(saveParticles);
            Assert.That(json, Is.Not.Empty, "Empty SaveParticles must produce non-empty JSON.");

            object restored = JsonUtility.FromJson(json, saveParticlesType);
            var restoredData = (Array)saveParticlesType.GetField("data").GetValue(restored);
            Assert.That(restoredData.Length, Is.EqualTo(0), "Empty dataset must round-trip to empty array.");
        }

        // ------------------------------------------------------------------ //
        // SettingData JSON structure                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that SettingData can be serialized to JSON and deserialized
        /// back with all fields intact.
        /// </summary>
        [Test]
        public void SettingData_JsonRoundTrip_PreservesAllFields()
        {
            // SettingData is a public type; we can use it directly.
            var data = new SettingData
            {
                MaxDumpTracks          = 4,
                MaxCameras             = 3,
                MinScore               = -100,
                MiningCoef             = 2.0f,
                LoadSoilCoef           = 10.0f,
                UnloadSoilCoef         = 100.0f,
                CollisionCoef          = -5.0f,
                OffTruckCoef           = -1.0f,
                OverlappCoef           = -2.0f,
                GameTime               = 900.0f,
                TimeBarRedThreshold    = 33.3f,
                TimeBarYellowThreshold = 66.7f,
                datapath               = "/tmp/test",
                RosIP                  = "192.168.0.100"
            };

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            Assert.That(json, Is.Not.Empty);
            Assert.That(json, Does.Contain("MaxDumpTracks"));
            Assert.That(json, Does.Contain("MaxCameras"));
            Assert.That(json, Does.Contain("MiningCoef"));
            Assert.That(json, Does.Contain("192.168.0.100"));

            SettingData restored = JsonUtility.FromJson<SettingData>(json);
            Assert.That(restored,                     Is.Not.Null);
            Assert.That(restored.MaxDumpTracks,        Is.EqualTo(4));
            Assert.That(restored.MaxCameras,           Is.EqualTo(3));
            Assert.That(restored.MinScore,             Is.EqualTo(-100));
            Assert.That(restored.MiningCoef,           Is.EqualTo(2.0f).Within(1e-5f));
            Assert.That(restored.GameTime,             Is.EqualTo(900.0f).Within(1e-5f));
            Assert.That(restored.TimeBarRedThreshold,  Is.EqualTo(33.3f).Within(1e-4f));
            Assert.That(restored.RosIP,                Is.EqualTo("192.168.0.100"));
            Assert.That(restored.datapath,             Is.EqualTo("/tmp/test"));
        }

        /// <summary>
        /// Verifies that SettingData written to a file with JsonUtility can be
        /// read back and its values are identical to the original.
        /// </summary>
        [Test]
        public void SettingData_WrittenToFile_CanBeReadBackByJsonUtility()
        {
            var data = new SettingData
            {
                MaxDumpTracks = 2,
                MaxCameras    = 1,
                MinScore      = -50,
                MiningCoef    = 1.5f,
                GameTime      = 600.0f,
                RosIP         = "10.0.0.1"
            };

            string json = JsonUtility.ToJson(data, prettyPrint: false);
            string filePath = Path.Combine(_tempDir, "SettingData.json");
            File.WriteAllText(filePath, json);

            Assert.That(File.Exists(filePath), Is.True);

            string readJson = File.ReadAllText(filePath);
            SettingData restored = JsonUtility.FromJson<SettingData>(readJson);

            Assert.That(restored.MaxDumpTracks, Is.EqualTo(2));
            Assert.That(restored.MaxCameras,    Is.EqualTo(1));
            Assert.That(restored.MinScore,      Is.EqualTo(-50));
            Assert.That(restored.MiningCoef,    Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(restored.GameTime,      Is.EqualTo(600.0f).Within(1e-5f));
            Assert.That(restored.RosIP,         Is.EqualTo("10.0.0.1"));
        }

        // ------------------------------------------------------------------ //
        // GlobalVariables / SettingSaveLoadManager round-trip                 //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that the SettingSaveLoadManager saves GlobalVariables to a
        /// JSON file and LoadSetting restores them correctly.
        ///
        /// The manager is tested via reflection because it is a MonoBehaviour
        /// and requires AddComponent to create a live instance.
        /// </summary>
        [Test]
        public void SettingsSaveLoad_GlobalVariablesRoundTrip_AllFieldsRestored()
        {
            Assert.That(_settingSaveLoadManagerType, Is.Not.Null, "SettingSaveLoadManager type not found.");
            Assert.That(_globalVariablesType,        Is.Not.Null, "GlobalVariables type not found.");

            // Snapshot existing GlobalVariables state so we can restore later.
            int origMaxDump       = GlobalVariables.MaxDunpTracks;
            int origMaxCam        = GlobalVariables.MaxCameras;
            int origMinScore      = GlobalVariables.MinScore;
            float origMining      = GlobalVariables.MiningCoef;
            float origLoad        = GlobalVariables.LoadSoilCoef;
            float origUnload      = GlobalVariables.UnloadSoilCoef;
            float origCollision   = GlobalVariables.CollisionCoef;
            float origOffTruck    = GlobalVariables.OffTruckCoef;
            float origOverlapp    = GlobalVariables.OverlappCoef;
            float origGameTime    = GlobalVariables.GameTime;
            float origRedT        = GlobalVariables.TimeBarRedThreshold;
            float origYellowT     = GlobalVariables.TimeBarYellowThreshold;
            string origDatapath   = GlobalVariables.datapath;
            string origRosIP      = GlobalVariables.RosIP;

            try
            {
                // Set known values.
                GlobalVariables.MaxDunpTracks           = 5;
                GlobalVariables.MaxCameras              = 2;
                GlobalVariables.MinScore                = -200;
                GlobalVariables.MiningCoef              = 3.0f;
                GlobalVariables.LoadSoilCoef            = 8.0f;
                GlobalVariables.UnloadSoilCoef          = 80.0f;
                GlobalVariables.CollisionCoef           = -10.0f;
                GlobalVariables.OffTruckCoef            = -2.0f;
                GlobalVariables.OverlappCoef            = -3.0f;
                GlobalVariables.GameTime                = 1200.0f;
                GlobalVariables.TimeBarRedThreshold     = 25.0f;
                GlobalVariables.TimeBarYellowThreshold  = 60.0f;
                GlobalVariables.datapath                = _tempDir;
                GlobalVariables.RosIP                   = "172.16.0.1";

                // Create a SettingSaveLoadManager GameObject and set its file path.
                var go = new GameObject("TestSettingManager");
                var manager = (SettingSaveLoadManager)go.AddComponent(_settingSaveLoadManagerType);
                string savePath = Path.Combine(_tempDir, "SettingData.json");
                manager.filePath = savePath;

                // Save.
                manager.SaveSetting();
                Assert.That(File.Exists(savePath), Is.True, "Settings file must be created after SaveSetting.");

                // Corrupt GlobalVariables so we can confirm LoadSetting restores them.
                GlobalVariables.MaxDunpTracks  = 0;
                GlobalVariables.MaxCameras     = 0;
                GlobalVariables.MiningCoef     = 0.0f;
                GlobalVariables.GameTime       = 0.0f;
                GlobalVariables.RosIP          = "";

                // Load.
                manager.filePath = savePath;
                manager.LoadSetting();

                // Assert.
                Assert.That(GlobalVariables.MaxDunpTracks,          Is.EqualTo(5),     "MaxDunpTracks must be restored.");
                Assert.That(GlobalVariables.MaxCameras,             Is.EqualTo(2),     "MaxCameras must be restored.");
                Assert.That(GlobalVariables.MinScore,               Is.EqualTo(-200),  "MinScore must be restored.");
                Assert.That(GlobalVariables.MiningCoef,             Is.EqualTo(3.0f).Within(1e-5f), "MiningCoef must be restored.");
                Assert.That(GlobalVariables.LoadSoilCoef,           Is.EqualTo(8.0f).Within(1e-5f), "LoadSoilCoef must be restored.");
                Assert.That(GlobalVariables.GameTime,               Is.EqualTo(1200.0f).Within(1e-5f), "GameTime must be restored.");
                Assert.That(GlobalVariables.TimeBarRedThreshold,    Is.EqualTo(25.0f).Within(1e-4f), "TimeBarRedThreshold must be restored.");
                Assert.That(GlobalVariables.TimeBarYellowThreshold, Is.EqualTo(60.0f).Within(1e-4f), "TimeBarYellowThreshold must be restored.");
                Assert.That(GlobalVariables.datapath,               Is.EqualTo(_tempDir), "datapath must be restored.");
                Assert.That(GlobalVariables.RosIP,                  Is.EqualTo("172.16.0.1"), "RosIP must be restored.");

                UnityEngine.Object.DestroyImmediate(go);
            }
            finally
            {
                // Restore original GlobalVariables state.
                GlobalVariables.MaxDunpTracks          = origMaxDump;
                GlobalVariables.MaxCameras             = origMaxCam;
                GlobalVariables.MinScore               = origMinScore;
                GlobalVariables.MiningCoef             = origMining;
                GlobalVariables.LoadSoilCoef           = origLoad;
                GlobalVariables.UnloadSoilCoef         = origUnload;
                GlobalVariables.CollisionCoef          = origCollision;
                GlobalVariables.OffTruckCoef           = origOffTruck;
                GlobalVariables.OverlappCoef           = origOverlapp;
                GlobalVariables.GameTime               = origGameTime;
                GlobalVariables.TimeBarRedThreshold    = origRedT;
                GlobalVariables.TimeBarYellowThreshold = origYellowT;
                GlobalVariables.datapath               = origDatapath;
                GlobalVariables.RosIP                  = origRosIP;
            }
        }

        /// <summary>
        /// Verifies that SettingSaveLoadManager.LoadDefaultSetting populates
        /// GlobalVariables with the expected hard-coded default values.
        /// </summary>
        [Test]
        public void LoadDefaultSetting_SetsExpectedDefaultValues()
        {
            Assert.That(_settingSaveLoadManagerType, Is.Not.Null, "SettingSaveLoadManager type not found.");

            var go = new GameObject("TestSettingManagerDefaults");
            var manager = (SettingSaveLoadManager)go.AddComponent(_settingSaveLoadManagerType);

            // Snapshot.
            int origMaxDump  = GlobalVariables.MaxDunpTracks;
            int origMaxCam   = GlobalVariables.MaxCameras;
            float origGT     = GlobalVariables.GameTime;

            try
            {
                manager.LoadDefaultSetting();

                Assert.That(GlobalVariables.MaxDunpTracks, Is.EqualTo(4),     "Default MaxDunpTracks should be 4.");
                Assert.That(GlobalVariables.MaxCameras,    Is.EqualTo(3),     "Default MaxCameras should be 3.");
                Assert.That(GlobalVariables.MinScore,      Is.EqualTo(-100),  "Default MinScore should be -100.");
                Assert.That(GlobalVariables.GameTime,      Is.EqualTo(60.0f * 15.0f).Within(1e-4f), "Default GameTime should be 900 s.");
                Assert.That(GlobalVariables.RosIP,         Is.EqualTo("192.168.0.74"), "Default RosIP should match constant.");
            }
            finally
            {
                GlobalVariables.MaxDunpTracks = origMaxDump;
                GlobalVariables.MaxCameras    = origMaxCam;
                GlobalVariables.GameTime      = origGT;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ //
        // TerrainSaveUtility.IsSavedDumpTruckRootName (cross-reference check) //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Sanity check that IsSavedDumpTruckRootName is exercised through
        /// this test suite as well (the canonical unit tests live in
        /// TerrainSaveUtilityTests.cs).
        /// </summary>
        [Test]
        public void IsSavedDumpTruckRootName_SanityCheck_MatchesKnownPatterns()
        {
            var utilityType = Type.GetType("PWRISimulator.TerrainSaveUtility, Assembly-CSharp");
            Assert.That(utilityType, Is.Not.Null, "TerrainSaveUtility type not found.");

            var method = utilityType.GetMethod(
                "IsSavedDumpTruckRootName",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "IsSavedDumpTruckRootName method not found.");

            Assert.That((bool)method.Invoke(null, new object[] { "ic120_0" }),       Is.True,  "ic120_0 must match.");
            Assert.That((bool)method.Invoke(null, new object[] { "ic120_5" }),       Is.True,  "ic120_5 must match.");
            Assert.That((bool)method.Invoke(null, new object[] { "zx200_0" }),       Is.False, "zx200_0 must not match.");
            Assert.That((bool)method.Invoke(null, new object[] { "Terrain_merge" }), Is.False, "Terrain_merge must not match.");
        }
    }
}

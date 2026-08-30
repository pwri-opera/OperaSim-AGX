using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using AGXUnity;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// Play-mode integration tests that combine:
    ///
    ///   1. Machine-count validation
    ///      Verifies the expected number of root machine GameObjects (zx200
    ///      excavator + ic120 dump trucks) are present in GameScene.
    ///
    ///   2. Physical stability
    ///      Starts the AGX physics simulation via GlobalVariables.ActionMode = 3,
    ///      waits SettleTime seconds, then asserts every machine's rigid bodies
    ///      have near-zero velocity (machines are stable on the terrain).
    ///
    ///   3. Save round-trip
    ///      Calls saveScript.OnClick() to write a real save, then reads back the
    ///      MachinesJoints JSON and verifies:
    ///        - data.Length == 1 + number_of_dump_trucks  (excavator at [0],
    ///          dump trucks at [1..N])
    ///        - each machine's saved position is within PositionTolerance of its
    ///          actual live world position
    ///        - each machine's name matches the scene GameObject name
    ///
    /// Access strategy
    /// ---------------
    ///   GlobalVariables, saveScript  →  reflection (live in Assembly-CSharp)
    ///   AGXUnity.RigidBody           →  direct reference (AGXUnity asmdef)
    ///
    /// Stability thresholds
    /// --------------------
    ///   A rigid body is "stable" when:
    ///     linearSpeed  (m/s)  < LinearSpeedThreshold
    ///     angularSpeed (rad/s) < AngularSpeedThreshold
    ///
    /// The defaults are intentionally lenient to accommodate minor oscillations
    /// from track constraints and soil interaction.
    /// </summary>
    public class SimulationSaveLoadTests
    {
        // ------------------------------------------------------------------ //
        // Tunable constants                                                    //
        // ------------------------------------------------------------------ //

        private const string SceneName = "GameScene";

        /// <summary>Seconds to wait after ActionMode=3 before sampling.</summary>
        private const float SettleTime = 5.0f;

        /// <summary>Linear speed (m/s) above which a rigid body is "unstable".</summary>
        private const float LinearSpeedThreshold = 0.5f;

        /// <summary>Angular speed (rad/s) above which a rigid body is "unstable".</summary>
        private const float AngularSpeedThreshold = 0.5f;

        /// <summary>
        /// Maximum positional error (m) allowed between a machine's saved
        /// position and its actual live world position.
        /// </summary>
        private const float PositionTolerance = 0.5f;

        /// <summary>
        /// Maximum drift (m) allowed between a machine's initial and final
        /// position over the settle period.
        /// </summary>
        private const float MaxDrift = 2.0f;

        // ActionMode values (matches ControlPhysics.Update logic)
        private const int ActionModeSimulation = 3;
        private const int ActionModeIdle = -1;

        // Expected machine composition in GameScene
        private const int ExpectedExcavatorCount = 1;  // zx200
        private const int ExpectedDumpTruckCount  = 1;  // ic120_0

        // ------------------------------------------------------------------ //
        // Reflection handles (initialised in SetUp)                           //
        // ------------------------------------------------------------------ //

        private Type      _globalVariablesType;
        private FieldInfo _actionModeField;
        private FieldInfo _backupFolderField;
        private FieldInfo _dumpObjListField;

        private Type      _saveScriptType;
        private Type      _saveMachinesType;
        private Type      _objPropertiesType;
        private FieldInfo _saveMachinesDataField;
        private FieldInfo _objPropertiesNameField;
        private FieldInfo _objPropertiesPosField;

        private int    _originalActionMode;
        private string _originalBackupFolder;
        private string _tempSaveDir;

        // ------------------------------------------------------------------ //
        // Lifecycle                                                            //
        // ------------------------------------------------------------------ //

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // --- Resolve GlobalVariables members via reflection ---------------
            _globalVariablesType = Type.GetType("PWRISimulator.GlobalVariables, Assembly-CSharp");
            Assert.That(_globalVariablesType, Is.Not.Null, "GlobalVariables not found in Assembly-CSharp.");

            _actionModeField = _globalVariablesType.GetField(
                "ActionMode", BindingFlags.Public | BindingFlags.Static);
            Assert.That(_actionModeField, Is.Not.Null, "GlobalVariables.ActionMode field not found.");

            _backupFolderField = _globalVariablesType.GetField(
                "BACKUP_FOLDER", BindingFlags.Public | BindingFlags.Static);
            Assert.That(_backupFolderField, Is.Not.Null, "GlobalVariables.BACKUP_FOLDER field not found.");

            _dumpObjListField = _globalVariablesType.GetField(
                "Dump_ObjList", BindingFlags.Public | BindingFlags.Static);
            Assert.That(_dumpObjListField, Is.Not.Null, "GlobalVariables.Dump_ObjList field not found.");

            // --- Resolve saveScript and its nested SaveMachines via reflection
            _saveScriptType = Type.GetType("PWRISimulator.saveScript, Assembly-CSharp");
            Assert.That(_saveScriptType, Is.Not.Null, "saveScript not found in Assembly-CSharp.");

            _saveMachinesType = _saveScriptType.GetNestedType("SaveMachines");
            Assert.That(_saveMachinesType, Is.Not.Null, "saveScript.SaveMachines not found.");

            _objPropertiesType = _saveScriptType.GetNestedType("objProperties");
            Assert.That(_objPropertiesType, Is.Not.Null, "saveScript.objProperties not found.");

            _saveMachinesDataField = _saveMachinesType.GetField("data");
            Assert.That(_saveMachinesDataField, Is.Not.Null, "SaveMachines.data field not found.");

            _objPropertiesNameField = _objPropertiesType.GetField("name");
            Assert.That(_objPropertiesNameField, Is.Not.Null, "objProperties.name field not found.");

            _objPropertiesPosField = _objPropertiesType.GetField("p");
            Assert.That(_objPropertiesPosField, Is.Not.Null, "objProperties.p field not found.");

            // --- Snapshot mutable globals so TearDown can restore them --------
            _originalActionMode   = (int)_actionModeField.GetValue(null);
            _originalBackupFolder = (string)_backupFolderField.GetValue(null);

            // Redirect saves to a temp directory so we never touch source/project files.
            _tempSaveDir = Path.Combine(
                Application.temporaryCachePath,
                "SimulationSaveLoadTests",
                TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(_tempSaveDir);
            _backupFolderField.SetValue(null, _tempSaveDir);

            // --- Load GameScene (single mode unloads any previous scene) ------
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
                // Allow Awake/Start to run on all MonoBehaviours.
                yield return null;
                yield return null;
            }

            // Start with physics paused.
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_actionModeField != null)
                _actionModeField.SetValue(null, _originalActionMode);

            if (_backupFolderField != null && _originalBackupFolder != null)
                _backupFolderField.SetValue(null, _originalBackupFolder);

            if (_tempSaveDir != null && Directory.Exists(_tempSaveDir))
                Directory.Delete(_tempSaveDir, recursive: true);

            yield return null;
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns every root-level GameObject that has at least one
        /// AGXUnity.RigidBody among its (active) descendants.
        /// </summary>
        private static List<GameObject> FindMachineRoots()
        {
            var roots = new List<GameObject>();
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.transform.parent != null) continue;
                if (go.GetComponentsInChildren<RigidBody>(includeInactive: false).Length > 0)
                    roots.Add(go);
            }
            return roots;
        }

        /// <summary>
        /// Returns (machine-root-name, rb-name, linearSpeed, angularSpeed) for
        /// every active RigidBody that is a descendant of a machine root.
        /// </summary>
        private static List<(string machine, string rb, float lin, float ang)>
            SampleAllMachineVelocities()
        {
            var samples = new List<(string, string, float, float)>();
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                if (go.transform.parent != null) continue;
                var rbs = go.GetComponentsInChildren<RigidBody>(includeInactive: false);
                if (rbs.Length == 0) continue;
                foreach (var rb in rbs)
                    samples.Add((go.name, rb.gameObject.name,
                                 rb.LinearVelocity.magnitude,
                                 rb.AngularVelocity.magnitude));
            }
            return samples;
        }

        /// <summary>
        /// Finds the saveScript MonoBehaviour in the scene and calls OnClick()
        /// on it directly via reflection.  Using SendMessage is intentionally
        /// avoided because it broadcasts to ALL components on the same
        /// GameObject (including loadScript), which would cause loadScript to
        /// try reading files that have not been written yet.
        /// </summary>
        /// <returns>True if the component was found and the method was invoked.</returns>
        private bool InvokeSaveScriptOnClick()
        {
            var comp = GameObject.FindObjectOfType(_saveScriptType) as MonoBehaviour;
            if (comp == null) return false;

            var method = _saveScriptType.GetMethod(
                "OnClick", BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return false;

            method.Invoke(comp, null);
            return true;
        }

        /// <summary>
        /// Reads MachinesJoints from the temp save directory (sub-folder
        /// "simulation" when ActionMode==3, or "setting" otherwise) and
        /// deserialises it into a SaveMachines object (via JsonUtility through
        /// reflection).
        /// </summary>
        private object ReadSavedMachines()
        {
            // saveScript.OnClick writes to BACKUP_FOLDER/simulation/ (ActionMode==3)
            // or BACKUP_FOLDER/setting/ (otherwise).  We call it while in simulation
            // mode, so the sub-folder is "simulation".
            string filePath = Path.Combine(_tempSaveDir, "simulation", "MachinesJoints");
            Assert.That(File.Exists(filePath), Is.True,
                $"MachinesJoints save file not found at: {filePath}");

            string json = File.ReadAllText(filePath);
            Assert.That(json, Is.Not.Empty, "MachinesJoints file is empty.");

            object saved = JsonUtility.FromJson(json, _saveMachinesType);
            Assert.That(saved, Is.Not.Null, "Failed to deserialise MachinesJoints JSON.");
            return saved;
        }

        // ------------------------------------------------------------------ //
        // 1. Machine-count validation                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that the expected number of excavators and dump trucks are
        /// present in the loaded GameScene.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_HasExpectedMachineRootCounts()
        {
            var machineRoots = FindMachineRoots();
            Assert.That(machineRoots, Is.Not.Empty,
                $"No machine roots (GameObjects with RigidBody children) found in {SceneName}.");

            // Count excavators (zx200-family) and dump trucks (ic120-family).
            int excavatorCount = 0;
            int dumpTruckCount = 0;
            foreach (var go in machineRoots)
            {
                // DumpSoil creates an auxiliary "<machine>_SoilMassBody" rigid body at the
                // scene root (it cannot live under an ArticulatedRoot), which would match
                // the ic120 prefix below even though it is not a machine (#151).
                if (go.name.Contains("_SoilMassBody"))
                    continue;

                if (go.name.StartsWith("zx200", StringComparison.OrdinalIgnoreCase))
                    excavatorCount++;
                else if (go.name.StartsWith("ic120", StringComparison.OrdinalIgnoreCase))
                    dumpTruckCount++;
            }

            Assert.That(excavatorCount, Is.EqualTo(ExpectedExcavatorCount),
                $"Expected {ExpectedExcavatorCount} excavator(s) (zx200*) in {SceneName}, " +
                $"found {excavatorCount}. Machines present: " +
                string.Join(", ", machineRoots.ConvertAll(g => g.name)));

            Assert.That(dumpTruckCount, Is.EqualTo(ExpectedDumpTruckCount),
                $"Expected {ExpectedDumpTruckCount} dump truck(s) (ic120*) in {SceneName}, " +
                $"found {dumpTruckCount}. Machines present: " +
                string.Join(", ", machineRoots.ConvertAll(g => g.name)));

            yield return null;  // keep as UnityTest (coroutine) for consistency
        }

        // ------------------------------------------------------------------ //
        // 2. Physical stability                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Starts simulation (ActionMode=3), waits SettleTime, then asserts
        /// every machine's rigid bodies have near-zero velocity.
        /// </summary>
        [UnityTest]
        public IEnumerator Simulation_AfterSettleTime_AllMachineRigidBodiesAreStable()
        {
            var machineRoots = FindMachineRoots();
            Assert.That(machineRoots, Is.Not.Empty,
                $"No machine roots found in {SceneName} — stability test cannot run.");

            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            var samples = SampleAllMachineVelocities();
            Assert.That(samples, Is.Not.Empty,
                "No RigidBody velocities could be sampled after simulation start.");

            var unstable = new List<string>();
            foreach (var (machine, rb, lin, ang) in samples)
            {
                if (lin >= LinearSpeedThreshold || ang >= AngularSpeedThreshold)
                    unstable.Add($"  {machine}/{rb}: lin={lin:F3} m/s, ang={ang:F3} rad/s");
            }

            Assert.That(unstable, Is.Empty,
                $"Rigid bodies exceeded stability thresholds " +
                $"(linear < {LinearSpeedThreshold} m/s, angular < {AngularSpeedThreshold} rad/s) " +
                $"after {SettleTime}s:\n" +
                string.Join("\n", unstable));
        }

        /// <summary>
        /// Verifies machines do not drift more than MaxDrift metres from their
        /// initial positions during the settle period.  Catches machines that
        /// fall through the terrain or teleport.
        /// </summary>
        [UnityTest]
        public IEnumerator Simulation_AfterSettleTime_MachineRootsDoNotDriftFar()
        {
            // Snapshot initial root positions.
            var initial = new Dictionary<int, (string name, Vector3 pos)>();
            foreach (var go in FindMachineRoots())
                initial[go.GetInstanceID()] = (go.name, go.transform.position);

            Assert.That(initial, Is.Not.Empty,
                $"No machine roots found in {SceneName}.");

            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            var drifted = new List<string>();
            foreach (var go in FindMachineRoots())
            {
                if (!initial.TryGetValue(go.GetInstanceID(), out var snap)) continue;
                float drift = Vector3.Distance(snap.pos, go.transform.position);
                if (drift > MaxDrift)
                    drifted.Add($"  {go.name}: {drift:F2} m (was {snap.pos}, now {go.transform.position})");
            }

            Assert.That(drifted, Is.Empty,
                $"Machines drifted more than {MaxDrift} m after {SettleTime}s:\n" +
                string.Join("\n", drifted));
        }

        // ------------------------------------------------------------------ //
        // 3. Save round-trip                                                   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Runs simulation for SettleTime, then calls saveScript.OnClick() to
        /// produce a real save.  Verifies the MachinesJoints file contains the
        /// correct number of machine entries (1 excavator + N dump trucks).
        /// </summary>
        [UnityTest]
        public IEnumerator Save_MachinesJointsFile_HasCorrectMachineCount()
        {
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            // Invoke save via direct reflection (not SendMessage, which would
            // also trigger loadScript.OnClick on the same GameObject).
            Assume.That(InvokeSaveScriptOnClick(), Is.True,
                "saveScript MonoBehaviour not found in GameScene — skipping save round-trip test.");

            // Give the save one frame to complete.
            yield return null;

            object saved = ReadSavedMachines();
            var data = (Array)_saveMachinesDataField.GetValue(saved);
            Assert.That(data, Is.Not.Null, "SaveMachines.data is null after save.");

            // data[0] = excavator, data[1..] = dump trucks
            int dumpCount = (int)((System.Collections.IList)_dumpObjListField.GetValue(null)).Count;
            int expectedCount = 1 + dumpCount;

            Assert.That(data.Length, Is.EqualTo(expectedCount),
                $"MachinesJoints data array length should be 1 (excavator) + {dumpCount} (dump trucks) = {expectedCount}. " +
                $"Actual length: {data.Length}");
        }

        /// <summary>
        /// Runs simulation for SettleTime, saves, then verifies that each
        /// machine entry's saved name matches its scene GameObject name, and
        /// its saved position is within PositionTolerance of the live position.
        /// </summary>
        [UnityTest]
        public IEnumerator Save_MachinesJointsFile_PositionsMatchLiveScene()
        {
            // Snapshot live positions before save.
            var livePositions = new Dictionary<string, Vector3>();
            foreach (var go in FindMachineRoots())
            {
                // Use the last entry if duplicate names exist (unlikely in this scene).
                livePositions[go.name] = go.transform.position;
            }

            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            // Update live positions after settle (machines may have shifted slightly).
            livePositions.Clear();
            foreach (var go in FindMachineRoots())
                livePositions[go.name] = go.transform.position;

            // Invoke save via direct reflection (not SendMessage).
            Assume.That(InvokeSaveScriptOnClick(), Is.True,
                "saveScript MonoBehaviour not found in GameScene — skipping position validation test.");
            yield return null;

            object saved = ReadSavedMachines();
            var data = (Array)_saveMachinesDataField.GetValue(saved);
            Assert.That(data, Is.Not.Null.And.Not.Empty, "SaveMachines.data is null or empty.");

            var mismatches = new List<string>();
            for (int i = 0; i < data.Length; i++)
            {
                object entry = data.GetValue(i);
                string savedName = (string)_objPropertiesNameField.GetValue(entry);
                Vector3 savedPos  = (Vector3)_objPropertiesPosField.GetValue(entry);

                if (string.IsNullOrEmpty(savedName))
                {
                    mismatches.Add($"  data[{i}]: name is null/empty");
                    continue;
                }

                if (!livePositions.TryGetValue(savedName, out Vector3 livePos))
                {
                    // Machine name may include an index suffix (e.g. "zx200" in scene vs
                    // "zx200" in save, or "ic120_0").  Try a prefix-based lookup.
                    bool found = false;
                    foreach (var kvp in livePositions)
                    {
                        if (kvp.Key.StartsWith(savedName, StringComparison.OrdinalIgnoreCase) ||
                            savedName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            livePos = kvp.Value;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        mismatches.Add($"  data[{i}] name='{savedName}': no matching live GameObject found");
                        continue;
                    }
                }

                float err = Vector3.Distance(savedPos, livePos);
                if (err > PositionTolerance)
                    mismatches.Add($"  data[{i}] '{savedName}': saved={savedPos}, live={livePos}, error={err:F3} m");
            }

            Assert.That(mismatches, Is.Empty,
                $"Saved machine positions differ from live positions by more than {PositionTolerance} m:\n" +
                string.Join("\n", mismatches));
        }

        /// <summary>
        /// Full round-trip: run simulation, save, then reload the MachinesJoints
        /// JSON a second time and verify the parsed data is consistent (JSON is
        /// idempotent on a second parse).
        /// </summary>
        [UnityTest]
        public IEnumerator Save_MachinesJointsJson_IsIdempotentOnDoubleDeserialise()
        {
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            // Invoke save via direct reflection (not SendMessage).
            Assume.That(InvokeSaveScriptOnClick(), Is.True,
                "saveScript MonoBehaviour not found — skipping idempotency test.");
            yield return null;

            string filePath = Path.Combine(_tempSaveDir, "simulation", "MachinesJoints");
            Assert.That(File.Exists(filePath), Is.True, "MachinesJoints not written.");

            string json1 = File.ReadAllText(filePath);
            object parsed1 = JsonUtility.FromJson(json1, _saveMachinesType);

            // Re-serialise and re-parse.
            string json2 = JsonUtility.ToJson(parsed1);
            object parsed2 = JsonUtility.FromJson(json2, _saveMachinesType);

            var data1 = (Array)_saveMachinesDataField.GetValue(parsed1);
            var data2 = (Array)_saveMachinesDataField.GetValue(parsed2);

            Assert.That(data2.Length, Is.EqualTo(data1.Length),
                "Machine count must be identical after double deserialise.");

            for (int i = 0; i < data1.Length; i++)
            {
                string name1 = (string)_objPropertiesNameField.GetValue(data1.GetValue(i));
                string name2 = (string)_objPropertiesNameField.GetValue(data2.GetValue(i));
                Assert.That(name2, Is.EqualTo(name1),
                    $"data[{i}].name changed after re-serialise: '{name1}' → '{name2}'");

                Vector3 p1 = (Vector3)_objPropertiesPosField.GetValue(data1.GetValue(i));
                Vector3 p2 = (Vector3)_objPropertiesPosField.GetValue(data2.GetValue(i));
                Assert.That(Vector3.Distance(p1, p2), Is.LessThan(1e-4f),
                    $"data[{i}] '{name1}' position changed after re-serialise: {p1} → {p2}");
            }
        }
    }
}

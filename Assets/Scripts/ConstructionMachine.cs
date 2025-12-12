using System;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// 一般的なインタフェースで物理演算の汎用的な Constraint にアクセスできるようにするベースクラス。
    /// 
    /// 派生の物理クラス（例えばシリンダやダンプトラックなど）はこのクラスを利用します。生成された各 Constraint について、関連する
    /// ConstraintControl パラメータをユーザに公開し、Initialize から RegisterConstraintControl を呼び出して登録します。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public abstract class ConstructionMachine : ScriptComponent
    {
        #region Public

        /// <summary>
        /// 各種の ConstraintControl から controlValue をそれぞれ AGXUnity の Constraint に設定する
        /// （AGXUnity の PreStepForward コールバック内）。false の場合には、controlValue を変更した場合にマニュアルで UpdateConstraintControls を呼び出す必要があります。
        /// </summary>
        public bool autoUpdateConstraints = true;

        /// <summary>
        /// ConstraintControl から controlValue をそれぞれ AGXUnity の Constraint に設定する。
        /// autoUpdateConstraints が true の場合は、自動的に Ord() から呼び出されます。
        /// </summary>
        public void UpdateConstraintControls()
        {
            foreach (ConstraintControl cc in contraintControls)
                cc.UpdateConstraintControl();
        }

        /// <summary>
        /// 指定した GameObject の両側に 2 つの Track が存在し、それぞれの sprocket ホイールを取得し、
        /// separation という値を sprocket ホイール間の左右の距離に設定し、
        /// radius という値を sprocket の半径（Track 接地半径）に設定して True を返す。
        /// 失敗した場合は、separation および radius をゼロに設定し False を返す。
        /// </summary>
        public bool GetTracksSeparationAndRadius(out double separation, out double radius)
        {
            Track[] tracks = GetComponentsInChildren<Track>();
            if (tracks != null && tracks.Length == 2)
            {
                return TrackUtil.GetSeparationAndTractionRadius(tracks[0], tracks[1], out separation, out radius);
            }
            else
            {
                Debug.LogError($"{name} : GetTracksSeparationAndRadius() failed because exactly two tracks could not " +
                                " be found.");
                separation = 0.0;
                radius = 0.0;
                return false;
            }
        }
        
        #endregion

        #region Private

        /// <summary>
        /// 登録された各 ConstraintControl の一覧。ConstraintControl をまとめて保持し、
        /// AGXUnity の Constraint に controlValue を送信するためのリスト。
        /// Editor では設定されず、Play 開始時に設定され、使用されます。
        /// </summary>
        List<ConstraintControl> contraintControls = new List<ConstraintControl>();
        
        /// <summary>
        /// Unity の Start の段階で、AGXUnity 用の初期化を行うメソッド。
        /// </summary>
        /// <returns></returns>

        protected override bool Initialize()
        {
            bool success = base.Initialize();

            Simulation sim = Simulation.Instance?.GetInitialized<Simulation>();
            if (sim != null)
                sim.StepCallbacks.PreStepForward += OnPreStepForward;

            return success;
        }

        protected override void OnDestroy()
        {
            if (Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PreStepForward -= OnPreStepForward;
            base.OnDestroy();
        }

        /// <summary>
        /// ConstraintControl を登録する。派生クラスで生成された Constraint については
        /// このメソッドを Initialize() から呼び出す必要があります。
        /// </summary>
        protected void RegisterConstraintControl(ConstraintControl constraintControl)
        {
            contraintControls.Add(constraintControl);
            constraintControl.Initialize();
        }

        /// <summary>
        /// AGXUnity の PreStepForward イベント。
        /// 派生クラスでオーバーライドする場合には、必ず基底クラスのこのメソッドを呼び出すようにしてください。
        /// </summary>

        protected virtual void OnPreStepForward()
        {
            RequestCommands();
            if (enabled && gameObject.activeInHierarchy && autoUpdateConstraints)
                UpdateConstraintControls();
        }

        protected virtual void RequestCommands()
        {

        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionMachine))]
    public class ConstructionMachineEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var machine = (ConstructionMachine)target;

            // デフォルト GUI を表示
            base.OnInspectorGUI();

            if (showDumpContainers)
            {
                EditorGUILayout.Space();
                OnDumpContainersGui(machine);
            }

            if (showNoMergeZones)
            {
                EditorGUILayout.Space();
                OnNoMergeZonesGui(machine);
            }
        }

        bool showDumpContainers = true;
        List<DumpSoil> dumpContainers = null;
        
        void OnDumpContainersGui(ConstructionMachine machine)
        {
            if (dumpContainers == null)
            {
                dumpContainers = new List<DumpSoil>();
                machine.GetComponentsInChildren<DumpSoil>(false, dumpContainers);
            }
            
            if(dumpContainers.Count > 0)
            {
                EditorGUILayout.LabelField("Dump Container Soil", EditorStyles.boldLabel);

                var dump = dumpContainers[0];
                if (dump.showOutputInInspector = EditorGUILayout.Toggle("Show Soil Data", dump.showOutputInInspector))
                {
                    DumpSoilEditor.OnSoilDataGUI(dump);
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            if (!showDumpContainers || dumpContainers == null || dumpContainers.Count == 0)
                return false;
            return DumpSoilEditor.RequiresConstantRepaint(dumpContainers[0]);
        }

        bool showNoMergeZones = true;
        List<TerrainNoMergeZone> noMergeZones = null;
        List<bool> noMergeZoneFoldedOut = new List<bool>();
        List<Editor> noMergeZoneEditors = new List<Editor>();

        void OnNoMergeZonesGui(ConstructionMachine machine)
        {
            if (noMergeZones == null)
            {
                //Debug.Log($"Searching for TerrainNoMergeZone in {machine.name}...");
                noMergeZones = new List<TerrainNoMergeZone>();
                machine.GetComponentsInChildren<TerrainNoMergeZone>(true, noMergeZones);

                while (noMergeZoneFoldedOut.Count < noMergeZones.Count)
                    noMergeZoneFoldedOut.Add(false);

                noMergeZoneEditors = new List<Editor>(noMergeZones.Count);
                while (noMergeZoneEditors.Count < noMergeZones.Count)
                    noMergeZoneEditors.Add(null);
            }

            if (noMergeZones.Count > 0)
            {
                EditorGUILayout.LabelField("Terrain No Merge Zones", EditorStyles.boldLabel);

                for (int i = 0; i < noMergeZones.Count; ++i)
                {
                    if (noMergeZoneFoldedOut[i] = EditorGUILayout.Foldout(noMergeZoneFoldedOut[i], noMergeZones[i].name, true))
                    {
                        if (noMergeZoneEditors[i] == null)
                            noMergeZoneEditors[i] = Editor.CreateEditor(noMergeZones[i]);

                        bool enabled = noMergeZones[i].isActiveAndEnabled;
                        bool enabledToggled = EditorGUILayout.Toggle("Enabled", enabled);
                        if (enabledToggled != enabled)
                        {
                            // gameObject の active プロパティをフラグとして管理する（非表示、無効化などの用途を想定）
                            noMergeZones[i].enabled = true;
                            noMergeZones[i].gameObject.SetActive(enabledToggled);
                        }
                        noMergeZoneEditors[i].OnInspectorGUI();
                    }
                }
            }
        }
    }
#endif
}
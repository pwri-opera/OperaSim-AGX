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
    /// 簡単な共通のインタフェースで建機の様々なコンストレイントにアクセスできるようにするベースクラス。
    /// 
    /// 実際の建機クラスに(油圧ショベル、ダンプトラックなど)このクラスを継承させる。操作させたいConstraintごとに、子クラスから
    /// ConstraintControlパラメータをユーザに提供し、InitializeからRegisterConstraintControlを呼び出して登録する。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public abstract class ConstructionMachine : ScriptComponent
    {
        #region Public

        /// <summary>
        /// 自動的にConstraintControlごとのcontrolValueをそれぞれのAGXUnityのConstraintに設定する（AGXUnityのPreStepForward
        /// コールバックから）。falseの場合は、controlValueを変更するとマニュアルでUpdateConstraintControlsを呼び出す必要。
        /// </summary>
        public bool autoUpdateConstraints = true;

        /// <summary>
        /// ConstraintControlごとのcontrolValueをそれぞれのAGXUnityのConstraintに設定する。autoUpdateConstraintsがtrue場合は
        /// 自動的にOnPreStepForward()から呼び出されている。
        /// </summary>
        public void UpdateConstraintControls()
        {
            foreach (ConstraintControl cc in contraintControls)
                cc.UpdateConstraintControl();
        }

        /// <summary>
        /// このGameObjectの階層に２つのTrackを探してそれぞれのsprocketホイールを取得し、separationという出力をsprocketホイール
        /// 同士の距離に設定し、radiusという出力をsprocket半径＋Track厚さに設定し、Trueを返す。失敗の場合は、separation、radius
        /// をゼロに設定しFalseを返す。
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
        /// 登録された１ConstraintControlの一覧。ConstraintControlを初期化したり、AGXUnityのConstraintにcontrolValueを通信
        /// したりするためのリスト。Editor時に設定されなく、Play時だけに設定され、使われている。
        /// </summary>
        List<ConstraintControl> contraintControls = new List<ConstraintControl>();
        
        /// <summary>
        /// UnityのStartの代わりに、AGXUnity用の初期化メソッド。
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
        /// ConstraintControlを登録する。子クラスが操作したいConstraintごとにこのメソッドをInitialize()から呼び出す必要。
        /// </summary>
        protected void RegisterConstraintControl(ConstraintControl constraintControl)
        {
            contraintControls.Add(constraintControl);
            constraintControl.Initialize();
        }

        /// <summary>
        /// AGXUnityのPreStepForwardエベント。子クラスからオーバライドする場合は、必ずこのペアレントクラスのメソッドを
        /// 呼び出してください。
        /// </summary>
        protected virtual void OnPreStepForward()
        {
            if (enabled && gameObject.activeInHierarchy && autoUpdateConstraints)
                UpdateConstraintControls();
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

            // デフォルトGUIを表示
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
                            // gameObjectのactiveプロパティ経由で有効化を管理する（子ビジュアルなども影響されるように）
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
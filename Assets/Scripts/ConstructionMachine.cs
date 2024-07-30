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
    /// �ȒP�ȋ��ʂ̃C���^�t�F�[�X�Ō��@�̗l�X�ȃR���X�g���C���g�ɃA�N�Z�X�ł���悤�ɂ���x�[�X�N���X�B
    /// 
    /// ���ۂ̌��@�N���X��(�����V���x���A�_���v�g���b�N�Ȃ�)���̃N���X���p��������B���삳������Constraint���ƂɁA�q�N���X����
    /// ConstraintControl�p�����[�^�����[�U�ɒ񋟂��AInitialize����RegisterConstraintControl���Ăяo���ēo�^����B
    /// </summary>
    [DefaultExecutionOrder(100)]
    public abstract class ConstructionMachine : ScriptComponent
    {
        #region Public

        /// <summary>
        /// �����I��ConstraintControl���Ƃ�controlValue�����ꂼ���AGXUnity��Constraint�ɐݒ肷��iAGXUnity��PreStepForward
        /// �R�[���o�b�N����j�Bfalse�̏ꍇ�́AcontrolValue��ύX����ƃ}�j���A����UpdateConstraintControls���Ăяo���K�v�B
        /// </summary>
        public bool autoUpdateConstraints = true;

        /// <summary>
        /// ConstraintControl���Ƃ�controlValue�����ꂼ���AGXUnity��Constraint�ɐݒ肷��BautoUpdateConstraints��true�ꍇ��
        /// �����I��OnPreStepForward()����Ăяo����Ă���B
        /// </summary>
        public void UpdateConstraintControls()
        {
            foreach (ConstraintControl cc in contraintControls)
                cc.UpdateConstraintControl();
        }

        /// <summary>
        /// ����GameObject�̊K�w�ɂQ��Track��T���Ă��ꂼ���sprocket�z�C�[�����擾���Aseparation�Ƃ����o�͂�sprocket�z�C�[��
        /// ���m�̋����ɐݒ肵�Aradius�Ƃ����o�͂�sprocket���a�{Track�����ɐݒ肵�ATrue��Ԃ��B���s�̏ꍇ�́Aseparation�Aradius
        /// ���[���ɐݒ肵False��Ԃ��B
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
        /// �o�^���ꂽ�PConstraintControl�̈ꗗ�BConstraintControl��������������AAGXUnity��Constraint��controlValue��ʐM
        /// �����肷�邽�߂̃��X�g�BEditor���ɐݒ肳��Ȃ��APlay�������ɐݒ肳��A�g���Ă���B
        /// </summary>
        List<ConstraintControl> contraintControls = new List<ConstraintControl>();
        
        /// <summary>
        /// Unity��Start�̑���ɁAAGXUnity�p�̏��������\�b�h�B
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
        /// ConstraintControl��o�^����B�q�N���X�����삵����Constraint���Ƃɂ��̃��\�b�h��Initialize()����Ăяo���K�v�B
        /// </summary>
        protected void RegisterConstraintControl(ConstraintControl constraintControl)
        {
            contraintControls.Add(constraintControl);
            constraintControl.Initialize();
        }

        /// <summary>
        /// AGXUnity��PreStepForward�G�x���g�B�q�N���X����I�[�o���C�h����ꍇ�́A�K�����̃y�A�����g�N���X�̃��\�b�h��
        /// �Ăяo���Ă��������B
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

            // �f�t�H���gGUI��\��
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
                            // gameObject��active�v���p�e�B�o�R�ŗL�������Ǘ�����i�q�r�W���A���Ȃǂ��e�������悤�Ɂj
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
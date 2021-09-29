using System.Collections;
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
    /// Excavatorのバケットを使ってTerrainを掘削すること関して実測データを提供するスクリプト。C#プロパティまたはInspectorGUI経由
    /// でデータにアクセスする。このスクリプトはPlayするときに自動的にDeforambleTerrainShovel、DeformableTerrainを見つけるが、
    /// 明示的に設定したい場合はプロパティ経由で設定する。
    /// </summary>
    public class ExcavationData : ScriptComponent
    {
        public bool showDataInGUI = false;

        public DeformableTerrainShovel shovel;
        public DeformableTerrain terrain;

        public Vector3 penetrationForce
        {
            get
            {
                if (shovel != null && terrain != null)
                {
                    agx.Vec3 force = new agx.Vec3();
                    agx.Vec3 torque = new agx.Vec3();
                    if (terrain.Native.getPenetrationForce(shovel.Native, ref force, ref torque))
                        return force.ToHandedVector3();
                }
                return Vector3.zero;
            }
        }

        public Vector3 penetrationTorque
        {
            get
            {
                if (shovel != null && terrain != null)
                {
                    agx.Vec3 force = new agx.Vec3();
                    agx.Vec3 torque = new agx.Vec3();
                    if (terrain.Native.getPenetrationForce(shovel.Native, ref force, ref torque))
                        return torque.ToHandedVector3();
                }
                return Vector3.zero;
            }
        }

        public Vector3 separationForce
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getSeparationContactForce(shovel.Native).ToHandedVector3();
                return Vector3.zero;
            }
        }

        public Vector3 separationForceTorque
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getSeparationContactForce(shovel.Native).ToHandedVector3();
                return Vector3.zero;
            }
        }

        public Vector3 deformationForce
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getDeformationContactForce(shovel.Native).ToHandedVector3();
                return Vector3.zero;
            }
        }

        public Vector3 contactForce
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getContactForce(shovel.Native).ToHandedVector3();
                return Vector3.zero;
            }
        }

        public double shovelInnerVolume
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getInnerVolume(shovel.Native);
                return 0.0;
            }
        }

        public double shovelDeadloadFraction
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getLastDeadLoadFraction(shovel.Native);
                return 0.0;
            }
        }

        public double shovelSoilVolume
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getInnerVolume(shovel.Native) *
                           terrain.Native.getLastDeadLoadFraction(shovel.Native);
                return 0.0;
            }
        }

        public double shovelDynamicMass
        {
            get
            {
                if (shovel != null && terrain != null)
                    return terrain.Native.getDynamicMass(shovel.Native);
                return 0.0;
            }
        }

        public double totalMass
        {
            get
            {
                if (terrain != null)
                    return terrain.Native.calculateTotalMass();
                return 0.0;
            }
        }

        public double totalSolidMass
        {
            get
            {
                if (terrain != null)
                    return terrain.Native.calculateTotalSolidMass();
                return 0.0;
            }
        }

        public double totalDynamicMass
        {
            get
            {
                if (terrain != null)
                    return terrain.Native.calculateTotalDynamicMass();
                return 0.0;
            }
        }

        protected override bool Initialize()
        {
            if(shovel == null)
                shovel = GetComponentInChildren<DeformableTerrainShovel>().GetInitialized<DeformableTerrainShovel>();

            if(terrain == null)
                terrain = FindObjectOfType<DeformableTerrain>().GetInitialized<DeformableTerrain>();

            return base.Initialize();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExcavationData))]
    public class ExcavationDataEditor : Editor
    {
        SerializedProperty shovelProperty;
        SerializedProperty terrainProperty;
        bool sourceObjectsFoldedOut;

        double totalMass = 0.0;
        double totalSolidMass = 0.0;
        double totalDynamicMass = 0.0;

        void OnEnable()
        {
            ExcavationData data = (ExcavationData)target;
            shovelProperty = serializedObject.FindProperty(nameof(data.shovel));
            terrainProperty = serializedObject.FindProperty(nameof(data.terrain));
        }

        public override bool RequiresConstantRepaint()
        {
            return (target as ExcavationData).showDataInGUI;
        }

        public override void OnInspectorGUI()
        {
            ExcavationData data = (ExcavationData)target;

            data.showDataInGUI = EditorGUILayout.Toggle("Show Live Data In GUI", data.showDataInGUI);

            if (data.showDataInGUI)
            {
                Vector3 penatrationForceKn = data.penetrationForce * 1e3f;
                Vector3 separationForceKn = data.separationForce * 1e3f;
                Vector3 deformationForceKn = data.deformationForce * 1e3f;
                Vector3 contactForceKn = data.contactForce * 1e3f;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Volumes", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Shovel Inner Volume", data.shovelInnerVolume.ToString("0.####") + " m3");
                EditorGUILayout.LabelField("Shovel Soil Volume", data.shovelSoilVolume.ToString("0.####") + " m3");
                EditorGUILayout.LabelField("Shovel Deadload Fraction", (data.shovelDeadloadFraction * 100).ToString("0.#") + " %");
                EditorGUILayout.LabelField("Shovel Dynamic Mass", data.shovelDynamicMass.ToString("0.##") + " kg");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Force Magnitudes", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Penetration Force Size", penatrationForceKn.magnitude.ToString("0.####") + " kN");
                EditorGUILayout.LabelField("Separation Force Size", separationForceKn.magnitude.ToString("0.####") + " kN");
                EditorGUILayout.LabelField("Deformation Force Size", deformationForceKn.magnitude.ToString("0.####") + " kN");
                EditorGUILayout.LabelField("Contact Force Size", contactForceKn.magnitude.ToString("0.####") + " kN");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Force Vectors", EditorStyles.boldLabel);

                EditorGUILayout.Vector3Field("Penetration Force", penatrationForceKn);
                EditorGUILayout.Vector3Field("Separation Force", separationForceKn);
                EditorGUILayout.Vector3Field("Deformation Force", deformationForceKn);
                EditorGUILayout.Vector3Field("Contact Force", contactForceKn);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Mass", EditorStyles.boldLabel);
            if (GUILayout.Button("Update", GUILayout.Width(100)))
            {
                totalMass = data.totalMass;
                totalSolidMass = data.totalSolidMass;
                totalDynamicMass = data.totalDynamicMass;
            }
            EditorGUILayout.DoubleField("Total Mass", totalMass);
            EditorGUILayout.DoubleField("Total Solid Mass", totalSolidMass);
            EditorGUILayout.DoubleField("Total Dynamic Mass", totalDynamicMass);

            EditorGUILayout.Space();

            if (sourceObjectsFoldedOut = EditorGUILayout.Foldout(sourceObjectsFoldedOut, "Source Objects", true))
            {
                EditorGUILayout.PropertyField(shovelProperty, new GUIContent("Shovel"));
                EditorGUILayout.PropertyField(terrainProperty, new GUIContent("Terrain"));
                EditorGUILayout.LabelField("(auto-assigned on Play if None)", EditorStyles.centeredGreyMiniLabel);
            }

        }
    }
#endif

}

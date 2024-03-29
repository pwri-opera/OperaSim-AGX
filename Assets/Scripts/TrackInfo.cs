// Copyright 2021 VMC Motion Technologies Co., Ltd.
using UnityEngine;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Utils;
using VMT.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VMT.Util
{
    /// <summary>
    /// Playの開始後、AGXUnity.Model.Trackの様々な情報を取得し、InspectorまたはConsoleに表示する
    /// </summary>
    public class TrackInfo : ScriptComponent
    {
        public Track track;

        public bool printToConsole = true;
                
        public Vector3 boxSize { get; private set; }
        public Vector3 boxLocalPosition { get; private set; }

        public double bodyMass { get; private set; }
        public Vector3 bodyLocalMassCenter { get; private set; }
        public agx.SPDMatrix3x3 bodyInertiaTensor { get; private set; }

        public int nodeCount { get; private set; }

        protected override bool Initialize()
        {
            if (track == null)
                track = GetComponent<Track>();

            UpdateData();

            if (printToConsole)
                PrintData();

            return base.Initialize();
        }

        void UpdateData()
        {
            if (track == null)
                return;

            agxVehicle.Track nativeTrack = track.GetInitialized<Track>().Native;
            if (nativeTrack == null)
                return;

            agxVehicle.TrackNodeRange nodes = nativeTrack.nodes();
            if (nodes.empty())
                return;

            agx.RigidBody rb = nodes.front().getRigidBody();
            agx.MassProperties mp = rb.getMassProperties();
            agxCollide.Geometry g = rb.getGeometries().Count > 0 ? rb.getGeometries()[0]?.get() : null;
            agxCollide.Box box = g != null && g.getShapes().Count > 0 ? g.getShapes()[0]?.get().asBox() : null;

            boxSize = 2.0f * box.getHalfExtents().ToVector3();
            boxLocalPosition = rb.getFrame().transformPointToLocal(box.getTransform().getTranslate()).ToHandedVector3();

            bodyMass = mp.getMass();
            bodyLocalMassCenter = rb.getCmLocalTranslate().ToHandedVector3();
            bodyInertiaTensor = mp.getInertiaTensor();

            nodeCount = nodes.Length;
        }

        void PrintData()
        {
            Debug.Log($"Track \"{track.name}\" : " +
                      $"mass = {bodyMass.ToString("F3")} " +
                      $"localMassCenter = {bodyLocalMassCenter.ToString("F3")} " +
                      $"boxSize = {boxSize.ToString("F3")} " +
                      $"boxLocalPos = {boxLocalPosition.ToString("F3")} " + 
                      $"¥ninertiaTensor =¥n  {bodyInertiaTensor.ToString("¥n  ")}");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TrackInfo))]
    public class TrackInfoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TrackInfo data = (TrackInfo)target;

            // デフォルトGUIを表示する
            base.OnInspectorGUI();

            // Track情報を表示する
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Track", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Number Of Nodes:", data.nodeCount.ToString());
            EditorGUILayout.LabelField("Total Mass:", (data.bodyMass * data.nodeCount).ToString("0.###") + " kg");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rigid Body", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mass:", data.bodyMass.ToString("0.###") + " kg");
            EditorGUILayout.Vector3Field("Local Mass Center:", data.bodyLocalMassCenter);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Collision Box Shape", EditorStyles.boldLabel);
            EditorGUILayout.Vector3Field("Size:", data.boxSize);
            EditorGUILayout.Vector3Field("Local Position:", data.boxLocalPosition);
        }
    }
#endif

}
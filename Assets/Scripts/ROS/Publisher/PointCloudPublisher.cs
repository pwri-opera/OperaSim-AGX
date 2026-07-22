using System;
using System.Runtime.InteropServices;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator.ROS
{
    /// <summary>
    /// PointCloudGenerator が生成する点群を sensor_msgs/PointCloud2 として ROS 配信する汎用 Publisher。
    /// </summary>
    [RequireComponent(typeof(PointCloudGenerator))]
    public class PointCloudPublisher : MonoBehaviour
    {
        [SerializeField] string topicName = "/area_pc";
        [SerializeField] string frameId = "map";
        [SerializeField, Min(0.01f)] float publishPeriod = 0.5f;
        [SerializeField, Min(0.01f)] float resolution = 0.2f;
        [SerializeField] bool snapYToTerrainOnStart = true;

        const int PointStep = 12;
        static readonly PointFieldMsg[] Fields =
        {
            new PointFieldMsg("x", 0, PointFieldMsg.FLOAT32, 1),
            new PointFieldMsg("y", 4, PointFieldMsg.FLOAT32, 1),
            new PointFieldMsg("z", 8, PointFieldMsg.FLOAT32, 1),
        };

        PointCloudGenerator generator;
        ROSConnection rosConnection;
        PointCloud2Msg msg;
        byte[] dataBuffer = Array.Empty<byte>();
        double scheduleOrigin;
        long publishedCount;

        void Start()
        {
            generator = GetComponent<PointCloudGenerator>();

            if (generator is TerrainPointCloudGenerator terrainGen)
                terrainGen.pointDistance = resolution;

            if (snapYToTerrainOnStart)
                SnapYToTerrain();

            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<PointCloud2Msg>(topicName);

            msg = new PointCloud2Msg
            {
                height = 1,
                is_bigendian = false,
                is_dense = true,
                point_step = PointStep,
                fields = Fields,
            };

            scheduleOrigin = Time.fixedTimeAsDouble;
        }

        // sim-time 定義の周期を保つため FixedUpdate 起点で publish する(#56)。
        // 発火時刻は scheduleOrigin + n×publishPeriod の均一グリッドで、stamp もグリッド時刻を使う
        void FixedUpdate()
        {
            if (rosConnection == null)
                return;
            double now = Time.fixedTimeAsDouble;
            while (scheduleOrigin + publishedCount * (double)publishPeriod <= now)
            {
                PublishOnce(scheduleOrigin + publishedCount * (double)publishPeriod);
                publishedCount++;
            }
        }

        void PublishOnce(double stampTime)
        {
            float[] pts = generator.GeneratePointCloud(flipX: false);
            int numPoints = pts.Length / 3;
            int byteSize = numPoints * PointStep;

            if (dataBuffer.Length != byteSize)
                dataBuffer = new byte[byteSize];

            // Unity (FRU, left-handed) -> ROS (FLU, right-handed):
            // x_ros = z_u, y_ros = -x_u, z_ros = y_u
            var floatView = MemoryMarshal.Cast<byte, float>(dataBuffer);
            for (int i = 0; i < numPoints; i++)
            {
                float ux = pts[3 * i + 0];
                float uy = pts[3 * i + 1];
                float uz = pts[3 * i + 2];
                floatView[3 * i + 0] = uz;
                floatView[3 * i + 1] = -ux;
                floatView[3 * i + 2] = uy;
            }

            msg.header = MessageUtil.ToHeadermessage(stampTime, frameId);
            msg.width = (uint)numPoints;
            msg.row_step = (uint)byteSize;
            msg.data = dataBuffer;

            rosConnection.Publish(topicName, msg);
        }

        /// <summary>
        /// 自身の transform.position.y をアクティブな Terrain の表面高さに合わせる。
        /// サンプリングは x/z しか参照しないので結果に影響せず、Gizmo の見た目だけ点群と揃う。
        /// </summary>
        public void SnapYToTerrain()
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogWarning($"{name}: SnapYToTerrain skipped — no active Terrain found.");
                return;
            }

            Vector3 localPos = terrain.transform.InverseTransformPoint(transform.position);
            float u = localPos.x / terrain.terrainData.size.x;
            float v = localPos.z / terrain.terrainData.size.z;
            float terrainHeight = terrain.terrainData.GetInterpolatedHeight(u, v);

            Vector3 pos = transform.position;
            pos.y = terrain.transform.position.y + terrainHeight;
            transform.position = pos;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PointCloudPublisher))]
    public class PointCloudPublisherEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            if (GUILayout.Button("Snap Y to Terrain", GUILayout.Width(200)))
            {
                var publisher = target as PointCloudPublisher;
                Undo.RecordObject(publisher.transform, "Snap Y to Terrain");
                publisher.SnapYToTerrain();
            }
        }
    }
#endif
}

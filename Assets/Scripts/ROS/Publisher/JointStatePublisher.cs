using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using UnityEditor;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// JointStateMsgをPublishするabstractクラス
    /// </summary>
    public abstract class JointStatePublisher : MonoBehaviour
    {
        private ROSConnection rosConnection;
        private string topicName;
        protected JointStateMsg jointStateMsg;
        private double publishPeriod;
        private double scheduleOrigin;
        private long publishedCount;

        // Start is called before the first frame update
        void Start()
        {
            RegisterTopic();
            publishPeriod = 1.0 / Math.Max(1, Frequency());
            scheduleOrigin = Time.fixedTimeAsDouble;
        }

        // sim-time 定義の周波数を保つため FixedUpdate 起点で publish する(#56)。
        // 発火時刻は scheduleOrigin + n×period の均一グリッドで、stamp もグリッド時刻を使う。
        // fixed step (20ms) より細かい周波数では1ステップに複数回 publish される(データは直近 step の状態)
        void FixedUpdate()
        {
            if (rosConnection == null)
                return;
            double now = Time.fixedTimeAsDouble;
            while (scheduleOrigin + publishedCount * publishPeriod <= now)
            {
                DoUpdate();
                MessageUtil.UpdateTimeMsg(jointStateMsg.header.stamp, scheduleOrigin + publishedCount * publishPeriod);
                PublishMessage();
                publishedCount++;
            }
        }

        void RegisterTopic()
        {
            topicName = $"/{MachineName()}{TopicPhrase()}";
            // new JointMsg
            jointStateMsg = new(
                header: new(),
                name: new string[NumberOfJoints()],
                position: new double[NumberOfJoints()],
                velocity: new double[NumberOfJoints()],
                effort: new double[NumberOfJoints()]
            );
            string[] jointNames = JointNames();
            for (int i = 0; i < NumberOfJoints(); i++)
            {
                jointStateMsg.name[i] = jointNames[i];
            }
            // register publisher
            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<JointStateMsg>(topicName);
        }

        /// <summary>
        /// 各更新タイミングで実行する処理
        /// </summary>
        abstract protected void DoUpdate();

        /// <returns>建設機械の名前. 共通制御信号における車体名 例:zx120</returns>
        abstract protected string MachineName();

        /// <returns>トピック名. 共通制御信号における/車体名/abcの/abcの部分 /をつける必要がある 例:joint_state</returns>
        abstract protected string TopicPhrase();

        /// <returns>更新周期(FPS)</returns>
        abstract protected uint Frequency();

        /// <returns>jointStateMsgの要素数</returns>
        abstract protected uint NumberOfJoints();

        /// <returns>jointStateMsgの各要素の名前</returns>
        abstract protected string[] JointNames();

        void PublishMessage()
        {
            rosConnection.Publish(topicName, jointStateMsg);
        }
    }
}

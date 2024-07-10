using System.Collections;
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

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(UpdateAndPublishMessage());
        }
        IEnumerator UpdateAndPublishMessage()
        {
            RegisterTopic();
            while(true)
            {
                yield return new WaitForSecondsRealtime(1.0f / Math.Max(1, Frequency()));
                DoUpdate();
                PublishMessage();
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

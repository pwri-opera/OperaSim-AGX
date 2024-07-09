using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// OdometryMsgをPublishするabstractクラス
    /// 派生クラスでMachineName(), TopicPhrase(), Frequency(), DoUpdate()を定義して使用する
    ///</summary>
    public abstract class OdometryPublisher: MonoBehaviour
    {
        private ROSConnection rosConnection;
        private string topicName;
        protected OdometryMsg odometryMsg;

        void Start()
        {
            StartCoroutine(UpdateAndPublishMessage());
        }

        public IEnumerator UpdateAndPublishMessage()
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
            odometryMsg = new();
            odometryMsg.header.frame_id = $"{MachineName()}{TopicPhrase()}";
            odometryMsg.child_frame_id = $"{MachineName()}/base_link";

            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<OdometryMsg>(topicName);
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
        void PublishMessage()
        {
            rosConnection.Publish(topicName, odometryMsg);
        }
    }
}
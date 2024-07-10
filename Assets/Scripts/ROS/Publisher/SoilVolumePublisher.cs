using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using System.Collections;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 土量体積(m^3)をPublishするabstractクラス
    /// </summary>
    public abstract class SoilVolumePublisher : MonoBehaviour
    {
        private ROSConnection rosConnection;
        private string topicName;
        protected Float64Msg soilVolumeMsg;
        // Start is called before the first frame update
        void Start()
        {
            DoStart();
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
            soilVolumeMsg = new();
            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<Float64Msg>(topicName);
        }

        /// <summary>
        /// 初期化時に実行する処理
        /// </summary>
        abstract protected void DoStart();

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
            rosConnection.Publish(topicName, soilVolumeMsg);
        }
    }
}

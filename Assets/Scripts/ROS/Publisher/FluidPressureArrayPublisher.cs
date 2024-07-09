using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Com3;
using Unity.Robotics.ROSTCPConnector;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// FluidPressureArrayMsgをPublishするabstractクラス
    /// </summary>
    public abstract class FluidPressureArrayPublisher : MonoBehaviour
    {
        private ROSConnection rosConnection;
        private string topicName;
        protected FluidPressureArrayMsg fluidPressureArrayMsg;
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
            uint numberOfItems = NumberOfItems();
            topicName = $"/{MachineName()}{TopicPhrase()}";
            // new JointMsg
            fluidPressureArrayMsg = new(new  FluidPressureMsg[numberOfItems]);

            for (int i = 0; i < numberOfItems; i++)
            {
                fluidPressureArrayMsg.array[i] = new FluidPressureMsg();
            }

            // register publisher
            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<FluidPressureArrayMsg>(topicName);
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

        /// <returns>fluidPressureArrayMsgの要素数</returns>
        abstract protected uint NumberOfItems();
        void PublishMessage()
        {
            rosConnection.Publish(topicName, fluidPressureArrayMsg);
        }
    }
}

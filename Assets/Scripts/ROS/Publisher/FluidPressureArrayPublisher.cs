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
            if (rosConnection == null || publishPeriod <= 0)
                return;
            double now = Time.fixedTimeAsDouble;
            while (scheduleOrigin + publishedCount * publishPeriod <= now)
            {
                DoUpdate();
                PublishMessage(CreateSnapshot(scheduleOrigin + publishedCount * publishPeriod));
                publishedCount++;
            }
        }

        // Publish はメッセージ参照をキューに積むだけで、直列化は送信スレッドが後から行う。
        // 使い回しの fluidPressureArrayMsg をそのまま渡すと、送信前に次の publish で内容が上書きされ、
        // 同一ステップ内の連続 publish で stamp が重複・欠落する。複製を渡す (#138)
        FluidPressureArrayMsg CreateSnapshot(double stampTime)
        {
            var array = new FluidPressureMsg[fluidPressureArrayMsg.array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                var src = fluidPressureArrayMsg.array[i];
                array[i] = new FluidPressureMsg(
                    MessageUtil.ToHeadermessage(stampTime, src.header.frame_id),
                    src.fluid_pressure,
                    src.variance);
            }
            return new FluidPressureArrayMsg(array);
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
        void PublishMessage(FluidPressureArrayMsg msg)
        {
            rosConnection.Publish(topicName, msg);
        }
    }
}

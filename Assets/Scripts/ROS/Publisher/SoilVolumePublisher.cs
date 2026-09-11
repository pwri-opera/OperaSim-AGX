using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
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
        private double publishPeriod;
        private double scheduleOrigin;
        private long publishedCount;
        // Start is called before the first frame update
        void Start()
        {
            DoStart();
            RegisterTopic();
            publishPeriod = 1.0 / Math.Max(1, Frequency());
            scheduleOrigin = Time.fixedTimeAsDouble;
        }

        // sim-time 定義の周波数を保つため FixedUpdate 起点で publish する(#56)。
        // 発火時刻は scheduleOrigin + n×period の均一グリッドで、fixed step (20ms) より
        // 細かい周波数では1ステップに複数回 publish される(Float64Msg のため stamp は無い)
        void FixedUpdate()
        {
            if (rosConnection == null || publishPeriod <= 0)
                return;
            double now = Time.fixedTimeAsDouble;
            while (scheduleOrigin + publishedCount * publishPeriod <= now)
            {
                DoUpdate();
                PublishMessage();
                publishedCount++;
            }
        }

        void RegisterTopic()
        {
            topicName = $"/{MachineName()}{TopicPhrase()}";
            soilVolumeMsg = new();
            rosConnection = ROSConnection.GetOrCreateInstance();
            // 処理落ち後の追いつき publish のバーストで既定の送信キュー (10) が溢れて
            // メッセージが捨てられるため、1 秒分を保持できる深さにする (#139)
            rosConnection.RegisterPublisher<Float64Msg>(topicName, (int)Math.Max(10, Frequency()));
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

        // Publish はメッセージ参照をキューに積むだけで、直列化は送信スレッドが後から行う。
        // 使い回しの soilVolumeMsg をそのまま渡すと、送信前に次の publish で値が上書きされ得るため複製を渡す (#138)
        void PublishMessage()
        {
            rosConnection.Publish(topicName, new Float64Msg(soilVolumeMsg.data));
        }
    }
}

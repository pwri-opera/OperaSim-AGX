using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
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
        private double publishPeriod;
        private double scheduleOrigin;
        private long publishedCount;

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
        // 使い回しの odometryMsg をそのまま渡すと、送信前に次の publish で内容が上書きされ、
        // 同一ステップ内の連続 publish で stamp が重複・欠落する。
        // 派生クラスが odometryMsg に odometry を累積するため、作り直しではなく複製を渡す (#138)
        OdometryMsg CreateSnapshot(double stampTime)
        {
            var src = odometryMsg;
            return new OdometryMsg(
                header: MessageUtil.ToHeadermessage(stampTime, src.header.frame_id),
                child_frame_id: src.child_frame_id,
                pose: new PoseWithCovarianceMsg(
                    new PoseMsg(
                        new PointMsg(src.pose.pose.position.x, src.pose.pose.position.y, src.pose.pose.position.z),
                        new QuaternionMsg(src.pose.pose.orientation.x, src.pose.pose.orientation.y, src.pose.pose.orientation.z, src.pose.pose.orientation.w)),
                    (double[])src.pose.covariance.Clone()),
                twist: new TwistWithCovarianceMsg(
                    new TwistMsg(
                        new Vector3Msg(src.twist.twist.linear.x, src.twist.twist.linear.y, src.twist.twist.linear.z),
                        new Vector3Msg(src.twist.twist.angular.x, src.twist.twist.angular.y, src.twist.twist.angular.z)),
                    (double[])src.twist.covariance.Clone()));
        }

        void RegisterTopic()
        {
            topicName = $"/{MachineName()}{TopicPhrase()}";
            odometryMsg = new();
            odometryMsg.header.frame_id = $"{MachineName()}{TopicPhrase()}";
            odometryMsg.child_frame_id = $"{MachineName()}/base_link";

            rosConnection = ROSConnection.GetOrCreateInstance();
            // 処理落ち後の追いつき publish のバーストで既定の送信キュー (10) が溢れて
            // メッセージが捨てられるため、1 秒分を保持できる深さにする (#139)
            rosConnection.RegisterPublisher<OdometryMsg>(topicName, (int)Math.Max(10, Frequency()));
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
        void PublishMessage(OdometryMsg msg)
        {
            rosConnection.Publish(topicName, msg);
        }
    }
}
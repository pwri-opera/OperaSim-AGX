using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using AGXUnity;
using System;
using agxPowerLine;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System.Security.AccessControl;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 角度をImuMsgでPublishするクラス
    /// </summary>
    public class ImuPublisher : MonoBehaviour
    {
        ROSConnection rosConnection;
        string topicName;
        ImuMsg imuMsg;

        /// <summary>
        /// 上部構造体のRigidBody
        /// </summary>
        RigidBody rigidBody;

        /// <summary>
        /// 上部構造体のオブジェクトを指定する
        /// 今回の場合body_link
        /// </summary>
        [SerializeField]GameObject upperBody;

        [SerializeField]uint frequency = 60;
        [SerializeField]string frameId = "";

        double publishPeriod;
        double scheduleOrigin;
        long publishedCount;

        void Start()
        {
            if (upperBody == null)
            {
                Debug.LogError($"{MachineName()} upper body not found");
                return;
            }
            rigidBody = upperBody.GetComponent<RigidBody>();
            if (rigidBody == null)
            {
                Debug.LogError($"{MachineName()} upper body has not RigidBody");
                return;
            }

            RegisterTopic();
            publishPeriod = 1.0 / Math.Max(1, frequency);
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
        // 使い回しの imuMsg をそのまま渡すと、送信前に次の publish で内容が上書きされ、
        // 同一ステップ内の連続 publish で stamp が重複・欠落する。複製を渡す (#138)。
        // orientation などは DoUpdate が毎回新しいインスタンスを代入するため参照共有でよい
        ImuMsg CreateSnapshot(double stampTime)
        {
            return new ImuMsg
            {
                header = MessageUtil.ToHeadermessage(stampTime, frameId),
                orientation = imuMsg.orientation,
                angular_velocity = imuMsg.angular_velocity,
                linear_acceleration = imuMsg.linear_acceleration,
            };
        }

        void RegisterTopic()
        {
            topicName = $"/{MachineName()}{TopicPhrase()}";
            imuMsg = new();

            rosConnection = ROSConnection.GetOrCreateInstance();
            rosConnection.RegisterPublisher<ImuMsg>(topicName);
        }

        void DoUpdate()
        {
            imuMsg.orientation = upperBody.transform.rotation.To<FLU>();
            imuMsg.angular_velocity = rigidBody.AngularVelocity.To<FLU>();
            imuMsg.linear_acceleration = rigidBody.LinearVelocity.To<FLU>();

            imuMsg.header = MessageUtil.ToHeadermessage(Time.fixedTimeAsDouble, frameId);
        }

        string MachineName()
        {
            return gameObject.name;
        }

        string TopicPhrase()
        {
            return "/upper_body_rot";
        }
        void PublishMessage(ImuMsg msg)
        {
            rosConnection.Publish(topicName, msg);
        }
    }
}

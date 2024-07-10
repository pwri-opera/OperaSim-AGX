using System.Collections;
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

            StartCoroutine(UpdateAndPublishMessage());
        }

        public IEnumerator UpdateAndPublishMessage()
        {
            RegisterTopic();
            while(true)
            {
                yield return new WaitForSecondsRealtime(1.0f / Math.Max(1, frequency));
                DoUpdate();
                PublishMessage();
            }
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
        void PublishMessage()
        {
            rosConnection.Publish(topicName, imuMsg);
        }
    }
}

using System.Collections;
using UnityEngine;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using AGXUnity;
using System;
using agxPowerLine;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Awsim.Entity;

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

        [SerializeField] ImuSensor imuSensor;
        Vector3 latestLinearAcceleration;
        Vector3 latestAngularVelocity;
        bool hasSensor;

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

            if (imuSensor == null && upperBody != null)
            {
                imuSensor = upperBody.GetComponent<ImuSensor>() ?? upperBody.GetComponentInChildren<ImuSensor>();
                if (imuSensor == null)
                {
                    imuSensor = upperBody.AddComponent<ImuSensor>();
                }
            }

            imuSensor.OnOutput += HandleImuSensorOutput;
            if (!imuSensor.IsInvoking("Output"))
            {
                imuSensor.Initialize();
            }
            hasSensor = true;

            StartCoroutine(UpdateAndPublishMessage());
        }

        void OnDisable()
        {
            if (imuSensor != null)
            {
                imuSensor.OnOutput -= HandleImuSensorOutput;
            }
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
            var orientationTransform = hasSensor && imuSensor != null ? imuSensor.transform : upperBody.transform;

            imuMsg.orientation = orientationTransform.rotation.To<FLU>();
            imuMsg.angular_velocity = latestAngularVelocity.To<FLU>();
            imuMsg.linear_acceleration = -latestLinearAcceleration.To<FLU>();

            imuMsg.header = MessageUtil.ToHeadermessage(Time.fixedTimeAsDouble, frameId);
        }

        void HandleImuSensorOutput(ImuSensor.IReadOnlyOutputData data)
        {
            latestLinearAcceleration = data.LinearAcceleration;
            latestAngularVelocity = data.AngularVelocity;
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

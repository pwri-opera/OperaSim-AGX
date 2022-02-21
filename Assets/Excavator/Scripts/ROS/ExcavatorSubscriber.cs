using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;
using TwistMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Twist;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// RosBridgeClient�𗘗p���Ė����V���x���̊e�A�N�`���G�[�^��ROS�g�s�b�N��subscribe���A�󂯂����b�Z�[�W�̎w�ߒl��
    /// Excavator��Constraint������͂�ݒ肷��X�N���v�g�B
    /// </summary>
    public class ExcavatorSubscriber : MonoBehaviour
    {
        [Header("Target Machine")]

        public Excavator excavator;
        
        [Header("ROS Bridge")]

        public RosConnector rosConnector;
        public int throttleRate = 0;

        [Header("Time Correction")]

        public bool useTimeCorrectedValues = true;

        [ConditionalHide("useTimeCorrectedValues")]
        public RealTimeTracker realTimeTracker;

        [ConditionalHide("useTimeCorrectedValues")]
        public int maxBufferSize = 200;

        [ConditionalHide("useTimeCorrectedValues")]
        public bool interpolatePositions = true;

        [Header("Topic Names")]

        [InspectorLabel("Tracks Twist")]
        // public string tracksTopicName = "/cmd_vel";
        public string tracksTopicName = "/zx120/tracks/cmd_vel";

        [InspectorLabel("Swing")]
        public string swingTopic = "/zx120/rotator/cmd";
        
        [InspectorLabel("Boom Tilt")]
        public string boomTopic = "/zx120/boom/cmd";
        
        [InspectorLabel("Arm Tilt")]
        public string armTopic = "/zx120/arm/cmd";
        
        [InspectorLabel("Bucket Tilt")]
        public string bucketTopic = "/backet/cmd";

        List<IMessageSubscriptionHandler> subscriptionHandlers = new List<IMessageSubscriptionHandler>();

        void Start()
        {
            CreateSubscriptions();
        }

        void CreateSubscriptions()
        {
            if (useTimeCorrectedValues && realTimeTracker == null)
            {
                Debug.LogError($"{name} cannot useTimeCorrectedValues because realTimeTracker is not set.");
                useTimeCorrectedValues = false;
            }

            if (rosConnector?.RosSocket == null)
            {
                Debug.LogWarning($"{name} Cannot create subscriptions because RosConnector or RosSocket is null.");
                return;
            }

            if (excavator?.GetInitialized<Excavator>() == null)
            {
                Debug.LogWarning($"{name} Cannot create subscriptions because excavator is null.");
                return;
            }
            
            // ���̃X�N���v�g��excavator.UpdateConstraintControl�����s����̂ŁA�����I�ȌĂяo���͕s�v
            excavator.autoUpdateConstraints = false;

            // Float64���Ԃ��郁�\�b�h
            var float64PositionInterpolator = interpolatePositions ? MessageUtil.Interpolate :
                (RealTimeDataBuffer<Float64Msg>.Interpolator)null;

            if (excavator.swing != null)
            {
                AddSubscriptionHandler<Float64Msg>(swingTopic, msg => excavator.swing.controlValue = msg.data,
                    float64PositionInterpolator);
            }

            if (excavator.boomTilt != null)
            {
                AddSubscriptionHandler<Float64Msg>(boomTopic, msg => excavator.boomTilt.controlValue = msg.data,
                    float64PositionInterpolator);
            }

            if (excavator.armTilt != null)
            {
                AddSubscriptionHandler<Float64Msg>(armTopic, msg => excavator.armTilt.controlValue = msg.data,
                    float64PositionInterpolator);
            }

            if (excavator.bucketTilt != null)
            {
                AddSubscriptionHandler<Float64Msg>(bucketTopic, msg => excavator.bucketTilt.controlValue = msg.data,
                    float64PositionInterpolator);
            }

            if (excavator.leftSprocket != null && excavator.rightSprocket != null)
            {
                // ���ѓ��m�̋����Asprocket�z�C�[�����a(���ь������܂�)���擾
                double separation, radius;
                if (!excavator.GetTracksSeparationAndRadius(out separation, out radius))
                    Debug.LogWarning($"{name} failed to get tracks separation and radius from {excavator.name}.");

                AddSubscriptionHandler<TwistMsg>(tracksTopicName, msg =>
                    MessageUtil.ConvertTwistToAngularWheelVelocity(
                        msg, separation, radius,
                        out excavator.leftSprocket.controlValue,
                        out excavator.rightSprocket.controlValue));
            }
        }

        void AddSubscriptionHandler<T>(string topicName, RosSharp.RosBridgeClient.SubscriptionHandler<T> messageAction,
            RealTimeDataBuffer<T>.Interpolator interpolator = null) where T : Message
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return;

            if (useTimeCorrectedValues)
            {
                var handler = new TimeCorrectedMessageSubscriptionHandler<T>(rosConnector, topicName, messageAction,
                    realTimeTracker, interpolator, throttleRate, maxBufferSize);

                subscriptionHandlers.Add(handler);
            }
            else
            {
                var handler = new MessageSubscriptionHandler<T>(rosConnector, topicName, messageAction, throttleRate);
                subscriptionHandlers.Add(handler);
            }
        }
        
        void FixedUpdate()
        {
            ExecuteSubscriptionHandlerActions(Time.fixedTimeAsDouble - Time.fixedDeltaTime);
        }

        void ExecuteSubscriptionHandlerActions(double time)
        {
            foreach (var handler in subscriptionHandlers)
                handler.ExecuteMessageAction(time);

            excavator.UpdateConstraintControls();
        }
    }
}

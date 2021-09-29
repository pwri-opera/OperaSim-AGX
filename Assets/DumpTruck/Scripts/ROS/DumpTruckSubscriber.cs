using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;
using TwistMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Twist;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// RosBridgeClientを利用してクローラダンプの各アクチュエータ指令のROSトピックにsubscribeし、受けたメッセージの指令値で
    /// DumpTruckのConstraint制御入力を設定する。
    /// </summary>
    public class DumpTruckSubscriber : MonoBehaviour
    {
        [Header("Target Machine")]

        public DumpTruck dumpTruck;
        
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
        public string tracksTopicName = "/ic120/tracks/cmd_vel";

        [InspectorLabel("Container Tilt")]
        public string containerTopic = "/ic120/vessel/cmd";

        List<IMessageSubscriptionHandler> subscriptionHandlers = new List<IMessageSubscriptionHandler>();

        void Start()
        {
            CreateSubscriptions();
        }

        void CreateSubscriptions()
        {
            if (useTimeCorrectedValues && realTimeTracker == null)
            {
                Debug.LogError($"{name} cannot useTimeCorrectedValues because realTimeTracker property is not set.");
                useTimeCorrectedValues = false;
            }

            if (rosConnector?.RosSocket == null)
            {
                Debug.LogWarning($"{name} Cannot create subscriptions because RosConnector or RosSocket is null.");
                return;
            }

            if (dumpTruck?.GetInitialized<DumpTruck>() == null)
            {
                Debug.LogWarning($"{name} Cannot create subscriptions because dumpTruck property is null.");
                return;
            }

            // このスクリプトがdumpTruck.UpdateConstraintControlを実行するので、自動的な呼び出しは不要
            dumpTruck.autoUpdateConstraints = false;

            // Float64を補間するメソッド
            var float64PositionInterpolator = interpolatePositions ? MessageUtil.Interpolate :
                (RealTimeDataBuffer<Float64Msg>.Interpolator)null;

            if (dumpTruck.containerTilt != null)
            {
                AddSubscriptionHandler<Float64Msg>(containerTopic, msg => dumpTruck.containerTilt.controlValue = msg.data,
                    float64PositionInterpolator);
            }

            if (dumpTruck.leftSprocket != null && dumpTruck.rightSprocket != null)
            {
                // 履帯同士の距離、sprocketホイール半径(履帯厚さを含む)を取得
                double separation, radius;
                if (!dumpTruck.GetTracksSeparationAndRadius(out separation, out radius))
                    Debug.LogWarning($"{name} failed to get tracks separation and radius from {dumpTruck.name}.");

                AddSubscriptionHandler<TwistMsg>(tracksTopicName, msg =>
                    MessageUtil.ConvertTwistToAngularWheelVelocity(
                        msg, separation, radius,
                        out dumpTruck.leftSprocket.controlValue,
                        out dumpTruck.rightSprocket.controlValue));
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

            dumpTruck.UpdateConstraintControls();
        }
    }
}

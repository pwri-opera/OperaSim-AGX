using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace PWRISimulator.ROS
{
    public abstract class MessageSubscriptionBase : MonoBehaviour
    {
        [SerializeField] bool useTimeCorrectedValues;
        [SerializeField] RealTimeTracker realTimeTracker;
        [SerializeField] int maxBufferSize = 200;
        List<IMessageSubscriptionHandler> subscriptionHandlers = new();

        void Start()
        {
            InitSettings();
            CreateSubscriptions();
        }

        void InitSettings()
        {
            if (useTimeCorrectedValues && realTimeTracker == null)
            {
                Debug.LogError($"{name} cannot useTimeCorrectedValues because realTimeTracker is not set.");
                useTimeCorrectedValues = false;
            }
        }

        protected abstract void CreateSubscriptions();

        protected void AddSubscriptionHandler<T>(string topicName,
                                       SubscriptionHandler<T> messageAction,
                                       RealTimeDataBuffer<T>.Interpolator interpolator = null) where T : Message
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return;

            if (useTimeCorrectedValues)
            {
                // 実行された時刻に受信したデータを使用
                var handler = new TimeCorrectedMessageSubscriptionHandler<T>(topicName, messageAction, realTimeTracker, interpolator, maxBufferSize);
                subscriptionHandlers.Add(handler);
            }
            else
            {
                // 実行された時点で直近に受信していたデータを使用
                var handler = new MessageSubscriptionHandler<T>(topicName, messageAction);
                subscriptionHandlers.Add(handler);
            }

        }

        public void ExecuteSubscriptionHandlerActions(double time)
        {
            foreach(var handler in subscriptionHandlers)
                handler.ExecuteMessageAction(time);
        }
    }
}

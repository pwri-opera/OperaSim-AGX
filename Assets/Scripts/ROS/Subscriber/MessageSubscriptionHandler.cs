using System.Collections;
using System.Collections.Generic;
using agx;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ROSの周期で送信されたメッセージをUnityが適切なタイミングで処理するためのインターフェース
    /// UnityでExecuteMessageActionを呼び出したタイミングでメッセージの処理を行う
    /// </summary>
    public interface IMessageSubscriptionHandler
    {
        void ExecuteMessageAction(double inputTime);
    }

    public delegate void SubscriptionHandler<T>(T t) where T: Message;

    /// <summary>
    /// メッセージを使用する時に直近に受信したメッセージを呼び出すクラス
    /// </summary>
    public class MessageSubscriptionHandler<T> : IMessageSubscriptionHandler where T : Message
    {
        readonly SubscriptionHandler<T> messageAction;
        T lastReceivedMsg = null;

        public MessageSubscriptionHandler(
            string topicName,
            SubscriptionHandler<T> subscriptionHandler)
        {
            messageAction = subscriptionHandler;
            ROSConnection.GetOrCreateInstance().Subscribe<T>(topicName, OnReceivedMessage);
        }

        /// <summary>
        /// メッセージ受信時の処理.rosConnectionが呼び出す
        /// </summary>
        void OnReceivedMessage(T msg)
        {
            lastReceivedMsg = msg;
        }

        /// <summary>
        /// メッセージを利用する処理.建設機械モデルに付与したSubscriberによって呼び出される想定
        /// inputTimeはゲーム内時刻
        /// </summary>
        public void ExecuteMessageAction(double inputTime)
        {
            if (lastReceivedMsg != null)
                messageAction(lastReceivedMsg);
        }
    }
    /// <summary
    /// メッセージを使用するときにゲーム内時刻と対応したメッセージを呼び出すクラス
    /// データ保持、補間のためにRealTimeTracker, RealTimeDataBufferを使用する
    /// </summary>
    public class TimeCorrectedMessageSubscriptionHandler<T> : IMessageSubscriptionHandler where T : Message
    {
        readonly SubscriptionHandler<T> messageAction;
        RealTimeTracker realTimeTracker;
        RealTimeDataBuffer<T> realTimeBuffer;

        public TimeCorrectedMessageSubscriptionHandler(
            string topicName,
            SubscriptionHandler<T> subscriptionHandler,
            RealTimeTracker synchronizer,
            RealTimeDataBuffer<T>.Interpolator interpolator = null,
            int maxBufferSize = 200)
        {
            messageAction = subscriptionHandler;
            ROSConnection.GetOrCreateInstance().Subscribe<T>(topicName, OnReceivedMessage);
            RealTimeDataAccessType accessType = interpolator != null ? RealTimeDataAccessType.Interpolate : RealTimeDataAccessType.Previous;

            realTimeBuffer = new RealTimeDataBuffer<T>(maxBufferSize, accessType, interpolator);
            realTimeTracker = synchronizer;
        }

        /// <summary>
        /// メッセージ受信時の処理.rosConnectionが呼び出す
        /// </summary>
        void OnReceivedMessage(T msg)
        {
            double realTime = realTimeTracker.RealTime;
            if (realTimeTracker.printToLog)
                Debug.Log($"OnReceivedMessage() realTime = {realTime}, msg = {MessageUtil.MessageToString(msg)}");
            realTimeBuffer.Add(msg, realTime);
        }

        /// <summary>
        /// メッセージを利用する処理.建設機械モデルに付与したSubscriberによって呼び出される想定
        /// inputTimeはゲーム内時刻
        /// </summary>
        public void ExecuteMessageAction(double inputTime)
        {
            double realTime = realTimeTracker.ConvertUnityTimeToRealTime(inputTime);
            T msg = realTimeBuffer.Get(realTime);

            if (realTimeTracker.printToLog)
                Debug.Log($"ExecuteMessageAction() time = {inputTime}, realTime = {realTime} " + 
                          $"msg = {MessageUtil.MessageToString(msg)}");

            if (msg != null)
                messageAction(msg);
        }
    }
}

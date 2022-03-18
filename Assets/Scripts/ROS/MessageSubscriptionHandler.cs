using System;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;

namespace PWRISimulator.ROS
{
    /// <summary>
    ///  継続的な周期に届くメッセージは、オーナーが管理する周期に適用できるようにするスクリプト。
    /// </summary>
    public interface IMessageSubscriptionHandler
    {
        /// <summary>
        /// メッセージをUnityの対象のパラメータとかに設定するメソッド。オーナーが周期的に呼び出すはずだ。
        /// </summary>
        /// <param name="time">現在のGameTimeやFixedTimeやシミュレーション(どこからの使い方によって)</param>
        void ExecuteMessageAction(double time);
    };

    /// <summary>
    /// メッセージを使いたいときに、最終に届いたメッセージを使用するIMessageSubscriptionHandlerスクリプト。
    /// 詳細：
    /// * メッセージが届くとOnReceivedMessageが別途なスレッドから呼び出され、メッセージを保存する（以前のメッセージを削除）。
    /// * オーナーがExecuteMessageActionを周期的にメインスレッドから実行して、最終に届いたメッセージを適用する。
    /// </summary>
    /// <typeparam name="T">ROSメッセージクラス</typeparam>
    public class MessageSubscriptionHandler<T> : IMessageSubscriptionHandler where T : Message
    {
        RosSharp.RosBridgeClient.SubscriptionHandler<T> messageAction;
        T lastReceivedValue = null;

        public MessageSubscriptionHandler(RosConnector rosConnector, string topicName,
            RosSharp.RosBridgeClient.SubscriptionHandler<T> messageAction, int throttleRate = 0)
        {
            this.messageAction = messageAction;
            if(rosConnector?.RosSocket == null)
            {
                Debug.LogError($"Failed to subscribe to topic \"{topicName}\" because RosConnector or RosSocket is null.");
                return;
            }
            rosConnector.RosSocket.Subscribe<T>(topicName, OnReceivedMessage, throttleRate);
        }

        /// <summary>
        /// メッセージが届いたときにROSBridgeClientから呼び出されるコールバックメソッド。メインスレッド以外スレッドから実行される。
        /// </summary>
        void OnReceivedMessage(T msg)
        {
            lastReceivedValue = msg;
        }

        /// <summary>
        /// 最終に届いたメッセージを適用する。オーナーが周期的に呼び出すはずだ。
        /// </summary>
        /// <param name="time">現在のGameTimeやFixedTimeやシミュレーション(どこからの使い方によって)</param>
        public void ExecuteMessageAction(double time)
        {
            T msg = lastReceivedValue;
            if (msg != null)
                messageAction(msg);
        }
    }

    /// <summary>
    /// メッセージを使いたいに、現在のGame時点に対応したリアルタイム時点に届いたメッセージを使用するIMessageSubscriptionHandler。
    /// 詳細：
    /// * メッセージが届くとOnReceivedMessageが別途なスレッドから呼び出され、メッセージをRealTimeDataBufferに挿入する。
    /// * ExecuteMessageActionを実行すると、現在のFixedTime時点を対応するリアルタイム時点に変換し、その時点に届いたメッセージを
    ///   適用する。リアルタイム時点が正確に一致しない問題を解決ために、２つの隣の届いたデータに基づいて補間する仕組みを提供する。
    /// </summary>
    /// <typeparam name="T">ROSメッセージクラス</typeparam>
    public class TimeCorrectedMessageSubscriptionHandler<T> : IMessageSubscriptionHandler where T : Message
    {
        RosSharp.RosBridgeClient.SubscriptionHandler<T> messageAction;
        RealTimeTracker realTimeTracker;
        RealTimeDataBuffer<T> realTimeDataBuffer;

        public TimeCorrectedMessageSubscriptionHandler(RosConnector rosConnector, string topicName,
            RosSharp.RosBridgeClient.SubscriptionHandler<T> messageAction,　RealTimeTracker synchronizer,
            RealTimeDataBuffer<T>.Interpolator interpolator = null, int throttleRate = 0, int maxBufferSize = 200)
        {
            this.messageAction = messageAction;
            if (rosConnector?.RosSocket == null)
            {
                Debug.LogError($"Failed to subscribe to topic \"{topicName}\" because RosConnector or RosSocket is null.");
                return;
            }
            
            rosConnector?.RosSocket.Subscribe<T>(topicName, OnReceivedMessage, throttleRate);

            RealTimeDataAccessType accessType = interpolator != null ? 
                RealTimeDataAccessType.Interpolate : RealTimeDataAccessType.Previous;

            realTimeDataBuffer = new RealTimeDataBuffer<T>(maxBufferSize, accessType, interpolator);
            realTimeTracker = synchronizer;
        }

        /// <summary>
        /// メッセージが届いたときにROSBridgeClientから呼び出されるコールバックメソッド。メインスレッド以外スレッドから実行される。
        /// </summary>
        void OnReceivedMessage(T msg)
        {
            double realTime = realTimeTracker.RealTime;
            if (realTimeTracker.printToLog)
                Debug.Log($"OnReceivedMessage() realTime = {realTime}, msg = {MessageUtil.MessageToString(msg)}");
            realTimeDataBuffer.Add(msg, realTime); 
        }

        /// <summary>
        /// 現在のtime時点を対応するリアルタイム時点に変換し、その時点に届いたメッセージを適用する。オーナーが周期的に呼び出す
        /// はずだ。
        /// </summary>
        /// <param name="time">現在のGameTimeやFixedTimeやシミュレーション(どこからの使い方によって)</param>
        public void ExecuteMessageAction(double time)
        {
            double realTime = realTimeTracker.ConvertUnityTimeToRealTime(time);
            T msg = realTimeDataBuffer.Get(realTime);

            if (realTimeTracker.printToLog)
                Debug.Log($"ExecuteMessageAction() time = {time}, realTime = {realTime} " + 
                          $"msg = {MessageUtil.MessageToString(msg)}");

            if (msg != null)
                messageAction(msg);
        }
    }
}

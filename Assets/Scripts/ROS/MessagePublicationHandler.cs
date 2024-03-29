using System;
using UnityEngine;
using RosSharp.RosBridgeClient;

namespace PWRISimulator.ROS
{
    public interface IMessagePublicationHandler
    {
        void UpdateAndSendMessage();

        void UnAdvertise();

    };

    /// <summary>
    /// 指示したデータ取得メソッドを使って、オーナーが管理する周期によるROSのtopicへpublishする。
    /// </summary>
    public class MessagePublicationHandler<T> : IMessagePublicationHandler where T : Message
    {
        RosConnector rosConnector;
        Func<T> getMessageFunction;
        string publicationId;

        public MessagePublicationHandler(RosConnector rosConnector, string topicName, Func<T> getMessageFunction)
        {
            this.rosConnector = rosConnector;
            this.getMessageFunction = getMessageFunction;

            if (getMessageFunction == null)
            {
                Debug.LogError($"Failed to advertise topic \"{topicName}\" because getMessageFunction null.");
                return;
            }

            if (rosConnector?.RosSocket == null)
            {
                Debug.LogError($"Failed to advertise topic \"{topicName}\" because RosConnector or RosSocket is null.");
                return;
            }

            publicationId = rosConnector.RosSocket.Advertise<T>(topicName);

            Debug.Log($"Advertised topic \"{topicName}\".");

        }

        public void UnAdvertise()
        {
            if (rosConnector?.RosSocket == null || publicationId == null)
                return;

            Debug.Log($"UnAdvertise topic \"{publicationId}\".");

            rosConnector.RosSocket.Unadvertise(publicationId);
            publicationId = null;
        }

        /// <summary>
        /// 指示したデータ取得メソッドを呼び出し、戻り値をpublishする。
        /// </summary>
        public void UpdateAndSendMessage()
        {
            if (rosConnector?.RosSocket == null || publicationId == null || getMessageFunction == null)
                return;

            T msg = getMessageFunction();
            rosConnector.RosSocket.Publish(publicationId, msg);
        }
    }
}

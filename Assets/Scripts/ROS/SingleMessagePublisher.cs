using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// １つROSトピックをpublishするコンポネントベースクラス。具体的な子クラスがこのベースクラスを継承してメッセージを作成し
    /// Publishメソッドを呼び出してpublishする。このベースクラスにadvertise/unadvertiseを任せる。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleMessagePublisher<T> : MonoBehaviour where T : Message
    {
        #region Inspector Properties

        public string topic;
        public RosConnector rosConnector;

        #endregion

        #region Private Variables

        bool hasStarted = false;
        bool isQuitting = false;

        protected string publicationId { get; set; } = null;

        #endregion

        #region Private Methods

        protected virtual void Reset()
        {
            // rosConnectorのデフォルト値にはシーンにあるRosConnectorを探して設定する
            rosConnector = FindObjectOfType<RosConnector>(includeInactive: false);
        }

        protected virtual void Start()
        {
            hasStarted = true;
            Advertise();
        }

        protected virtual void OnEnable()
        {
            // 開始にOnEnableはStartより早く呼び出されているが、そのときはRosConnectorはまだ初期化されていないので、
            // Start()までAdvertiseを待たせる。
            if (hasStarted)
                Advertise();
        }

        protected virtual void OnDisable()
        {
            if(!isQuitting)
                UnAdvertise();
        }

        protected virtual void OnDestroy()
        {
            publicationId = null;
        }
        
        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        void Advertise()
        {
            if (rosConnector?.RosSocket == null)
            {
                Debug.LogWarning($"{name} : Cannot advertise because RosConnector or RosSocket is null.");
                return;
            }

            if (string.IsNullOrEmpty(topic))
            {
                Debug.LogWarning($"{name} : Cannot advertise because topic is empty or null.");
                return;
            }

            if (publicationId != null)
                UnAdvertise();

            publicationId = rosConnector.RosSocket.Advertise<T>(topic);

            Debug.Log($"{name} : Advertised topic \"{topic}\".");

            OnAdvertised();
        }

        void UnAdvertise()
        {
            Debug.Log($"{name} : Unadvertise topic \"{topic ?? "null"}\".");

            try
            {
                if (rosConnector?.RosSocket != null && publicationId != null)
                    rosConnector.RosSocket.Unadvertise(publicationId);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"{name} : Failed to unadvertise. Exception: \n" + ex);
            }
            finally { publicationId = null;}

            OnUnadvertised();
        }

        protected virtual void OnAdvertised()
        { }

        protected virtual void OnUnadvertised()
        { }

        protected void Publish(T message)
        {
            if (rosConnector?.RosSocket == null || publicationId == null)
                return;

            rosConnector.RosSocket.Publish(publicationId, message);
        }

        #endregion
    }
}

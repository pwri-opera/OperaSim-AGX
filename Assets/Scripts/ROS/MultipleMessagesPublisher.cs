using System;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 複数のROSトピックをpublishするコンポネントベースクラス。具体的な子クラスがこのベースクラスを継承してOnAdvertiseメソッド
    /// から各トピックごとにAddPublicationHandler()を呼び出すようにしてください。このベースクラスは自動的に追加した
    /// PublicationHandlerをFrequencyによる周期的にPublishしている。
    /// </summary>
    public abstract class MultipleMessagesPublisher : MonoBehaviour
    {
        public RosConnector rosConnector;
        public int frequency = 20;
        
        List<IMessagePublicationHandler> publicationHandlers = new List<IMessagePublicationHandler>();

        bool hasStarted = false;
        bool isQuitting = false;

        /// <summary>
        /// 子クラスからオーバライドするメソッド。インプリケーション内には、各トピックにAddPublicationHandler()を呼び出すはずだ。
        /// </summary>
        protected abstract void OnAdvertise();

        protected virtual void Reset()
        {
            // rosConnectorのデフォルト値にはシーンにあるRosConnectorを探して設定する
            rosConnector = FindObjectOfType<RosConnector>(includeInactive: false);
        }

        protected virtual void Start()
        {
            hasStarted = true;
            StartCoroutine(UpdateAndPublishMessagesCoroutine());
            OnAdvertise();
        }

        protected virtual void OnEnable()
        {
            // 開始にOnEnableはStartより早く呼び出されているが、そのときはRosConnectorはまだ初期化されていないので、
            // Start()までAdvertiseを待たせる。
            if (hasStarted)
            {
                StartCoroutine(UpdateAndPublishMessagesCoroutine());
                OnAdvertise();
            }
        }

        protected virtual void OnDisable()
        {
            if (!isQuitting)
            {
                StopCoroutine(nameof(UpdateAndPublishMessagesCoroutine));

                foreach (var handler in publicationHandlers)
                    handler.UnAdvertise();

                publicationHandlers.Clear();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        protected void AddPublicationHandler<T>(string topicName, Func<T> getMessageFunction) where T : Message
        {
            if (string.IsNullOrWhiteSpace(topicName))
                return;

            var handler = new MessagePublicationHandler<T>(rosConnector, topicName, getMessageFunction);
            publicationHandlers.Add(handler);
        }

        void UpdateAndPublishMessages()
        {
            foreach (var handler in publicationHandlers)
                handler.UpdateAndSendMessage();
        }

        System.Collections.IEnumerator UpdateAndPublishMessagesCoroutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.0f / Math.Max(1, frequency));
                UpdateAndPublishMessages();
            }
        }
    }
}

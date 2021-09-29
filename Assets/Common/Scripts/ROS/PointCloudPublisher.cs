using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Unity.Profiling;
using RosSharp;
using RosSharp.RosBridgeClient;
using PointCloud2Msg = RosSharp.RosBridgeClient.MessageTypes.Sensor.PointCloud2;
using PointFieldMsg = RosSharp.RosBridgeClient.MessageTypes.Sensor.PointField;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 指示したPointCloudGeneratorオブジェクトから点群データを取得しROSへ通信する。
    /// </summary>
    public class PointCloudPublisher : SingleMessagePublisher<PointCloud2Msg>
    {
        #region Inspector Properties

        public PointCloudGenerator pointCloudGenerator;
        public string frameId = "";
        public int frequency = 1;
        public bool includeTimeInMessage = false;
        public bool publishFromThread = false;

        #endregion

        #region Private Variables

        PointCloud2Msg message = null;
        Vector3[] points = new Vector3[0];
        float[] pointsAsFloats = new float[0];
        byte[] pointsAsBytes = new byte[0];

        // 別途のpublishスレッド用：

        Thread publishThread;
        ManualResetEventSlim publishResetEvent = new ManualResetEventSlim(false);
        CancellationTokenSource cancellationTokenSource = null;

        // プロファイリング用：

        static readonly ProfilerMarker profileMarker_GetPoints = new ProfilerMarker(
            ProfilerCategory.Scripts, nameof(PointCloudPublisher) + ".GetPoints");

        static readonly ProfilerMarker profileMarker_ConvertPoints = new ProfilerMarker(
            ProfilerCategory.Scripts, nameof(PointCloudPublisher) + ".ConvertPoints");

        static readonly ProfilerMarker profileMarker_CreateMessage = new ProfilerMarker(
            ProfilerCategory.Scripts, nameof(PointCloudPublisher) + ".CreateMessage");

        static readonly ProfilerMarker profileMarker_SendMessage = new ProfilerMarker(
            ProfilerCategory.Scripts, nameof(PointCloudPublisher) + ".SendMessage");

        #endregion

        #region Private Methods

        protected override void OnAdvertised()
        {
            base.OnAdvertised();
            
            // 指示した周波数にpublishするCoroutineを開始
            StartCoroutine(PublishCoroutine());
        }

        protected override void OnUnadvertised()
        {
            base.OnUnadvertised();
            
            // publishするCoroutineを中止
            StopCoroutine(nameof(PublishCoroutine));
            
            // 別途なスレッドからpublishを行うバイアはそのスレッドを中止
            if (publishThread != null)
                StopPublishThread();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (publishThread != null)
                StopPublishThread();
        }

        /// <summary>
        /// 指示した周波数にpublishする。
        /// </summary>
        System.Collections.IEnumerator PublishCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f / Math.Max(1, frequency));
                UpdateMessageAndPublish();
            }
        }

        /// <summary>
        /// PointCloudGeneratorから点群データを取得して、ROS Messageを更新してpublishする。
        /// publishFromThreadはtrueの場合は、直接このスレッドからpublishしなく、publishResetEventをsetして別途のPublishThread
        /// がpublishさせる。
        /// </summary>
        void UpdateMessageAndPublish()
        {
            if (rosConnector?.RosSocket == null || publicationId == null)
                return;

            if (publishFromThread && publishResetEvent.IsSet)
            {
                Debug.LogWarning($"{name} : Attempting to generate point cloud while Publish Thread is not done yet." +
                                 " Will skip point cloud generation and wait until occurance.");
                return;
            }

            using (profileMarker_GetPoints.Auto())
            {
                // ポイントデータを取得
                if (pointCloudGenerator != null)
                    pointsAsFloats = pointCloudGenerator.GeneratePointCloud(flipX: true);
                else
                    Debug.LogWarning($"{name} Cannot generate point cloud because Point Cloud Generator is null.");
            }

            using (profileMarker_ConvertPoints.Auto())
            {
                // ポイントデータをbyte配列へ変換
                ConvertFloatsToRosByteArray(pointsAsFloats, ref pointsAsBytes);
            }

            using (profileMarker_CreateMessage.Auto())
            {
                // 一回だけ再使用できるメッセージベースを作成
                if (message == null)
                {
                    message = new PointCloud2Msg();
                    message.header = MessageUtil.ToHeaderMessage(0, frameId);
                    message.data = new byte[0];
                    message.width = 0;
                    message.height = 1;
                    message.fields = CreatePointFields(reorder: true);
                    message.is_bigendian = false;
                    message.point_step = 3 * sizeof(float);
                    message.row_step = 1;
                    message.is_dense = true;
                }

                // 変わるデータだけを書き込む
                message.header = MessageUtil.ToHeaderMessage(includeTimeInMessage ? Time.timeAsDouble : 0, frameId);
                message.data = pointsAsBytes;
                message.width = (uint)pointsAsBytes.Length / message.point_step;
            }

            using (profileMarker_SendMessage.Auto())
            {
                // 通信させる
                if (publishFromThread)
                {
                    if (publishThread == null || !publishThread.IsAlive)
                    {
                        if (cancellationTokenSource == null)
                            cancellationTokenSource = new CancellationTokenSource();
                        var token = cancellationTokenSource.Token;
                        publishThread = new Thread(() => PublishThread(token));
                        publishThread.Name = nameof(PointCloudPublisher) + ".PublishThread";
                        publishThread.Start();
                    }
                    publishResetEvent.Set();
                }
                else
                {
                    Publish(message);
                }
            }
        }

        /// <summary>
        /// publishするスレッド。publishResetEventがsetになるとpublishをして、publishが終わったらpublishResetEventをreset。
        /// </summary>
        /// <param name="cancellationToken"></param>
        void PublishThread(CancellationToken cancellationToken)
        {
            Debug.Log("Publish thread is starting.");
            int timeoutMs = 10000; // timeoutにならないコードを書いたけど年の民にtimeout使用
            while (publicationId != null)
            {
                try
                {
                    if (!publishResetEvent.Wait(timeoutMs, cancellationToken))
                        continue;
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is ObjectDisposedException ||
                        ex is OperationCanceledException || ex is InvalidOperationException)
                        break; // わざとCancellationTokenをキャンセルしたせいのException
                    else
                        throw;
                }
                if (publicationId != null)
                {
                    Publish(message);
                    publishResetEvent.Reset();
                }
            }
            publishResetEvent.Reset();
            Debug.Log("Publish thread is exiting.");
        }

        void StopPublishThread(int timeoutUntilAbort = 2000)
        {
            Debug.Log($"{name} : Stopping publish thread.");
            publicationId = null;

            if (cancellationTokenSource != null)
            {
                Debug.Log($"{name} : Cancelling the cancellation token source.");
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            if (publishThread != null && !publishThread.Join(timeoutUntilAbort))
            {
                Debug.Log($"{name} : Failed to stop the publish thread. Aborting it.");
                publishThread.Abort();
                publishThread = null;
            }
            Debug.Log($"{name} : Publish thread stopped.");
        }

        /// <summary>
        /// １つPointの定義を作成。
        /// </summary>
        /// <returns></returns>
        static PointFieldMsg[] CreatePointFields(bool reorder)
        {
            // ROS X = Unity Z
            // ROS Y = Unity -X
            // ROS Z = Unity Y
            // つまり、
            // Unity X = ROS -Y
            // Unity Y = ROS Z
            // Unity Z = ROS X

            // ※ x座標は既にデータを生成のときに逆にしておいたので負号を付けない（rvizは負号のField記載を対応したいないようだから）
            PointFieldMsg[] pointFields = {
                new PointFieldMsg(reorder ? "y" : "x", 0 * sizeof(float), PointFieldMsg.FLOAT32, 1),
                new PointFieldMsg(reorder ? "z" : "y", 1 * sizeof(float), PointFieldMsg.FLOAT32, 1),
                new PointFieldMsg(reorder ? "x" : "z", 2 * sizeof(float), PointFieldMsg.FLOAT32, 1)};

            return pointFields;
        }

        /// <summary>
        /// float配列をbyte配列へ変換。座標系変換は行わない。
        /// </summary>
        /// <param name="floats">入力のfloat配列</param>
        /// <param name="bytes">出力のbyte配列</param>
        void ConvertFloatsToRosByteArray(float[] floats, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length != floats.Length * sizeof(float))
            {
                bytes = new byte[floats.Length * sizeof(float)];
                //Debug.Log($"{name} : Resized byte array to {bytes.Length} ({floats.Length} floats).");
            }
            Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Vector3配列をbyte配列へ変換。UnityからROSへの座標系変換を行う。
        /// </summary>
        /// <param name="floats">入力のVector3配列</param>
        /// <param name="bytes">出力のbyte配列</param>
        void ConvertPointsToRosByteArray(Vector3[] points, ref byte[] bytes)
        {
            if (bytes == null || bytes.Length != points.Length * 3 * sizeof(float))
            {
                bytes = new byte[points.Length * 3 * sizeof(float)];
                Debug.Log($"{name} : Resized byte array to {bytes.Length} ({points.Length} points).");
            }

            float[] comp = new float[1];
            for (int i = 0, j = 0; i < points.Length; i++, j += 12)
            {
                // 以降のコードは、floatをbytesへ変換するときに、同時にROS座標系へ変換（x = z, y = -x, z = y）
                Vector3 p = points[i];

                // X座標のfloatをbyteとして書き込む
                comp[0] = p.z;
                Buffer.BlockCopy(comp, 0, bytes, j + 0, sizeof(float));

                // Y座標のfloatをbyteとして書き込む
                comp[0] = -p.x;
                Buffer.BlockCopy(comp, 0, bytes, j + 4, sizeof(float));

                // Z座標のfloatをbyteとして書き込む
                comp[0] = p.y;
                Buffer.BlockCopy(comp, 0, bytes, j + 8, sizeof(float));
            }
        }

        #endregion
    }
}

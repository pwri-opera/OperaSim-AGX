using System;
using RosSharp.RosBridgeClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// Unityのメインループの仕組みの性質上、UnityのGameTimeとFixedTimeがリアルタイムと外れている。UnityのGameTimeと
    /// FixedTimeは継続的なリアルタイムに対して遅れ、離散的なステップで進められる。このスクリプトはUnityのGameTimeとリアルタイム
    /// の関係を測り、２つのタイムラインの間変換できる機能を提供する。
    /// 
    /// 外部のシステムからネットワークデータが周期的に届き、そのデータを正しいGameTimeに使用したい場合に役に立つ。
    /// RealTimeDataBufferと一緒に使用したらと、リアルタイムの時点に届いたネットワークデータがその時点に対して適当な
    /// GameTime時点から取得することができるようになる。
    /// 
    /// 例：
    /// 1. 初期化に、各データごとにRealTimeDataBufferというバッファオブジェクトを作成(generic仕組みを適用ので、型はなんでもOK）
    /// 2. 別途なスレッドにネットワークデータが届くと、RealTimeTracker.RealtimeのタイムスタンプでRealTimeDataBuffer
    ///    バッファにデータを挿入。
    /// 3. メインスレッドのFixedUpdateメソッドから、RealTimeTracker.ConvertUnityTimeToRealTime()メソッドを使って現在の
    ///    FixedTime時点を対するリアルタイム時点に変換し、そのタイムスタンプでRealTimeDataBufferバッファからをデータを取得し,
    ///    そのデータで対象のUnityパラメーターを更新する。（一致するタイムスタンプバッファにない場合は、オプションによって隣の
    ///    タイムスタンプのデータを取得する、または周りの２つのデータに基づいて補間する）
    /// </summary>
    [DefaultExecutionOrder(-32000)] // 各Frameの開始に時間を測るために、他のスクリプトより早く実行されるようにOrderを最低に設定
    public class RealTimeTracker : MonoBehaviour
    {
        /// <summary>
        /// デバッグ用のメッセージをコンソールウィンドウにプリントするか。
        /// </summary>
        public bool printToLog = false;

        /// <summary>
        /// リアルタイムとUnityのタイムラインの時間差。0にしてもメインループの仕組みでUnityのタイムラインがリアルタイムより１つ
        /// メインループフレーム遅いので、一般的に0より大きくしなくてもOK。ただし、ネットワークデータが低い周波数で届いている場合は、
        /// 補間するためのデータ範囲が十分になるように０より少し大きく設定しても良いが、自然的にに遅延が発生する。
        /// </summary>
        public double inputValueDelay = 0.0;

        /// <summary>
        /// 第一Frameの開始から掛かったライルタイム（秒）。
        /// </summary>
        public double RealTime
        {
            get { return realTimeWatch.Elapsed.TotalSeconds; }
        }

        /// <summary>
        /// 第一Frameから現在のFrameの開始までかかったライルタイム（秒）。
        /// </summary>
        public double RealTimeAtStartOfFrame
        {
            get
            {
                if (!realtimeAtStartOfFrameIsUpToDate)
                    Debug.LogWarning("Reading RealTimeAtStartOfFrame at a time when it has not yet been updated.");
                return realtimeAtStartOfFrame;
            }
        }

        /// <summary>
        /// UnityがカットしたGameTime(つまり、リアルタイムに対して相対的な総計時間の差)。各Frameに一回更新される(第一Frame以外)。
        /// </summary>
        public double SkippedGameTime
        {
            get { return skippedGameTime; }
        }

        /// <summary>
        /// inputValueDelay及びUnityがカットしたGameTimeに対応して、UnityのGameTimeの時点をリアルタイム時点へ変換する。
        /// </summary>
        /// <param name="gameTimeOrFixedTime">UnityのGameTimeまたはFixedTime</param>
        public double ConvertUnityTimeToRealTime(double gameTimeOrFixedTime)
        {
            return gameTimeOrFixedTime - inputValueDelay + skippedGameTime;
        }

        #region Private

        // 最初のFrameの始めからのリアルタイムを測るWatch
        System.Diagnostics.Stopwatch realTimeWatch = new System.Diagnostics.Stopwatch();

        // realTimeWatchは開始したか
        bool watchStarted = false;

        double realtimeAtStartOfFrame = 0.0;  // 現在のFrameの開始に測ったrealTimeWatch値
        double realtimeAtStartOfFramePrev = 0.0;
        bool realtimeAtStartOfFrameIsUpToDate = false;

        double skippedGameTime = 0.0; // パフォーマンスなどのせいでUnityがカットしたGameTime（とFixedTime）。 
        double skippedGameTimePrev = 0.0;

        bool resyncRequested = true;  // unscaledTimeCorrectionをまた計算して更新したいかどうか
        double unscaledTimeCorrection = 0.0; //　Time.unscaledTimeとrealTimeWatchの差

        void Update()
        {
            // 今回のFrameにFixedUpdateが呼び出されなかった場合は、Updateから開始時間を測る。
            MeasureRealTimeAtBeginFrame();

            if (printToLog)
                Debug.Log($"{name} : Update() F{Time.frameCount} " +
                          $"DeltaTime = {Time.deltaTime: 0.####} " +
                          $"UnscaledDeltaTime = {Time.unscaledDeltaTime: 0.####} " +
                          $"unscaledTimeAsDouble = {Time.unscaledTimeAsDouble: 0.####} " +
                          $"GameTime = {Time.time: 0.####} " +
                          $"RealTime = {RealTime: 0.####} " +
                          $"RealTimeDeltaTime = {RealTimeAtStartOfFrame - realtimeAtStartOfFramePrev: 0.####} " +
                          $"RealTimeAtStartOfFrame = {RealTimeAtStartOfFrame: 0.####} " +
                          $"RealTimeDiffAtStartOfFrame = {RealTimeAtStartOfFrame - Time.time: 0.####} " +
                          $"SkippedGameTime (updated previous frame) = {skippedGameTime: 0.####} "
                          );
        }

        /// <summary>
        /// まだ行っていない場合は、現在のFrameの開始時間(リアルタイム）を測る。
        /// </summary>
        void MeasureRealTimeAtBeginFrame()
        {
            if (!watchStarted)
            {
                realTimeWatch.Start();
                watchStarted = true;
                realtimeAtStartOfFrame = 0.0;
                realtimeAtStartOfFrameIsUpToDate = true;
            }
            else if (!realtimeAtStartOfFrameIsUpToDate)
            {
                realtimeAtStartOfFramePrev = realtimeAtStartOfFrame;
                realtimeAtStartOfFrame = realTimeWatch.Elapsed.TotalSeconds;
                realtimeAtStartOfFrameIsUpToDate = true;
            }
        }

        void LateUpdate()
        {
            // skippedGameTimeを更新
            UpdateSkippedGameTime();

            // realtimeAtStartOfFrameが次のFrameの開始に測られるように
            realtimeAtStartOfFrameIsUpToDate = false;
        }

        /// <summary>
        /// skippedGameTimeを計算する。LateUpdateまたはUpdateから呼び出す必要（絶対FixedUpdateから実行しない）。
        /// </summary>
        void UpdateSkippedGameTime()
        {
            skippedGameTimePrev = skippedGameTime;

            // 第一Frameに、リアルタイムと比べてunscaledTimeがおかしく増えてくるので使えない。Frame２からリアルタイムと同じ
            // ように増えるので、Frame2からunscaledTimeとtimeを比べてskippedGameTimeを計算できる。
            if (Time.frameCount >= 2)
            {
                // 第一FrameにunscaledTimeがおかしく増えてくたので、Frame2にunscaledTimeとリアルタイムの差を保存して、これから
                // unscaledTimeとtimeを比べるのにその差を含む。
                if (resyncRequested)
                {
                    unscaledTimeCorrection = Time.unscaledTimeAsDouble - RealTimeAtStartOfFrame;
                    resyncRequested = false;
                }
                skippedGameTime = (Time.unscaledTimeAsDouble - unscaledTimeCorrection) - Time.timeAsDouble;
            }
            
            if (Math.Abs(skippedGameTime - skippedGameTimePrev) >= 1e-04)
                Debug.Log($"{name} : Unity has skipped {skippedGameTime - skippedGameTimePrev: 0.####}s game time " +
                          $"(at frame {Time.frameCount - 1}). Total skipped game time: {skippedGameTime: 0.####}s");

            if (skippedGameTime - skippedGameTimePrev <= -1e-04)
                Debug.LogError("Critical Timing Problem: SkippedGameTime has decreased.");

            if (printToLog)
                Debug.Log($"{name} : skippedGameTime = {skippedGameTime}");
        }

        void FixedUpdate()
        {
            MeasureRealTimeAtBeginFrame();

            if (printToLog)
                Debug.Log($"{name} : FixedUpdate() F{Time.frameCount} fixedTime = {Time.fixedTime: 0.###}");
        }

        void OnDestroy()
        {
            realTimeWatch.Stop();
        }

        #endregion
    }
}

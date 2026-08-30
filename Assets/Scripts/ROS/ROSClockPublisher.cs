using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Rosgraph;
using Unity.Robotics.Core;
// From: https://github.com/Unity-Technologies/Robotics-Nav2-SLAM-Example/tree/main/Nav2SLAMExampleProject/Assets/Scripts

namespace PWRISimulator.ROS
{
    public class ROSClockPublisher : MonoBehaviour
    {
        [SerializeField]
        Clock.ClockMode m_ClockMode;

        [SerializeField, HideInInspector]
        Clock.ClockMode m_LastSetClockMode;
        
        [SerializeField]
        double m_PublishRateHz = 100f;

        int m_StepInterval;
        int m_StepCount;

        ROSConnection m_ROS;

        void OnValidate()
        {
            var clocks = FindObjectsOfType<ROSClockPublisher>();
            if (clocks.Length > 1)
            {
                Debug.LogWarning("Found too many clock publishers in the scene, there should only be one!");
            }

            if (Application.isPlaying && m_LastSetClockMode != m_ClockMode)
            {
                Debug.LogWarning("Can't change ClockMode during simulation! Setting it back...");
                m_ClockMode = m_LastSetClockMode;
            }
            
            SetClockMode(m_ClockMode);
        }

        void SetClockMode(Clock.ClockMode mode)
        {
            Clock.Mode = mode;
            m_LastSetClockMode = mode;
        }

        // Start is called before the first frame update
        void Start()
        {
            SetClockMode(m_ClockMode);
            m_ROS = ROSConnection.GetOrCreateInstance();
            // publish 周期を fixed step 数に換算する。fixed step (50 Hz) より細かい設定は毎ステップに丸まる
            m_StepInterval = Math.Max(1,
                (int)Math.Round(1.0 / (Math.Max(m_PublishRateHz, 0.01) * Time.fixedDeltaTime)));
            // 処理落ち後の追いつき publish のバーストで既定の送信キュー (10) が溢れて
            // メッセージが捨てられるため、実効レートの 1 秒分を保持できる深さにする (#139)
            int queueSize = Math.Max(10, (int)Math.Round(1.0 / (m_StepInterval * Time.fixedDeltaTime)));
            m_ROS.RegisterPublisher<ClockMsg>("clock", queueSize);
        }

        void PublishMessage()
        {
            var publishTime = Clock.time;
            var timestamp = new TimeStamp(publishTime);
            var clockMsg = new TimeMsg
            {
                sec = timestamp.Seconds,
                nanosec = timestamp.NanoSeconds
            };
            m_ROS.Publish("clock", clockMsg);
        }

        // /clock の sim-time 刻みを fps 非依存で fixed step に揃えるため FixedUpdate 起点で publish する(#58)。
        // Clock.time (UnityScaled = Time.timeAsDouble) は FixedUpdate 内では fixed step 時刻を返す
        void FixedUpdate()
        {
            if (m_ROS == null)
                return;
            if (++m_StepCount < m_StepInterval)
                return;
            m_StepCount = 0;
            PublishMessage();
        }
    }
}
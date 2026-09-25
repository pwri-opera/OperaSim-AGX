using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Rosgraph;
using Unity.Robotics.Core;
// From: https://github.com/Unity-Technologies/Robotics-Nav2-SLAM-Example/tree/main/Nav2SLAMExampleProject/Assets/Scripts

namespace PWRISimulator.ROS
{
    [DefaultExecutionOrder(100)] // Publish after the default-order sensor publishers in each fixed step.
    public class ROSClockPublisher : MonoBehaviour
    {
        [SerializeField]
        Clock.ClockMode m_ClockMode;

        [SerializeField, HideInInspector]
        Clock.ClockMode m_LastSetClockMode;
        
        [SerializeField]
        double m_PublishRateHz = 60f;

        double m_PublishPeriod;
        double m_ScheduleOrigin;
        long m_PublishedCount;

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
            // データパブリッシャ(joint_states, odom等)と同じ scheduleOrigin + n×period
            // スケジューリングを採用し、stamp グリッドを一致させる (#96)。
            // m_PublishRateHz はデータパブリッサの frequency と同じ値にすること。
            m_PublishPeriod = 1.0 / Math.Max(1, m_PublishRateHz);
            m_ScheduleOrigin = Time.fixedTimeAsDouble;
            // 処理落ち後の追いつき publish のバーストで既定の送信キュー (10) が溢れて
            // メッセージが捨てられるため、実効レートの 1 秒分を保持できる深さにする (#139)
            int queueSize = Math.Max(10, (int)Math.Round(1.0 / m_PublishPeriod));
            m_ROS.RegisterPublisher<ClockMsg>("clock", queueSize);
        }

        void PublishMessage(double stampTime)
        {
            var timestamp = new TimeStamp(stampTime);
            var clockMsg = new TimeMsg
            {
                sec = timestamp.Seconds,
                nanosec = timestamp.NanoSeconds
            };
            m_ROS.Publish("clock", clockMsg);
        }

        // Keep /clock after sensor publication within each physics step. Publishing
        // only in Update lets a catch-up batch send future-dated odom/TF while ROS
        // time is still at the previous rendered frame; the EKF cannot consume those
        // measurements yet. Execution order preserves data-before-clock ordering
        // without waiting for the entire FixedUpdate batch to finish.
        void FixedUpdate()
        {
            if (m_ROS == null || m_PublishPeriod <= 0)
                return;
            double now = Time.fixedTimeAsDouble;
            while (m_ScheduleOrigin + m_PublishedCount * m_PublishPeriod <= now)
            {
                PublishMessage(m_ScheduleOrigin + m_PublishedCount * m_PublishPeriod);
                m_PublishedCount++;
            }
        }
    }
}
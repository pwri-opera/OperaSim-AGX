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

        // /clock を Update() で publish する: Update はすべての FixedUpdate() 完了後に実行されるため、
        // データパブリッシャが FixedUpdate で publish したメッセージが TF broadcaster に届く前に
        // /clock が consumer に到達するのを防げる (#96 のメッセージ到着順序問題)。
        // now には Time.timeAsDouble ではなく Time.fixedTimeAsDouble を使う: Update の timeAsDouble は
        // 補間時間を含むため fixedTime より進んでおり、clock stamp がデータ stamp より未来になると
        // ExtrapolationException が発生する。fixedTimeAsDouble を使うことで clock がデータと同じ時刻
        // グリッドに留まり、TF lookup が成功する。fps 非依存は scheduled time で保証 (#58)。
        void Update()
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
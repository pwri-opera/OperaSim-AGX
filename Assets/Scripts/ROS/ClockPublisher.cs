using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using RosSharp;
using RosSharp.RosBridgeClient;
using ClockMsg = RosSharp.RosBridgeClient.MessageTypes.Rosgraph.Clock;

namespace PWRISimulator.ROS
{
    public class ClockPublisher : SingleMessagePublisher<ClockMsg>
    {
        public enum TimeSource {
            GameTime,
            FixedTime,
            AgxTime,
            RealTime,
            RealTimeAtStartOfFrame,
            UnixTime,
            RealtimeSyncedGameTime,
            RealtimeSyncedFixedTime,
            RealtimeSyncedAgxTime,
        };

        public TimeSource timeSource = TimeSource.GameTime;
        public RealTimeTracker realtimeTracker;

        ClockMsg message = null;

        static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        void Update()
        {
            if (publicationId == null)
                return;

            if (message == null)
                message = new ClockMsg();
            
            MessageUtil.UpdateTimeMsg(message.clock, GetTime());
            Publish(message);
        }

        /// <summary>
        /// timeSourceによるの時間を測って返す（単位：秒）。
        /// </summary>
        double GetTime()
        {
            switch(timeSource)
            {
                case TimeSource.GameTime:
                    return Time.timeAsDouble;

                case TimeSource.FixedTime:
                    return Time.fixedTimeAsDouble;

                case TimeSource.AgxTime:
                    return Simulation.HasInstance && Simulation.Instance.Native != null ?
                        Simulation.Instance.Native.getTimeStamp() : 0;

                case TimeSource.RealTime:
                    return realtimeTracker != null ? realtimeTracker.RealTime : Time.realtimeSinceStartupAsDouble;

                case TimeSource.UnixTime:
                    return (DateTime.Now.ToUniversalTime() - UNIX_EPOCH).TotalMilliseconds * 0.001;

                case TimeSource.RealTimeAtStartOfFrame:
                    return realtimeTracker != null ? realtimeTracker.RealTimeAtStartOfFrame : 0;

                case TimeSource.RealtimeSyncedGameTime:
                    return realtimeTracker != null ? realtimeTracker.ConvertUnityTimeToRealTime(Time.timeAsDouble) : 0;

                case TimeSource.RealtimeSyncedFixedTime:
                    return realtimeTracker != null ? realtimeTracker.ConvertUnityTimeToRealTime(Time.fixedTimeAsDouble) : 0;

                case TimeSource.RealtimeSyncedAgxTime:
                    return realtimeTracker != null && Simulation.HasInstance && Simulation.Instance.Native != null ? 
                        realtimeTracker.ConvertUnityTimeToRealTime(Simulation.Instance.Native.getTimeStamp()) : 0;

                default:
                    return 0;
            }
        }
    }
}

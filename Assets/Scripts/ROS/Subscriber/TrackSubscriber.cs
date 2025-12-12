using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RosMessageTypes.Com3;
using RosMessageTypes.Geometry;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 下部走行体(=履帯)の制御信号を受信するクラス
    /// </summary>
    public class TrackMessageSubscriber : MessageSubscriptionBase
    {
        JointCmdMsg trackCmd = new(2);
        public JointCmdMsg TrackCmd 
        {
            get => trackCmd;
            private set => trackCmd = value;
        }
        JointCmdMsg volumeCmd = new(2);
        public JointCmdMsg VolumeCmd 
        {
            get => volumeCmd; 
            private set => volumeCmd = value;
        }
        TwistMsg velocityCmd = new();
        public TwistMsg VelocityCmd 
        {
            get => velocityCmd; 
            private set => velocityCmd = value;
        }

        readonly string trackCmdPhrase = "/track_cmd";
        readonly string volumeCmdPhrase = "/track_volume_cmd";
        readonly string velocityCmdPhrase = "/cmd_vel";


        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;

            AddSubscriptionHandler<JointCmdMsg>($"/{machineName}{trackCmdPhrase}", msg => TrackCmd = msg);
            AddSubscriptionHandler<JointCmdMsg>($"/{machineName}{volumeCmdPhrase}", msg => VolumeCmd = msg);
            AddSubscriptionHandler<TwistMsg>($"/{machineName}{velocityCmdPhrase}", msg => VelocityCmd = msg);
        }
    }
}

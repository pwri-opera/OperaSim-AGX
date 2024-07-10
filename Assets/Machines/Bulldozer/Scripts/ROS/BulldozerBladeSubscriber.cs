using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Com3;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ブルドーザの上部旋回体の制御信号を受信するクラス
    /// </summary>
    public class BulldozerBladeSubscriber : MessageSubscriptionBase
    {
        JointCmdMsg bladeCmd = new(3);
        public JointCmdMsg BladeCmd 
        {
            get => bladeCmd;
            private set => bladeCmd = value;
        }

        readonly string bledeCmdPhrase = "/blade_cmd";
        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;

            AddSubscriptionHandler<JointCmdMsg>($"/{machineName}{bledeCmdPhrase}", msg => BladeCmd = msg);

        }
    }
}

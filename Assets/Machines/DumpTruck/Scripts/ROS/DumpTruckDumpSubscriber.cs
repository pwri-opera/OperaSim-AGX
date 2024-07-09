using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Com3;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ダンプトラックの上部構造体の制御信号を受信するクラス
    /// </summary>
    public class DumpTruckDumpSubscriber : MessageSubscriptionBase
    {
        JointCmdMsg dumpCmd = new(2);
        public JointCmdMsg DumpCmd {
            get => dumpCmd;
            private set => dumpCmd = value;
        }

        readonly string rotDumpCmdPhrase = "/rot_dump_cmd";

        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;
            
            AddSubscriptionHandler<JointCmdMsg>($"/{machineName}{rotDumpCmdPhrase}", msg => DumpCmd = msg);
        }
    }
}

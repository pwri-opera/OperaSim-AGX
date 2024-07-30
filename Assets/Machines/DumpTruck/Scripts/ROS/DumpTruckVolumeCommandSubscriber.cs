using System;
using RosMessageTypes.Com3;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ダンプの走行ボリュームの動作指令を受信するクラス
    /// </summary>
    public class DumpTruckVolumeCommandSubscriber : MessageSubscriptionBase
    {
        JointCmdMsg jointCmdMsg = new(2);

        // 仕様では-1.0~1.0なので、その範囲を超えた場合切り捨てる
        public double forwardVolume
        {
            get => Math.Min(Math.Max(jointCmdMsg.effort[0], -1), 1);
        }

        // 仕様では-1.0~1.0なので、その範囲を超えた場合切り捨てる
        public double turnVolume
        {
            get => Math.Min(Math.Max(jointCmdMsg.effort[1], -1), 1);
        }

        readonly string volumeCmdPhrase = "/track_volume_cmd";

        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;
            AddSubscriptionHandler<JointCmdMsg>($"{machineName}{volumeCmdPhrase}", 
                                                msg => jointCmdMsg = msg);
        }
    }
}

using Unity;
using RosMessageTypes.Com3;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧ショベルの上部構造体の制御信号を受信するクラス
    /// </summary>
    public class ExcavatorFrontSubscriber : MessageSubscriptionBase
    {
        JointCmdMsg frontCmd = new(4);
        public JointCmdMsg FrontCmd  
        {
            get => frontCmd;
            private set => frontCmd = value;
        }

        readonly string frontCmdPhrase = "/front_cmd";

        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;
            AddSubscriptionHandler<JointCmdMsg>($"/{machineName}{frontCmdPhrase}", msg => FrontCmd = msg);
        }
    }
}

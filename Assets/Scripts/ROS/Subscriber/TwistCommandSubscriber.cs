using RosMessageTypes.Geometry;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 車体中心の並進移動速度/回転角速度指令を受信するクラス
    /// </summary>
    public class TwistCommandSubscriber : MessageSubscriptionBase
    {
        TwistMsg twistMsg = new();

        public Vector3Msg Linear
        {
            get => twistMsg.linear;
        }
        public Vector3Msg Angular
        {
            get => twistMsg.angular;
        }

        readonly string twistCmdPhrase = "/cmd_vel";

        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;
            AddSubscriptionHandler<TwistMsg>($"{machineName}{twistCmdPhrase}",
                                             msg => twistMsg = msg);
        }
    }
}

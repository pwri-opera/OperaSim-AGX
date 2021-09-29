using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using HeaderMsg = RosSharp.RosBridgeClient.MessageTypes.Std.Header;
using TimeMsg = RosSharp.RosBridgeClient.MessageTypes.Std.Time;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;
using TwistMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Twist;
using Vector3Msg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Vector3;
using WrenchMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Wrench;
using WrenchStampedMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.WrenchStamped;

namespace PWRISimulator.ROS
{
    public static class MessageUtil
    {
        public const string DefaultFrameId = "map";
        public const string WorldFrameId = "world";

        public static void ConvertTwistToAngularWheelVelocity(TwistMsg msg, double separation, double radius,
            out double angularVelocityLeftWheel, out double angularVelocityRightWheel)
        {
            double angularFromTwistLinear = radius != 0.0 ? msg.linear.x / radius : 0.0;
            double angularFromTwistAngular = radius != 0.0 ? msg.angular.z * separation * 0.5 / radius : 0.0;
            angularVelocityLeftWheel = angularFromTwistLinear - angularFromTwistAngular;
            angularVelocityRightWheel = angularFromTwistLinear + angularFromTwistAngular;
        }

        public static string MessageToString<T>(T msg) where T : Message
        {
            if (msg == null)
                return "null";
            else if (msg is Float64Msg)
                return MessageToString((Float64Msg)(object)msg);
            else
                return msg != null ? msg.ToString() : "null";
        }

        public static string MessageToString(Float64Msg msg)
        {
            return msg.data.ToString();
        }

        public static Float64Msg Interpolate(Float64Msg msgA, Float64Msg msgB, double t)
        {
            return new Float64Msg(MathUtil.Lerp(msgA.data, msgB.data, t));
        }

        public static Vector3Msg ToVector3Msg(Vector3 unityVector)
        {
            Vector3 rosVector = unityVector.Unity2Ros();
            return new Vector3Msg(rosVector.x, rosVector.y, rosVector.z);
        }

        public static HeaderMsg ToHeaderMessage(double time, string frameId)
        {
            uint secs = (uint)time;
            uint nsecs = (uint)((time - secs) * 1e+9);
            // RosBridge(?)は自動的にseqを生成するようなので、0に設定してtime、frameIdだけに更新
            return new HeaderMsg(0, new TimeMsg(secs, nsecs), frameId);
        }

        public static void UpdateTimeMsg(TimeMsg msg, double time)
        {
            msg.secs = (uint)time;
            msg.nsecs = (uint)((time - msg.secs) * 1e+9);
        }

        public static WrenchMsg ToWrenchMsg(Vector3 forceUnity, Vector3 torqueUnity)
        {
            Vector3 forceRos = forceUnity.Unity2Ros();
            Vector3 torqueRos = torqueUnity.Unity2Ros();
            return new WrenchMsg(ToVector3Msg(forceRos), ToVector3Msg(torqueRos));
        }

        public static WrenchStampedMsg ToWrenchStampedMsg(Vector3 forceUnity, Vector3 torqueUnity, double time, string frameId)
        {
            HeaderMsg header = ToHeaderMessage(time, frameId);
            WrenchMsg wrench = ToWrenchMsg(forceUnity, torqueUnity);
            return new WrenchStampedMsg(header, wrench);
        }
    }
}

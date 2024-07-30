using System.Data.SqlTypes;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using agxROS2;

namespace PWRISimulator.ROS
{
    public static class MessageUtil
    {
        /// <summary>
        /// メッセージを文字列に変換します
        /// Float64Msgは変換できますが、他のメッセージは変換できる保証がありません
        /// </summary>
        /// <typeparam name="T">メッセージの型</typeparam>
        /// <param name="msg">変換したいメッセージ</param>
        /// <returns>変換した文字列</returns>
        public static string MessageToString<T>(T msg) where T : Message
        {
            if (msg == null)
                return "null";
            else if (msg is Float64Msg)
                return MessageToString((Float64Msg)(object)msg);
            else
                return msg != null ? msg.ToString() : "null";
        }

        /// <summary>
        /// Float64Msgの内容を文字列に変換します
        /// </summary>
        /// <param name="msg">変換したいメッセージ</param>
        /// <returns>変換した文字列</returns>
        public static string MessageToString(Float64Msg msg)
        {
            return msg.data.ToString();
        }

        /// <summary>
        /// 2つのFloat64msg型メッセージの線形補間を行います
        /// </summary>
        /// <param name="msgA">1つ目のメッセージです</param>
        /// <param name="msgB">2つ目のメッセージです</param>
        /// <param name="t">補間係数. 0.0~1.0で指定する. 0.0の場合msgAを返す. 1.0の場合msgBを返す</param>
        /// <returns>補間した値</returns>
        public static Float64Msg Interpolate(Float64Msg msgA, Float64Msg msgB, double t)
        {
            return new Float64Msg(MathUtil.Lerp(msgA.data, msgB.data, t));
        }

        /// <summary>
        /// 指定した時刻のパラメータを持つHeaderMsgを生成します.
        /// ROS1とROS2に対応しています
        /// </summary>
        /// <param name="time">HeaderMsgに設定したい時刻</param>
        /// <param name="frameId">ヘッダー名</param>
        /// <returns>生成したHeaderMsg</returns>
        public static HeaderMsg ToHeadermessage(double time, string frameId)
        {
        #if !ROS2
            uint secs = (uint)time;
            uint nsecs = (uint)((time - secs) * 1e+9);

            return new HeaderMsg(0, new TimeMsg(secs, nsecs), frameId);
        #else
            int secs = (int)time;
            uint nsecs = (uint)((time - secs) * 1e+9);

            return new HeaderMsg(new TimeMsg(secs, nsecs), frameId);
        #endif
        }

        /// <summary>
        /// 引数で入力したmsgのパラメータをtimeを使って更新します.
        /// ROS1とROS2に対応しています
        /// </summary>
        /// <param name="msg">更新したいTimeMsg型メッセージです</param>
        /// <param name="time">メッセージに設定したい時刻です</param>
        public static void UpdateTimeMsg(TimeMsg msg, double time)
        {
        #if !ROS2
            msg.secs = (uint)time;
            msg.nsecs = (uint)((time - (uint)time) * 1e+9);
        #else
            msg.sec = (int)time;
            msg.nanosec = (uint)((time - (uint)time) * 1e+9);
        #endif
        }
    }
}

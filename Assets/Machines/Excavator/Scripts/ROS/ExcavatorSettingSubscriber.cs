using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧ショベルの制御値を受信するクラス
    /// 上部旋回体、下部走行体を直接制御しないMsgを処理する
    /// </summary>
    public class ExcavatorSettingSubscriber : MessageSubscriptionBase
    {
        public bool EmergencyStopCmd {get; private set;}

        readonly string EmergencyStopCmdPhrase = "/emg_stop_cmd";
        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;

            AddSubscriptionHandler<BoolMsg>($"/{machineName}{EmergencyStopCmdPhrase}", msg => EmergencyStopCmd = msg.data);
        }
    }
}

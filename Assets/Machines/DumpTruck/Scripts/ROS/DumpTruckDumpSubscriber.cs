using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Com3;

namespace PWRISimulator.ROS
{
    public class DumpTruckDumpSubscriber : MessageSubscriptionBase
    {
        private JointCmdMsg dumpCmd = new JointCmdMsg(2);
        private float nextLogTime;

        [SerializeField] private bool logReceivedCommands;

        public JointCmdMsg DumpCmd
        {
            get => dumpCmd;
            private set => dumpCmd = value;
        }

        private const string RotDumpCmdPhrase = "/rot_dump_cmd";

        protected override void CreateSubscriptions()
        {
            string machineName = gameObject.name;
            string topicName = $"/{machineName}{RotDumpCmdPhrase}";

            Debug.Log($"[DumpTruckDumpSubscriber] 購読: {topicName}", this);

            AddSubscriptionHandler<JointCmdMsg>(
                topicName,
                msg =>
                {
                    DumpCmd = msg;

                    if (logReceivedCommands && Time.unscaledTime >= nextLogTime)
                    {
                        nextLogTime = Time.unscaledTime + 1.0f;
                        Debug.Log(
                            $"[DumpTruckDumpSubscriber] 受信内容: {JsonUtility.ToJson(msg)}",
                            this
                        );
                    }
                }
            );
        }
    }
}

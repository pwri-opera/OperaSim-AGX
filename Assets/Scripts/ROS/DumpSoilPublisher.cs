using System;
using UnityEngine;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;

namespace PWRISimulator.ROS
{
    public class DumpSoilPublisher : MultipleMessagesPublisher
    {
        public DumpSoil dumpSoilSource;
        public string massTopic;
        public string volumeTopic;

        protected override void Reset()
        {
            base.Reset();
            // dumpSoilSourceのデフォルト値にはシーンにあるDumpSoilを探して設定する
            dumpSoilSource = FindObjectOfType<DumpSoil>(false);
        }

        protected override void OnAdvertise()
        {
            if (dumpSoilSource != null)
            {
                AddPublicationHandler(massTopic, () => new Float64Msg(dumpSoilSource.soilMass));
                AddPublicationHandler(volumeTopic, () => new Float64Msg(dumpSoilSource.soilVolume));
            }
        }
    }
}

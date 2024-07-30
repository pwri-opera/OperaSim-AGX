using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    public class DumpTruckSoilVolumePublisher : SoilVolumePublisher
    {
        [SerializeField] uint frequency = 60;
        DumpSoil data;

        protected override void DoStart()
        {
            data = gameObject.GetComponentInChildren<DumpSoil>();
            if (data == null)
            {
                Debug.LogError("DumpSoil is not found");
            }
        }

        protected override void DoUpdate()
        {
            soilVolumeMsg.data = data.soilVolume;
        }

        protected override uint Frequency()
        {
            return frequency;
        }

        protected override string MachineName()
        {
            return gameObject.name;
        }

        protected override string TopicPhrase()
        {
            return "/soil_volume";
        }
    }
}

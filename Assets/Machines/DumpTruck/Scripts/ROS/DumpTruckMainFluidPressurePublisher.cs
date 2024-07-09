using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// クローラダンプのメイン圧力
    /// 現時点では出力項目はない
    /// </summary>
    public class DumpTruckMainFluidPressurePublisher : FluidPressureArrayPublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] DumpTruckFluid dumpTruckFluid;

        readonly string[] item_name = {};
        protected override void DoUpdate()
        {
            // no output :(
        }

        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            return "/main_fluid_pressure";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
        protected override uint NumberOfItems()
        {
            return 0;
        }
    }
}

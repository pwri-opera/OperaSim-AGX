using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ブルドーザのメイン油圧
    /// items[6]
    /// </summary>
    public class BulldozerMainFluidPressurePublisher : FluidPressureArrayPublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] BulldozerFluid bulldozerFluid;
        readonly string[] item_name = {"lift_up_main_pressure", "lift_down_main_pressure", "tilt_forward_main_pressure",
                                       "tilt_back_main_pressure", "angle_right_main_pressure", "angle_left_main_pressure"};
        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;
            fluidPressureArrayMsg.array[0].fluid_pressure = bulldozerFluid.LiftUp.MainFluidPressure;
            fluidPressureArrayMsg.array[0].header = MessageUtil.ToHeadermessage(time, item_name[0]);
            fluidPressureArrayMsg.array[1].fluid_pressure = bulldozerFluid.LiftDown.MainFluidPressure;
            fluidPressureArrayMsg.array[1].header = MessageUtil.ToHeadermessage(time, item_name[1]);
            fluidPressureArrayMsg.array[2].fluid_pressure = bulldozerFluid.TiltForward.MainFluidPressure;
            fluidPressureArrayMsg.array[2].header = MessageUtil.ToHeadermessage(time, item_name[2]);
            fluidPressureArrayMsg.array[3].fluid_pressure = bulldozerFluid.TiltBackward.MainFluidPressure;
            fluidPressureArrayMsg.array[3].header = MessageUtil.ToHeadermessage(time, item_name[3]);
            fluidPressureArrayMsg.array[4].fluid_pressure = bulldozerFluid.AngleRight.MainFluidPressure;
            fluidPressureArrayMsg.array[4].header = MessageUtil.ToHeadermessage(time, item_name[4]);
            fluidPressureArrayMsg.array[5].fluid_pressure = bulldozerFluid.AngleLeft.MainFluidPressure;
            fluidPressureArrayMsg.array[5].header = MessageUtil.ToHeadermessage(time, item_name[5]);
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
            return 6;
        }
    }
}

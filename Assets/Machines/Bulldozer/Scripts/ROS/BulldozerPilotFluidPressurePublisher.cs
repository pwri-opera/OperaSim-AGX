using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ブルドーザのパイロット油圧
    /// items[6]
    /// </summary>

    public class BulldozerPilotFluidPressurePublisher : FluidPressureArrayPublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] BulldozerFluid bulldozerFluid;
        readonly string[] item_name = {"lift_up_pilot_pressure", "lift_down_pilot_pressure", "tilt_forward_pilot_pressure",
                                       "tilt_back_pilot_pressure", "angle_right_pilot_pressure", "angle_left_pilot_pressure"};
        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;
            fluidPressureArrayMsg.array[0].fluid_pressure = bulldozerFluid.LiftUp.PilotFluidPressure;
            fluidPressureArrayMsg.array[0].header = MessageUtil.ToHeadermessage(time, item_name[0]);
            fluidPressureArrayMsg.array[1].fluid_pressure = bulldozerFluid.LiftDown.PilotFluidPressure;
            fluidPressureArrayMsg.array[1].header = MessageUtil.ToHeadermessage(time, item_name[1]);
            fluidPressureArrayMsg.array[2].fluid_pressure = bulldozerFluid.TiltForward.PilotFluidPressure;
            fluidPressureArrayMsg.array[2].header = MessageUtil.ToHeadermessage(time, item_name[2]);
            fluidPressureArrayMsg.array[3].fluid_pressure = bulldozerFluid.TiltBackward.PilotFluidPressure;
            fluidPressureArrayMsg.array[3].header = MessageUtil.ToHeadermessage(time, item_name[3]);
            fluidPressureArrayMsg.array[4].fluid_pressure = bulldozerFluid.AngleRight.PilotFluidPressure;
            fluidPressureArrayMsg.array[4].header = MessageUtil.ToHeadermessage(time, item_name[4]);
            fluidPressureArrayMsg.array[5].fluid_pressure = bulldozerFluid.AngleLeft.PilotFluidPressure;
            fluidPressureArrayMsg.array[5].header = MessageUtil.ToHeadermessage(time, item_name[5]);
        }
        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            return "/pilot_fluid_pressure";
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

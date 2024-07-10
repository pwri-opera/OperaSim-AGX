using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧ショベルのパイロット油圧
    /// items[16]
    /// </summary>

    public class ExcavatorPilotFluidPressurePublisher : FluidPressureArrayPublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] ExcavatorJoints excavatorFluid;
        readonly string[] item_name = {"boom_up_pilot_pressure", "boom_down_pilot_pressure", "arm_crowed_pilot_pressure", "arm_dump_pilot_pressure",
                                       "bucket_crowed_pilot_pressure", "bucket_dump_pilot_pressure", "swing_right_pilot_pressure", "swing_left_pilot_pressure",
                                       "right_track_forward_pilot_prs", "right_track_backward_pilot_prs", "left_track_forward_pilot_prs", "left_track_backward_pilot_prs",
                                       "attachment_a_pilot_pressure", "attachment_b_pilot_pressure", "assist_a_pilot_pressure", "assist_b_pilot_pressure"};
        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;

            fluidPressureArrayMsg.array[0].fluid_pressure = excavatorFluid.boomTilt.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[0].header = MessageUtil.ToHeadermessage(time, item_name[0]);
            fluidPressureArrayMsg.array[1].fluid_pressure = excavatorFluid.boomTilt.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[1].header = MessageUtil.ToHeadermessage(time, item_name[1]);
            fluidPressureArrayMsg.array[2].fluid_pressure = excavatorFluid.armTilt.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[2].header = MessageUtil.ToHeadermessage(time, item_name[2]);
            fluidPressureArrayMsg.array[3].fluid_pressure = excavatorFluid.armTilt.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[3].header = MessageUtil.ToHeadermessage(time, item_name[3]);
            fluidPressureArrayMsg.array[4].fluid_pressure = excavatorFluid.bucketTilt.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[4].header = MessageUtil.ToHeadermessage(time, item_name[4]);
            fluidPressureArrayMsg.array[5].fluid_pressure = excavatorFluid.bucketTilt.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[5].header = MessageUtil.ToHeadermessage(time, item_name[5]);
            fluidPressureArrayMsg.array[6].fluid_pressure = excavatorFluid.swing.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[6].header = MessageUtil.ToHeadermessage(time, item_name[6]);
            fluidPressureArrayMsg.array[7].fluid_pressure = excavatorFluid.armTilt.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[7].header = MessageUtil.ToHeadermessage(time, item_name[7]);
            fluidPressureArrayMsg.array[8].fluid_pressure = excavatorFluid.rightSprocket.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[8].header = MessageUtil.ToHeadermessage(time, item_name[8]);
            fluidPressureArrayMsg.array[9].fluid_pressure = excavatorFluid.armTilt.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[9].header = MessageUtil.ToHeadermessage(time, item_name[9]);
            fluidPressureArrayMsg.array[10].fluid_pressure = excavatorFluid.leftSprocket.hydraulicActuator.GetUpperPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[10].header = MessageUtil.ToHeadermessage(time, item_name[10]);
            fluidPressureArrayMsg.array[11].fluid_pressure = excavatorFluid.leftSprocket.hydraulicActuator.GetLowerPressure().PilotFluidPressure;
            fluidPressureArrayMsg.array[11].header = MessageUtil.ToHeadermessage(time, item_name[11]);
            // below is no output
            fluidPressureArrayMsg.array[12].fluid_pressure = 0.0f;
            fluidPressureArrayMsg.array[12].header = MessageUtil.ToHeadermessage(time, item_name[12]);
            fluidPressureArrayMsg.array[13].fluid_pressure = 0.0f;
            fluidPressureArrayMsg.array[13].header = MessageUtil.ToHeadermessage(time, item_name[13]);
            fluidPressureArrayMsg.array[14].fluid_pressure = 0.0f;
            fluidPressureArrayMsg.array[14].header = MessageUtil.ToHeadermessage(time, item_name[14]);
            fluidPressureArrayMsg.array[15].fluid_pressure = 0.0f;
            fluidPressureArrayMsg.array[15].header = MessageUtil.ToHeadermessage(time, item_name[15]);
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
            return 16;
        }
    }
}

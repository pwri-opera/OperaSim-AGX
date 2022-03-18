using System;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;
using JointStateMsg = RosSharp.RosBridgeClient.MessageTypes.Sensor.JointState;

namespace PWRISimulator.ROS
{
    public class DumpTruckPublisher : MultipleMessagesPublisher
    {
        [Header("Target Machine")]
        public DumpTruck dumpTruck;

        [Header("Left Sprocket Topics")]

        [InspectorLabel("Position")]
        public string leftSprocketPosTopic = "/ic120/sprocket_left/actual_pos";
        [InspectorLabel("Speed")]
        public string leftSprocketSpeedTopic = "/ic120/sprocket_left/actual_speed";
        [InspectorLabel("Force")]
        public string leftSprocketForceTopic = "/ic120/sprocket_left/actual_force";

        [Header("Right Sprocket Topics")]

        [InspectorLabel("Position")]
        public string rightSprocketPosTopic = "/ic120/sprocket_right/actual_pos";
        [InspectorLabel("Speed")]
        public string rightSprocketSpeedTopic = "/ic120/sprocket_right/actual_speed";
        [InspectorLabel("Force")]
        public string rightSprocketForceTopic = "/ic120/sprocket_right/actual_force";

        // [Header("Container Topics")]
        [Header("vessel Topics")]

        // [InspectorLabel("Position")]
        // public string containerPosTopic = "/ic120/vessel/actual_pos";
        // [InspectorLabel("Speed")]
        // public string containerSpeedTopic = "/ic120/vessel/actual_speed";        
        // [InspectorLabel("Force")]
        // public string containerForceTopic = "/ic120/vessel/actual_force";
        [InspectorLabel("Position")]
        public string vesselPosTopic = "/ic120/vessel/actual_pos";
        [InspectorLabel("Speed")]
        public string vesselSpeedTopic = "/ic120/vessel/actual_speed";        
        [InspectorLabel("Force")]
        public string vesselForceTopic = "/ic120/vessel/actual_force";

        [Header("Joint State Topic")]
        [InspectorLabel("Joint State")]
        // public string containerJsTopic = "/ic120/joint_states";
        public string vesselJsTopic = "/ic120/joint_states";
        public string FrameId = "";
        // public bool includeTimeInMessage = false;

        protected override void OnAdvertise()
        {
            if (rosConnector?.RosSocket == null)
            {
                Debug.LogWarning($"{name} Cannot create publications because RosConnector or RosSocket is null.");
                return;
            }

            if (dumpTruck?.GetInitialized<DumpTruck>() == null)
            {
                Debug.LogWarning($"{name} Cannot create publications because dumpTruck property is null.");
                return;
            }

            if (dumpTruck.leftSprocket != null)
            {
                AddPublicationHandler<Float64Msg>(leftSprocketPosTopic, () => new Float64Msg(dumpTruck.leftSprocket.currentPosition));
                AddPublicationHandler<Float64Msg>(leftSprocketSpeedTopic, () => new Float64Msg(dumpTruck.leftSprocket.currentSpeed));
                AddPublicationHandler<Float64Msg>(leftSprocketForceTopic, () => new Float64Msg(dumpTruck.leftSprocket.currentForce));

                // AddPublicationHandler<JointStateMsg>(containerJsTopic, () => new JointStateMsg(
                AddPublicationHandler<JointStateMsg>(vesselJsTopic, () => new JointStateMsg(
                    MessageUtil.ToHeaderMessage(Time.fixedTimeAsDouble,FrameId),
                    new string[1]{"sprocket_left_joint"},
                    // new string[1]{"sprocket_L_joint"},
                    new double[1]{dumpTruck.leftSprocket.currentPosition},
                    new double[1]{dumpTruck.leftSprocket.currentSpeed},
                    new double[1]{dumpTruck.leftSprocket.currentForce}
                ));

            }

            if (dumpTruck.rightSprocket != null)
            {
                AddPublicationHandler<Float64Msg>(rightSprocketPosTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentPosition));
                AddPublicationHandler<Float64Msg>(rightSprocketSpeedTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentSpeed));
                AddPublicationHandler<Float64Msg>(rightSprocketForceTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentForce));

                // AddPublicationHandler<JointStateMsg>(containerJsTopic, () => new JointStateMsg(
                AddPublicationHandler<JointStateMsg>(vesselJsTopic, () => new JointStateMsg(
                    MessageUtil.ToHeaderMessage(Time.fixedTimeAsDouble,FrameId),
                    new string[1]{"sprocket_right_joint"},
                    // new string[1]{"sprocket_R_joint"},
                    new double[1]{dumpTruck.rightSprocket.currentPosition},
                    new double[1]{dumpTruck.rightSprocket.currentSpeed},
                    new double[1]{dumpTruck.rightSprocket.currentForce}
                ));
            }

            // if (dumpTruck.containerTilt != null)
             if (dumpTruck.vesselTilt != null)
            {
                // AddPublicationHandler<Float64Msg>(containerPosTopic, () => new Float64Msg(dumpTruck.containerTilt.currentPosition*-1));
                // AddPublicationHandler<Float64Msg>(containerSpeedTopic, () => new Float64Msg(dumpTruck.containerTilt.currentSpeed*-1));
                // AddPublicationHandler<Float64Msg>(containerForceTopic, () => new Float64Msg(dumpTruck.containerTilt.currentForce*-1));
                AddPublicationHandler<Float64Msg>(vesselPosTopic, () => new Float64Msg(dumpTruck.vesselTilt.currentPosition*-1));
                AddPublicationHandler<Float64Msg>(vesselSpeedTopic, () => new Float64Msg(dumpTruck.vesselTilt.currentSpeed*-1));
                AddPublicationHandler<Float64Msg>(vesselForceTopic, () => new Float64Msg(dumpTruck.vesselTilt.currentForce*-1));

                // double[] js_position = new double[1]{dumpTruck.containerTilt.currentPosition};
                // double[] js_velocity = new double[1]{0};
                // double[] js_effort = new double[1]{0};
                // Debug.Log("Time:"+10);

                // AddPublicationHandler<JointStateMsg>(containerJsTopic, () => new JointStateMsg(
                AddPublicationHandler<JointStateMsg>(vesselJsTopic, () => new JointStateMsg(
                    MessageUtil.ToHeaderMessage(Time.fixedTimeAsDouble,FrameId),
                    // new string[1]{"container_joint"},
                    // new double[1]{dumpTruck.containerTilt.currentPosition*-1},
                    new string[1]{"vessel_pin_joint"},
                    new double[1]{dumpTruck.vesselTilt.currentPosition*-1},
                    new double[1]{0},
                    new double[1]{0}
                    ));
            }
        }
    }
}

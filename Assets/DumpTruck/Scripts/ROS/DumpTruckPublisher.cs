using System;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;

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

        [Header("Container Topics")]

        [InspectorLabel("Position")]
        public string containerPosTopic = "/ic120/vessel/actual_pos";
        [InspectorLabel("Speed")]
        public string containerSpeedTopic = "/ic120/vessel/actual_speed";        
        [InspectorLabel("Force")]
        public string containerForceTopic = "/ic120/vessel/actual_force";

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
            }

            if (dumpTruck.rightSprocket != null)
            {
                AddPublicationHandler<Float64Msg>(rightSprocketPosTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentPosition));
                AddPublicationHandler<Float64Msg>(rightSprocketSpeedTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentSpeed));
                AddPublicationHandler<Float64Msg>(rightSprocketForceTopic, () => new Float64Msg(dumpTruck.rightSprocket.currentForce));
            }

            if (dumpTruck.containerTilt != null)
            {
                AddPublicationHandler<Float64Msg>(containerPosTopic, () => new Float64Msg(dumpTruck.containerTilt.currentPosition));
                AddPublicationHandler<Float64Msg>(containerSpeedTopic, () => new Float64Msg(dumpTruck.containerTilt.currentSpeed));
                AddPublicationHandler<Float64Msg>(containerForceTopic, () => new Float64Msg(dumpTruck.containerTilt.currentForce));
            }
        }
    }
}

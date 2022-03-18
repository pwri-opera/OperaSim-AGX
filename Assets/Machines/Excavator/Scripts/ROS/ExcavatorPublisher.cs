using System;
using System.Collections.Generic;
using UnityEngine;
using RosSharp;
using RosSharp.RosBridgeClient;
using Float64Msg = RosSharp.RosBridgeClient.MessageTypes.Std.Float64;
using WrenchStampedMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.WrenchStamped;
using Vector3Msg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Vector3;
using JointStateMsg = RosSharp.RosBridgeClient.MessageTypes.Sensor.JointState;

namespace PWRISimulator.ROS
{
    public class ExcavatorPublisher : MultipleMessagesPublisher
    {
        [Header("Target Machine")]
        public Excavator excavator;

        [Header("Left Sprocket Topics")]

        [InspectorLabel("Position")]
        public string leftSprocketPosTopic = "/zx120/sprocket_left/actual_pos";
        [InspectorLabel("Speed")]
        public string leftSprocketSpeedTopic = "/zx120/sprocket_left/actual_speed";
        [InspectorLabel("Force")]
        public string leftSprocketForceTopic = "/zx120/sprocket_left/actual_force";

        [Header("Right Sprocket Topics")]

        [InspectorLabel("Position")]
        public string rightSprocketPosTopic = "/zx120/sprocket_right/actual_pos";
        [InspectorLabel("Speed")]
        public string rightSprocketSpeedTopic = "/zx120/sprocket_right/actual_speed";
        [InspectorLabel("Force")]
        public string rightSprocketForceTopic = "/zx120/sprocket_right/actual_force";

        [Header("Swing Topisc")]

        // [InspectorLabel("Position")]
        // public string swingPosTopic = "/zx120/rotator/actual_pos";
        // [InspectorLabel("Speed")]
        // public string swingSpeedTopic = "/zx120/rotator/actual_speed";        
        // [InspectorLabel("Force")]
        // public string swingForceTopic = "/zx120/rotator/actual_force";
        [InspectorLabel("Position")]
        public string swingPosTopic = "/zx120/swing/actual_pos";
        [InspectorLabel("Speed")]
        public string swingSpeedTopic = "/zx120/swing/actual_speed";        
        [InspectorLabel("Force")]
        public string swingForceTopic = "/zx120/swing/actual_force";

        [Header("Boom Topics")]

        [InspectorLabel("Position")]
        public string boomPosTopic = "/zx120/boom/actual_pos";
        [InspectorLabel("Speed")]
        public string boomSpeedTopic = "/zx120/boom/actual_speed";
        [InspectorLabel("Force")]
        public string boomForceTopic = "/zx120/boom/actual_force";

        [Header("Arm Topics")]

        [InspectorLabel("Position")]
        public string armPosTopic = "/zx120/arm/actual_pos";
        [InspectorLabel("Speed")]
        public string armSpeedTopic = "/zx120/arm/actual_speed";
        [InspectorLabel("Force")]
        public string armForceTopic = "/zx120/arm/actual_force";

        [Header("Bucket Topics")]

        // [InspectorLabel("Position")]
        // public string bucketPosTopic = "/zx120/backet/actual_pos";
        // [InspectorLabel("Speed")]
        // public string bucketSpeedTopic = "/zx120/backet/actual_speed";
        // [InspectorLabel("Force")]
        // public string bucketForceTopic = "/zx120/backet/actual_force";
        [InspectorLabel("Position")]
        public string bucketPosTopic = "/zx120/bucket/actual_pos";
        [InspectorLabel("Speed")]
        public string bucketSpeedTopic = "/zx120/bucket/actual_speed";
        [InspectorLabel("Force")]
        public string bucketForceTopic = "/zx120/bucket/actual_force";
        
        [Header("Excavation Topics")]

        [InspectorLabel("Shovel Inner Volume")]
        public string shovelInnerVolumeTopic = "/zx120/excavation/inner_volume";
        [InspectorLabel("Shovel Soil Volume")]
        public string shovelSoilVolumeTopic = "/zx120/excavation/soil_volume";
        [InspectorLabel("Shovel Deadload Fraction")]
        public string shovelDeadLoadFractionTopic = "/zx120/excavation/deadload_fraction";
        [InspectorLabel("Dynamic Mass")]
        public string shovelDynamicMassTopic = "/zx120/excavation/dynamic_mass";

        [InspectorLabel("Penetration Force")]
        public string penetrationForceTopic = "/zx120/excavation/penetration_force";
        [InspectorLabel("Separation Force")]
        public string separationForceTopic = "/zx120/excavation/separation_force";
        [InspectorLabel("Deformation Force")]
        public string deformationForceTopic = "/zx120/excavation/deformation_force";
        [InspectorLabel("Contact Force")]
        public string shovelContactForceTopic = "/zx120/excavation/contact_force";

        [Header("Joint State Topic")]
        [InspectorLabel("Joint State")]
        public string containerJsTopic = "/zx120/joint_states";
        public string FrameId = "";


        protected override void OnAdvertise()
        {
            if (rosConnector?.RosSocket == null)
            {
                Debug.LogWarning($"{name} Cannot create publications because RosConnector or RosSocket is null.");
                return;
            }

            if (excavator?.GetInitialized<Excavator>() == null)
            {
                Debug.LogWarning($"{name} Cannot create publications because excavator is null.");
                return;
            }

            if (excavator.leftSprocket != null)
            {
                AddPublicationHandler<Float64Msg>(leftSprocketPosTopic, () => new Float64Msg(excavator.leftSprocket.currentPosition));
                AddPublicationHandler<Float64Msg>(leftSprocketSpeedTopic, () => new Float64Msg(excavator.leftSprocket.currentSpeed));
                AddPublicationHandler<Float64Msg>(leftSprocketForceTopic, () => new Float64Msg(excavator.leftSprocket.currentForce));
            }

            if (excavator.rightSprocket != null)
            {
                AddPublicationHandler<Float64Msg>(rightSprocketPosTopic, () => new Float64Msg(excavator.rightSprocket.currentPosition));
                AddPublicationHandler<Float64Msg>(rightSprocketSpeedTopic, () => new Float64Msg(excavator.rightSprocket.currentSpeed));
                AddPublicationHandler<Float64Msg>(rightSprocketForceTopic, () => new Float64Msg(excavator.rightSprocket.currentForce));
            }

            if (excavator.swing != null)
            {
                AddPublicationHandler<Float64Msg>(swingPosTopic, () => new Float64Msg(excavator.swing.currentPosition));
                AddPublicationHandler<Float64Msg>(swingSpeedTopic, () => new Float64Msg(excavator.swing.currentSpeed));
                AddPublicationHandler<Float64Msg>(swingForceTopic, () => new Float64Msg(excavator.swing.currentForce));
            }

            if (excavator.boomTilt != null)
            {
                AddPublicationHandler<Float64Msg>(boomPosTopic, () => new Float64Msg(excavator.boomTilt.currentPosition));
                AddPublicationHandler<Float64Msg>(boomSpeedTopic, () => new Float64Msg(excavator.boomTilt.currentSpeed));
                AddPublicationHandler<Float64Msg>(boomForceTopic, () => new Float64Msg(excavator.boomTilt.currentForce));
            }

            if (excavator.armTilt != null)
            {
                AddPublicationHandler<Float64Msg>(armPosTopic, () => new Float64Msg(excavator.armTilt.currentPosition));
                AddPublicationHandler<Float64Msg>(armSpeedTopic, () => new Float64Msg(excavator.armTilt.currentSpeed));
                AddPublicationHandler<Float64Msg>(armForceTopic, () => new Float64Msg(excavator.armTilt.currentForce));
            }

            if (excavator.bucketTilt != null)
            {
                AddPublicationHandler<Float64Msg>(bucketPosTopic, () => new Float64Msg(excavator.bucketTilt.currentPosition));
                AddPublicationHandler<Float64Msg>(bucketSpeedTopic, () => new Float64Msg(excavator.bucketTilt.currentSpeed));
                AddPublicationHandler<Float64Msg>(bucketForceTopic, () => new Float64Msg(excavator.bucketTilt.currentForce));
            }

            ExcavationData excavationData = excavator.excavationData;
            if (excavationData != null)
            {
                AddPublicationHandler<Float64Msg>(shovelInnerVolumeTopic, () => new Float64Msg(excavationData.shovelInnerVolume));
                AddPublicationHandler<Float64Msg>(shovelDeadLoadFractionTopic, () => new Float64Msg(excavationData.shovelDeadloadFraction));
                AddPublicationHandler<Float64Msg>(shovelSoilVolumeTopic, () => new Float64Msg(excavationData.shovelSoilVolume));
                AddPublicationHandler<Float64Msg>(shovelDynamicMassTopic, () => new Float64Msg(excavationData.shovelDynamicMass));

                AddPublicationHandler<WrenchStampedMsg>(penetrationForceTopic, () => MessageUtil.ToWrenchStampedMsg(
                    excavationData.penetrationForce, excavationData.penetrationTorque, Time.timeAsDouble, MessageUtil.DefaultFrameId));

                AddPublicationHandler<WrenchStampedMsg>(separationForceTopic, () => MessageUtil.ToWrenchStampedMsg(
                    excavationData.separationForce, Vector3.zero, Time.timeAsDouble, MessageUtil.DefaultFrameId));

                AddPublicationHandler<WrenchStampedMsg>(deformationForceTopic, () => MessageUtil.ToWrenchStampedMsg(
                    excavationData.deformationForce, Vector3.zero, Time.timeAsDouble, MessageUtil.DefaultFrameId));

                AddPublicationHandler<WrenchStampedMsg>(shovelContactForceTopic, () => MessageUtil.ToWrenchStampedMsg(
                    excavationData.contactForce, Vector3.zero, Time.timeAsDouble, MessageUtil.DefaultFrameId));
            }
            if (excavator.swing != null && excavator.boomTilt != null && excavator.armTilt != null && excavator.bucketTilt != null){
                AddPublicationHandler<JointStateMsg>(containerJsTopic, () => new JointStateMsg(
                    MessageUtil.ToHeaderMessage(Time.fixedTimeAsDouble,FrameId),
                    new string[4]{"swing_joint","boom_joint","arm_joint","bucket_joint"},
                    new double[4]{excavator.swing.currentPosition,excavator.boomTilt.currentPosition,excavator.armTilt.currentPosition,excavator.bucketTilt.currentPosition},
                    new double[4]{0,0,0,0},
                    new double[4]{0,0,0,0}
                    ));
            }
        }
    }
}

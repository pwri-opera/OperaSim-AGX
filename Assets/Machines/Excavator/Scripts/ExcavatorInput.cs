using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Drawing.Text;

namespace PWRISimulator.ROS
{
    public class ExcavatorInput : MonoBehaviour
    {
        public ExcavatorFrontSubscriber frontSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public ExcavatorSettingSubscriber settingSubscriber;
        [SerializeField] ConstractionMovementControlType movementControlType;
        [SerializeField] ControlType controlType = ControlType.Position;

        private ExcavatorJoints joints;

        // for Joint Command
        public BoomAngleToCylinderLengthConvertor boomCylConv;
        public ArmAngleToCylinderLengthConvertor armCylConv;
        public BucketAngleToCylinderLengthConvertor bucketCylConv;

        // for Twist Command
        public TrackTwistCommandConvertor twistCommandConvertor;

        private 

        void Start()
        {
            joints = gameObject.GetComponent<ExcavatorJoints>();
        }
        // Update is called once per frame
        void FixedUpdate()
        {
            // 受信
            double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;

            frontSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            trackSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            settingSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
        }

        public void SetCommands()
        {
            // 制御値の反映
            if (settingSubscriber.EmergencyStopCmd)
            {
                // 緊急停止
                joints.bucketTilt.actuator.controlType = ControlType.Position;
                joints.bucketTilt.actuator.controlValue = joints.bucketTilt.actuator.CurrentPosition;

                joints.armTilt.actuator.controlType = ControlType.Position;
                joints.armTilt.actuator.controlValue = joints.armTilt.actuator.CurrentPosition;

                joints.boomTilt.actuator.controlType = ControlType.Position;
                joints.boomTilt.actuator.controlValue = joints.boomTilt.actuator.CurrentPosition;

                joints.swing.actuator.controlType = ControlType.Position;
                joints.swing.actuator.controlValue = joints.swing.actuator.CurrentPosition;

                joints.leftSprocket.actuator.controlType = ControlType.Position;
                joints.leftSprocket.actuator.controlValue = joints.leftSprocket.actuator.CurrentPosition;

                joints.rightSprocket.actuator.controlType = ControlType.Position;
                joints.rightSprocket.actuator.controlValue = joints.rightSprocket.actuator.CurrentPosition;
            }
            else
            {
                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:  
                        joints.bucketTilt.actuator.controlType = ControlType.Position;
                        joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescoping((float)frontSubscriber.FrontCmd.position[0]);

                        joints.armTilt.actuator.controlType = ControlType.Position;
                        joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescoping((float)frontSubscriber.FrontCmd.position[1]);

                        joints.boomTilt.actuator.controlType = ControlType.Position;
                        joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescoping((float)frontSubscriber.FrontCmd.position[2]);

                        joints.swing.actuator.controlType = ControlType.Position;
                        joints.swing.actuator.controlValue = frontSubscriber.FrontCmd.position[3];
                        break;
                    case ControlType.Speed:
                        joints.bucketTilt.actuator.controlType = ControlType.Speed;
                        joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescopingVelocity((float)frontSubscriber.FrontCmd.velocity[0]);

                        joints.armTilt.actuator.controlType = ControlType.Speed;
                        joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescopingVelocity((float)frontSubscriber.FrontCmd.velocity[1]);

                        joints.boomTilt.actuator.controlType = ControlType.Speed;
                        joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescopingVelocity((float)frontSubscriber.FrontCmd.velocity[2]);

                        joints.swing.actuator.controlType = ControlType.Speed;
                        double vel = frontSubscriber.FrontCmd.velocity[3];
                        if (vel > 0) 
                        {
                            joints.swing.actuator.controlValue = vel * 0.52;
                        }
                        else
                        {
                            joints.swing.actuator.controlValue = vel;
                        }
                        break;
                    case ControlType.Force:
                        joints.bucketTilt.actuator.controlType = ControlType.Force;
                        joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescopingForce((float)frontSubscriber.FrontCmd.effort[0]);

                        joints.armTilt.actuator.controlType = ControlType.Force;
                        joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescopingForce((float)frontSubscriber.FrontCmd.effort[1]);

                        joints.boomTilt.actuator.controlType = ControlType.Force;
                        joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescopingForce((float)frontSubscriber.FrontCmd.effort[2]);

                        joints.swing.actuator.controlType = ControlType.Force;
                        joints.swing.actuator.controlValue = frontSubscriber.FrontCmd.effort[3];
                        break;
                    default:
                        break;
                }
                // 下部走行体
                switch (movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch (controlType)
                        {
                            case ControlType.Position:
                                joints.leftSprocket.actuator.controlType = ControlType.Position;
                                joints.leftSprocket.actuator.controlValue = trackSubscriber.TrackCmd.position[0];

                                joints.rightSprocket.actuator.controlType = ControlType.Position;
                                joints.rightSprocket.actuator.controlValue = trackSubscriber.TrackCmd.position[1];
                                break;
                            case ControlType.Speed:
                                joints.leftSprocket.actuator.controlType = ControlType.Speed;
                                joints.leftSprocket.actuator.controlValue = trackSubscriber.TrackCmd.velocity[0];

                                joints.rightSprocket.actuator.controlType = ControlType.Speed;
                                joints.rightSprocket.actuator.controlValue = trackSubscriber.TrackCmd.velocity[1];
                                break;
                            case ControlType.Force:
                                joints.leftSprocket.actuator.controlType = ControlType.Force;
                                joints.leftSprocket.actuator.controlValue = trackSubscriber.TrackCmd.effort[0];

                                joints.rightSprocket.actuator.controlType = ControlType.Force;
                                joints.rightSprocket.actuator.controlValue = trackSubscriber.TrackCmd.effort[1];
                                break;
                            default:
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        twistCommandConvertor.SetCommand(trackSubscriber.VelocityCmd.linear, trackSubscriber.VelocityCmd.angular);

                        joints.leftSprocket.actuator.controlType = ControlType.Speed;
                        joints.leftSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_L;

                        joints.rightSprocket.actuator.controlType = ControlType.Speed;
                        joints.rightSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_R;
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

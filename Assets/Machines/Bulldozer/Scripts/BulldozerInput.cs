using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PWRISimulator.ROS
{
    public class BulldozerInput : MonoBehaviour
    {
        public BulldozerBladeSubscriber BladeSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public BulldozerSettingSubscriber settingSubscriber;
        [SerializeField] ConstractionMovementControlType movementControlType;
        [SerializeField] ControlType controlType = ControlType.Position;

        public BladeLiftToCylinderLengthConvertor bladeLiftCylConv;
        public BladeTiltToCylinderLengthConvertor bladeTiltCylConv;
        public BladeAngleToCylinderLengthConvertor bladeAngleCylConv;

        public TrackTwistCommandConvertor twistCommandConvertor;

        [Header("Dummy")]
        [SerializeField] bool enabledDummy;
        [SerializeField] double lift_joint;
        [SerializeField] double tilt_joint;
        [SerializeField] double angle_joint;
        [SerializeField] double right_track;
        [SerializeField] double left_track;
        [SerializeField] bool emergencyStop;


        private BulldozerJoints joints;

        void Start()
        {
            joints = gameObject.GetComponent<BulldozerJoints>();
        }
        // Update is called once per frame
        void FixedUpdate()
        {
            if (enabledDummy)
            {
                BladeSubscriber.BladeCmd.position[0] = lift_joint;
                BladeSubscriber.BladeCmd.position[1] = tilt_joint;
                BladeSubscriber.BladeCmd.position[2] = angle_joint;
                trackSubscriber.TrackCmd.position[0] = right_track;
                trackSubscriber.TrackCmd.position[1] = left_track;
            }
            else
            {
                // 受信
                double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;

                BladeSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
                trackSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
                settingSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            }
            // 制御値の反映
            if (settingSubscriber.EmergencyStopCmd)
            {
                // 緊急停止
            }
            else
            {
                // 上部旋回体
                switch(controlType)
                {
                    case ControlType.Position:
                        break;
                    case ControlType.Speed:
                        break;
                    case ControlType.Force:
                        break;
                    default:
                        break;
                }
                // 下部走行体
                switch(movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch(controlType)
                        {
                            case ControlType.Position:
                                break;
                            case ControlType.Speed:
                                break;
                            case ControlType.Force:
                                break;
                            default:
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        break;
                    default:
                        break;
                }
            }
        }

        public void SetCommands()
        {
            // 制御値の反映
            if (enabledDummy ? emergencyStop : settingSubscriber.EmergencyStopCmd)
            {
                // 緊急停止
                joints.bladeLift.controlType = ControlType.Position;
                joints.bladeLift.controlValue = joints.bladeLift.CurrentPosition;

                joints.bladeTilt.controlType = ControlType.Position;
                joints.bladeTilt.controlValue = joints.bladeTilt.CurrentPosition;

                joints.bladeAngleLeft.controlType = ControlType.Position;
                joints.bladeAngleLeft.controlValue = joints.bladeAngleLeft.CurrentPosition;

                joints.bladeAngleRight.controlType = ControlType.Position;
                joints.bladeAngleRight.controlValue = joints.bladeAngleRight.CurrentPosition;

                joints.rightSprocket.controlType = ControlType.Position;
                joints.rightSprocket.controlValue = joints.rightSprocket.CurrentPosition;

                joints.leftSprocket.controlType = ControlType.Position;
                joints.leftSprocket.controlValue = joints.leftSprocket.CurrentPosition;
            }
            else
            {
                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:
                        joints.bladeLift.controlType = ControlType.Position;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescoping((float)BladeSubscriber.BladeCmd.position[0]);

                        joints.bladeTilt.controlType = ControlType.Position;
                        joints.bladeTilt.controlValue = bladeTiltCylConv.CalculateCylinderRodTelescoping((float)BladeSubscriber.BladeCmd.position[1]);

                        float telescoping = bladeAngleCylConv.CalculateCylinderRodTelescoping((float)BladeSubscriber.BladeCmd.position[2]);
                        joints.bladeAngleLeft.controlType = ControlType.Position;
                        joints.bladeAngleLeft.controlValue = -telescoping;

                        joints.bladeAngleRight.controlType = ControlType.Position;
                        joints.bladeAngleRight.controlValue = telescoping;
                        break;
                    case ControlType.Speed:
                        joints.bladeLift.controlType = ControlType.Speed;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescopingVelocity((float)BladeSubscriber.BladeCmd.velocity[0]);

                        joints.bladeTilt.controlType = ControlType.Speed;
                        joints.bladeTilt.controlValue = bladeTiltCylConv.CalculateCylinderRodTelescopingVelocity((float)BladeSubscriber.BladeCmd.velocity[1]);

                        float telescopingVelocity = bladeAngleCylConv.CalculateCylinderRodTelescopingVelocity((float)BladeSubscriber.BladeCmd.velocity[2]);
                        joints.bladeAngleLeft.controlType = ControlType.Speed;
                        joints.bladeAngleLeft.controlValue = -telescopingVelocity;

                        joints.bladeAngleRight.controlType = ControlType.Speed;
                        joints.bladeAngleRight.controlValue = telescopingVelocity;
                        break;
                    case ControlType.Force:
                        joints.bladeLift.controlType = ControlType.Force;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescopingForce((float)BladeSubscriber.BladeCmd.effort[0]);

                        joints.bladeTilt.controlType = ControlType.Force;
                        joints.bladeTilt.controlValue = bladeTiltCylConv.CalculateCylinderRodTelescopingForce((float)BladeSubscriber.BladeCmd.effort[1]);

                        float telescopingForce = bladeAngleCylConv.CalculateCylinderRodTelescopingForce((float)BladeSubscriber.BladeCmd.effort[2]);
                        joints.bladeAngleLeft.controlType = ControlType.Force;
                        joints.bladeAngleLeft.controlValue = -telescopingForce;

                        joints.bladeAngleRight.controlType = ControlType.Force;
                        joints.bladeAngleRight.controlValue = telescopingForce;
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
                                joints.rightSprocket.controlType = ControlType.Position;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.position[0];

                                joints.leftSprocket.controlType = ControlType.Position;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.position[1];
                                break;
                            case ControlType.Speed:
                                joints.rightSprocket.controlType = ControlType.Speed;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.velocity[0];

                                joints.leftSprocket.controlType = ControlType.Speed;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.velocity[1];
                                break;
                            case ControlType.Force:
                                joints.rightSprocket.controlType = ControlType.Force;
                                joints.rightSprocket.controlValue = trackSubscriber.TrackCmd.effort[0];

                                joints.leftSprocket.controlType = ControlType.Force;
                                joints.leftSprocket.controlValue = trackSubscriber.TrackCmd.effort[1];
                                break;
                            default:
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        twistCommandConvertor.SetCommand(trackSubscriber.VelocityCmd.linear, trackSubscriber.VelocityCmd.angular);

                        joints.leftSprocket.controlType = ControlType.Speed;
                        joints.leftSprocket.controlValue = twistCommandConvertor.sprocketSpeed_L;

                        joints.rightSprocket.controlType = ControlType.Speed;
                        joints.rightSprocket.controlValue = twistCommandConvertor.sprocketSpeed_R;
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

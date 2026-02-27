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

        [Header("Blade lift limit (blade edge height)")]
        [SerializeField] Transform bladeEdge;
        [SerializeField] float bladeEdgeHeightUpperLimitMeters = 0.8f;
        [SerializeField] float bladeEdgeHeightLowerLimitMeters = -0.38f;
        [SerializeField] float liftPositionRateLimitRadPerSec = 0.5f;

        [Header("Length convertor")]
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
        private const string BladeEdgeGameObjectName = "AGXUnity.Collide.Box_blade3";
        private float bladeEdgeInitialLocalY;
        private float appliedLiftCmdAngle;

        void Start()
        {
            joints = gameObject.GetComponent<BulldozerJoints>();
            appliedLiftCmdAngle = joints != null ? (float)joints.bladeLift.CurrentPosition : 0.0f;

            if (bladeEdge == null)
            {
                var bladeEdgeGo = GameObject.Find(BladeEdgeGameObjectName);
                bladeEdge = bladeEdgeGo != null ? bladeEdgeGo.transform : null;
            }

            if (bladeEdge != null)
            {
                bladeEdgeInitialLocalY = transform.InverseTransformPoint(bladeEdge.position).y;
            }
            else
            {
                Debug.LogWarning($"[{nameof(BulldozerInput)}] Failed to find blade edge GameObject '{BladeEdgeGameObjectName}' at Start(). Height-based lift limiting will be disabled.", this);
            }
        }

        private bool TryGetBladeEdgeHeightAboveGround(out float heightMeters)
        {
            heightMeters = 0.0f;
            if (bladeEdge == null)
                return false;

            float bladeEdgeLocalY = transform.InverseTransformPoint(bladeEdge.position).y;
            heightMeters = bladeEdgeLocalY - bladeEdgeInitialLocalY;
            return true;
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
                appliedLiftCmdAngle = (float)joints.bladeLift.CurrentPosition;

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
                        float targetLiftCmdAngle = (float)BladeSubscriber.BladeCmd.position[0];
                        float maxDelta = liftPositionRateLimitRadPerSec > 0.0f ? liftPositionRateLimitRadPerSec * Time.fixedDeltaTime : float.PositiveInfinity;
                        float liftCmdAngle = Mathf.MoveTowards(appliedLiftCmdAngle, targetLiftCmdAngle, maxDelta);

                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeight) && joints != null)
                        {
                            float currentLiftAngle = (float)joints.bladeLift.CurrentPosition;
                            if (bladeEdgeHeight >= bladeEdgeHeightUpperLimitMeters && liftCmdAngle > currentLiftAngle)
                                liftCmdAngle = currentLiftAngle;
                            else if (bladeEdgeHeight <= bladeEdgeHeightLowerLimitMeters && liftCmdAngle < currentLiftAngle)
                                liftCmdAngle = currentLiftAngle;
                        }

                        appliedLiftCmdAngle = liftCmdAngle;
                        joints.bladeLift.controlType = ControlType.Position;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescoping(liftCmdAngle);

                        joints.bladeTilt.controlType = ControlType.Position;
                        joints.bladeTilt.controlValue = bladeTiltCylConv.CalculateCylinderRodTelescoping((float)BladeSubscriber.BladeCmd.position[1]);

                        float telescoping = bladeAngleCylConv.CalculateCylinderRodTelescoping((float)BladeSubscriber.BladeCmd.position[2]);
                        joints.bladeAngleLeft.controlType = ControlType.Position;
                        joints.bladeAngleLeft.controlValue = -telescoping;

                        joints.bladeAngleRight.controlType = ControlType.Position;
                        joints.bladeAngleRight.controlValue = telescoping;
                        break;
                    case ControlType.Speed:
                        float liftCmdVelocity = (float)BladeSubscriber.BladeCmd.velocity[0];
                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeightVel))
                        {
                            if (bladeEdgeHeightVel >= bladeEdgeHeightUpperLimitMeters && liftCmdVelocity > 0.0f)
                                liftCmdVelocity = 0.0f;
                            else if (bladeEdgeHeightVel <= bladeEdgeHeightLowerLimitMeters && liftCmdVelocity < 0.0f)
                                liftCmdVelocity = 0.0f;
                        }

                        joints.bladeLift.controlType = ControlType.Speed;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescopingVelocity(liftCmdVelocity);

                        joints.bladeTilt.controlType = ControlType.Speed;
                        joints.bladeTilt.controlValue = bladeTiltCylConv.CalculateCylinderRodTelescopingVelocity((float)BladeSubscriber.BladeCmd.velocity[1]);

                        float telescopingVelocity = bladeAngleCylConv.CalculateCylinderRodTelescopingVelocity((float)BladeSubscriber.BladeCmd.velocity[2]);
                        joints.bladeAngleLeft.controlType = ControlType.Speed;
                        joints.bladeAngleLeft.controlValue = -telescopingVelocity;

                        joints.bladeAngleRight.controlType = ControlType.Speed;
                        joints.bladeAngleRight.controlValue = telescopingVelocity;
                        break;
                    case ControlType.Force:
                        float liftCmdForce = (float)BladeSubscriber.BladeCmd.effort[0];
                        if (TryGetBladeEdgeHeightAboveGround(out float bladeEdgeHeightForce))
                        {
                            if (bladeEdgeHeightForce >= bladeEdgeHeightUpperLimitMeters && liftCmdForce > 0.0f)
                                liftCmdForce = 0.0f;
                            else if (bladeEdgeHeightForce <= bladeEdgeHeightLowerLimitMeters && liftCmdForce < 0.0f)
                                liftCmdForce = 0.0f;
                        }

                        joints.bladeLift.controlType = ControlType.Force;
                        joints.bladeLift.controlValue = bladeLiftCylConv.CalculateCylinderRodTelescopingForce(liftCmdForce);

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

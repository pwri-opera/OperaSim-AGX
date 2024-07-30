using UnityEngine;

namespace PWRISimulator.ROS
{
    public class DumpTruckInput : MonoBehaviour
    {
        public DumpTruckDumpSubscriber RotDumpSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public DumpTruckSettingSubscriber settingSubscriber;
        [SerializeField] ConstractionMovementControlType movementControlType;
        [SerializeField] ControlType controlType = ControlType.Position;

        [Header("Dummy")]
        [SerializeField] bool enabledDummy;
        [SerializeField] double rotate_joint;
        [SerializeField] double dump_joint;
        [SerializeField] double right_track;
        [SerializeField] double left_track;
        [SerializeField] bool emergencyStop;

        public TrackTwistCommandConvertor twistCommandConvertor;
        public TrackVolumeCommandConvertor volumeCommandConvertor;

        private DumpTruckJoint joints;

        void Start()
        {
            joints = gameObject.GetComponent<DumpTruckJoint>();
        }
        // Update is called once per frame
        void FixedUpdate()
        {
            if ( enabledDummy )
            {
                RotDumpSubscriber.DumpCmd.position[0] = rotate_joint;
                RotDumpSubscriber.DumpCmd.position[1] = dump_joint;
                trackSubscriber.TrackCmd.position[0] = right_track;
                trackSubscriber.TrackCmd.position[1] = left_track;
            }
            else
            {
                // 受信
                double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;

                RotDumpSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
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
                joints.rightSprocket.controlType = ControlType.Position;
                joints.rightSprocket.controlValue = joints.rightSprocket.CurrentPosition;

                joints.leftSprocket.controlType = ControlType.Position;
                joints.leftSprocket.controlValue = joints.leftSprocket.CurrentPosition;

                joints.dump_joint.controlType = ControlType.Position;
                joints.dump_joint.controlValue = joints.dump_joint.CurrentPosition;
            }
            else
            {
                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:
                        joints.dump_joint.controlType = ControlType.Position;
                        joints.dump_joint.controlValue = RotDumpSubscriber.DumpCmd.position[1];
                        break;
                    case ControlType.Speed:
                        joints.dump_joint.controlType = ControlType.Speed;
                        joints.dump_joint.controlValue = RotDumpSubscriber.DumpCmd.velocity[1];
                        break;
                    case ControlType.Force:
                        joints.dump_joint.controlType = ControlType.Force;
                        joints.dump_joint.controlValue = RotDumpSubscriber.DumpCmd.effort[1];
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
                        volumeCommandConvertor.SetCommand(trackSubscriber.VolumeCmd.effort[0], trackSubscriber.VolumeCmd.effort[1]);

                        joints.leftSprocket.controlType = ControlType.Speed;
                        joints.leftSprocket.controlValue = volumeCommandConvertor.twistCommandConvertor.sprocketSpeed_L;

                        joints.rightSprocket.controlType = ControlType.Speed;
                        joints.rightSprocket.controlValue = volumeCommandConvertor.twistCommandConvertor.sprocketSpeed_R;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

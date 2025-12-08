using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using UnityEditor;
using UnityEngine;

using RosMessageTypes.Geometry;  // geometry_msgs/msg/Twist 型メッセージを使用するために定義

namespace PWRISimulator.ROS
{
    public class ExcavatorInput : MonoBehaviour
    {
        public ExcavatorFrontSubscriber frontSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public ExcavatorSettingSubscriber settingSubscriber;
        //[SerializeField] ConstractionMovementControlType movementControlType;
        //[SerializeField] ControlType controlType = ControlType.Position;
        [SerializeField] public ConstractionMovementControlType movementControlType;
        [SerializeField] public ControlType controlType = ControlType.Position;

        //private ExcavatorJoints joints;
        public ExcavatorJoints joints;

        // for Joint Command
        public BoomAngleToCylinderLengthConvertor boomCylConv;
        public ArmAngleToCylinderLengthConvertor armCylConv;
        public BucketAngleToCylinderLengthConvertor bucketCylConv;

        // for Twist Command
        public TrackTwistCommandConvertor twistCommandConvertor;

        // Mapping for joint_name → index num 
        private readonly Dictionary<string, int> _frontIndexMap = new Dictionary<string, int>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _trackIndexMap = new Dictionary<string, int>(System.StringComparer.Ordinal);

        private const string JOINT_BUCKET = "bucket_joint";
        private const string JOINT_ARM    = "arm_joint";
        private const string JOINT_BOOM   = "boom_joint";
        private const string JOINT_SWING  = "swing_joint";

        private const string JOINT_R_SPROCKET  = "right_track";
        private const string JOINT_L_SPROCKET  = "left_track";
        private const string JOINT_DIFF_TRACK  = "diff_track";

        private DeadTimeDelay<double> swingDeadTimeDelay;
        private DeadTimeDelay<double> boomDeadTimeDelay;
        private DeadTimeDelay<double> armDeadTimeDelay;
        private DeadTimeDelay<double> bucketDeadTimeDelay;

        private DeadTimeDelay<double> leftSprocketDeadTimeDelay;
        private DeadTimeDelay<double> rightSprocketDeadTimeDelay;
        private DeadTimeDelay<TwistMsg> trackModuleDeadTimeDelay;

        void Start()
        {
            joints = gameObject.GetComponent<ExcavatorJoints>();

            swingDeadTimeDelay  = new DeadTimeDelay<double> (joints.swing.actuator.deadTime);
            boomDeadTimeDelay   = new DeadTimeDelay<double> (joints.boomTilt.actuator.deadTime);
            armDeadTimeDelay    = new DeadTimeDelay<double> (joints.armTilt.actuator.deadTime);
            bucketDeadTimeDelay = new DeadTimeDelay<double> (joints.bucketTilt.actuator.deadTime);

            leftSprocketDeadTimeDelay  = new DeadTimeDelay<double>(joints.leftSprocket.actuator.deadTime);
            rightSprocketDeadTimeDelay = new DeadTimeDelay<double>(joints.rightSprocket.actuator.deadTime);

            trackModuleDeadTimeDelay = new DeadTimeDelay<TwistMsg>(joints.trackDeadTime);
        }
        void FixedUpdate()
        {
            // 受信
            double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;

            frontSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            trackSubscriber.ExecuteSubscriptionHandlerActions(currentTime);
            settingSubscriber.ExecuteSubscriptionHandlerActions(currentTime);

            BuildJointIndexMap();

            if (joints.activateDeadTime)
            {
                var front_cmd = frontSubscriber.FrontCmd;
                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:
                        if (GetJointValue(front_cmd.position, JOINT_BUCKET, out double bucketPos))
                            bucketDeadTimeDelay.addInputData(currentTime*1000.0, bucketPos); 
                        if (GetJointValue(front_cmd.position, JOINT_ARM, out double armPos))
                            armDeadTimeDelay.addInputData(currentTime*1000.0, armPos); 
                        if (GetJointValue(front_cmd.position, JOINT_BOOM, out double boomPos))
                            boomDeadTimeDelay.addInputData(currentTime*1000.0, boomPos); 
                        if (GetJointValue(front_cmd.position, JOINT_SWING, out double swingPos))
                            swingDeadTimeDelay.addInputData(currentTime*1000.0, swingPos); 
                        break;
                    case ControlType.Speed:
                        if (GetJointValue(front_cmd.velocity, JOINT_BUCKET, out double bucketVel))
                            bucketDeadTimeDelay.addInputData(currentTime*1000.0, bucketVel); 
                        if (GetJointValue(front_cmd.velocity, JOINT_ARM, out double armVel))
                            armDeadTimeDelay.addInputData(currentTime*1000.0, armVel); 
                        if (GetJointValue(front_cmd.velocity, JOINT_BOOM, out double boomVel))
                            boomDeadTimeDelay.addInputData(currentTime*1000.0, boomVel); 
                        if (GetJointValue(front_cmd.velocity, JOINT_SWING, out double swingVel))
                            swingDeadTimeDelay.addInputData(currentTime*1000.0, swingVel); 
                        break;
                    case ControlType.Force:
                        if (GetJointValue(front_cmd.effort, JOINT_BUCKET, out double bucketEff))
                            bucketDeadTimeDelay.addInputData(currentTime*1000.0, bucketEff); 
                        if (GetJointValue(front_cmd.effort, JOINT_ARM, out double armEff))
                            armDeadTimeDelay.addInputData(currentTime*1000.0, armEff); 
                        if (GetJointValue(front_cmd.effort, JOINT_BOOM, out double boomEff))
                            boomDeadTimeDelay.addInputData(currentTime*1000.0, boomEff); 
                        if (GetJointValue(front_cmd.effort, JOINT_SWING, out double swingEff))
                            swingDeadTimeDelay.addInputData(currentTime*1000.0, swingEff); 
                        break;                        
                    default:
                        break;
                }

                    // 下部走行体
                switch (movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        var track_cmd = trackSubscriber.TrackCmd;

                        switch (controlType)
                        {
                            case ControlType.Position:
                                if (GetJointValue(track_cmd.position, JOINT_R_SPROCKET, out double trackR_Pos))
                                    rightSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackR_Pos);
                                if (GetJointValue(track_cmd.position, JOINT_L_SPROCKET, out double trackL_Pos))
                                    leftSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackL_Pos);                                
                                break;
                            case ControlType.Speed:
                                if (GetJointValue(track_cmd.velocity, JOINT_R_SPROCKET, out double trackR_Vel))
                                    rightSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackR_Vel);
                                if (GetJointValue(track_cmd.velocity, JOINT_L_SPROCKET, out double trackL_Vel))
                                    leftSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackL_Vel);  
                                break;
                            case ControlType.Force:
                                if (GetJointValue(track_cmd.effort, JOINT_R_SPROCKET, out double trackR_Eff))
                                    rightSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackR_Eff);
                                if (GetJointValue(track_cmd.effort, JOINT_L_SPROCKET, out double trackL_Eff))
                                    leftSprocketDeadTimeDelay.addInputData(currentTime*1000.0, trackL_Eff);  
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        var cmd_vel = trackSubscriber.VelocityCmd;
                        if  (cmd_vel != null)
                        {
                            trackModuleDeadTimeDelay.addInputData(currentTime*1000.0, cmd_vel);
                        }                            
                        break;

                    case ConstractionMovementControlType.VolumeCommand:
                        break;
                    default:
                        break;
                }
            }    
        }

        // Making a map (Dictionary) of Front_cmd's joint_name <-> position/velocity/effort
        private void BuildJointIndexMap()
        {
            _frontIndexMap.Clear();
            var front_msg = frontSubscriber?.FrontCmd;
            if (front_msg == null || front_msg.joint_name == null) return;

            for (int i = 0; i < front_msg.joint_name.Length; i++)
            {
                var name = front_msg.joint_name[i];
                if (string.IsNullOrEmpty(name)) continue;
                if (!_frontIndexMap.ContainsKey(name))
                    _frontIndexMap.Add(name, i);
            }

            var track_msg = trackSubscriber?.TrackCmd;
            if (track_msg == null || track_msg.joint_name == null) return;

            _trackIndexMap.Clear();
            for (int i = 0; i < track_msg.joint_name.Length; i++)
            {
                var name = track_msg.joint_name[i];
                if (string.IsNullOrEmpty(name)) continue;
                if (!_trackIndexMap.ContainsKey(name))
                    _trackIndexMap.Add(name, i);
            }
        }

        // Get the position/velocity/effort corresponding to joint_name in front_cmd
        private bool GetJointValue(double[] arr, string jointName, out double value)
        {
            value = 0.0;
            if (arr == null) return false;
            if (_frontIndexMap.TryGetValue(jointName, out int idx))
            {
                if (idx >= 0 && idx < arr.Length)
                {
                    value = arr[idx];
                    return true;
                }
            }
            return false;
        }

        private bool GetEffectiveJointValue(double currentTimeMs, string jointName, out double jointValue)
        {
            bool flag = false;
            jointValue = 0.0;

            if (joints.activateDeadTime)
            {
                if (jointName == JOINT_BUCKET) {
                    flag = bucketDeadTimeDelay.drainInputDataLatest(currentTimeMs, out jointValue);

                } else if (jointName == JOINT_ARM) {
                    flag = armDeadTimeDelay.drainInputDataLatest(currentTimeMs, out jointValue);

                } else if (jointName == JOINT_BOOM) {
                    flag = boomDeadTimeDelay.drainInputDataLatest(currentTimeMs, out jointValue);

                } else if (jointName == JOINT_SWING) {
                    flag = swingDeadTimeDelay.drainInputDataLatest(currentTimeMs, out jointValue);
                }
                else {
                    flag = false;
                }
                return flag;
            }
            else 
            {
                var cmd = frontSubscriber.FrontCmd;
                switch (controlType)
                {
                    case ControlType.Position:
                        flag = GetJointValue(cmd.position, jointName, out jointValue);
                        break;
                    case ControlType.Speed:
                        flag = GetJointValue(cmd.velocity, jointName, out jointValue);
                        break;
                    case ControlType.Force:
                        flag = GetJointValue(cmd.effort, jointName, out jointValue); 
                        break;
                    default:
                        flag =  false;
                        break;
                }
                return flag;
            }
            return false;
        }

        public void SetCommands()
        {
            // Debug.Log("BucketTest: " + joints.bucketTilt.actuator.deadTime);
            // Debug.Log("ArmTest: " + joints.armTilt.actuator.deadTime);
            // Debug.Log("BoomTest: " + joints.boomTilt.actuator.deadTime);
            // Debug.Log("SwingTest: " + joints.swing.actuator.deadTime);

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

            /// <summary>
            /// 各関節に対する入力処理
            /// </summary>
            else
            {
                double currentTime = (Time.fixedTimeAsDouble - Time.fixedDeltaTime) * 1000.0;
                var cmd = frontSubscriber.FrontCmd;

                // 上部旋回体
                switch (controlType)
                {
                    case ControlType.Position:
                        if (GetEffectiveJointValue(currentTime, JOINT_BUCKET, out double bucketPos))
                        {
                            joints.bucketTilt.actuator.controlType = ControlType.Position;
                            joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescoping((float)bucketPos);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_ARM, out double armPos))
                        {
                            joints.armTilt.actuator.controlType = ControlType.Position;
                            joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescoping((float)armPos);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_BOOM, out double boomPos))
                        {
                            joints.boomTilt.actuator.controlType = ControlType.Position;
                            joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescoping((float)boomPos);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_SWING, out double swingPos))
                        {
                            joints.swing.actuator.controlType = ControlType.Position;
                            joints.swing.actuator.controlValue = swingPos;
                        }
                        break;
                    case ControlType.Speed:
                        if (GetEffectiveJointValue(currentTime, JOINT_BUCKET, out double bucketVel))
                        {
                            joints.bucketTilt.actuator.controlType = ControlType.Speed;
                            joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescopingVelocity((float)bucketVel);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_ARM, out double armVel))
                        {
                            joints.armTilt.actuator.controlType = ControlType.Speed;
                            joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescopingVelocity((float)armVel);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_BOOM, out double boomVel))
                        {
                            joints.boomTilt.actuator.controlType = ControlType.Speed;
                            joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescopingVelocity((float)boomVel);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_SWING, out double swingVel))
                        {
                            joints.swing.actuator.controlType = ControlType.Speed;
                            // 既存ロジック踏襲: 正方向のみ0.52倍
                            joints.swing.actuator.controlValue = (swingVel > 0.0) ? (swingVel * 0.52) : swingVel;
                        }
                        break;
                    case ControlType.Force:
                        if (GetEffectiveJointValue(currentTime, JOINT_BUCKET, out double bucketEff))
                        {
                            joints.bucketTilt.actuator.controlType = ControlType.Force;
                            joints.bucketTilt.actuator.controlValue = bucketCylConv.CalculateCylinderRodTelescopingForce((float)bucketEff);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_ARM, out double armEff))
                        {
                            joints.armTilt.actuator.controlType = ControlType.Force;
                            joints.armTilt.actuator.controlValue = armCylConv.CalculateCylinderRodTelescopingForce((float)armEff);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_BOOM, out double boomEff))
                        {
                            joints.boomTilt.actuator.controlType = ControlType.Force;
                            joints.boomTilt.actuator.controlValue = boomCylConv.CalculateCylinderRodTelescopingForce((float)boomEff);
                        }

                        if (GetEffectiveJointValue(currentTime, JOINT_SWING, out double swingEff))
                        {
                            joints.swing.actuator.controlType = ControlType.Force;
                            joints.swing.actuator.controlValue = swingEff;
                        }
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
                                joints.rightSprocket.actuator.controlType = ControlType.Position;

                                if (GetEffectiveJointValue(currentTime, JOINT_R_SPROCKET, out double trackR_Pos))
                                    joints.leftSprocket.actuator.controlValue = trackR_Pos;
                                if (GetEffectiveJointValue(currentTime, JOINT_L_SPROCKET, out double trackL_Pos))
                                    joints.rightSprocket.actuator.controlValue = trackL_Pos;  
                                break;
                                
                            case ControlType.Speed:
                                joints.leftSprocket.actuator.controlType = ControlType.Speed;
                                joints.rightSprocket.actuator.controlType = ControlType.Speed;

                                if (GetEffectiveJointValue(currentTime, JOINT_R_SPROCKET, out double trackR_Vel))
                                    joints.leftSprocket.actuator.controlValue = trackR_Vel;
                                if (GetEffectiveJointValue(currentTime, JOINT_L_SPROCKET, out double trackL_Vel))
                                    joints.rightSprocket.actuator.controlValue = trackL_Vel;
                                break;

                            case ControlType.Force:
                                joints.leftSprocket.actuator.controlType = ControlType.Force;
                                joints.rightSprocket.actuator.controlType = ControlType.Force;

                                if (GetEffectiveJointValue(currentTime, JOINT_R_SPROCKET, out double trackR_Eff))
                                    joints.leftSprocket.actuator.controlValue = trackR_Eff;
                                if (GetEffectiveJointValue(currentTime, JOINT_L_SPROCKET, out double trackL_Eff))
                                    joints.rightSprocket.actuator.controlValue = trackL_Eff;
                                break;

                            default:
                                break;
                        }
                        break;

                    case ConstractionMovementControlType.TwistCommand:
                        joints.leftSprocket.actuator.controlType = ControlType.Speed;
                        joints.rightSprocket.actuator.controlType = ControlType.Speed;

                        if (joints.activateDeadTime)
                        {
                            if (trackModuleDeadTimeDelay.drainInputDataLatest(currentTime, out TwistMsg cmdVelValue))
                            {
                                // ここにスクリプトを追記（vw 調整）
                                twistCommandConvertor.SetCommand(cmdVelValue.linear, cmdVelValue.angular);
                                joints.leftSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_L;
                                joints.rightSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_R;
                            }
                        }
                        else 
                        {
                            // ここにスクリプトを追記（vw 調整）
                            twistCommandConvertor.SetCommand(trackSubscriber.VelocityCmd.linear, trackSubscriber.VelocityCmd.angular);
                            joints.leftSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_L;
                            joints.rightSprocket.actuator.controlValue = twistCommandConvertor.sprocketSpeed_R;
                        }
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

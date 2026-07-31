using UnityEngine;
using System.Collections.Generic;
using RosMessageTypes.Geometry;   // TwistMsg
using RosMessageTypes.Com3;       //Com3Msg

namespace PWRISimulator.ROS
{
    public class DumpTruckInput : MonoBehaviour
    {
        public DumpTruckDumpSubscriber RotDumpSubscriber;
        public TrackMessageSubscriber trackSubscriber;
        public DumpTruckSettingSubscriber settingSubscriber;
        public DumpVesselStateController vesselStateController;

        [SerializeField] DumpTruckJoint dumpTruckJoint;
        [SerializeField] public ConstractionMovementControlType movementControlType;
        [SerializeField] public ControlType trackControlType = ControlType.Position;
        [SerializeField] public ControlType vesselControlType = ControlType.Position;
        [SerializeField] public ControlType rotateControlType = ControlType.Position;

        [Header("Diagnostics")]
        [SerializeField] private bool logDumpCommandDiagnostics = false;
        private float _nextDumpLogTime;

        [Header("Dummy")]
        [SerializeField] bool enabledDummy;
        [SerializeField] double rotate_joint;
        //[SerializeField] double dump_joint;
        //[SerializeField] double right_track;
        //[SerializeField] double left_track;
        [SerializeField] bool emergencyStop;

        [SerializeField] public double dump_joint;
        [SerializeField] public double right_track;
        [SerializeField] public double left_track;

        public TrackTwistCommandConvertor twistCommandConvertor;
        public TrackVolumeCommandConvertor volumeCommandConvertor;

        //private DumpTruckJoint joints;
        public DumpTruckJoint joints;

        private DeadTimeDelay<double>   rotateDeadTimeDelay;
        private DeadTimeDelay<double>   dumpDeadTimeDelay;
        private DeadTimeDelay<double>   rightSprocketDeadTimeDelay;
        private DeadTimeDelay<double>   leftSprocketDeadTimeDelay;
        private DeadTimeDelay<TwistMsg> trackModuleDeadTimeDelay;
        private DeadTimeDelay<JointCmdMsg>  trackVolumeCmdDeadTimeDelay;

        // joint_name → index
        private readonly Dictionary<string, int> _dumpIndexMap = new Dictionary<string, int>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _trackIndexMap = new Dictionary<string, int>(System.StringComparer.Ordinal);
        private readonly Dictionary<string, int> _volumeIndexMap = new Dictionary<string, int>(System.StringComparer.Ordinal);

        // 名称は Excavator 側に合わせる（メッセージの joint_name 想定）
        private const string JOINT_ROTATE = "rotate_joint";
        private const string JOINT_DUMP = "dump_joint";
        private const string JOINT_R_SPROCKET = "right_track";
        private const string JOINT_L_SPROCKET = "left_track";
        private const string FORWARD_VOLUME = "forward_volume";
        private const string TURN_VOLUME = "turn_volume";

        private const byte POSITION_CONTROL = 0;
        private const byte SPEED_CONTROL = 1;
        private const byte FORCE_CONTROL = 2;

        void Start()
        {
            joints = dumpTruckJoint != null ? dumpTruckJoint : GetComponent<DumpTruckJoint>();
            if (joints == null)
            {
                Debug.LogError("[DumpTruckInput] DumpTruckJoint is not assigned.", this);
                enabled = false;
                return;
            }

            dumpTruckJoint = joints;
            if (vesselStateController == null)
                vesselStateController = GetComponent<DumpVesselStateController>();

            rotateDeadTimeDelay         = new DeadTimeDelay<double>(joints.rotate_joint.deadTime);
            dumpDeadTimeDelay           = new DeadTimeDelay<double>(joints.dump_joint.deadTime);
            rightSprocketDeadTimeDelay  = new DeadTimeDelay<double>(joints.rightSprocket.deadTime);
            leftSprocketDeadTimeDelay   = new DeadTimeDelay<double>(joints.leftSprocket.deadTime);
            trackModuleDeadTimeDelay    = new DeadTimeDelay<TwistMsg>(joints.trackDeadTime);
            trackVolumeCmdDeadTimeDelay    = new DeadTimeDelay<JointCmdMsg>(joints.trackDeadTime);
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

            BuildJointIndexMap();

            if (joints.activateDeadTime)
            {
                double nowMs = (Time.fixedTimeAsDouble - Time.fixedDeltaTime) * 1000.0;

                // 上部（回転・ダンプ）
                ControlType dumpControlType = GetDumpControlType();
                switch (dumpControlType)
                {
                    case ControlType.Position:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.position, JOINT_DUMP, _dumpIndexMap, out double dumpPos))
                            dumpDeadTimeDelay.addInputData(nowMs, dumpPos);
                        break;
                    case ControlType.Speed:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.velocity, JOINT_DUMP, _dumpIndexMap, out double dumpVel))
                            dumpDeadTimeDelay.addInputData(nowMs, dumpVel);
                        break;
                    case ControlType.Force:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.effort, JOINT_DUMP, _dumpIndexMap, out double dumpEff))
                            dumpDeadTimeDelay.addInputData(nowMs, dumpEff);
                        break;
                }

                switch (rotateControlType)
                {
                    case ControlType.Position:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.position, JOINT_ROTATE, _dumpIndexMap, out double rotPos))
                            rotateDeadTimeDelay.addInputData(nowMs, rotPos);
                        break;
                    case ControlType.Speed:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.velocity, JOINT_ROTATE, _dumpIndexMap, out double rotVel))
                            rotateDeadTimeDelay.addInputData(nowMs, rotVel);
                        break;
                    case ControlType.Force:
                        if (GetJointValue(RotDumpSubscriber.DumpCmd.effort, JOINT_ROTATE, _dumpIndexMap, out double rotEff))
                            rotateDeadTimeDelay.addInputData(nowMs, rotEff);
                        break;
                }

                // 下部走行体
                switch(movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch(trackControlType)
                        {
                            case ControlType.Position:
                                if (GetJointValue(trackSubscriber.TrackCmd.position, JOINT_R_SPROCKET, _trackIndexMap, out double rPos))
                                    rightSprocketDeadTimeDelay.addInputData(nowMs, rPos);
                                if (GetJointValue(trackSubscriber.TrackCmd.position, JOINT_L_SPROCKET, _trackIndexMap, out double lPos))
                                    leftSprocketDeadTimeDelay.addInputData(nowMs, lPos);
                                break;
                            case ControlType.Speed:
                                if (GetJointValue(trackSubscriber.TrackCmd.velocity, JOINT_R_SPROCKET, _trackIndexMap, out double rVel))
                                    rightSprocketDeadTimeDelay.addInputData(nowMs, rVel);
                                if (GetJointValue(trackSubscriber.TrackCmd.velocity, JOINT_L_SPROCKET, _trackIndexMap, out double lVel))
                                    leftSprocketDeadTimeDelay.addInputData(nowMs, lVel);
                                break;
                            case ControlType.Force:
                                if (GetJointValue(trackSubscriber.TrackCmd.effort, JOINT_R_SPROCKET, _trackIndexMap, out double rEff))
                                    rightSprocketDeadTimeDelay.addInputData(nowMs, rEff);
                                if (GetJointValue(trackSubscriber.TrackCmd.effort, JOINT_L_SPROCKET, _trackIndexMap, out double lEff))
                                    leftSprocketDeadTimeDelay.addInputData(nowMs, lEff);
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        if (trackSubscriber.VelocityCmd != null)
                        {
                            trackModuleDeadTimeDelay.addInputData(nowMs, trackSubscriber.VelocityCmd);
                        }
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        if (trackSubscriber.VolumeCmd != null)
                        {
                            trackVolumeCmdDeadTimeDelay.addInputData(nowMs, trackSubscriber.VolumeCmd);
                        }
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

                if (joints.rotateJointEnabled)
                {
                    joints.rotate_joint.controlType = ControlType.Position;
                    joints.rotate_joint.controlValue = joints.dump_joint.CurrentPosition;                    
                }
            }
            else
            {
                double currentTimeMs = (Time.fixedTimeAsDouble - Time.fixedDeltaTime) * 1000.0;

                // ベッセル角度
                double currentVesselPos = joints.dump_joint.CurrentPosition;

                ControlType dumpControlType = GetDumpControlType();
                switch (dumpControlType)
                {
                    case ControlType.Position:
                        if (GetEffectiveJointValue(currentTimeMs, JOINT_DUMP, dumpControlType, _dumpIndexMap, out double dumpPos))
                        {
                            ApplyDumpCommand(ControlType.Position, dumpPos, currentVesselPos, "ROS");
                        }
                        break;
                    case ControlType.Speed:
                        if (GetEffectiveJointValue(currentTimeMs, JOINT_DUMP, dumpControlType, _dumpIndexMap, out double dumpVel))
                        {
                            ApplyDumpCommand(ControlType.Speed, dumpVel, currentVesselPos, "ROS");
                        }
                        break;
                    case ControlType.Force:
                        if (GetEffectiveJointValue(currentTimeMs, JOINT_DUMP, dumpControlType, _dumpIndexMap, out double dumpEff))
                        {
                            ApplyDumpCommand(ControlType.Force, dumpEff, currentVesselPos, "ROS");
                        }
                        break;
                }

                // 荷台回転
                if (joints.rotateJointEnabled)
                {
                    switch (rotateControlType)
                    {
                        case ControlType.Position:
                            if (GetEffectiveJointValue(currentTimeMs, JOINT_ROTATE, rotateControlType, _dumpIndexMap, out double rotPos))
                            {
                                joints.rotate_joint.controlType = ControlType.Position;
                                joints.rotate_joint.controlValue = rotPos;
                            }
                            break;
                        case ControlType.Speed:
                            if (GetEffectiveJointValue(currentTimeMs, JOINT_ROTATE, rotateControlType, _dumpIndexMap, out double rotVel))
                            {
                                joints.rotate_joint.controlType = ControlType.Speed;
                                joints.rotate_joint.controlValue = rotVel;
                            }
                            break;
                        case ControlType.Force:
                            if (GetEffectiveJointValue(currentTimeMs, JOINT_ROTATE, rotateControlType, _dumpIndexMap, out double rotEff))
                            {
                                joints.rotate_joint.controlType = ControlType.Force;
                                joints.rotate_joint.controlValue = rotEff;
                            }
                            break;
                    }
                }                

                // 下部走行体
                switch (movementControlType)
                {
                    case ConstractionMovementControlType.ActuatorCommand:
                        switch (trackControlType)
                        {
                            case ControlType.Position:
                                joints.rightSprocket.controlType = ControlType.Position;
                                joints.leftSprocket.controlType = ControlType.Position;

                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_R_SPROCKET, trackControlType, out double rPos))
                                    joints.rightSprocket.controlValue = rPos;
                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_L_SPROCKET, trackControlType, out double lPos))
                                    joints.leftSprocket.controlValue = lPos;
                                break;
                            case ControlType.Speed:
                                joints.rightSprocket.controlType = ControlType.Speed;
                                joints.leftSprocket.controlType = ControlType.Speed;

                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_R_SPROCKET, trackControlType, out double rVel))
                                    joints.rightSprocket.controlValue = rVel;
                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_L_SPROCKET, trackControlType, out double lVel))
                                    joints.leftSprocket.controlValue = lVel;
                                break;
                            case ControlType.Force:
                                joints.rightSprocket.controlType = ControlType.Force;
                                joints.leftSprocket.controlType = ControlType.Force;

                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_R_SPROCKET, trackControlType, out double rEff))
                                    joints.rightSprocket.controlValue = rEff;
                                if (GetEffectiveTrackValue(currentTimeMs, JOINT_L_SPROCKET, trackControlType, out double lEff))
                                    joints.leftSprocket.controlValue = lEff;
                                break;
                        }
                        break;
                    case ConstractionMovementControlType.TwistCommand:
                        joints.leftSprocket.controlType = ControlType.Speed;
                        joints.leftSprocket.controlType = ControlType.Speed;

                        if (joints.activateDeadTime)
                        {
                            if (trackModuleDeadTimeDelay.drainInputDataLatest(currentTimeMs, out TwistMsg cmdVel))
                            {
                                twistCommandConvertor.SetCommand(cmdVel.linear, cmdVel.angular);
                                joints.leftSprocket.controlValue = twistCommandConvertor.sprocketSpeed_L;
                                joints.rightSprocket.controlValue = twistCommandConvertor.sprocketSpeed_R;
                            }
                        }
                        else
                        {
                            if (trackSubscriber.VelocityCmd != null)
                            {
                                twistCommandConvertor.SetCommand(trackSubscriber.VelocityCmd.linear, trackSubscriber.VelocityCmd.angular);
                                joints.leftSprocket.controlValue = twistCommandConvertor.sprocketSpeed_L;
                                joints.rightSprocket.controlValue = twistCommandConvertor.sprocketSpeed_R;
                            }
                        }
                        break;
                    case ConstractionMovementControlType.VolumeCommand:
                        joints.leftSprocket.controlType = ControlType.Speed;
                        joints.leftSprocket.controlType = ControlType.Speed;

                        if (joints.activateDeadTime)
                        {
                            if (trackVolumeCmdDeadTimeDelay.drainInputDataLatest(currentTimeMs, out JointCmdMsg cmdVel))
                            {
                                if (GetJointValue(cmdVel.effort, FORWARD_VOLUME, _volumeIndexMap, out double forwardValue) &&
                                    GetJointValue(cmdVel.effort, TURN_VOLUME, _volumeIndexMap, out double turnValue))
                                {
                                    volumeCommandConvertor.SetCommand(forwardValue, turnValue);
                                    joints.leftSprocket.controlValue = twistCommandConvertor.sprocketSpeed_L;
                                    joints.rightSprocket.controlValue = twistCommandConvertor.sprocketSpeed_R;

                                }
                            }
                        }
                        else
                        {
                            if (trackSubscriber.VolumeCmd != null)
                            {
                                if (GetJointValue(trackSubscriber.VolumeCmd.effort, FORWARD_VOLUME, _volumeIndexMap, out double forwardValue) &&
                                    GetJointValue(trackSubscriber.VolumeCmd.effort, TURN_VOLUME, _volumeIndexMap, out double turnValue))
                                {
                                    volumeCommandConvertor.SetCommand(forwardValue, turnValue);
                                    joints.leftSprocket.controlValue = volumeCommandConvertor.twistCommandConvertor.sprocketSpeed_L;
                                    joints.rightSprocket.controlValue = volumeCommandConvertor.twistCommandConvertor.sprocketSpeed_R;
                                }
                            }
                        }
                        break;
                    
                }
            }
        }

        private void BuildJointIndexMap()
        {
            _dumpIndexMap.Clear();
            var dumpMsg = RotDumpSubscriber.DumpCmd;
            if (dumpMsg.joint_name != null)
            {
                for (int i = 0; i < dumpMsg.joint_name.Length; i++)
                {
                    var name = dumpMsg.joint_name[i];
                    if (!string.IsNullOrEmpty(name) && !_dumpIndexMap.ContainsKey(name))
                        _dumpIndexMap.Add(name, i);
                }
            }

            _trackIndexMap.Clear();
            var trackMsg = trackSubscriber.TrackCmd;
            if (trackMsg.joint_name != null)
            {
                for (int i = 0; i < trackMsg.joint_name.Length; i++)
                {
                    var name = trackMsg.joint_name[i];
                    if (!string.IsNullOrEmpty(name) && !_trackIndexMap.ContainsKey(name))
                        _trackIndexMap.Add(name, i);
                }
            }

            _volumeIndexMap.Clear();
            var volumeMsg = trackSubscriber.TrackCmd;
            if (volumeMsg.joint_name != null)
            {
                for (int i = 0; i < volumeMsg.joint_name.Length; i++)
                {
                    var name = volumeMsg.joint_name[i];
                    if (!string.IsNullOrEmpty(name) && !_volumeIndexMap.ContainsKey(name))
                        _volumeIndexMap.Add(name, i);
                }
            }
        }

        private static bool GetJointValue(double[] arr, string jointName, Dictionary<string, int> map, out double value)
        {
            value = 0.0;
            if (arr == null || map == null) return false;
            if (map.TryGetValue(jointName, out int idx))
            {
                if (idx >= 0 && idx < arr.Length)
                {
                    value = arr[idx];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// JointCmd.control_type: 0=position, 1=velocity, 2=effort.
        /// Unknown values retain the Inspector setting for backward compatibility.
        /// </summary>
        private ControlType GetDumpControlType()
        {
            JointCmdMsg cmd = RotDumpSubscriber != null ? RotDumpSubscriber.DumpCmd : null;
            if (cmd == null)
                return vesselControlType;

            switch (cmd.control_type)
            {
                case POSITION_CONTROL:
                    return ControlType.Position;
                case SPEED_CONTROL:
                    return ControlType.Speed;
                case FORCE_CONTROL:
                    return ControlType.Force;
                default:
                    return vesselControlType;
            }
        }

        private void ApplyDumpCommand(ControlType requestedType, double requestedValue, double currentPosition, string source)
        {
            ControlType appliedType = requestedType;
            double appliedValue = requestedValue;

            // The IC120 position command is driven through the tuned vessel speed controller.
            // Legacy scene objects without that component use AGX position control directly.
            if (requestedType == ControlType.Position && vesselStateController != null)
            {
                appliedType = ControlType.Speed;
                appliedValue = vesselStateController.computeAngularVelocity(currentPosition, requestedValue);
            }

            joints.dump_joint.controlType = appliedType;
            joints.dump_joint.controlValue = appliedValue;

            // Physics Stop/Start 後はAGX側のコントローラ値がリセットされる場合がある。
            // 指令値が前回と同じでも、ダンプ制御値はネイティブ側へ再適用する。
            joints.dump_joint.ReapplyControlValue();

            if (logDumpCommandDiagnostics && Time.unscaledTime >= _nextDumpLogTime)
            {
                _nextDumpLogTime = Time.unscaledTime + 1.0f;
                int index = _dumpIndexMap.TryGetValue(JOINT_DUMP, out int mappedIndex) ? mappedIndex : -1;
                byte messageControlType = RotDumpSubscriber != null && RotDumpSubscriber.DumpCmd != null
                    ? RotDumpSubscriber.DumpCmd.control_type
                    : byte.MaxValue;
                bool nativeEnabled = joints.dump_joint.constraint != null &&
                                     joints.dump_joint.constraint.Native != null &&
                                     joints.dump_joint.constraint.Native.getEnable();

                Debug.Log(
                    $"[DumpTruckInput] source={source}, joint={JOINT_DUMP}, index={index}, " +
                    $"control_type={messageControlType}, requestedType={requestedType}, target={requestedValue:F6}, " +
                    $"current={currentPosition:F6}, speed={joints.dump_joint.CurrentSpeed:F6}, " +
                    $"force={joints.dump_joint.CurrentForce:F3}, maxForce={joints.dump_joint.controlMaxForce:F3}, " +
                    $"nativeEnabled={nativeEnabled}, appliedType={appliedType}, appliedValue={appliedValue:F6}",
                    this
                );
            }
        }

        public void ApplyManualDumpSpeed(double speed)
        {
            if (joints == null)
                joints = dumpTruckJoint != null ? dumpTruckJoint : GetComponent<DumpTruckJoint>();

            if (joints != null && joints.dump_joint != null)
                ApplyDumpCommand(ControlType.Speed, speed, joints.dump_joint.CurrentPosition, "Manual");
        }

        private bool GetEffectiveJointValue(double nowMs, string jointName, ControlType mode, Dictionary<string, int> map, out double jointValue)
        {
            jointValue = 0.0;

            if (joints.activateDeadTime)
            {
                if (jointName == JOINT_DUMP)
                {
                    return dumpDeadTimeDelay.drainInputDataLatest(nowMs, out jointValue);
                }
                if (jointName == JOINT_ROTATE)
                {
                    return rotateDeadTimeDelay.drainInputDataLatest(nowMs, out jointValue);
                }
                return false;
            }
            else
            {
                var cmd = RotDumpSubscriber.DumpCmd;
                if (cmd == null) return false;

                switch (mode)
                {
                    case ControlType.Position:
                        return GetJointValue(cmd.position, jointName, map, out jointValue);
                    case ControlType.Speed:
                        return GetJointValue(cmd.velocity, jointName, map, out jointValue);
                    case ControlType.Force:
                        return GetJointValue(cmd.effort, jointName, map, out jointValue);
                }
                return false;
            }
        }

        private bool GetEffectiveTrackValue(double nowMs, string jointName, ControlType mode, out double value)
        {
            value = 0.0;

            if (joints.activateDeadTime)
            {
                if (jointName == JOINT_R_SPROCKET)
                    return rightSprocketDeadTimeDelay.drainInputDataLatest(nowMs, out value);
                if (jointName == JOINT_L_SPROCKET)
                    return leftSprocketDeadTimeDelay.drainInputDataLatest(nowMs, out value);
                return false;
            }
            else
            {
                var cmd = trackSubscriber.TrackCmd;
                if (cmd == null) return false;

                switch (mode)
                {
                    case ControlType.Position:
                        return GetJointValue(cmd.position, jointName, _trackIndexMap, out value);
                    case ControlType.Speed:
                        return GetJointValue(cmd.velocity, jointName, _trackIndexMap, out value);
                    case ControlType.Force:
                        return GetJointValue(cmd.effort, jointName, _trackIndexMap, out value);
                }
                return false;
            }
        }
    }
}

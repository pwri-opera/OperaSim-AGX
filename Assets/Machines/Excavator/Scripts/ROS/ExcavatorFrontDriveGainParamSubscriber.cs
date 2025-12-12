using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;        // Float64MultiArrayMsg
using AGXUnity;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// ROS2 から std_msgs/msg/Float64MultiArray を購読し、
/// Inspector で指定した ArticulationBody 群の xDrive.stiffness / damping を更新する。
/// data[0] = stiffness, data[1] = damping を想定。
/// </summary>

namespace PWRISimulator.ROS
{
    public class ExcavatorFrontDriveGainParamSubscriber : MessageSubscriptionBase
    {

        // [Header("ROS Settings")]
        // [Tooltip("購読する ROS2 トピック名。例: /drive_gains")]
        // public string topicName = "/drive_gains";

        [Header("Target Joints")]
        [Tooltip("stiffness / damping を適用する対象 ConstraintControl 群。")]
        [SerializeField] public ConstraintControl[] actuator;

        [Header("Debug")]
        [Tooltip("受信した値を Debug.Log で表示するか。")]
        public bool verbose = false;

        Float64MultiArrayMsg prevMsg = new();
        Float64MultiArrayMsg frontParamCmd = new();

        public Float64MultiArrayMsg FrontParamCmd 
        {
            get => frontParamCmd;
            private set => frontParamCmd = value;
        }

        readonly string FrontParamCmdPhrase = "/unity/drive_params";

        protected override void CreateSubscriptions()
        {
            if (actuator == null) return;
            foreach (var ac in actuator)
            {
                if (ac == null) continue;
                ac.Initialize();
            }
            // Debug.Log($"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] :44");
            // string machineName = gameObject.name;
            string machineName = "zx200";
            AddSubscriptionHandler<Float64MultiArrayMsg>($"/{machineName}{FrontParamCmdPhrase}", msg => FrontParamCmd = msg);
        }


        void FixedUpdate()
        {
            double currentTime = Time.fixedTimeAsDouble - Time.fixedDeltaTime;
            this.ExecuteSubscriptionHandlerActions(currentTime);

            var msg = this.FrontParamCmd;
            Debug.Log($"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] : {msg.data.Length}");
            // Debug.Log($"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] :53");

            if (msg == null || msg.data == null || msg.data.Length < 2 || msg == prevMsg )
            {
                Debug.LogWarning(
                    $"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] Received message but data length < 2. Ignored.");
                return;
            }
            else
            {
                Debug.Log($"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] Applying Params...");
                ApplyDriveGains(msg.data[0],  msg.data[1]);
                prevMsg = msg;
            }


        }


        private void ApplyDriveGains(double complianceIn, double dampingIn)
        {
            double c = complianceIn;
            double d = dampingIn;

            if (actuator == null) return;

            foreach (var ac in actuator)
            {
                if (ac == null) continue;

                double curr_c;
                double curr_d;

                (curr_c, curr_d) = ac.UpdateComplianceDamping(c, d);

                if (verbose)
                {
                    Debug.Log($"[{nameof(ExcavatorFrontDriveGainParamSubscriber)}] Applying compliance={curr_c}, damping={curr_d} to {actuator?.Length ?? 0} joints.");
                }                
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using agxDriveTrain;
using RosMessageTypes.Com3;
using RosMessageTypes.Sensor;
using Unity.Mathematics;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧ショベルのjoint_state
    /// joint[6]
    /// bucket_joint, arm_joint, boom_joint, swing_joint,
    /// right_track_joint, left_track_joint
    ///</summary>
    public class ExcavatorJointStatePublisher : JointStatePublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] ExcavatorJoints excavatorJoint;
        [SerializeField] string frameId = "";
        readonly string[] joint_name = {"swing_joint", "boom_joint", "arm_joint", "bucket_joint", "right_track", "left_track"};

        // ラジアン値を -π～+π に正規化するヘルパー ---
        static double NormalizeRadians(double angle)
        {
            // angle を (-π, +π] に畳み込み
            double twoPi = 2.0 * System.Math.PI;
            double a = (angle + System.Math.PI) % twoPi;
            if (a < 0) a += twoPi;
            return a - System.Math.PI;
        }

        override protected void DoUpdate()
        {
            jointStateMsg.position[0] = excavatorJoint.swing.JointCurrentPosition;
            jointStateMsg.position[0] = NormalizeRadians(jointStateMsg.position[0]);   // 正規化
            jointStateMsg.velocity[0] = excavatorJoint.swing.JointCurrentSpeed;
            jointStateMsg.effort[0]   = excavatorJoint.swing.JointCurrentForce;

            jointStateMsg.position[1] = excavatorJoint.boomTilt.JointCurrentPosition;
            jointStateMsg.velocity[1] = excavatorJoint.boomTilt.JointCurrentSpeed;
            jointStateMsg.effort[1]   = excavatorJoint.boomTilt.JointCurrentForce;

            jointStateMsg.position[2] = excavatorJoint.armTilt.JointCurrentPosition;
            jointStateMsg.velocity[2] = excavatorJoint.armTilt.JointCurrentSpeed;
            jointStateMsg.effort[2]   = excavatorJoint.armTilt.JointCurrentForce;

            jointStateMsg.position[3] = excavatorJoint.bucketTilt.JointCurrentPosition;
            jointStateMsg.velocity[3] = excavatorJoint.bucketTilt.JointCurrentSpeed;
            jointStateMsg.effort[3]   = excavatorJoint.bucketTilt.JointCurrentForce;

            jointStateMsg.position[4] = excavatorJoint.rightSprocket.JointCurrentPosition;
            jointStateMsg.velocity[4] = excavatorJoint.rightSprocket.JointCurrentSpeed;
            jointStateMsg.effort[4]   = excavatorJoint.rightSprocket.JointCurrentForce;
            
            jointStateMsg.position[5] = excavatorJoint.leftSprocket.JointCurrentPosition;
            jointStateMsg.velocity[5] = excavatorJoint.leftSprocket.JointCurrentSpeed;
            jointStateMsg.effort[5]   = excavatorJoint.leftSprocket.JointCurrentForce;

            jointStateMsg.header = MessageUtil.ToHeadermessage(Time.fixedTimeAsDouble, frameId);
        }
        override protected string MachineName() 
        {
            return this.gameObject.name;
        }

        override protected string TopicPhrase()
        {
            return "/joint_states";
        }

        override protected uint Frequency()
        {
            return frequency;
        }

        protected override uint NumberOfJoints()
        {
            return 6;
        }

        protected override string[] JointNames()
        {
            return joint_name;
        }
    }
}

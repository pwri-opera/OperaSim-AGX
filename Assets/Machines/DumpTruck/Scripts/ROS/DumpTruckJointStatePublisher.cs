using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ダンプトラックのjoint_state
    /// joint[4]
    /// rotate_joint, dump_joint, right_track_joint, left_track_joint
    /// ic120は上部が旋回しないため、rotate_jointには値が出力されない
    /// </summary>
    public class DumpTruckJointStatePublisher : JointStatePublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] DumpTruckJoint dumpTruckJoint;
        [SerializeField] string frameId = "world";
        readonly string[] joint_name = {"rotate_joint", "dump_joint", "right_track_joint", "left_track_joint"};

        protected override void DoUpdate()
        {
            /// <remarks>
            /// ic120ではrotate_jointの出力値には意味が無い
            /// </remarks>
            jointStateMsg.position[0] = dumpTruckJoint.rotate_joint.CurrentPosition;
            jointStateMsg.velocity[0] = dumpTruckJoint.rotate_joint.CurrentSpeed;
            jointStateMsg.effort[0]   = dumpTruckJoint.rotate_joint.CurrentForce;
            jointStateMsg.position[1] = dumpTruckJoint.dump_joint.CurrentPosition;
            jointStateMsg.velocity[1] = dumpTruckJoint.dump_joint.CurrentSpeed;
            jointStateMsg.effort[1]   = dumpTruckJoint.dump_joint.CurrentForce;
            jointStateMsg.position[2] = dumpTruckJoint.rightSprocket.CurrentPosition;
            jointStateMsg.velocity[2] = dumpTruckJoint.rightSprocket.CurrentSpeed;
            jointStateMsg.effort[2]   = dumpTruckJoint.rightSprocket.CurrentForce;
            jointStateMsg.position[3] = dumpTruckJoint.leftSprocket.CurrentPosition;
            jointStateMsg.velocity[3] = dumpTruckJoint.leftSprocket.CurrentSpeed;
            jointStateMsg.effort[3]   = dumpTruckJoint.leftSprocket.CurrentForce;

            jointStateMsg.header = MessageUtil.ToHeadermessage(Time.fixedTimeAsDouble, frameId);
        }

        protected override string MachineName()
        {
            return this.gameObject.name;
        }

        protected override string TopicPhrase()
        {
            return "/joint_states";
        }

        protected override uint Frequency()
        {
            return frequency;
        }

        protected override uint NumberOfJoints()
        {
            return 4;
        }

        protected override string[] JointNames()
        {
            return joint_name;
        }
    }
}

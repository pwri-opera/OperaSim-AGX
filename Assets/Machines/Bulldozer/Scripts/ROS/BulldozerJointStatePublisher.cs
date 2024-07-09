using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ブルドーザのjoint_state
    /// joint[5]
    /// lift_joint, tilt_joint, angle_joint
    /// right_track, left_track
    /// </summary>
    public class BulldozerJointStatePublisher : JointStatePublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] BulldozerJoints bulldozerJoint;
        [SerializeField] string frameId = "";
        readonly string[] joint_name = {"lift_joint", "tilt_joint", "angle_joint", "right_track", "left_track"};
        protected override void DoUpdate()
        {
            jointStateMsg.position[0] = bulldozerJoint.bladeLift.CurrentPosition;
            jointStateMsg.velocity[0] = bulldozerJoint.bladeLift.CurrentSpeed;
            jointStateMsg.effort[0]   = bulldozerJoint.bladeLift.CurrentForce;
            jointStateMsg.position[1] = bulldozerJoint.bladeTilt.CurrentPosition;
            jointStateMsg.velocity[1] = bulldozerJoint.bladeTilt.CurrentSpeed;
            jointStateMsg.effort[1]   = bulldozerJoint.bladeTilt.CurrentForce;
            jointStateMsg.position[2] = bulldozerJoint.bladeAngleRight.CurrentPosition;
            jointStateMsg.velocity[2] = bulldozerJoint.bladeAngleRight.CurrentSpeed;
            jointStateMsg.effort[2]   = bulldozerJoint.bladeAngleRight.CurrentForce;
            jointStateMsg.position[3] = bulldozerJoint.rightSprocket.CurrentPosition;
            jointStateMsg.velocity[3] = bulldozerJoint.rightSprocket.CurrentSpeed;
            jointStateMsg.effort[3]   = bulldozerJoint.rightSprocket.CurrentForce;
            jointStateMsg.position[4] = bulldozerJoint.leftSprocket.CurrentPosition;
            jointStateMsg.velocity[4] = bulldozerJoint.leftSprocket.CurrentSpeed;
            jointStateMsg.effort[4]   = bulldozerJoint.leftSprocket.CurrentForce;

            jointStateMsg.header = MessageUtil.ToHeadermessage(Time.fixedTimeAsDouble, frameId);
        }
        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            return "/joint_state";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
        protected override uint NumberOfJoints()
        {
            return 5;
        }
        protected override string[] JointNames()
        {
            return joint_name;
        }
    }
}

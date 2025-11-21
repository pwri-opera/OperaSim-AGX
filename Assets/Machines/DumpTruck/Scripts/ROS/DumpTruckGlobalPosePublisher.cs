using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// クローラダンプのglobal_pose
    /// グローバル座標系における車両の中心位置および速度
    /// </summary>
    public class DumpTruckGlobalPosePublisher : OdometryPublisher
    {
        [SerializeField] DumpTruckJoint dumptruck;
        [SerializeField] uint frequency = 60;
        private double previousTime = 0;
        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            if (time > 0 && deltaTime > 0)
            {
                GameObject trackLink = dumptruck.gameObject.GetComponentInChildren<AGXUnity.Model.Track>().gameObject;
                MessageUtil.UpdateTimeMsg(odometryMsg.header.stamp, time);

                odometryMsg.header.frame_id="world";
                odometryMsg.child_frame_id=$"{MachineName()}_tf/base_link";
                odometryMsg.pose.pose.position = trackLink.transform.position.To<FLU>();
                odometryMsg.pose.pose.orientation = trackLink.transform.rotation.To<FLU>();
            }
        }

        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            //return "/base_link/pose";
            return "/global_pose";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
    }
}

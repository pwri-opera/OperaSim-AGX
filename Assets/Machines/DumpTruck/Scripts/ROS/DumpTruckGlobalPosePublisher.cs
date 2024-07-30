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
                MessageUtil.UpdateTimeMsg(odometryMsg.header.stamp, time);
                odometryMsg.pose.pose = new RosMessageTypes.Geometry.PoseMsg
                {
                    position = dumptruck.transform.position.To<FLU>(),
                    orientation = dumptruck.transform.rotation.To<FLU>()
                };
                odometryMsg.header.frame_id="world";
                odometryMsg.child_frame_id="ic120_tf/base_link";
                double seconds = Math.Floor(time);
                previousTime = time;
            }
        }

        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            //return "/base_link/pose";
            return "/tracking/ground_truth";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
    }
}

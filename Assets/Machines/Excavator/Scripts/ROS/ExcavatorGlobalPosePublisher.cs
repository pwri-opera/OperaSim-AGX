using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧ショベルのglobal_pose
    /// グローバル座標系における車両の中心位置および速度
    /// </summary>
    public class ExcavatorGlobalPosePublisher : OdometryPublisher
    {
        [SerializeField] ExcavatorJoints excavator;
        [SerializeField] uint frequency = 60;
        private double previousTime = 0;

        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            if (time > 0 && deltaTime > 0)
            {
                MessageUtil.UpdateTimeMsg(odometryMsg.header.stamp, time);
                odometryMsg.pose.pose = new PoseMsg
                {
                    position = excavator.transform.position.To<FLU>(),
                    orientation = excavator.transform.rotation.To<FLU>()
                };

                previousTime = time;
            }
        }

        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            return "/global_pose";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
    }
}

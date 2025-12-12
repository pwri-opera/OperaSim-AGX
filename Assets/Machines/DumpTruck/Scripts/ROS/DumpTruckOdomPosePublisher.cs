using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// クローラダンプのodom_pose
    /// ローカル座標系における車両の中心位置および速度
    /// </summary>
    public class DumpTruckOdomPosePublisher : OdometryPublisher
    {
        [SerializeField] uint frequency = 60;
        [SerializeField] DumpTruckJoint dumpTruck;
        [SerializeField] double tread = 2.0;
        [SerializeField] double wheelRadius = 0.42;
        private double previousTime = 0;
        private const double angularScaleFactor = 0.522;
        private double sumOfYaw = 0;
        protected override void DoUpdate()
        {
            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            if (time > 0 && deltaTime > 0)
            {
                MessageUtil.UpdateTimeMsg(odometryMsg.header.stamp, time);

                double rightVelocity = dumpTruck.rightSprocket.CurrentSpeed * wheelRadius;
                double leftVelocity = dumpTruck.leftSprocket.CurrentSpeed * wheelRadius;
                // 速度
                double v = (rightVelocity + leftVelocity) / 2.0;
                // 角速度
                double w = (rightVelocity - leftVelocity) / tread * angularScaleFactor;

                sumOfYaw += w * deltaTime;
                if (Math.Abs(sumOfYaw) > Math.PI)
                {
                    sumOfYaw -= 2 * Math.PI * Math.Sign(sumOfYaw);
                }

                odometryMsg.pose.pose.position.x += v * Math.Cos(sumOfYaw) * deltaTime;
                odometryMsg.pose.pose.position.y += v * Math.Sin(sumOfYaw) * deltaTime;

                Quaternion quaternion = Quaternion.Euler(0, 0, (float)(sumOfYaw * Mathf.Rad2Deg));
                odometryMsg.pose.pose.orientation.w = quaternion.w;
                odometryMsg.pose.pose.orientation.x = quaternion.x;
                odometryMsg.pose.pose.orientation.y = quaternion.y;
                odometryMsg.pose.pose.orientation.z = quaternion.z;
                odometryMsg.header.frame_id=$"{MachineName()}_tf/odom";
                odometryMsg.child_frame_id=$"{MachineName()}_tf/base_link";


                previousTime = time;
            }
        }
        protected override string MachineName()
        {
            return this.gameObject.name;
        }
        protected override string TopicPhrase()
        {
            return "/odom_pose";
        }
        protected override uint Frequency()
        {
            return frequency;
        }
    }
}

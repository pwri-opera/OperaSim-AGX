using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using RosSharp;
using RosSharp.RosBridgeClient;
using OdomMsg = RosSharp.RosBridgeClient.MessageTypes.Nav.Odometry;

namespace PWRISimulator.ROS
{
    public class OdomPublisher_zx120 : SingleMessagePublisher<OdomMsg>
    {
        public Transform sourceTransform;
        public int frequency = 60;
        public string frameId;
        public Excavator excavator;
    
        OdomMsg message;

        double previousTime = 0;
        Vector3 previousPosition;
        Quaternion previousOrientation;

        private OdomMsg odom;
        private double yaw = 0;

        protected override void Reset()
        {
            base.Reset();
            // �f�t�H���g�ł���Component��GameObject���g�p
            sourceTransform = gameObject.transform;
        }

        protected override void OnAdvertised()
        {
            base.OnAdvertised();
            StartCoroutine(UpdateAndPublishCoroutine());
        }

        protected override void OnUnadvertised()
        {
            base.OnUnadvertised();
            StopCoroutine(nameof(UpdateAndPublishCoroutine));
        }        
        System.Collections.IEnumerator UpdateAndPublishCoroutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1.0f / Math.Max(1, frequency));
                UpdateAndPublishMessage();
            }
        }

        void UpdateAndPublishMessage()
        {
            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            if (time > 0 && deltaTime > 0)
            {

                // static double x=0,y=0,z=0;
                const double Tread = 2.0;
                const double wheel_radius = 0.5*0.840;//calibrated
                double left_vel = excavator.leftSprocket.currentSpeed*wheel_radius;
                double right_vel = excavator.rightSprocket.currentSpeed*wheel_radius;
                double v = (right_vel + left_vel)/2.0;
                double w = (right_vel - left_vel)/(Tread)*0.522;//calibrated
                // double w = (right_vel - left_vel);

                if(odom == null)
                    odom = new OdomMsg();
                
                odom.header.frame_id = "zx120_tf/odom";
                odom.child_frame_id = "zx120_tf/base_link";
                // odom.header.frame_id = "ic120_tf/base_link";
                // odom.child_frame_id = "ic120_tf/odom";

                MessageUtil.UpdateTimeMsg(odom.header.stamp, Time.fixedTimeAsDouble);

                yaw += w * deltaTime;
                if(Math.Abs(yaw) > Math.PI){
                    yaw -= 2*Math.PI*Math.Sign(yaw);
                }

                odom.pose.pose.position.x += v * Math.Cos(yaw) * deltaTime;
                odom.pose.pose.position.y += v * Math.Sin(yaw) * deltaTime;


                // Quaternion rotation = Quaternion.Euler(0, 0, (float)yaw);

                Quaternion rotation = Quaternion.Euler(0, 0, (float)(yaw*180.0/Math.PI));

                odom.pose.pose.orientation.w = rotation.w;
                odom.pose.pose.orientation.x = rotation.x;
                odom.pose.pose.orientation.y = rotation.y;
                odom.pose.pose.orientation.z = rotation.z;
                // odom.pose.pose.orientation.w = sourceTransform.rotation.eulerAngles.y;
                // odom.pose.pose.orientation.x = sourceTransform.rotation.eulerAngles.y-270;
                // odom.pose.pose.orientation.y = sourceTransform.rotation.eulerAngles.y*Math.PI/180.0;
                // odom.pose.pose.orientation.z = yaw/Math.PI*180.0;
                // odom.pose.pose.orientation.w = yaw;
                // odom.pose.pose.orientation.z = rotation.z;

                Publish(odom);

                previousTime = time;
            }
            // odom.pose.x = v
        //     if (sourceTransform == null)
        //         return;

        //     if(message == null)
        //         message = new TwistStampedMsg();

        //     Vector3 linearVelocity, angularVelocity;
        //     if (CalcVelocities(out linearVelocity, out angularVelocity))
        //     {
        //         message.header.frame_id = frameId;
        //         MessageUtil.UpdateTimeMsg(message.header.stamp, Time.fixedTimeAsDouble);
        //         message.twist.linear.x = linearVelocity.x;
        //         message.twist.linear.y = linearVelocity.y;
        //         message.twist.linear.z = linearVelocity.z;
        //         message.twist.angular.x = angularVelocity.x;
        //         message.twist.angular.y = angularVelocity.y;
        //         message.twist.angular.z = angularVelocity.z;

        //         Publish(message);
        //     }
        }
    }
}

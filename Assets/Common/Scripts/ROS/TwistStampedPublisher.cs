using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using RosSharp;
using RosSharp.RosBridgeClient;
using TwistStampedMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.TwistStamped;
using Vector3Msg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Vector3;

namespace PWRISimulator.ROS
{
    public class TwistStampedPublisher : SingleMessagePublisher<TwistStampedMsg>
    {
        public Transform sourceTransform;
        public int frequency = 60;
        public string frameId;

        TwistStampedMsg message;

        double previousTime = 0;
        Vector3 previousPosition;
        Quaternion previousOrientation;

        protected override void Reset()
        {
            base.Reset();
            // デフォルトでこのComponentのGameObjectを使用
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
            if (sourceTransform == null)
                return;

            if(message == null)
                message = new TwistStampedMsg();

            Vector3 linearVelocity, angularVelocity;
            if (CalcVelocities(out linearVelocity, out angularVelocity))
            {
                message.header.frame_id = frameId;
                MessageUtil.UpdateTimeMsg(message.header.stamp, Time.fixedTimeAsDouble);
                message.twist.linear.x = linearVelocity.x;
                message.twist.linear.y = linearVelocity.y;
                message.twist.linear.z = linearVelocity.z;
                message.twist.angular.x = angularVelocity.x;
                message.twist.angular.y = angularVelocity.y;
                message.twist.angular.z = angularVelocity.z;

                Publish(message);
            }
        }

        static Vector3 ShortestEulerAngles(Vector3 eulerAngles)
        {
            return new Vector3(
                Mathf.Abs(eulerAngles.x) <= 180 ? eulerAngles.x : eulerAngles.x - Mathf.Sign(eulerAngles.x) * 360f,
                Mathf.Abs(eulerAngles.y) <= 180 ? eulerAngles.y : eulerAngles.y - Mathf.Sign(eulerAngles.y) * 360f,
                Mathf.Abs(eulerAngles.z) <= 180 ? eulerAngles.z : eulerAngles.z - Mathf.Sign(eulerAngles.z) * 360f);

        }

        bool CalcVelocities(out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            double time = Time.fixedTimeAsDouble;
            double deltaTime = time - previousTime;

            if (time > 0 && deltaTime > 0)
            {
                linearVelocity = (sourceTransform.position - previousPosition).Unity2Ros() / (float)deltaTime;
                Quaternion deltaOrientation = sourceTransform.rotation * Quaternion.Inverse(previousOrientation);
                angularVelocity = ShortestEulerAngles(deltaOrientation.Unity2Ros().eulerAngles) / (float)deltaTime;

                previousTime = time;
                previousPosition = sourceTransform.position;
                previousOrientation = sourceTransform.rotation;
                return true;
            }
            else
            {
                linearVelocity = angularVelocity = Vector3.zero;
                return false;
            }
        }
    }
}

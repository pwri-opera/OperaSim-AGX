using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using RosSharp;
using RosSharp.RosBridgeClient;
using PoseStampedMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.PoseStamped;
using PointMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Point;
using QuaterniontMsg = RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion;

namespace PWRISimulator.ROS
{
    public class PoseStampedPublisher : SingleMessagePublisher<PoseStampedMsg>
    {
        public Transform sourceTransform;
        public int frequency = 60;
        public string frameId;
        public bool urdfRotationCompensation = false;

        PoseStampedMsg message;

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
            if (sourceTransform == null)
                return;

            if(message == null)
                message = new PoseStampedMsg();

            message.header.frame_id = frameId;
            MessageUtil.UpdateTimeMsg(message.header.stamp, Time.fixedTimeAsDouble);
            UpdatePosition(message.pose.position);
            UpdateRotation(message.pose.orientation);

            Publish(message);
        }

        void UpdatePosition(PointMsg positionMsg)
        {
            Vector3 pos = sourceTransform.position.Unity2Ros();
            positionMsg.x = pos.x;
            positionMsg.y = pos.y;
            positionMsg.z = pos.z;
            // positionMsg.x = sourceTransform.position.x;
            // positionMsg.y = sourceTransform.position.y;
            // positionMsg.z = sourceTransform.position.z;
        }

        void UpdateRotation(QuaterniontMsg rotationMsg)
        {
            Quaternion rotation = sourceTransform.rotation;
            if(urdfRotationCompensation)
            {
                rotation = rotation * Quaternion.AngleAxis(90, Vector3.right);
            }
            rotation = rotation.Unity2Ros();
            rotationMsg.x = rotation.x;
            rotationMsg.y = rotation.y;
            rotationMsg.z = rotation.z;
            rotationMsg.w = rotation.w;
            // rotationMsg.x = sourceTransform.rotation.x;
            // rotationMsg.y = sourceTransform.rotation.y;
            // rotationMsg.z = sourceTransform.rotation.z;
            // rotationMsg.w = sourceTransform.rotation.w;
        }
    }
}

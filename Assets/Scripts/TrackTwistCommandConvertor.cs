using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using System;
using AGXUnity.Model;
using UnityEngine.InputSystem.LowLevel;
using RosMessageTypes.Geometry;
using agxDriveTrain;
using UnityEditor.Rendering;

namespace PWRISimulator
{
    public class TrackTwistCommandConvertor : MonoBehaviour
    {
        public GameObject trackLink;
        public GameObject leftTrack;
        public GameObject rightTrack;

        public PIDController speedController;
        public PIDController angularSpeedController;

        private double leftSprocketRadius = 0.25;
        private double rightSprocketRadius = 0.25;
        private double trackWidth = 2.0;

        private Vector3 lastPosition;
        private Vector3 lastRotation;

        public double sprocketSpeed_L { get; private set; }
        public double sprocketSpeed_R { get; private set; }



        private void Start()
        {

            AGXUnity.Model.Track leftTrackModel = leftTrack.GetComponentInChildren<AGXUnity.Model.Track>();
            AGXUnity.Model.Track rightTrackModel = rightTrack.GetComponentInChildren<AGXUnity.Model.Track>();

            if (leftTrackModel == null || leftTrackModel == null)
            {
                Debug.LogWarning("Track GameObject not Assigned.");
            }
            else
            {
                AGXUnity.Model.TrackWheel left_sp = null;
                AGXUnity.Model.TrackWheel right_sp = null;
                // get sprockets
                for (int i = 0; i < leftTrackModel.Wheels.Length; i++)
                {
                    if (leftTrackModel.Wheels[i].Model == TrackWheelModel.Sprocket)
                    {
                        left_sp = leftTrackModel.Wheels[i];
                        leftSprocketRadius = left_sp.Radius;
                    }
                }

                for (int i = 0; i < rightTrackModel.Wheels.Length; i++)
                {
                    if (rightTrackModel.Wheels[i].Model == TrackWheelModel.Sprocket)
                    {
                        right_sp = rightTrackModel.Wheels[i];
                        rightSprocketRadius = right_sp.Radius;
                    }
                }

                if (left_sp != null && right_sp != null)
                {
                    trackWidth = (left_sp.transform.position - right_sp.transform.position).magnitude;
                }
                else
                {
                    Debug.LogWarning("Could not find sprocket(s).");
                }
            }

            lastPosition = trackLink.transform.position;
            lastRotation = trackLink.transform.rotation.eulerAngles;

        }

        private void FixedUpdate()
        {

        }

        public void SetCommand(Vector3Msg cmd_linear, Vector3Msg cmd_angular)
        {
            // Feedback Control
            //double dt = Time.deltaTime;
            //Vector3 currentRotation = trackLink.transform.rotation.eulerAngles;

            //double currentSpeed = (-trackLink.transform.InverseTransformPoint(lastPosition).z) / dt;
            //double currentRotSpeed = (currentRotation - lastRotation).y * Mathf.Deg2Rad / dt;

            //lastPosition = trackLink.transform.position;
            //lastRotation = currentRotation;

            //double out_speed = speedController.Calculate(cmd_linear.x, currentSpeed, dt);
            //double out_omega = angularSpeedController.Calculate(cmd_angular.z, currentRotSpeed, dt);

            //sprocketSpeed_L = (out_speed - trackWidth * 0.5 * out_omega) / leftSprocketRadius;
            //sprocketSpeed_R = (out_speed + trackWidth * 0.5 * out_omega) / rightSprocketRadius;

            // Normal Calculation
            sprocketSpeed_L = (cmd_linear.x - trackWidth * 0.5 * cmd_angular.z) / leftSprocketRadius;
            sprocketSpeed_R = (cmd_linear.x + trackWidth * 0.5 * cmd_angular.z) / rightSprocketRadius;
        }

        public void SetCommand(double cmd_linear, double cmd_angular)
        {
            sprocketSpeed_L = (cmd_linear - trackWidth * 0.5 * cmd_angular) / leftSprocketRadius;
            sprocketSpeed_R = (cmd_linear + trackWidth * 0.5 * cmd_angular) / rightSprocketRadius;
        }
    }
}

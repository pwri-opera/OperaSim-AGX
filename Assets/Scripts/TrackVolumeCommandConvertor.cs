using RosMessageTypes.Geometry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator
{
    public class TrackVolumeCommandConvertor : MonoBehaviour
    {
        [SerializeField] public double maxVelocity = 3.05; // [m/s]
        [SerializeField] public double maxAngularVelocity = 1.0;// [rad/s]

        public TrackTwistCommandConvertor twistCommandConvertor;

        public void SetCommand(double forward, double turn)
        {
            twistCommandConvertor.SetCommand(forward * maxVelocity, turn * maxAngularVelocity);
        }
    }
}

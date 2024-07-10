using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator
{
    [Serializable]
    public class PIDController
    {
        public double Kp = 0.1;
        public double Ki = 0.01;
        public double Kd = 0.05;

        private double err_prev=0, err_integral=0;

        public double Calculate(double targetVal, double currentVal, double dt)
        {
            double error = targetVal - currentVal;
            err_integral+= error * dt;
            double derivative = (error - err_prev) / dt;
            // PID
            //double output = Kp * error + Ki * err_integral + Kd * derivative;
            
            // PD
            double output = Kp * error + Kd * derivative;
            err_prev = error;
            return output;
        }
    }
}

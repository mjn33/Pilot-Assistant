using System;
using UnityEngine;

namespace PilotAssistant.PID
{
    using Utility;

    public class PID_Tuning
    {
        private double kp;
        private double ki;
        private double kd;

        private double inMin = -1000000000; // Minimum input value
        private double inMax = 1000000000; // Maximum input value

        private double outMin; // Minimum output value
        private double outMax; // Maximum output value

        private double integralClampUpper; // AIW clamp
        private double integralClampLower; // AIW clamp

        private double scale = 1;
        private double easing = 1;

        public PID_Tuning(
            double kp, double ki, double kd, double outMin, double outMax, double integralClampLower,
            double integralClampUpper, double scale = 1, double easing = 1)
        {
            this.kp = kp;
            this.ki = ki;
            this.kd = kd;
            this.outMin = outMin;
            this.outMax = outMax;
            this.integralClampLower = integralClampLower;
            this.integralClampUpper = integralClampUpper;
            this.scale = scale;
            this.easing = easing;
        }

        // Copy constructor
        public PID_Tuning(PID_Tuning other)
        {
            this.kp = other.kp;
            this.ki = other.ki;
            this.kd = other.kd;
            this.outMin = other.outMin;
            this.outMax = other.outMax;
            this.integralClampLower = other.integralClampLower;
            this.integralClampUpper = other.integralClampUpper;
            this.scale = other.scale;
            this.easing = other.easing;
        }

        public double PGain
        {
            get { return kp; }
            set { kp = value; }
        }

        public double IGain
        {
            get { return ki; }
            set { ki = value; }
        }

        public double DGain
        {
            get { return kd; }
            set { kd = value; }
        }

        public double InMin
        {
            get { return inMin; }
            set { inMin = value; }
        }

        public double InMax
        {
            get { return inMax; }
            set { inMax = value; }
        }

        /// <summary>
        /// Set output minimum to value
        /// </summary>
        public double OutMin
        {
            get { return outMin; }
            set { outMin = value; }
        }

        /// <summary>
        /// Set output maximum to value
        /// </summary>
        public double OutMax
        {
            get { return outMax; }
            set { outMax = value; }
        }

        public double ClampLower
        {
            get { return integralClampLower; }
            set { integralClampLower = value; }
        }

        public double ClampUpper
        {
            get { return integralClampUpper; }
            set { integralClampUpper = value; }
        }

        public double Scale
        {
            get { return scale; }
            set { scale = Math.Max(value, 0.01); }
        }

        public double Easing
        {
            get { return easing; }
            set { easing =  Math.Max(value, 0.01); }
        }
    }
}

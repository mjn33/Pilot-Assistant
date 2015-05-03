using System;
using UnityEngine;

namespace PilotAssistant.PID
{
    using Utility;

    public class PID_Controller
    {
        private double setpoint = 0; // process setpoint

        private Vessel vessel;
        private PID_Tuning tun;

        private double sum = 0; // integral sum
        private double previous = 0; // previous value stored for derivative action
        private double rollingDiff = 0; // used for rolling average difference
        private double rollingFactor = 0.5; // rolling average proportion. 0 = all new, 1 = never changes
        private double error = 0; // error of current iteration

        private double dt = 1; // standardised response for any physics dt

        private bool skipDerivative = false;

        public PID_Controller(Vessel vessel, PID_Tuning tun)
        {
            this.vessel = vessel;
            this.tun = new PID_Tuning(tun);
        }

        public double Response(double input)
        {
            input = Functions.Clamp(input, tun.InMin, tun.InMax);
            dt = TimeWarp.fixedDeltaTime;
            error = input - setpoint;
            if (skipDerivative)
            {
                skipDerivative = false;
                previous = input;
            }
            double response = ProportionalError(error) + IntegralError(error) + DerivativeError(input);
            return Functions.Clamp(response, tun.OutMin, tun.OutMax);
        }

        private double ProportionalError(double input)
        {
            if (tun.PGain == 0)
                return 0;
            return input * tun.PGain / tun.Scale;
        }

        private double IntegralError(double input)
        {
            if (tun.IGain == 0 || vessel.checkLanded() || !vessel.IsControllable)
            {
                sum = 0;
                return sum;
            }

            sum += input * dt * tun.IGain / tun.Scale;
            sum = Functions.Clamp(sum, tun.ClampLower, tun.ClampUpper); // AIW
            return sum;
        }

        private double DerivativeError(double input)
        {
            if (tun.DGain == 0)
                return 0;

            double difference = (input - previous) / dt;
            // rolling average sometimes helps smooth out a jumpy derivative response
            rollingDiff = rollingDiff * rollingFactor + difference * (1 - rollingFactor);

            previous = input;
            return rollingDiff * tun.DGain / tun.Scale;
        }

        public void Clear()
        {
            sum = 0;
        }

        public void SkipDerivative()
        {
            skipDerivative = true;
        }

        public double SetPoint
        {
            get { return setpoint; }
            set { setpoint = value; }
        }

        public PID_Tuning Tuning
        {
            get { return tun; }
        }

        public double RollingFactor
        {
            set { rollingFactor = Functions.Clamp(value, 0, 1); }
        }
    }
}

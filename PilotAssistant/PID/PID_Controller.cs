using System;
using UnityEngine;

namespace PilotAssistant.PID
{
    using Utility;

    public class PID_Controller
    {
        // These are used for BumplessSetPoint
        private double targetSetpoint = 0; // target setpoint
        private double activeSetpoint = 0; // process setpoint
        private double increment = 0;

        private PID_Tuning tun;

        private double sum = 0; // integral sum
        private double previous = 0; // previous value stored for derivative action
        private double rollingDiff = 0; // used for rolling average difference
        private double rollingFactor = 0.5; // rolling average proportion. 0 = all new, 1 = never changes
        private double error = 0; // error of current iteration

        private double dt = 1; // standardised response for any physics dt

        private bool skipDerivative = false;

        public PID_Controller(PID_Tuning tun)
        {
            this.tun = new PID_Tuning(tun);
        }

        public double Response(double input, bool useIntegral)
        {
            if (activeSetpoint != targetSetpoint)
            {
                // Ease in quadratic fashion
                increment += tun.Easing * TimeWarp.fixedDeltaTime * 0.01;
                if (activeSetpoint < targetSetpoint)
                    activeSetpoint = Math.Min(activeSetpoint + increment, targetSetpoint);
                else
                    activeSetpoint = Math.Max(activeSetpoint - increment, targetSetpoint);
            }

            input = Functions.Clamp(input, tun.InMin, tun.InMax);
            dt = TimeWarp.fixedDeltaTime;
            error = input - activeSetpoint;

            if (skipDerivative)
            {
                skipDerivative = false;
                previous = input;
            }
            double pResponse = ProportionalError(error);
            double iResponse = IntegralError(error, useIntegral);
            double dResponse = DerivativeError(input);

            return Functions.Clamp(pResponse + iResponse + dResponse, tun.OutMin, tun.OutMax);
        }

        public double Response(double error, double rate, bool useIntegral)
        {
            // skipDerivative not relevant here
            double pResponse = ProportionalError(error);
            double iResponse = IntegralError(error, useIntegral);
            double dResponse = DerivativeError(rate);

            dResponse = rate * tun.DGain / tun.Scale;

            return Functions.Clamp(pResponse + iResponse + dResponse, tun.OutMin, tun.OutMax);
        }

        private double ProportionalError(double input)
        {
            if (tun.PGain == 0)
                return 0;
            return input * tun.PGain / tun.Scale;
        }

        private double IntegralError(double input, bool useIntegral)
        {
            if (tun.IGain == 0 || !useIntegral)
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
            get { return activeSetpoint; }
            set { activeSetpoint = targetSetpoint = value; }
        }

        // Smoothly transition to this new setpoint
        public virtual double BumplessSetPoint
        {
            get { return activeSetpoint; }
            set { targetSetpoint = value; increment = 0; }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PilotAssistant.Utility
{
    public static class Functions
    {
        /// <summary>
        /// Calculates the angle to feed corrected for 0/360 crossings. For example if the target is 350 and the
        /// current is 10, it will return 370 giving a diff of -20 degrees else you get +ve 340 and the turn is in the
        /// wrong direction.
        /// </summary>
        public static double CalcRelativeAngle(double current, double target)
        {
            if (target - current < -180)
                return current - 360;
            else if (target - current > 180)
                return current + 360;
            else
                return current;
        }

        /// <summary>
        /// Clamp double input between maximum and minimum value
        /// </summary>
        /// <param name="val">variable to be clamped</param>
        /// <param name="min">minimum output value of the variable</param>
        /// <param name="max">maximum output value of the variable</param>
        /// <returns>val clamped between max and min</returns>
        public static double Clamp(double val, double min, double max)
        {
            if (val < min)
                return min;
            else if (val > max)
                return max;
            else
                return val;
        }

        /// <summary>
        /// Linear interpolation between two points
        /// </summary>
        /// <param name="pct">fraction of travel from the minimum to maximum. Can be less than 0 or greater than 1</param>
        /// <param name="lower">reference point treated as the base (pct = 0)</param>
        /// <param name="upper">reference point treated as the target (pct = 1)</param>
        /// <param name="clamp">clamp pct input between 0 and 1?</param>
        /// <returns></returns>
        public static double Lerp(double pct, double lower, double upper, bool clamp = true)
        {
            if (clamp)
            {
                pct = Clamp(pct, 0, 1);
            }
            return (1 - pct) * lower + pct * upper;
        }

        /// <summary>
        /// Checks for player pitch input
        /// </summary>
        public static bool HasPitchInput()
        {
            return InputLockManager.IsUnlocked(ControlTypes.PITCH) &&
                   (GameSettings.PITCH_DOWN.GetKey() || GameSettings.PITCH_UP.GetKey() || !IsAxisNeutral(GameSettings.AXIS_PITCH));
        }

        /// <summary>
        /// Checks for player roll input
        /// </summary>
        public static bool HasRollInput()
        {
            return InputLockManager.IsUnlocked(ControlTypes.ROLL) &&
                   (GameSettings.ROLL_LEFT.GetKey() || GameSettings.ROLL_RIGHT.GetKey() || !IsAxisNeutral(GameSettings.AXIS_ROLL));
        }

        /// <summary>
        /// Checks for player yaw input
        /// </summary>
        public static bool HasYawInput()
        {
            return InputLockManager.IsUnlocked(ControlTypes.YAW) &&
                   (GameSettings.YAW_LEFT.GetKey() || GameSettings.YAW_RIGHT.GetKey() || !IsAxisNeutral(GameSettings.AXIS_YAW));
        }

        /// <summary>
        /// Checks for input on the given axis, ignores very small input
        /// </summary>
        public static bool IsAxisNeutral(AxisBinding axis)
        {
            return axis.IsNeutral() && Math.Abs(axis.GetAxis()) < 0.00001;
        }
    }
}

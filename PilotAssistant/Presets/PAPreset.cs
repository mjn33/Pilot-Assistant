using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PilotAssistant.Presets
{
    using PID;
    /// <summary>
    /// Holds all the PID tuning values for the PilotAssistant controllers.
    /// </summary>
    public class PAPreset
    {
        private string name;
        private PID_Tuning[] tunings = new PID_Tuning[Enum.GetNames(typeof(PIDList)).Length];

        // Create a preset from an array of PID_Tunings
        public PAPreset(string name, PID_Tuning[] tunings)
        {
            this.name = name;
            for (int i = 0; i < tunings.Length; i++)
                this.tunings[i] = new PID_Tuning(tunings[i]);
        }

        // Create a preset using the PID_Tunings from an array of PID_Controllers
        public PAPreset(string name, PID_Controller[] controllers)
        {
            this.name = name;
            for (int i = 0; i < tunings.Length; i++)
                this.tunings[i] = new PID_Tuning(controllers[i].Tuning);
        }

        // Create a preset using values written previous to a ConfigNode
        public PAPreset(ConfigNode node)
        {
            name = node.GetValue("name");
            tunings[(int)PIDList.HdgBank]   = LoadControllerGains(node.GetNode("HdgBankController"));
            tunings[(int)PIDList.HdgYaw]    = LoadControllerGains(node.GetNode("HdgYawController"));
            tunings[(int)PIDList.Aileron]   = LoadControllerGains(node.GetNode("AileronController"));
            tunings[(int)PIDList.Rudder]    = LoadControllerGains(node.GetNode("RudderController"));
            tunings[(int)PIDList.Altitude]  = LoadControllerGains(node.GetNode("AltitudeController"));
            tunings[(int)PIDList.VertSpeed] = LoadControllerGains(node.GetNode("AoAController"));
            tunings[(int)PIDList.Elevator]  = LoadControllerGains(node.GetNode("ElevatorController"));
        }

        private PID_Tuning LoadControllerGains(ConfigNode node)
        {
            double kp, ki, kd, outMin, outMax, clampLower, clampUpper, scale;
            double.TryParse(node.GetValue("PGain"), out kp);
            double.TryParse(node.GetValue("IGain"), out ki);
            double.TryParse(node.GetValue("DGain"), out kd);
            double.TryParse(node.GetValue("OutMin"), out outMin);
            double.TryParse(node.GetValue("OutMax"), out outMax);
            double.TryParse(node.GetValue("ClampLower"), out clampLower);
            double.TryParse(node.GetValue("ClampUpper"), out clampUpper);
            double.TryParse(node.GetValue("Scale"), out scale);
            return new PID_Tuning(kp, ki, kd, outMin, outMax, clampLower, clampUpper, scale);
        }

        private ConfigNode GainsToConfigNode(string name, PID_Tuning tuning)
        {
            ConfigNode node = new ConfigNode(name);
            node.AddValue("PGain", tuning.PGain);
            node.AddValue("IGain", tuning.IGain);
            node.AddValue("DGain", tuning.DGain);
            node.AddValue("OutMin", tuning.OutMin);
            node.AddValue("OutMax", tuning.OutMax);
            node.AddValue("ClampLower", tuning.ClampLower);
            node.AddValue("ClampUpper", tuning.ClampUpper);
            node.AddValue("Scale", tuning.Scale);
            return node;
        }

        public ConfigNode ToConfigNode()
        {
            ConfigNode node = new ConfigNode("PAPreset");
            node.AddValue("name", name);
            node.AddNode(GainsToConfigNode("HdgBankController",  tunings[(int)PIDList.HdgBank]));
            node.AddNode(GainsToConfigNode("HdgYawController",   tunings[(int)PIDList.HdgYaw]));
            node.AddNode(GainsToConfigNode("AileronController",  tunings[(int)PIDList.Aileron]));
            node.AddNode(GainsToConfigNode("RudderController",   tunings[(int)PIDList.Rudder]));
            node.AddNode(GainsToConfigNode("AltitudeController", tunings[(int)PIDList.Altitude]));
            node.AddNode(GainsToConfigNode("AoAController",      tunings[(int)PIDList.VertSpeed]));
            node.AddNode(GainsToConfigNode("ElevatorController", tunings[(int)PIDList.Elevator]));
            return node;
        }

        public void LoadPreset(PID_Controller[] controllers)
        {
            for (int i = 0; i < tunings.Length; i++)
            {
                controllers[i].Tuning.PGain      = tunings[i].PGain;
                controllers[i].Tuning.IGain      = tunings[i].IGain;
                controllers[i].Tuning.DGain      = tunings[i].DGain;
                controllers[i].Tuning.OutMin     = tunings[i].OutMin;
                controllers[i].Tuning.OutMax     = tunings[i].OutMax;
                controllers[i].Tuning.ClampLower = tunings[i].ClampLower;
                controllers[i].Tuning.ClampUpper = tunings[i].ClampUpper;
                controllers[i].Tuning.Scale      = tunings[i].Scale;
            }
        }

        public void Update(PID_Controller[] controllers)
        {
            for (int i = 0; i < tunings.Length; i++)
            {
                tunings[i].PGain      = controllers[i].Tuning.PGain;
                tunings[i].IGain      = controllers[i].Tuning.IGain;
                tunings[i].DGain      = controllers[i].Tuning.DGain;
                tunings[i].OutMin     = controllers[i].Tuning.OutMin;
                tunings[i].OutMax     = controllers[i].Tuning.OutMax;
                tunings[i].ClampLower = controllers[i].Tuning.ClampLower;
                tunings[i].ClampUpper = controllers[i].Tuning.ClampUpper;
                tunings[i].Scale      = controllers[i].Tuning.Scale;
            }
        }

        public string Name
        {
            get { return name; }
        }
    }
}

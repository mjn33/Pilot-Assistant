using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PilotAssistant.Presets
{
    using PID;
    using Utility;

    public class SASPreset
    {
        private string name;
        private PID_Tuning[] tunings = new PID_Tuning[Enum.GetNames(typeof(SASList)).Length];
        private bool isStockPreset = true;

        // Create a preset from an array of PID_Tunings
        public SASPreset(string name, PID_Tuning[] tunings)
        {
            this.name = name;
            isStockPreset = false;
            for (int i = 0; i < tunings.Length; i++)
                this.tunings[i] = new PID_Tuning(tunings[i]);
        }

        // Create a preset using the PID_Tunings from an array of PID_Controllers
        public SASPreset(string name, PID_Controller[] controllers)
        {
            this.name = name;
            isStockPreset = false;
            for (int i = 0; i < tunings.Length; i++)
                this.tunings[i] = new PID_Tuning(controllers[i].Tuning);
        }

        // Create a stock preset from values stored in VesselSAS class
        public SASPreset(string name, VesselAutopilot.VesselSAS sas)
        {
            this.name = name;
            isStockPreset = true;
            // Stock in PID_Tuning, .clamp stored in .Scale property
            tunings[(int)SASList.Pitch] = new PID_Tuning(sas.pidLockedPitch.kp, sas.pidLockedPitch.ki,
                                                         sas.pidLockedPitch.kd, 0, 0, 0, 0, sas.pidLockedPitch.clamp);
            tunings[(int)SASList.Roll]  = new PID_Tuning(sas.pidLockedRoll.kp, sas.pidLockedRoll.ki,
                                                         sas.pidLockedRoll.kd, 0, 0, 0, 0, sas.pidLockedRoll.clamp);
            tunings[(int)SASList.Yaw]   = new PID_Tuning(sas.pidLockedYaw.kp, sas.pidLockedYaw.ki,
                                                         sas.pidLockedYaw.kd, 0, 0, 0, 0, sas.pidLockedYaw.clamp);
        }

        // Create a preset using values written previous to a ConfigNode
        public SASPreset(ConfigNode node)
        {
            name = node.GetValue("name");
            isStockPreset = bool.Parse(node.GetValue("stock"));
            tunings[(int)SASList.Pitch] = LoadControllerGains(node.GetNode("ElevatorController"));
            tunings[(int)SASList.Roll]  = LoadControllerGains(node.GetNode("AileronController"));
            tunings[(int)SASList.Yaw]   = LoadControllerGains(node.GetNode("RudderController"));
        }

        public string Name
        {
            get { return name; }
        }

        public bool IsStockPreset
        {
            get { return isStockPreset; }
        }

        private PID_Tuning LoadControllerGains(ConfigNode node)
        {
            double kp, ki, kd, scale;
            double.TryParse(node.GetValue("PGain"), out kp);
            double.TryParse(node.GetValue("IGain"), out ki);
            double.TryParse(node.GetValue("DGain"), out kd);
            double.TryParse(node.GetValue("Scalar"), out scale);
            return new PID_Tuning(kp, ki, kd, 0, 0, 0, 0, scale);
        }

        private ConfigNode GainsToConfigNode(string name, PID_Tuning tuning)
        {
            ConfigNode node = new ConfigNode(name);
            node.AddValue("PGain", tuning.PGain);
            node.AddValue("IGain", tuning.IGain);
            node.AddValue("DGain", tuning.DGain);
            node.AddValue("Scalar", tuning.Scale);
            return node;
        }

        public ConfigNode ToConfigNode()
        {
            ConfigNode node = new ConfigNode("SASPreset");
            node.AddValue("name", name);
            node.AddValue("stock", isStockPreset);
            node.AddNode(GainsToConfigNode("ElevatorController", tunings[(int)SASList.Pitch]));
            node.AddNode(GainsToConfigNode("AileronController", tunings[(int)SASList.Roll]));
            node.AddNode(GainsToConfigNode("RudderController", tunings[(int)SASList.Yaw]));
            return node;
        }

        public void LoadPreset(PID_Controller[] controllers)
        {
            for (int i = 0; i < tunings.Length; i++)
            {
                controllers[i].Tuning.PGain = tunings[i].PGain;
                controllers[i].Tuning.IGain = tunings[i].IGain;
                controllers[i].Tuning.DGain = tunings[i].DGain;
                controllers[i].Tuning.Scale = tunings[i].Scale;
            }
        }

        public void LoadStockPreset(VesselAutopilot.VesselSAS sas)
        {
            sas.pidLockedPitch.kp    = tunings[(int)SASList.Pitch].PGain;
            sas.pidLockedPitch.ki    = tunings[(int)SASList.Pitch].IGain;
            sas.pidLockedPitch.kd    = tunings[(int)SASList.Pitch].DGain;
            sas.pidLockedPitch.clamp = tunings[(int)SASList.Pitch].Scale;

            sas.pidLockedRoll.kp     = tunings[(int)SASList.Roll].PGain;
            sas.pidLockedRoll.ki     = tunings[(int)SASList.Roll].IGain;
            sas.pidLockedRoll.kd     = tunings[(int)SASList.Roll].DGain;
            sas.pidLockedRoll.clamp  = tunings[(int)SASList.Roll].Scale;

            sas.pidLockedYaw.kp      = tunings[(int)SASList.Yaw].PGain;
            sas.pidLockedYaw.ki      = tunings[(int)SASList.Yaw].IGain;
            sas.pidLockedYaw.kd      = tunings[(int)SASList.Yaw].DGain;
            sas.pidLockedYaw.clamp   = tunings[(int)SASList.Yaw].Scale;
        }

        public void Update(PID_Controller[] controllers)
        {
            for (int i = 0; i < tunings.Length; i++)
            {
                tunings[i].PGain = controllers[i].Tuning.PGain;
                tunings[i].IGain = controllers[i].Tuning.IGain;
                tunings[i].DGain = controllers[i].Tuning.DGain;
                tunings[i].Scale = controllers[i].Tuning.Scale;
            }
        }

        public void UpdateStock(VesselAutopilot.VesselSAS sas)
        {
            tunings[(int)SASList.Pitch].PGain = sas.pidLockedPitch.kp;
            tunings[(int)SASList.Pitch].IGain = sas.pidLockedPitch.ki;
            tunings[(int)SASList.Pitch].DGain = sas.pidLockedPitch.kd;
            tunings[(int)SASList.Pitch].Scale = sas.pidLockedPitch.clamp;

            tunings[(int)SASList.Roll].PGain = sas.pidLockedRoll.kp;
            tunings[(int)SASList.Roll].IGain = sas.pidLockedRoll.ki;
            tunings[(int)SASList.Roll].DGain = sas.pidLockedRoll.kd;
            tunings[(int)SASList.Roll].Scale = sas.pidLockedRoll.clamp;

            tunings[(int)SASList.Yaw].PGain = sas.pidLockedYaw.kp;
            tunings[(int)SASList.Yaw].IGain = sas.pidLockedYaw.ki;
            tunings[(int)SASList.Yaw].DGain = sas.pidLockedYaw.kd;
            tunings[(int)SASList.Yaw].Scale = sas.pidLockedYaw.clamp;
        }
    }
}

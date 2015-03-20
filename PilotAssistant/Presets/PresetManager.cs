using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PilotAssistant.Presets
{
    using UI;
    using Utility;

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class PresetManager : MonoBehaviour
    {
        // Singleton pattern, as opposed to using semi-static classes
        private static PresetManager instance;
        public static PresetManager Instance
        {
            get { return instance; }
        }

        private PAPreset defaultPATuning = null;
        private List<PAPreset> paPresetList = new List<PAPreset>();
        private PAPreset activePAPreset = null;

        private SASPreset defaultSASTuning;
        private SASPreset defaultStockSASTuning;
        private List<SASPreset> sasPresetList = new List<SASPreset>();
        private SASPreset activeSASPreset = null;
        private SASPreset activeStockSASPreset = null;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            LoadPresetsFromFile();
            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            SavePresetsToFile();
        }

        private void LoadPresetsFromFile()
        {
            paPresetList.Clear();
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("PAPreset"))
            {
                if (node == null)
                    continue;

                paPresetList.Add(new PAPreset(node));
            }

            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("SASPreset"))
            {
                if (node == null)
                    continue;

                sasPresetList.Add(new SASPreset(node));
            }
        }

        public void SavePresetsToFile()
        {
            ConfigNode node = new ConfigNode();
            if (paPresetList.Count == 0 && sasPresetList.Count == 0)
                node.AddValue("dummy", "do not delete me");
            else
            {
                foreach (PAPreset p in paPresetList)
                {
                    node.AddNode(p.ToConfigNode());
                }
                foreach (SASPreset p in sasPresetList)
                {
                    node.AddNode(p.ToConfigNode());
                }
            }
            node.Save(KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/Pilot Assistant/Presets.cfg");
        }

        public void InitDefaultStockSASTuning(VesselAutopilot.VesselSAS sas)
        {
            defaultStockSASTuning = new SASPreset(sas, "Stock");
            if (activeStockSASPreset == null)
                activeStockSASPreset = defaultStockSASTuning;
            else if (activeStockSASPreset != defaultStockSASTuning)
                LoadStockSASPreset(sas, activeStockSASPreset);
        }

        public void InitDefaultSASTuning(PID.PID_Controller[] controllers)
        {
            defaultSASTuning = new SASPreset(controllers, "Default");
            if (activeSASPreset == null)
                activeSASPreset = defaultSASTuning;
            else if (activeSASPreset != defaultSASTuning)
                LoadSASPreset(controllers, activeSASPreset);
        }

        public void InitDefaultPATuning(PID.PID_Controller[] controllers)
        {
            defaultPATuning = new PAPreset(controllers, "Default");
            if (activePAPreset == null)
                activePAPreset = defaultPATuning;
            else if (activePAPreset != defaultPATuning)
                LoadPAPreset(controllers, activePAPreset);
        }

        public SASPreset GetActiveStockSASPreset()
        {
            return activeStockSASPreset;
        }

        public SASPreset GetActiveSASPreset()
        {
            return activeSASPreset;
        }

        public PAPreset GetActivePAPreset()
        {
            return activePAPreset;
        }

        public SASPreset GetDefaultStockSASTuning()
        {
            return defaultStockSASTuning;
        }

        public SASPreset GetDefaultSASTuning()
        {
            return defaultSASTuning;
        }

        public PAPreset GetDefaultPATuning()
        {
            return defaultPATuning;
        }

        public void RegisterStockSASPreset(VesselAutopilot.VesselSAS sas, string name)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return;
            }
            foreach (SASPreset p in sasPresetList)
            {
                if (name == p.GetName())
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return;
                }
            }

            SASPreset p2 = new SASPreset(sas, name);
            sasPresetList.Add(p2);
            LoadStockSASPreset(sas, p2);
            SavePresetsToFile();
        }

        public void RegisterSASPreset(PID.PID_Controller[] controllers, string name)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return;
            }
            foreach (SASPreset p in sasPresetList)
            {
                if (name == p.GetName())
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return;
                }
            }

            SASPreset p2 = new SASPreset(controllers, name);
            sasPresetList.Add(p2);
            LoadSASPreset(controllers, p2);
            SavePresetsToFile();
        }

        public void RegisterPAPreset(PID.PID_Controller[] controllers, string name)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return;
            }
            foreach (PAPreset p in paPresetList)
            {
                if (name == p.GetName())
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return;
                }
            }

            PAPreset p2 = new PAPreset(controllers, name);
            paPresetList.Add(p2);
            LoadPAPreset(controllers, p2);
            SavePresetsToFile();
        }

        public void LoadStockSASPreset(VesselAutopilot.VesselSAS sas, SASPreset p)
        {
            activeStockSASPreset = p;
            p.LoadStockPreset(sas);
            GeneralUI.PostMessage("Loaded preset \"" + p.GetName() + "\"");
        }

        public void LoadSASPreset(PID.PID_Controller[] controllers, SASPreset p)
        {
            activeSASPreset = p;
            p.LoadPreset(controllers);
            GeneralUI.PostMessage("Loaded preset \"" + p.GetName() + "\"");
        }

        public void LoadPAPreset(PID.PID_Controller[] controllers, PAPreset p)
        {
            activePAPreset = p;
            p.LoadPreset(controllers);
            GeneralUI.PostMessage("Loaded preset \"" + p.GetName() + "\"");
        }

        public List<SASPreset> GetAllSASPresets()
        {
            // return a shallow copy of the list
            List<SASPreset> l = new List<SASPreset>();
            foreach (SASPreset p in sasPresetList)
            {
                if (!p.IsStockSAS())
                    l.Add(p);
            }
            return l;
        }

        public List<SASPreset> GetAllStockSASPresets()
        {
            List<SASPreset> l = new List<SASPreset>();
            foreach (SASPreset p in sasPresetList)
            {
                if (p.IsStockSAS())
                    l.Add(p);
            }
            return l;
        }

        public List<PAPreset> GetAllPAPresets()
        {
            // return a shallow copy of the list
            return new List<PAPreset>(paPresetList);
        }

        public void RemovePreset(SASPreset p)
        {
            if (p.IsStockSAS())
            {
                if (activeStockSASPreset == p)
                    activeStockSASPreset = null;
                sasPresetList.Remove(p);
                SavePresetsToFile();
            }
            else
            {
                if (activeSASPreset == p)
                    activeSASPreset = null;
                sasPresetList.Remove(p);
                SavePresetsToFile();
            }
        }

        public void RemovePreset(PAPreset p)
        {
            if (activePAPreset == p)
                activePAPreset = null;
            paPresetList.Remove(p);
            SavePresetsToFile();
        }
    }
}

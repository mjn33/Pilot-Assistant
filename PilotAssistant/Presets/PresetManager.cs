using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PilotAssistant.Presets
{
    using PID;
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

        private List<PAPreset> paPresetList = new List<PAPreset>();
        private List<SASPreset> sasPresetList = new List<SASPreset>();

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

        public SASPreset RegisterStockSASPreset(string name, VesselAutopilot.VesselSAS sas)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return null;
            }
            foreach (SASPreset p in sasPresetList)
            {
                if (name == p.Name)
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return null;
                }
            }

            SASPreset p2 = new SASPreset(name, sas);
            sasPresetList.Add(p2);
            SavePresetsToFile();
            return p2;
        }

        public SASPreset RegisterSSASPreset(string name, PID_Controller[] controllers)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return null;
            }
            foreach (SASPreset p in sasPresetList)
            {
                if (name == p.Name)
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return null;
                }
            }

            SASPreset p2 = new SASPreset(name, controllers);
            sasPresetList.Add(p2);
            SavePresetsToFile();
            return p2;
        }

        public PAPreset RegisterPAPreset(string name, PID_Controller[] controllers)
        {
            if (name == "")
            {
                GeneralUI.PostMessage("Failed to add preset with no name.");
                return null;
            }
            foreach (PAPreset p in paPresetList)
            {
                if (name == p.Name)
                {
                    GeneralUI.PostMessage("Failed to add preset with duplicate name.");
                    return null;
                }
            }

            PAPreset p2 = new PAPreset(name, controllers);
            paPresetList.Add(p2);
            SavePresetsToFile();
            return p2;
        }

        public List<SASPreset> GetAllSASPresets()
        {
            // return a shallow copy of the list
            List<SASPreset> l = new List<SASPreset>();
            foreach (SASPreset p in sasPresetList)
            {
                if (!p.IsStockPreset)
                    l.Add(p);
            }
            return l;
        }

        public List<SASPreset> GetAllStockSASPresets()
        {
            List<SASPreset> l = new List<SASPreset>();
            foreach (SASPreset p in sasPresetList)
            {
                if (p.IsStockPreset)
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
            sasPresetList.Remove(p);
            SavePresetsToFile();
        }

        public void RemovePreset(PAPreset p)
        {
            paPresetList.Remove(p);
            SavePresetsToFile();
        }
    }
}

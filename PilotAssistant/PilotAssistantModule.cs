using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant
{
    using Utility;
    using Presets;

    public class PilotAssistantModule : VesselModule
    {
        private FlightData flightData;

        private PilotAssistant pilotAssistant;
        private SurfSAS surfSAS;

        public override void OnAwake()
        {
            flightData = new FlightData(gameObject.GetComponent<Vessel>());
            pilotAssistant = new PilotAssistant(this);
            surfSAS = new SurfSAS(this);
        }

        private void Start()
        {
            pilotAssistant.Start();
            surfSAS.Start();
            Vessel.OnPreAutopilotUpdate += new FlightInputCallback(OnPreAutopilotUpdate);
            Vessel.OnPostAutopilotUpdate += new FlightInputCallback(OnPostAutopilotUpdate);
        }

        private void Update()
        {
            pilotAssistant.Update();
            surfSAS.Update();
        }

        private void OnPreAutopilotUpdate(FlightCtrlState state)
        {
            flightData.UpdateAttitude();
        }

        private void OnPostAutopilotUpdate(FlightCtrlState state)
        {
            pilotAssistant.VesselController(state);
            surfSAS.VesselController(state);
        }

        private void OnGUI()
        {
            if (FlightGlobals.ActiveVessel == this.Vessel)
            {
                pilotAssistant.OnGUI();
                surfSAS.OnGUI();
            }
        }

        private void OnDestroy()
        {
            PresetManager.Instance.SavePresetsToFile();

            pilotAssistant.OnDestroy();
            surfSAS.OnDestroy();

            Vessel.OnPreAutopilotUpdate -= new FlightInputCallback(OnPreAutopilotUpdate);
            Vessel.OnPostAutopilotUpdate -= new FlightInputCallback(OnPostAutopilotUpdate);
        }

        public FlightData FlightData {
            get { return flightData; }
        }

        public Vessel Vessel
        {
            get { return flightData.Vessel; }
        }

        public SurfSAS SurfSAS
        {
            get { return surfSAS; }
        }

        public PilotAssistant PilotAssistant
        {
            get { return pilotAssistant; }
        }
    }
}

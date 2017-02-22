using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class SimuLite : MonoBehaviour
    {

        public static SimuLite Instance { get; set; }

        #region Public Properties
        public double RemainingCoreHours { get; set; } = 0;
        #endregion Public Properties


        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            showSimulationSetup = HighLogic.LoadedSceneIsEditor; //hacky hack for now
        }


        #region GUI Code
        public bool showSimulationSetup { get; set; }
        private Rect setupWindow = new Rect((Screen.width - 300) / 2, (Screen.height / 4), 300, 1);
        private SimulationConfigWindow simConfigWindow = new SimulationConfigWindow();

        private void OnGUI()
        {
            if (showSimulationSetup)
            {
                setupWindow = GUILayout.Window(8234, setupWindow, simConfigWindow.Draw, "Simulation Setup");
            }
        }
        #endregion GUI Code
    }
}

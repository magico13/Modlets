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



        #region GUI Code
        private bool showSimulationSetup = false;
        private Rect setupWindow = new Rect((Screen.width - 300) / 2, (Screen.height / 4), 300, 1);

        private void OnGUI()
        {
            if (showSimulationSetup)
            {
                setupWindow = GUILayout.Window(8234, setupWindow, SimulationConfigWindow.Draw, "Simulation Setup");
            }
        }
        #endregion GUI Code
    }
}

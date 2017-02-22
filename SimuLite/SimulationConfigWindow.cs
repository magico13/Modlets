using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class SimulationConfigWindow
    {
        internal SimulationConfiguration config = new SimulationConfiguration();

        #region UI Properties
        /// <summary>
        /// Whether to show advanced options (time options and such)
        /// </summary>
        public bool ShowAdvanced { get; set; }

        private string _durationString = "15m";

        public string DurationString
        {
            get { return _durationString; }
            set
            {
                if (value != _durationString)
                {
                    _durationString = value;
                    config.SetDuration(_durationString); //update viewmodel
                }
            }
        }

        private string _altitudeString = "0";

        public string AltitudeString
        {
            get { return _altitudeString; }
            set
            {
                if (value != _altitudeString)
                {
                    _altitudeString = value;
                    double alt;
                    if (double.TryParse(value, out alt))
                    {
                        config.Altitude = alt;
                    }
                }
            }
        }

        private string _inclinationString = "0";

        public string InclinationString
        {
            get { return _inclinationString; }
            set
            {
                if (value != _inclinationString)
                {
                    _inclinationString = value;
                    double inc;
                    if (double.TryParse(value, out inc))
                    {
                        config.Inclination = inc;
                    }
                }
            }
        }

        #endregion UI Properties


        public void Draw(int windowID)
        {
            //planet
            //orbit?
            //  //altitude
            //  //inclination
            //else(landed)
            //  //latitude
            //  //longitude
            //if Home, launchsite

            //time
            //is relative time?

            //complexity

            //?expected duration?
            //?expected core hour usage?
            GUILayout.BeginVertical();

            GUILayout.Label("Selected Body:");
            GUILayout.Label(config.SelectedBody.name);

            if (config.SelectedBody == Planetarium.fetch.Home)
            {
                config.OrbitalSimulation = GUILayout.Toggle(config.OrbitalSimulation, "Orbital Simulation");
            }
            else
            {
                config.OrbitalSimulation = true;
            }

            if (config.OrbitalSimulation)
            {
                GUILayout.Label("Altitude:");
                AltitudeString = GUILayout.TextField(AltitudeString);

                GUILayout.Label("Inclination:");
                InclinationString = GUILayout.TextField(InclinationString);
            }

            if (GUILayout.Button("Simulate!"))
            {
                config.StartSimulation();
            }

            GUILayout.EndVertical();
        }
    }
}

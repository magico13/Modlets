using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimuLite
{
    public class SimulationConfiguration
    {
        private ShipConstruct _ship;
        /// <summary>
        /// The Ship to simulate
        /// </summary>
        public ShipConstruct Ship
        {
            get { return _ship; }
            set { _ship = value; }
        }


        private CelestialBody _selectedBody;
        /// <summary>
        /// The planet/moon that the simulation will take place around
        /// </summary>
        public CelestialBody SelectedBody
        {
            get { return _selectedBody; }
            set
            {
                _selectedBody = value;
                //If not simulating at 
                if (value != null && value != Planetarium.fetch.Home)
                {
                    OrbitalSimulation = true;
                }
                else if (value == null)
                {
                    _selectedBody = Planetarium.fetch.Home;
                }
            }
        }

        private double _duration;

        public double Duration
        {
            get { return _duration; }
            set
            {
                if (value <= 0)
                {
                    _duration = 1000.0 * 365 * 86400; //one thousand Earth years
                }
                _duration = value;
            }
        }



        /// <summary>
        /// Whether to show advanced options (time options and such)
        /// </summary>
        public bool ShowAdvanced { get; set; }

        /// <summary>
        /// The time to simulate at. Default is null. Null means now.
        /// </summary>
        public double? UT { get; set; }

        /// <summary>
        /// Whether the given UT is relative to now (true) or absolute (false, default)
        /// </summary>
        public bool IsDeltaUT { get; set; }

        #region Orbital Parameters

        /// <summary>
        /// Whether the simulation is in orbit or on land
        /// </summary>
        public bool OrbitalSimulation { get; set; }

        private double _altitude = 0;
        /// <summary>
        /// The orbital altitude to orbit at. Default 0 or 1000+atmosphere height
        /// </summary>
        public double Altitude
        { 
            get
            {
                if (SelectedBody.atmosphere && _altitude < (SelectedBody.atmosphereDepth + 1000))
                {
                    return SelectedBody.atmosphereDepth + 1000;
                }
                return _altitude;
            }
            set { _altitude = value; }
        }

        private double _inclination;
        /// <summary>
        /// The inclination for the orbit. Default 0.
        /// </summary>
        public double Inclination
        {
            get { return _inclination; }
            set
            {
                _inclination = value % 360;
            }
        }


        #endregion Orbital Parameters



        #region Public Methods
        /// <summary>
        /// Sets the simulation time given the time string
        /// </summary>
        /// <param name="UTString">The string which is parsed to a time.</param>
        /// <param name="relative">If the time is relative to now, or absolute. Default: absolute (false)</param>
        public void SetTime(string UTString, bool relative=false)
        {
            IsDeltaUT = relative;
            UT = MagiCore.Utilities.ParseTimeString(UTString);
        }

        /// <summary>
        /// Sets the duration of the simulation given a duration string
        /// </summary>
        /// <param name="durationString">The duration, given as a string</param>
        public double SetDuration(string durationString)
        {
            Duration = MagiCore.Utilities.ParseTimeString(durationString, toUT: false);
            return Duration;
        }
        /// <summary>
        /// Calculates the cost of the simulation
        /// </summary>
        /// <returns>The simulation cost</returns>
        public double CalculateCost()
        {
            CelestialBody Kerbin = Planetarium.fetch.Home;
            Dictionary<string, string> vars = new Dictionary<string, string>();
            vars.Add("L", Duration.ToString()); //Sim length in seconds
            vars.Add("M", SelectedBody.Mass.ToString()); //Body mass
            vars.Add("KM", Kerbin.Mass.ToString()); //Kerbin mass
            vars.Add("A", SelectedBody.atmosphere ? "1" : "0"); //Presence of atmosphere
            vars.Add("S", (SelectedBody != Planetarium.fetch.Sun && SelectedBody.referenceBody != Planetarium.fetch.Sun) ? "1" : "0"); //Is a moon (satellite)

            float out1, out2;
            vars.Add("m", Ship.GetTotalMass().ToString()); //Vessel loaded mass
            vars.Add("C", Ship.GetShipCosts(out out1, out out2).ToString()); //Vessel loaded cost

            //vars.Add("s", SimCount.ToString()); //Number of times simulated this editor session //temporarily disabled


            CelestialBody Parent = SelectedBody;
            if (Parent != Planetarium.fetch.Sun)
            {
                while (Parent.referenceBody != Planetarium.fetch.Sun)
                {
                    Parent = Parent.referenceBody;
                }
            }
            double orbitRatio = 1;
            if (Parent.orbit != null)
            {
                if (Parent.orbit.semiMajorAxis >= Kerbin.orbit.semiMajorAxis)
                    orbitRatio = Parent.orbit.semiMajorAxis / Kerbin.orbit.semiMajorAxis;
                else
                    orbitRatio = Kerbin.orbit.semiMajorAxis / Parent.orbit.semiMajorAxis;
            }
            vars.Add("SMA", orbitRatio.ToString());
            vars.Add("PM", Parent.Mass.ToString());

            if ((SelectedBody == Kerbin) && !OrbitalSimulation)
            {
                return MagiCore.MathParsing.ParseMath(Configuration.RegularSimCost, vars);
            }
            else
            {
                return MagiCore.MathParsing.ParseMath(Configuration.OrbitalSimCost, vars);
            }
        }

        /// <summary>
        /// Starts the simulation with the defined parameters
        /// </summary>
        public void StartSimulation()
        {
            if (!OrbitalSimulation)
            {
                //start new launch on launchpad/runway

            }
            else
            {
                //start new launch in spaaaaacccceee
            }
        }

        #endregion Public Methods
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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


        private CelestialBody _selectedBody = Planetarium.fetch.Home;
        /// <summary>
        /// The planet/moon that the simulation will take place around
        /// </summary>
        public CelestialBody SelectedBody
        {
            get { return _selectedBody ?? Planetarium.fetch.Home; }
            set
            {
                if (_selectedBody != value)
                {
                    _selectedBody = value;
                    //If not simulating at home
                    //if (value != null && value != Planetarium.fetch.Home)
                    //{
                    //    OrbitalSimulation = true;
                    //}
                    //else 
                    if (value == null)
                    {
                        _selectedBody = Planetarium.fetch.Home;
                    }
                }
            }
        }

        private double _duration;
        /// <summary>
        /// The expected length of the simulation
        /// </summary>
        public double Duration
        {
            get { return _duration; }
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    if (value <= 0)
                    {
                        _duration = 1000.0 * 365 * 86400; //one thousand Earth years
                    }
                }
            }
        }

        private double _complexity;

        public double Complexity
        {
            get { return _complexity; }
            set { _complexity = value; }
        }

        private double? _ut = null;
        /// <summary>
        /// The time to simulate at. Default is null. Null means now.
        /// </summary>
        public double? UT
        {
            get { return _ut ?? Planetarium.GetUniversalTime(); }
            set
            {
                if (_ut != value)
                {
                    _ut = value;
                }
            }
        }

        /// <summary>
        /// Whether the given UT is relative to now (true) or absolute (false, default)
        /// </summary>
        public bool IsDeltaUT { get; set; }

        #region Orbital Parameters

        private bool _orbitalSimulation = false;
        /// <summary>
        /// Whether the simulation is in orbit or on land
        /// </summary>
        public bool OrbitalSimulation
        {
            get { return _orbitalSimulation; }
            set
            {
                if (_orbitalSimulation != value)
                {
                    _orbitalSimulation = value;
                    //CalculateComplexity();
                }
            }
        }

        private double _altitude = 0;
        /// <summary>
        /// The orbital altitude to orbit at. Default 0 or 1000+atmosphere height
        /// </summary>
        public double Altitude
        { 
            get { return _altitude; }
            set
            {
                if (_altitude != value)
                {
                    _altitude = value;
                    if (SelectedBody.atmosphere && _altitude < (SelectedBody.atmosphereDepth + 1000))
                    {
                        _altitude = SelectedBody.atmosphereDepth + 1000;
                    }
                    //CalculateComplexity(); //shouldn't affect it
                }
            }
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
                if (_inclination != value)
                {
                    _inclination = value % 360;
                    //CalculateComplexity(); //shouldn't affect it
                }
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
        /// Calculates the cost of the simulation and caches it in Complexity
        /// </summary>
        /// <returns>The simulation cost</returns>
        public double CalculateComplexity()
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
            vars.Add("dT", (UT - Planetarium.GetUniversalTime()).ToString()); //How far ahead in time the simulation is from right now (or negative for in the past)

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

            vars.Add("O", (OrbitalSimulation ? 1 : 0).ToString()); //is an orbital simulation


            Complexity = MagiCore.MathParsing.ParseMath(Configuration.SimComplexity, vars);
            return Complexity;
        }

        /// <summary>
        /// Starts the simulation with the defined parameters
        /// </summary>
        public void StartSimulation()
        {
            makeBackupFile();

            string tempFile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/temp.craft";
            VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
            if (manifest == null)
            {
                manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(EditorLogic.fetch.ship.SaveShip(), null, true);
            }
            EditorLogic.fetch.ship.SaveShip().Save(tempFile);

            if (!OrbitalSimulation)
            {
                //start new launch on launchpad/runway
                startRegularLaunch(tempFile, manifest);
            }
            else
            {
                //start new launch in spaaaaacccceee
                VesselSpawner.VesselData vessel = makeVessel(tempFile, manifest);
                if (VesselSpawner.CreateVessel(vessel))
                {
                    Debug.Log("[SimuLite] Vessel added to world.");
                    //FlightDriver.StartAndFocusVessel(HighLogic.CurrentGame, FlightGlobals.Vessels.FindIndex(v => v.id == vessel.id)); //well, let's try that. They want an index it seems
                    
                    //FlightGlobals.ForceSetActiveVessel(FlightGlobals.FindVessel(vessel.id.Value));
                }
                else
                {
                    Debug.Log("[SimuLite] Failed to create vessel.");
                }
                //vessel exists, now switch to it
            }
        }
        #endregion Public Methods

        private void makeBackupFile()
        {
            GamePersistence.SaveGame("SimuLite_backup", HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        private void startRegularLaunch(string craftFile, VesselCrewManifest manifest)
        {
            FlightDriver.StartWithNewLaunch(craftFile, EditorLogic.FlagURL, EditorLogic.fetch.launchSiteName, manifest);
        }

        private VesselSpawner.VesselData makeVessel(string craftFile, VesselCrewManifest manifest)
        {
            VesselSpawner.VesselData data = new VesselSpawner.VesselData();
            data.orbit = new Orbit(Inclination, 0, (Altitude + SelectedBody.Radius), 0, 0, 0, UT.Value, SelectedBody);
            data.orbit.Init();
            data.orbit.UpdateFromUT(UT.Value);

            data.body = SelectedBody;
            data.altitude = Altitude;
            data.craftURL = craftFile;
            //data.crew = manifest.GetAllCrew();
            data.flagURL = EditorLogic.FlagURL;
            data.orbiting = true;
            data.owned = true;
            data.vesselType = VesselType.Ship;

            


            foreach (ProtoCrewMember pcm in manifest.GetAllCrew(false))
            {
                VesselSpawner.CrewData crewData = new VesselSpawner.CrewData();
                crewData.name = pcm.name;
                if (data.crew == null)
                {
                    data.crew = new List<VesselSpawner.CrewData>();
                }
                data.crew.Add(crewData);
            }

            return data;
        }
    }
}

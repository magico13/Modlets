using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;
using System;
using System.Linq;

namespace NotInMyBackYard
{
    public class NotInMyBackYard : MonoBehaviour
    {
        protected Button.ButtonClickedEvent originalCallback;

        public List<IBeacon> StaticBeacons { get; set; } = new List<IBeacon>();
        public static double MobileBeaconRange { get; set; } = 2000;

        public void Start()
        {
            //initialize beacons
            LoadBeacons(KSPUtil.ApplicationRootPath+"/GameData/NIMBY/PluginData/Beacons.cfg", true);
        }

        public void LoadBeacons(string beaconFile, bool createIfNotExists = false)
        {
            StaticBeacons.Clear();
            if (System.IO.File.Exists(beaconFile))
            {
                ConfigNode beaconsNode = ConfigNode.Load(beaconFile);
                foreach (ConfigNode beacon in beaconsNode.GetNodes("Beacon"))
                {
                    StaticBeacons.Add(new StaticBeacon(beacon));
                }
            }
            else if (createIfNotExists)
            {
                //Set the defaults and save the file
                StaticBeacon KSC = new StaticBeacon("KSC", SpaceCenter.Instance.Latitude, SpaceCenter.Instance.Longitude, 100000);
                StaticBeacons.Add(KSC);

                ConfigNode beaconsNode = new ConfigNode("Beacons");
                beaconsNode.AddNode(KSC.AsNode());

                beaconsNode.Save(beaconFile);
            }
        }

        protected void NewRecoveryFunction(Vessel vessel)
        {
            Debug.Log("[NIMBY] Our recovery function is being called!");
            //check the distance to the KSC, if within 100km then recover, else pop up a message

            //We might be able to (temporarily) change the location of the KSC to the closest Beacon, which would also change the amount of funds we recover due to distance

            IBeacon closestBeacon = null; //Used for if we're not in range of any beacons
            double shortestDistance = double.PositiveInfinity;

            foreach (IBeacon beacon in GetAllBeacons())
            {
                if (!beacon.Active || beacon.Range <= 0)
                {
                    continue;
                }
                double distance = beacon.GreatCircleDistance(vessel);
                double adjustedDistance = distance - beacon.Range;

                Debug.Log($"[NIMBY] {beacon.Name} is {(distance / 1000).ToString("N2")}({(adjustedDistance / 1000).ToString("N2")})km away.");

                if (beacon.CanRecoverVessel(vessel))
                {
                    originalCallback.Invoke();
                    return;
                }
                else
                {
                    if (distance > 0 && adjustedDistance < shortestDistance) //the > 0 checks that it isn't the active vessel
                    {
                        shortestDistance = adjustedDistance;
                        closestBeacon = beacon;
                    }
                }
            }

            //No beacons in range
            //popup "error"
            Debug.Log("[NIMBY] Too far to recover!");

            string closestMessage = "There are no Recovery Beacons nearby.";
            if (closestBeacon != null)
            {
                double dist = closestBeacon.GreatCircleDistance(vessel) / 1000;
                closestMessage = $"Closest Recovery Beacon is {closestBeacon.Name} and is {(dist).ToString("N2")}km away ({(dist-closestBeacon.Range/1000).ToString("N2")}km to edge of Beacon's range).";
            }

            PopupDialog.SpawnPopupDialog(new Vector2(), new Vector2(), "tooFarPopup",
                "No Beacons In Range", $"Vessel is too far from any Recovery Beacons to recover. {closestMessage}", 
                "OK", false, HighLogic.UISkin);
        }

        public IList<IBeacon> GetAllBeacons()
        {
            List<IBeacon> beacons = new List<IBeacon>(StaticBeacons);
            int staticBeacons = beacons.Count;
            

            //find all vessels that have a mobile beacon module
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel == FlightGlobals.ActiveVessel)
                {
                    continue;
                }
                //make sure it's active
                IEnumerable<ModuleMobileRecoveryBeacon> modules;
                if (vessel.loaded && (modules = vessel.Parts.Select(p => p.Modules.GetModule<ModuleMobileRecoveryBeacon>())) != null)
                {
                    Debug.Log($"[NIMBY] {vessel.GetDisplayName()} has module");
                    IBeacon active = modules?.FirstOrDefault(m => m?.Active ?? false);
                    if (active != null)
                    {
                        Debug.Log($"[NIMBY] Module is Active.");
                        beacons.Add(active);
                    }
                }
                else if (MobileBeaconRequirementsMet(vessel))
                {
                    Debug.Log($"[NIMBY] Adding unloaded Beacon for {vessel.GetDisplayName()}.");
                    beacons.Add(new UnloadedMobileBeacon(vessel));
                }
            }
            Debug.Log($"[NIMBY] Counting {staticBeacons} static beacons and {beacons.Count - staticBeacons} mobile beacons.");
            return beacons;
        }

        public static double DefaultGreatCircleDistance(double radius, double latitude1, double longitude1, double latitude2, double longitude2)
        {
            //http://www.movable-type.co.uk/scripts/latlong.html
            double delLat = (latitude1 - latitude2) * Math.PI / 180;
            double delLon = (longitude1 - longitude2) * Math.PI / 180;
            double lat1 = latitude1 * Math.PI / 180;
            double lat2 = latitude2 * Math.PI / 180;

            double a = Math.Pow(Math.Sin(delLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(delLon / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = radius * c;

            return Math.Sqrt(d * d);
        }

        public static bool MobileBeaconRequirementsMet(Vessel vessel)
        {
            //Check that it's on Kerbin/Earth/whatever
            if (vessel.mainBody != Planetarium.fetch.Home)
            {
                return false;
            }
            //check for the module on the vessel
            if (!vessel.protoVessel.protoPartSnapshots.Exists(p => p.modules.Exists(m => m.moduleName == nameof(ModuleMobileRecoveryBeacon))))
            {
                //Debug.Log($"[NIMBY] {vessel.GetDisplayName()} doesn't have a recovery module.");
                return false;
            }
            //check for a lab on the vessel
            if (!vessel.protoVessel.protoPartSnapshots.Exists(p => p.modules.Exists(m => m.moduleName == nameof(ModuleScienceLab))))
            {
                //Debug.Log($"[NIMBY] {vessel.GetDisplayName()} doesn't have a science lab.");
                return false;
            }
            //Check for an antenna on the vessel
            if (!vessel.protoVessel.protoPartSnapshots.Exists(p => p.modules.Exists(m => m.moduleName == nameof(ModuleDataTransmitter))))
            {
                //Debug.Log($"[NIMBY] {vessel.GetDisplayName()} doesn't have an antenna.");
                return false;
            }
            //Has an Engineer aboard
            if (!vessel.GetVesselCrew().Exists(c => c.trait == "Engineer"))
            {
                //Debug.Log($"[NIMBY] {vessel.GetDisplayName()} doesn't have an engineer.");
                return false;
            }
            return true;
        }
    }

    //Start in the Flight Scene only, every time it's loaded
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class NotInMyBackYard_Flight : NotInMyBackYard
    {
        //Called once when the scene starts
        new public void Start()
        {
            base.Start(); //Call Start() in the parent class
            //Get the MonoBehaviour that controls the buttons at the top of the screen
            AltimeterSliderButtons buttons = (AltimeterSliderButtons)FindObjectOfType(typeof(AltimeterSliderButtons));

            if (buttons != null)
            {
                //back up the original function. We'll call that when we're within range
                originalCallback = buttons.vesselRecoveryButton.onClick;

                //Override the original function with ours, which checks the distance.
                buttons.vesselRecoveryButton.onClick = new Button.ButtonClickedEvent();
                buttons.vesselRecoveryButton.onClick.AddListener(NewRecoveryFunctionFlight);
            }
        }

        private void NewRecoveryFunctionFlight()
        {
            NewRecoveryFunction(FlightGlobals.ActiveVessel);
        }
    }

    //Run in the Tracking Station, every time.
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class NotInMyBackYard_TrackingStation : NotInMyBackYard
    {
        //Called once when the scene starts
        new public void Start()
        {
            base.Start(); //Call Start() in the parent class
            //Get the MonoBehaviour that controls the buttons at the top of the screen
            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
            
            if (trackingStation != null)
            {
                //back up the original function. We'll call that when we're within range
                originalCallback = trackingStation.RecoverButton.onClick;

                //Override the original function with ours, which checks the distance.
                trackingStation.RecoverButton.onClick = new Button.ButtonClickedEvent();
                trackingStation.RecoverButton.onClick.AddListener(NewRecoveryFunctionTrackingStation);
            }
        }

        private void NewRecoveryFunctionTrackingStation()
        {
            Vessel selectedVessel = null;

            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));

            selectedVessel = trackingStation.SelectedVessel;

            if (selectedVessel == null)
            {
                Debug.Log("[NIMBY] Error! No Vessel selected.");
                return;
            }

            NewRecoveryFunction(selectedVessel);
        }
    }

    //We should also consider the space center scene (you can recover there) except that's *probably* within range. Unless someone purposefully removes the KSC as a Beacon
}

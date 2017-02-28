/* This code is adapted from Contract Configurator by Nightingale and is licensed under the MIT license.
 * All credit goes toward the original author and this code is again licensed under MIT, no matter the license of the rest of the mod.
 * Source here: https://github.com/jrossignol/ContractConfigurator
 * 
 * With much gratitude toward Nightingale from me, magico13! 2017
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class VesselSpawner
    {

        public class CrewData
        {
            public string name = null;

            public CrewData() { }
            public CrewData(CrewData cd)
            {
                name = cd.name;
            }
        }

        public class VesselData
        {
            public string name = null;
            public Guid? id = null;
            public ShipConstruct shipConstruct = null;
            public AvailablePart craftPart = null;
            public string flagURL = null;
            public VesselType vesselType = VesselType.Ship;
            public CelestialBody body = null;
            public Orbit orbit = null;
            public double latitude = 0.0;
            public double longitude = 0.0;
            public double? altitude = null;
            public float height = 0.0f;
            public bool orbiting = false;
            public bool owned = false;
            public List<CrewData> crew = new List<CrewData>();
            public PQSCity pqsCity = null;
            public Vector3d pqsOffset;
            public float heading;
            public float pitch;
            public float roll;

            public VesselData() { }
            public VesselData(VesselData vd)
            {
                name = vd.name;
                id = vd.id;
                shipConstruct = vd.shipConstruct;
                craftPart = vd.craftPart;
                flagURL = vd.flagURL;
                vesselType = vd.vesselType;
                body = vd.body;
                orbit = vd.orbit;
                latitude = vd.latitude;
                longitude = vd.longitude;
                altitude = vd.altitude;
                height = vd.height;
                orbiting = vd.orbiting;
                owned = vd.owned;
                pqsCity = vd.pqsCity;
                pqsOffset = vd.pqsOffset;
                heading = vd.heading;
                pitch = vd.pitch;
                roll = vd.roll;

                foreach (CrewData cd in vd.crew)
                {
                    crew.Add(new CrewData(cd));
                }
            }
        }

        public static double TerrainHeight(double latitude, double longitude, CelestialBody body)
        {
            // Sun and Jool - bodies without terrain
            if (body.pqsController == null)
            {
                return 0;
            }

            // Figure out the terrain height
            double latRads = Math.PI / 180.0 * latitude;
            double lonRads = Math.PI / 180.0 * longitude;
            Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
            return Math.Max(body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius, 0.0);
        }

        private static ConfigNode GetNodeForPart(ProtoPartSnapshot p)
        {
            ConfigNode node = new ConfigNode("PART");
            p.Save(node);
            return node;
        }

        public static Guid? CreateVessel(VesselData vesselData)
        {
            Debug.Log("1");
            String gameDataDir = KSPUtil.ApplicationRootPath;
            gameDataDir = gameDataDir.Replace("\\", "/");
            if (!gameDataDir.EndsWith("/"))
            {
                gameDataDir += "/";
            }
            gameDataDir += "GameData";

            // Spawn the vessel in the game world
            Debug.Log("2");
            // Set additional info for landed vessels
            bool landed = false;
            if (!vesselData.orbiting)
            {
                Debug.Log("3.0");
                landed = true;
                if (vesselData.altitude == null)
                {
                    vesselData.altitude = TerrainHeight(vesselData.latitude, vesselData.longitude, vesselData.body);
                }

                Vector3d pos = vesselData.body.GetWorldSurfacePosition(vesselData.latitude, vesselData.longitude, vesselData.altitude.Value);

                vesselData.orbit = new Orbit(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, vesselData.body);
                vesselData.orbit.UpdateFromStateVectors(pos, vesselData.body.getRFrmVel(pos), vesselData.body, Planetarium.GetUniversalTime());
                Debug.Log("3.0.1");
            }
            else
            {
                Debug.Log("3.1");
                vesselData.orbit.referenceBody = vesselData.body;
            }
            Debug.Log("4");
            ConfigNode[] partNodes;
            //UntrackedObjectClass sizeClass;
            ShipConstruct shipConstruct = null;
            if (vesselData.shipConstruct != null)
            {
                Debug.Log("5");
                // Save the current ShipConstruction ship, otherwise the player will see the spawned ship next time they enter the VAB!
                //ConfigNode currentShip = ShipConstruction.ShipConfig;

                //shipConstruct = ShipConstruction.LoadShip(vesselData.craftURL);
                //if (shipConstruct == null)
                //{
                //    return false;
                //}

                // Restore ShipConstruction ship
                //ShipConstruction.ShipConfig = currentShip;

                shipConstruct = vesselData.shipConstruct;
                Debug.Log("6");
                // Set the name
                if (string.IsNullOrEmpty(vesselData.name))
                {
                    vesselData.name = shipConstruct.shipName;
                }

                // Set some parameters that need to be at the part level
                uint missionID = (uint)Guid.NewGuid().GetHashCode();
                uint launchID = HighLogic.CurrentGame.launchID++;
                foreach (Part p in shipConstruct.parts)
                {
                    p.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                    p.missionID = missionID;
                    p.launchID = launchID;
                    p.flagURL = vesselData.flagURL ?? HighLogic.CurrentGame.flagURL;

                    // Had some issues with this being set to -1 for some ships - can't figure out
                    // why.  End result is the vessel exploding, so let's just set it to a positive
                    // value.
                    p.temperature = 1.0;
                }
                Debug.Log("7");
                // Estimate an object class, numbers are based on the in game description of the
                // size classes.
                float size = shipConstruct.shipSize.magnitude / 2.0f;
                //if (size < 4.0f)
                //{
                //    sizeClass = UntrackedObjectClass.A;
                //}
                //else if (size < 7.0f)
                //{
                //    sizeClass = UntrackedObjectClass.B;
                //}
                //else if (size < 12.0f)
                //{
                //    sizeClass = UntrackedObjectClass.C;
                //}
                //else if (size < 18.0f)
                //{
                //    sizeClass = UntrackedObjectClass.D;
                //}
                //else
                //{
                //    sizeClass = UntrackedObjectClass.E;
                //}
                Debug.Log("8");
                foreach (CrewData cd in vesselData.crew)
                {
                    bool success = false;

                    // Find a seat for the crew
                    Part part = shipConstruct.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                    // Add the crew member
                    if (part != null)
                    {
                        // Get the ProtoCrewMember
                        ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(pcm => pcm.name == cd.name);

                        // Add them to the part
                        success = part.AddCrewmemberAt(crewMember, part.protoModuleCrew.Count);
                    }

                    if (!success)
                    {
                        break;
                    }
                }
                Debug.Log("9");
                // Create a dummy ProtoVessel, we will use this to dump the parts to a config node.
                // We can't use the config nodes from the .craft file, because they are in a
                // slightly different format than those required for a ProtoVessel.
                ConfigNode empty = new ConfigNode();
                ProtoVessel dummyProto = new ProtoVessel(empty, null);
                Vessel dummyVessel = new GameObject().AddComponent<Vessel>();
                dummyVessel.parts = shipConstruct.parts;
                dummyProto.vesselRef = dummyVessel;
                Debug.Log("10");
                // Create the ProtoPartSnapshot objects and then initialize them
                foreach (Part p in shipConstruct.parts)
                {
                    dummyProto.protoPartSnapshots.Add(new ProtoPartSnapshot(p, dummyProto));
                }
                foreach (ProtoPartSnapshot p in dummyProto.protoPartSnapshots)
                {
                    p.storePartRefs();
                }
                Debug.Log("11");
                // Create the ship's parts
                partNodes = dummyProto.protoPartSnapshots.Select<ProtoPartSnapshot, ConfigNode>(GetNodeForPart).ToArray();

                // Clean up
                GameObject.Destroy(dummyVessel.gameObject);
            }
            else
            {
                // Create crew member array
                ProtoCrewMember[] crewArray = new ProtoCrewMember[vesselData.crew.Count];
                int i = 0;
                foreach (CrewData cd in vesselData.crew)
                {
                    // Create the ProtoCrewMember
                    ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                    if (cd.name != null)
                    {
                        crewMember.ChangeName(cd.name);
                    }

                    crewArray[i++] = crewMember;
                }

                // Create part nodes
                uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                partNodes = new ConfigNode[1];
                partNodes[0] = ProtoVessel.CreatePartNode(vesselData.craftPart.name, flightId, crewArray);

                // Default the size class
                //sizeClass = UntrackedObjectClass.A;

                // Set the name
                if (string.IsNullOrEmpty(vesselData.name))
                {
                    vesselData.name = vesselData.craftPart.name;
                }
            }

            Debug.Log("12");
            // Create additional nodes //not needed?
            //ConfigNode[] additionalNodes = new ConfigNode[1];
            //DiscoveryLevels discoveryLevel = vesselData.owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned;
            //additionalNodes[0] = ProtoVessel.CreateDiscoveryNode(discoveryLevel, sizeClass, , contract.TimeDeadline);

            // Create the config node representation of the ProtoVessel
            ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(vesselData.name, vesselData.vesselType, vesselData.orbit, 0, partNodes);

            // Additional seetings for a landed vessel
            if (!vesselData.orbiting)
            {
                Vector3d norm = vesselData.body.GetRelSurfaceNVector(vesselData.latitude, vesselData.longitude);

                double terrainHeight = 0.0;
                if (vesselData.body.pqsController != null)
                {
                    terrainHeight = vesselData.body.pqsController.GetSurfaceHeight(norm) - vesselData.body.pqsController.radius;
                }
                bool splashed = landed && terrainHeight < 0.001;

                // Create the config node representation of the ProtoVessel
                // Note - flying is experimental, and so far doesn't work
                protoVesselNode.SetValue("sit", (splashed ? Vessel.Situations.SPLASHED : landed ?
                    Vessel.Situations.LANDED : Vessel.Situations.FLYING).ToString());
                protoVesselNode.SetValue("landed", (landed && !splashed).ToString());
                protoVesselNode.SetValue("splashed", splashed.ToString());
                protoVesselNode.SetValue("lat", vesselData.latitude.ToString());
                protoVesselNode.SetValue("lon", vesselData.longitude.ToString());
                protoVesselNode.SetValue("alt", vesselData.altitude.ToString());
                protoVesselNode.SetValue("landedAt", vesselData.body.name);

                // Figure out the additional height to subtract
                float lowest = float.MaxValue;
                if (shipConstruct != null)
                {
                    foreach (Part p in shipConstruct.parts)
                    {
                        foreach (Collider collider in p.GetComponentsInChildren<Collider>())
                        {
                            if (collider.gameObject.layer != 21 && collider.enabled)
                            {
                                lowest = Mathf.Min(lowest, collider.bounds.min.y);
                            }
                        }
                    }
                }
                else
                {
                    foreach (Collider collider in vesselData.craftPart.partPrefab.GetComponentsInChildren<Collider>())
                    {
                        if (collider.gameObject.layer != 21 && collider.enabled)
                        {
                            lowest = Mathf.Min(lowest, collider.bounds.min.y);
                        }
                    }
                }

                if (lowest == float.MaxValue)
                {
                    lowest = 0;
                }

                // Figure out the surface height and rotation
                Quaternion normal = Quaternion.LookRotation(new Vector3((float)norm.x, (float)norm.y, (float)norm.z));
                Quaternion rotation = Quaternion.identity;
                float heading = vesselData.heading;
                if (shipConstruct == null)
                {
                    rotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.back);
                }
                else if (shipConstruct.shipFacility == EditorFacility.SPH) //TODO: I need this for KCT
                {
                    rotation = rotation * Quaternion.FromToRotation(Vector3.forward, -Vector3.forward);
                    heading += 180.0f;
                }
                else
                {
                    rotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                }

                rotation = rotation * Quaternion.AngleAxis(vesselData.pitch, Vector3.right);
                rotation = rotation * Quaternion.AngleAxis(vesselData.roll, Vector3.down);
                rotation = rotation * Quaternion.AngleAxis(heading, Vector3.forward);

                // Set the height and rotation
                if (landed || splashed)
                {
                    float hgt = (shipConstruct != null ? shipConstruct.parts[0] : vesselData.craftPart.partPrefab).localRoot.attPos0.y - lowest;
                    hgt += vesselData.height;
                    protoVesselNode.SetValue("hgt", hgt.ToString());
                }
                protoVesselNode.SetValue("rot", KSPUtil.WriteQuaternion(rotation * normal));

                // Set the normal vector relative to the surface
                Vector3 nrm = (rotation * Vector3.forward);
                protoVesselNode.SetValue("nrm", nrm.x + "," + nrm.y + "," + nrm.z);

                protoVesselNode.SetValue("prst", false.ToString());
            }

            Debug.Log("13");

            // Add vessel to the game
            ProtoVessel protoVessel = new ProtoVessel(protoVesselNode, HighLogic.CurrentGame);
            protoVessel.Load(HighLogic.CurrentGame.flightState);
            HighLogic.CurrentGame.flightState.protoVessels.Add(protoVessel);
            // Store the id for later use
            vesselData.id = protoVessel.vesselRef.id;
            Debug.Log("14");
            // Associate it so that it can be used in contract parameters
            //ContractVesselTracker.Instance.AssociateVessel(vesselData.name, protoVessel.vesselRef);

            return vesselData.id;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;
using System.Text;

namespace ShipSaveSplicer
{
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class ShipSaveSplicer : MonoBehaviour
    {
        private static ApplicationLauncherButton _theButton;
        private static bool _includeCrew = false;
        private static bool _eventAdded = false;
        public void Start()
        {
            //add button to the Stock toolbar
            if (!_eventAdded)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
                _eventAdded = true;
            }
        }

        public void OnDestroy()
        {
            if (_theButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_theButton);
                _theButton = null;
            }
        }

        public void AddButton()
        {
            if (ApplicationLauncher.Ready && _theButton == null && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                _theButton = ApplicationLauncher.Instance.AddModApplication(
                    OnClick,
                    Dummy, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.TRACKSTATION, GameDatabase.Instance.GetTexture("ShipSaveSplicer/icon", false));
            }
        }

        private void Dummy() { }

        public void OnClick()
        {
            //figure out if mod+clicked
            _includeCrew = GameSettings.MODIFIER_KEY.GetKey();
            //includeCrew = false;
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                OpenConvertWindow(); //convert ships to craft files
                _theButton.SetFalse();
                return;
            }

            //get the selected craft
            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
            Vessel selectedVessel = trackingStation.SelectedVessel;

            if (ctrlHeld || _includeCrew) //ctrl or modifier held
            {
                OpenImportWindow();
            }
            else if (selectedVessel != null) 
            {
                ExportSelectedCraft(selectedVessel);
            }

            _theButton.SetFalse(false);
        }

        public void ExportSelectedCraft(Vessel vessel)
        {
            CreateFolder();

            string filename = KSPUtil.ApplicationRootPath + "/Ships/export/" + HighLogic.SaveFolder + "_" + vessel.vesselName;
            Log($"Exporting vessel: {vessel.vesselName}\nExporting to file: {filename}");

            ConfigNode nodeToSave = new ConfigNode();

            //save vessel
            ConfigNode vesselNode = new ConfigNode("VESSEL");
            ProtoVessel pVessel = vessel.BackupVessel();
            pVessel.Save(vesselNode);
            nodeToSave.AddNode("VESSEL", vesselNode);

            //save active crew member info
            foreach (ProtoCrewMember pcm in pVessel.GetVesselCrew())
            {
                ConfigNode pcmNode = new ConfigNode("CREW");
                pcm.Save(pcmNode);
                nodeToSave.AddNode("CREW", pcmNode);
            }

            nodeToSave.Save(filename);

            ScreenMessage message = new ScreenMessage(vessel.vesselName+" exported to "+filename, 6, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(message);
        }

        public void OpenConvertWindow()
        {
            //convert the selected vessel to a craft file and save it in the ships folder for the save
            string dir = KSPUtil.ApplicationRootPath + "/Ships/export/";
            //provide a list of all the craft we can import
            string[] files = System.IO.Directory.GetFiles(dir);
            int count = files.Length;

            DialogGUIBase[] options = new DialogGUIBase[count + 1];
            for (int i = 0; i < count; i++)
            {
                int select = i;
                options[i] = new DialogGUIButton(files[i].Split('/').Last(), () => { ConvertToCraft(files[select]); });
            }
            options[count] = new DialogGUIButton("Close", Dummy);
            string msg = "Select a vessel to convert to a .craft file.";

            MultiOptionDialog a = new MultiOptionDialog("convertPopup", msg, "Convert Vessel to .craft", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), a, false, HighLogic.UISkin);
        }

        public void ConvertToCraft(string name)
        {
            if (System.IO.File.Exists(name))
            {
                Log($"Converting ship to craft file: {name}");
                ConfigNode storedNode = ConfigNode.Load(name);

                ConfigNode vesselNode = storedNode.GetNode("VESSEL");

                if (WarnOfInvalidParts(vesselNode, false))
                {
                    return;
                }
                //clear out all crew on vessel
                foreach (ConfigNode partNode in vesselNode.GetNodes("PART"))
                {
                    if (partNode.HasValue("crew"))
                    {
                        partNode.RemoveValues("crew");
                    }
                }

                VesselToCraftFile(vesselNode);
                
            }
        }

        public void VesselToCraftFile(ConfigNode VesselNode)
        {
            //This code is taken from InflightShipSave by Claw, using the CC-BY-NC-SA license.
            //This code thus is licensed under the same license, despite the GPLv3 license covering original KCT code
            //See https://github.com/ClawKSP/InflightShipSave

            ProtoVessel VesselToSave = HighLogic.CurrentGame.AddVessel(VesselNode);
            if (VesselToSave.vesselRef == null)
            {
                Log("Vessel reference is null!");
                return;
            }

            try
            {
                string ShipName = VesselToSave.vesselName;

                //Vessel FromFlight = FlightGlobals.Vessels.Find(v => v.id == VesselToSave.vesselID);
                try
                {
                    VesselToSave.vesselRef.Load();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Log("Attempting to continue.");
                }

                ShipConstruct ConstructToSave = new ShipConstruct(ShipName, "", VesselToSave.vesselRef.Parts[0]);
                Quaternion OriginalRotation = VesselToSave.vesselRef.vesselTransform.rotation;
                Vector3 OriginalPosition = VesselToSave.vesselRef.vesselTransform.position;

                VesselToSave.vesselRef.SetRotation(new Quaternion(0, 0, 0, 1));
                Vector3 ShipSize = ShipConstruction.CalculateCraftSize(ConstructToSave);
                VesselToSave.vesselRef.SetPosition(new Vector3(0, ShipSize.y + 2, 0));


                ConfigNode CN = new ConfigNode("ShipConstruct");
                CN = ConstructToSave.SaveShip();
                //SanitizeShipNode(CN);
                CleanEditorNodes(CN);

                //VesselToSave.rotation = OriginalRotation;
                //VesselToSave.position = OriginalPosition;

                if (ConstructToSave.shipFacility == EditorFacility.SPH)
                {
                    CN.Save(UrlDir.ApplicationRootPath + "saves/" + HighLogic.SaveFolder
                        + "/Ships/SPH/" + ShipName + "_Rescued.craft");

                    ScreenMessage message = new ScreenMessage(ShipName + " converted to " + UrlDir.ApplicationRootPath + "saves/" + HighLogic.SaveFolder
                        + "/Ships/SPH/" + ShipName + "_Rescued.craft", 6, ScreenMessageStyle.UPPER_CENTER);
                    ScreenMessages.PostScreenMessage(message);
                }
                else
                {
                    CN.Save(UrlDir.ApplicationRootPath + "saves/" + HighLogic.SaveFolder
                        + "/Ships/VAB/" + ShipName + "_Rescued.craft");

                    ScreenMessage message = new ScreenMessage(ShipName + " converted to " + UrlDir.ApplicationRootPath + "saves/" + HighLogic.SaveFolder
                        + "/Ships/VAB/" + ShipName + "_Rescued.craft", 6, ScreenMessageStyle.UPPER_CENTER);
                    ScreenMessages.PostScreenMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                HighLogic.CurrentGame.DestroyVessel(VesselToSave.vesselRef);
                VesselToSave.vesselRef.Die();
            }
            //End of Claw's code. Thanks Claw!
        }

        public void OpenImportWindow()
        {
            string dir = KSPUtil.ApplicationRootPath + "/Ships/export/";
            //provide a list of all the craft we can import
            string[] files = System.IO.Directory.GetFiles(dir);
            int count = files.Length;

            DialogGUIBase[] options = new DialogGUIBase[count + 1];
            for (int i=0; i<count; i++)
            {
                int select = i;
                options[i] = new DialogGUIButton(files[i].Split('/').Last(), () => { ImportVessel(files[select]); });
            }
            options[count] = new DialogGUIButton("Close", Dummy);
            string msg = string.Format("Select a vessel to import. Will {0}import crew.{1}", (_includeCrew ? string.Empty : "not "), (_includeCrew ? string.Empty : "\n(modifier+click the SSS button to include crew)"));

            MultiOptionDialog a = new MultiOptionDialog("importPopup", msg, "Import Vessel", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), a, false, HighLogic.UISkin);
        }

        public void ImportVessel(string name)
        {
            if (System.IO.File.Exists(name))
            {
                Log($"Importing vessel: {name}");
                ConfigNode storedNode = ConfigNode.Load(name);

                ConfigNode vesselNode = storedNode.GetNode("VESSEL");

                vesselNode.SetValue("pid", Guid.NewGuid().ToString());

                if (WarnOfInvalidParts(vesselNode, true))
                {
                    return;
                }

                List<ProtoCrewMember> crewAdded = new List<ProtoCrewMember>();

                if (!_includeCrew)
                {
                    //clear out all crew on vessel
                    StripCrew(vesselNode);
                }
                else
                {
                    //create crewmembers if they don't exist, set all of them to assigned
                    try
                    {
                        foreach (ConfigNode partNode in vesselNode.GetNodes("PART"))
                        {
                            if (partNode.HasValue("crew"))
                            {
                                List<string> toRemove = new List<string>();
                                foreach (ConfigNode.Value crewValue in partNode.values)//(string crewmember in partNode.GetValues("crew"))
                                {
                                    if (crewValue.name != "crew")
                                    {
                                        continue;
                                    }

                                    string crewmember = crewValue.value;
                                    //find the confignode saved with the vessel
                                    ConfigNode crewNode = storedNode.GetNodes("CREW")?.FirstOrDefault(c => c.GetValue("name") == crewmember);
                                    if (crewNode == null || crewNode.GetValue("type") != "Crew") //if no data or is tourist then remove from ship
                                    {
                                        //can't find the required data, so remove that kerbal from the ship
                                        Log($"Vessel occupant is not crew: {crewmember}");
                                        toRemove.Add(crewmember);
                                        continue;
                                    }


                                    ProtoCrewMember newCrew = new ProtoCrewMember(HighLogic.CurrentGame.Mode, crewNode, ProtoCrewMember.KerbalType.Crew);
                                    if (HighLogic.CurrentGame.CrewRoster.Exists(crewmember)) //there's already a kerbal with that name (sadness :( )
                                    {
                                        //alright, rename this kerbal to a new name
                                        string newName = RenameKerbal(crewmember);
                                        newCrew.ChangeName(newName);
                                        crewValue.value = newName;
                                    }
                                    Log($"Creating crewmember {newCrew.name}");
                                    //add the crew member to the crew roster
                                    //the function to do this is hidden for some reason. yay!
                                    HighLogic.CurrentGame.CrewRoster.AddCrewMember(newCrew); //no longer hidden! MORE YAY!
                                    crewAdded.Add(newCrew);
                                }
                                foreach (string crewmember in toRemove) //remove all crews that shouldn't be here anymore
                                {
                                    //find the value with this kerbal and remove it
                                    foreach (ConfigNode.Value val in partNode.values)
                                    {
                                        if (val.name == "crew" && val.value == crewmember)
                                        {
                                            Log($"Removing non-valid crew member {val.value}");
                                            partNode.values.Remove(val);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Encountered exception while transferring crew. The exception follows. Stripping crew data.");
                        Debug.LogException(ex);

                        StripCrew(vesselNode);

                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "errorPopup", "Error Occurred", 
                            "Unable to import crew. An exception occurred and has been logged.",
                            "Ok", false, HighLogic.UISkin);

                        return;
                    }
                }

                ProtoVessel addedVessel = HighLogic.CurrentGame.AddVessel(vesselNode);
                foreach (ProtoCrewMember crew in crewAdded)
                {
                    Log($"Assigning {crew.name}");
                    addedVessel.AddCrew(crew);
                }
                //In 1.2.2+ saving fails when there are two copies of a ship with crew onboard both. Might be part ID related.
                //All I know is that if I terminate the original flight, I can import crew. Otherwise it NREs when it tries to save the flight state.
            }
        }

        private void StripCrew(ConfigNode vesselNode)
        {
            Log("Stripping out crew information");
            foreach (ConfigNode partNode in vesselNode.GetNodes("PART"))
            {
                if (partNode.HasValue("crew"))
                {
                    partNode.RemoveValues("crew");
                }
            }
        }

        public string RenameKerbal(string currentName)
        {
            string newName = currentName;
            int i = 2;
            while (HighLogic.CurrentGame.CrewRoster.Exists(newName))
            {
                newName = currentName + " " + RomanNumeral(i++); //gives results like "Jebediah Kerman II" or "Bob Kerman 14"
            }
            return newName;
        }

        public string RomanNumeral(int number)
        {
            string[] numerals = new string[11] {
                "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"
            };

            if (number < numerals.Length)
            {
                return numerals[number];
            }
            return number.ToString();
        }

        public void CreateFolder()
        {
            string filename = KSPUtil.ApplicationRootPath + "/Ships/export/";
            if (!System.IO.Directory.Exists(filename))
            {
                System.IO.Directory.CreateDirectory(filename);
            }
        }

        public List<string> InvalidParts(ConfigNode vesselNode)
        {
            List<string> invalid = new List<string>();
            //loop through the parts on the vessel and find AvailableParts for them
            //if we can't find one, then that part isn't loaded and is invalid

            foreach (ConfigNode partNode in vesselNode.GetNodes("PART"))
            {
                string partName = PartNameFromNode(partNode);
                if (!invalid.Contains(partName) && PartLoader.getPartInfoByName(partName) == null) //don't add duplicates
                {
                    invalid.Add(partName);
                }
            }
            return invalid;
        }

        public bool WarnOfInvalidParts(ConfigNode vesselNode, bool importing)
        {
            List<string> invalidParts = InvalidParts(vesselNode);
            if (invalidParts.Count > 0)
            {
                string action = importing ? "impoerted" : "converted";
                StringBuilder msg = new StringBuilder($"The selected vessel cannot be {action} because it contains the following invalid parts (perhaps you removed a mod?):").AppendLine();
                foreach (string invalid in invalidParts)
                {
                    msg.Append("    ").AppendLine(invalid);
                }

                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "missingPartsPopup", "Missing Parts", msg.ToString(), "Ok", false, HighLogic.UISkin);
                return true;
            }
            return false;
        }

        public string PartNameFromNode(ConfigNode part)
        {
            string name = part.GetValue("part");
            if (name != null)
            {
                name = name.Split('_')[0];
            }
            else
            {
                name = part.GetValue("name");
            }

            return name;
        }

        public void Log(object msg)
        {
            Debug.Log("[ShipSaveSplicer] " + msg.ToString());
        }

        /* The following is directly from Claw's InflightShipSave and credit goes to the original author */
        private void CleanEditorNodes(ConfigNode CN)
        {

            CN.SetValue("EngineIgnited", "False");
            CN.SetValue("currentThrottle", "0");
            CN.SetValue("Staged", "False");
            CN.SetValue("sensorActive", "False");
            CN.SetValue("throttle", "0");
            CN.SetValue("generatorIsActive", "False");
            CN.SetValue("persistentState", "STOWED");

            string ModuleName = CN.GetValue("name");

            // Turn off or remove specific things
            if ("ModuleScienceExperiment" == ModuleName)
            {
                CN.RemoveNodes("ScienceData");
            }
            else if ("ModuleScienceExperiment" == ModuleName)
            {
                CN.SetValue("Inoperable", "False");
                CN.RemoveNodes("ScienceData");
            }
            else if ("Log" == ModuleName)
            {
                CN.ClearValues();
            }


            for (int IndexNodes = 0; IndexNodes < CN.nodes.Count; IndexNodes++)
            {
                CleanEditorNodes(CN.nodes[IndexNodes]);
            }
        }

        private void PristineNodes(ConfigNode CN)
        {
            if (null == CN) { return; }

            if ("PART" == CN.name)
            {
                string PartName = ((CN.GetValue("part")).Split('_'))[0];

                Log("PART: " + PartName);

                Part NewPart = PartLoader.getPartInfoByName(PartName).partPrefab;
                ConfigNode NewPartCN = new ConfigNode();
                Log("New Part: " + NewPart.name);

                NewPart.InitializeModules();

                CN.ClearNodes();

                // EVENTS, ACTIONS, PARTDATA, MODULE, RESOURCE

                
                NewPart.Events.OnSave(CN.AddNode("EVENTS"));
                
                NewPart.Actions.OnSave(CN.AddNode("ACTIONS"));
                
                NewPart.OnSave(CN.AddNode("PARTDATA"));
                
                for (int IndexModules = 0; IndexModules < NewPart.Modules.Count; IndexModules++)
                {
                    NewPart.Modules[IndexModules].Save(CN.AddNode("MODULE"));
                }
                
                for (int IndexResources = 0; IndexResources < NewPart.Resources.Count; IndexResources++)
                {
                    NewPart.Resources[IndexResources].Save(CN.AddNode("RESOURCE"));
                }

                //CN.AddNode(CompiledNodes);

                return;
            }
            for (int IndexNodes = 0; IndexNodes < CN.nodes.Count; IndexNodes++)
            {
                PristineNodes(CN.nodes[IndexNodes]);
            }
        }


    }
}
/*
Copyright (C) 2018  Michael Marvin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
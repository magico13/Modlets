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
        public static ApplicationLauncherButton theButton;
        public static bool includeCrew = false;
        private static bool EventAdded = false;
        public void Start()
        {
            //add button to the Stock toolbar
            if (!EventAdded)
            {
                GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
                EventAdded = true;
            }

            //AddButton();
        }

        public void OnDestroy()
        {
            if (theButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(theButton);
                theButton = null;
            }
        }

        public void AddButton()
        {
            if (ApplicationLauncher.Ready && theButton == null)
            {
                theButton = ApplicationLauncher.Instance.AddModApplication(
                    OnClick,
                    Dummy, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.TRACKSTATION, GameDatabase.Instance.GetTexture("ShipSaveSplicer/icon", false));
            }
        }

        private void Dummy() { }

        public void OnClick()
        {
            //figure out if mod+clicked
            //includeCrew = GameSettings.MODIFIER_KEY.GetKey(); //TODO: Reenable when fixed
            includeCrew = false;
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                OpenConvertWindow(); //convert ships to craft files
                theButton.SetFalse();
                return;
            }

            //get the selected craft
            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
            Vessel selectedVessel = trackingStation.SelectedVessel;

            //1.2 made this non-private
            //foreach (FieldInfo f in trackingStation.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            //{
            //    if (f.FieldType == typeof(Vessel))
            //    {
            //        //FYI: the first one (0) is the currently selected vessel
            //        //The second one (1) is the one that the mouse is hovering over
            //        selectedVessel = f.GetValue(trackingStation) as Vessel;
            //        break;
            //    }
            //}

            if (ctrlHeld || includeCrew) //ctrl or modifier held
            {
                OpenImportWindow();
            }
            else if (selectedVessel != null) 
            {
                ExportSelectedCraft(selectedVessel);
            }

            theButton.SetFalse(false);
        }

        public void ExportSelectedCraft(Vessel vessel)
        {
            CreateFolder();

            string filename = KSPUtil.ApplicationRootPath + "/Ships/export/" + HighLogic.SaveFolder + "_" + vessel.vesselName;
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
                options[i] = new DialogGUIButton(files[i].Split('/').Last(), () => { ConvertVessel(files[select]); });
            }
            options[count] = new DialogGUIButton("Close", Dummy);
            string msg = "Select a vessel to convert to a .craft file.";

            MultiOptionDialog a = new MultiOptionDialog("convertPopup", msg, "Convert Vessel to .craft", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), a, false, HighLogic.UISkin);
        }

        public void ConvertVessel(string name)
        {
            if (System.IO.File.Exists(name))
            {
                ConfigNode storedNode = ConfigNode.Load(name);

                ConfigNode vesselNode = storedNode.GetNode("VESSEL");

                List<string> invalidParts = InvalidParts(vesselNode);
                if (invalidParts.Count > 0) //contains invalid parts and can't be loaded
                {
                    StringBuilder msg = new StringBuilder("The selected vessel cannot be converted because it contains the following invalid parts (perhaps you removed a mod?):\n");
                    foreach (string invalid in invalidParts)
                        msg.Append("    ").AppendLine(invalid);
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "missingPartsPopup", "Missing Parts", msg.ToString(), "Ok", false, HighLogic.UISkin);
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
                Debug.LogError("Vessel reference is null!");
                return;
            }

            try
            {
                string ShipName = VesselToSave.vesselName;
                // Debug.LogWarning("Saving: " + ShipName);

                //Vessel FromFlight = FlightGlobals.Vessels.Find(v => v.id == VesselToSave.vesselID);
                try
                {
                    VesselToSave.vesselRef.Load();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.Log("Attempting to continue.");
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
            string msg = "Select a vessel to import. Will " + (includeCrew ? "" : "not ") + "import crew members.";// +(includeCrew ? "": " (modifier+click the SSS button to include crew)");
            //TODO: Reenable when fixed

            MultiOptionDialog a = new MultiOptionDialog("importPopup", msg, "Import Vessel", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), a, false, HighLogic.UISkin);
        }

        public void ImportVessel(string name)
        {
            if (System.IO.File.Exists(name))
            {
                ConfigNode storedNode = ConfigNode.Load(name);

                ConfigNode vesselNode = storedNode.GetNode("VESSEL");

                vesselNode.SetValue("pid", Guid.NewGuid().ToString());

                List<string> invalidParts = InvalidParts(vesselNode);
                if (invalidParts.Count > 0) //contains invalid parts and can't be loaded
                {
                    string msg = "The selected vessel cannot be imported because it contains the following invalid parts (perhaps you removed a mod?):\n";
                    foreach (string invalid in invalidParts)
                        msg += "    " + invalid + "\n";
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "missingPartsPopup", "Missing Parts", msg, "Ok", false, HighLogic.UISkin);
                    return;
                }



                if (!includeCrew)
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
                                        continue;
                                    string crewmember = crewValue.value;
                                    //find the confignode saved with the vessel
                                    ConfigNode crewNode = storedNode.GetNodes("CREW")?.FirstOrDefault(c => c.GetValue("name") == crewmember);
                                    if (crewNode == null || crewNode.GetValue("type") != "Crew") //if no data or is tourist then remove from ship
                                    {
                                        //can't find the required data, so remove that kerbal from the ship
                                        toRemove.Add(crewmember);
                                        continue;
                                    }


                                    ProtoCrewMember newCrew = new ProtoCrewMember(HighLogic.CurrentGame.Mode, crewNode, ProtoCrewMember.KerbalType.Crew);
                                    if (HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(c => c.name == crewmember) != null) //there's already a kerbal with that name (sadness :( )
                                    {
                                        //alright, rename this kerbal to a new name
                                        string newName = RenameKerbal(crewmember);
                                        newCrew.ChangeName(newName);
                                        crewValue.value = newName;
                                    }
                                    //add the crew member to the crew roster
                                    //the function to do this is hidden for some reason. yay!
                                    HighLogic.CurrentGame.CrewRoster.AddCrewMember(newCrew); //no longer hidden! MORE YAY!
                                }
                                foreach (string crewmember in toRemove) //remove all crews that shouldn't be here anymore
                                {
                                    //find the value with this kerbal and remove it
                                    foreach (ConfigNode.Value val in partNode.values)
                                    {
                                        if (val.name == "crew" && val.value == crewmember)
                                        {
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
                        Debug.Log("[ShipSaveSplicer] Encountered exception while transferring crew. The exception follows. Stripping crew data.");
                        Debug.LogException(ex);

                        StripCrew(vesselNode);

                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "errorPopup", "Error Occurred", 
                            "Unable to import crew. An exception occurred and has been logged.",
                            "Ok", false, HighLogic.UISkin);

                        return;
                    }
                }

                ProtoVessel addedVessel = HighLogic.CurrentGame.AddVessel(vesselNode);
                //we might have to assign the kerbals to the vessel here

            //related issue, in 1.2.2 (at least) we fail validation of assignments when there are two ships with the same part ids (guessing).
            //All I know is that if I terminate the original flight, I can import crew. Otherwise it NREs when I save.
            }
        }

        private void StripCrew(ConfigNode vesselNode)
        {
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
                System.IO.Directory.CreateDirectory(filename);
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
                    invalid.Add(partName);
            }
            return invalid;
        }

        public string PartNameFromNode(ConfigNode part)
        {
            string name = part.GetValue("part");
            if (name != null)
                name = name.Split('_')[0];
            else
                name = part.GetValue("name");
            return name;
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

                Debug.LogWarning("PART: " + PartName);

                Part NewPart = PartLoader.getPartInfoByName(PartName).partPrefab;
                ConfigNode NewPartCN = new ConfigNode();
                Debug.LogWarning("New Part: " + NewPart.name);

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
Copyright (C) 2017  Michael Marvin

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
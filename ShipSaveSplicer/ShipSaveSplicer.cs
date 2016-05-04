using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;


namespace ShipSaveSplicer
{
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class ShipSaveSplicer : MonoBehaviour
    {
        public static ApplicationLauncherButton theButton;
        public static bool includeCrew = false; //currently not working when true
        public void Start()
        {
            //add button to the Stock toolbar
            AddButton();
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
            if (GameSettings.MODIFIER_KEY.GetKey())
                includeCrew = true;
            else
                includeCrew = false;

            //get the selected craft
            Vessel selectedVessel = null;

            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));

            foreach (FieldInfo f in trackingStation.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(Vessel))
                {
                    //FYI: the first one (0) is the currently selected vessel
                    //The second one (1) is the one that the mouse is hovering over
                    selectedVessel = f.GetValue(trackingStation) as Vessel;
                    break;
                }
            }

            if (selectedVessel != null)
                ExportSelectedCraft(selectedVessel);
            else
                OpenImportWindow();

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
            string msg = "Select a vessel to import. Will "+(includeCrew ? "" : "not ")+"import crew members."+(includeCrew ? "": " (modifier+click the SSS button to include crew)");

            MultiOptionDialog a = new MultiOptionDialog(msg, "Import Vessel", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), a, false, HighLogic.UISkin);
        }

        public void ImportVessel(string name)
        {
            if (System.IO.File.Exists(name))
            {
                ConfigNode storedNode = ConfigNode.Load(name);

                ConfigNode vesselNode = storedNode.GetNode("VESSEL");

                List<string> invalidParts = InvalidParts(vesselNode);
                if (invalidParts.Count > 0) //contains invalid parts and can't be loaded
                {
                    string msg = "The selected vessel cannot be imported because it contains the following invalid parts (perhaps you removed a mod?):\n";
                    foreach (string invalid in invalidParts)
                        msg += "    " + invalid + "\n";
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Missing Parts", msg, "Ok", false, HighLogic.UISkin);
                    return;
                }



                if (!includeCrew)
                {
                    //clear out all crew on vessel
                    foreach (ConfigNode partNode in vesselNode.GetNodes("PART"))
                    {
                        if (partNode.HasValue("crew"))
                        {
                            partNode.RemoveValues("crew");
                        }
                    }
                }
                else
                {
                    //create crewmembers if they don't exist, set all of them to assigned
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
                                ConfigNode crewNode = storedNode.GetNodes("CREW").FirstOrDefault(c => c.GetValue("name") == crewmember);
                                if (crewNode == null || crewNode.GetValue("type") != "Crew") //if no data or is tourist then remove from ship
                                {
                                    //can't find the required data, so remove that kerbal from the ship
                                    toRemove.Add(crewmember);
                                    /*foreach (ConfigNode.Value val in partNode.values)
                                    {
                                        if (val.name == "crew" && val.value == crewmember)
                                            partNode.values.Remove(val);
                                    }*/
                                    continue;
                                }

                                
                                    ProtoCrewMember newCrew = new ProtoCrewMember(HighLogic.CurrentGame.Mode, crewNode, ProtoCrewMember.KerbalType.Crew);

                                    if (HighLogic.CurrentGame.CrewRoster.Crew.FirstOrDefault(c => c.name == crewmember) != null) //there's already a kerbal with that name (sadness :( )
                                    {
                                        //alright, rename this kerbal to a new name
                                        string newName = RenameKerbal(crewmember);
                                        newCrew.name = newName;
                                        crewValue.value = newName;
                                    }
                                    //add the crew member to the crew roster
                                    //the function to do this is hidden for some reason. yay!
                                    MethodInfo addMethod = HighLogic.CurrentGame.CrewRoster.GetType().GetMethod("AddCrewMember", BindingFlags.NonPublic | BindingFlags.Instance);
                                    addMethod.Invoke(HighLogic.CurrentGame.CrewRoster, new object[] { newCrew });
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

                ProtoVessel addedVessel = HighLogic.CurrentGame.AddVessel(vesselNode);
                //we might have to assign the kerbals to the vessel here
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
    }
}
/*
Copyright (C) 2016  Michael Marvin

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
using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using System;

namespace TreeToppler
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class TreeToppler : MonoBehaviour
    {
        private bool RnDOpen = false;
        private bool OverrideCost = false;
        private string lastNode = "";
        public void Start()
        {
            GameEvents.onGUIRnDComplexSpawn.Add(RnDOpened);
            GameEvents.onGUIRnDComplexDespawn.Add(RnDClosed);
            GameEvents.OnTechnologyResearched.Add(TechUnlockEventTriggered);
        }
        public void OnDestroy()
        {
            GameEvents.onGUIRnDComplexSpawn.Remove(RnDOpened);
            GameEvents.onGUIRnDComplexDespawn.Remove(RnDClosed);
            GameEvents.OnTechnologyResearched.Remove(TechUnlockEventTriggered);
        }

        public void OnGUI()
        {
            if (RnDOpen)
            {
                if (GUI.Button(new Rect(2, Screen.height-40, 80, 20), "Topple"))
                {
                    OverrideCost = false;
                    List<DialogGUIBase> options = new List<DialogGUIBase>();
                    options.Add(new DialogGUIButton("Unlock All Nodes", UnlockAllNoLvl));
                    options.Add(new DialogGUIButton("Unlock Up To Level", UnlockAllLvl));
                    options.Add(new DialogGUIButton("Disable Costs", DisableCosts));
                    options.Add(new DialogGUIButton("Lock All Nodes", LockAll));
                    options.Add(new DialogGUIButton("Cancel", DummyVoid));
                    MultiOptionDialog diag = new MultiOptionDialog("treeTopplerPopup", "What would you like to do?", "Tree Toppler", null, options.ToArray());
                    PopupDialog.SpawnPopupDialog(diag, false, HighLogic.UISkin);
                }

                if (OverrideCost && RDController.Instance.node_selected != null)
                {
                    if (GUI.Button(new Rect(Screen.width - 225, 60, 100, 40), "Force Unlock"))
                    {
                        ForceUnlockTech(RDController.Instance.node_selected.tech.techID);
                        RDController.Instance.UpdatePanel();
                    }
                    if (GUI.Button(new Rect(Screen.width - 120, 60, 100, 40), "Force Lock"))
                    {
                        ForceLockTech(RDController.Instance.node_selected.tech.techID);
                        RDController.Instance.UpdatePanel();
                    }
                }
            }
        }

        public void DummyVoid() { }

        public void RnDOpened()
        {
            RnDOpen = true;
        }

        public void RnDClosed()
        {
            RnDOpen = false;
            OverrideCost = false;
        }

        private void UnlockAllNoLvl()
        {
            UnlockAll(false);
        }
        
        private void UnlockAllLvl()
        {
            UnlockAll(true);
        }

        private void UnlockAll(bool respectLvl = false)
        {
            float level = float.PositiveInfinity;
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                level = GameVariables.Instance.GetScienceCostLimit(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment));
            //Debug.Log(level);
            foreach (RDNode node in RDController.Instance.nodes)
            {
                if (!respectLvl || node.tech.scienceCost < level)
                    ForceUnlockTech(node.tech.techID);
                    //node.tech.UnlockTech(true);
            }
        }

        private void LockAll()
        {
            foreach (RDNode node in RDController.Instance.nodes)
            {
                ForceLockTech(node.tech.techID);
            }
        }

        private void DisableCosts()
        {
            OverrideCost = true;
        }

        private void TechUnlockEventTriggered(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> ev)
        {
            if (OverrideCost && ev.target != RDTech.OperationResult.Successful)
            {
                lastNode = ev.host.techID; //prevent giving free science
                //ev.host.UnlockTech(true);
                ForceUnlockTech(lastNode);
            }
            else if (OverrideCost && ev.target == RDTech.OperationResult.Successful && ev.host.techID != lastNode)
            {
                //refund any spent science
                lastNode = ev.host.techID;
                ResearchAndDevelopment.Instance.AddScience(ev.host.scienceCost, TransactionReasons.RnDTechResearch);
            }
        }

        private void ForceUnlockTech(string techID)
        {
            Debug.Log("[Toppler] Force unlocking " + techID);
            RDNode node = RDController.Instance.nodes.Find(n => n.tech.techID == techID);
            lastNode = techID;
            if (node != null)
                node.tech.UnlockTech(true);
            
        }

        private void ForceLockTech(string techID)
        {
            Debug.Log("[Toppler] Force locking " + techID);
            try
            {
                ProtoTechNode protoNode = ResearchAndDevelopment.Instance.GetTechState(techID);
                if (protoNode != null && protoNode.scienceCost > 0) //can't force close the initial tech
                {
                    protoNode.state = RDTech.State.Unavailable;
                    protoNode.partsPurchased.Clear();
                    ResearchAndDevelopment.Instance.SetTechState(techID, protoNode);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error while force locking node.");
                Debug.LogException(e);
            }
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
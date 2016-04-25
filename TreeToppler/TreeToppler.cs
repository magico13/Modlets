using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;

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
                    DialogGUIBase[] options = new DialogGUIBase[5];
                    options[0] = new DialogGUIButton("Unlock All Nodes", UnlockAllNoLvl);
                    options[1] = new DialogGUIButton("Unlock Up To Level", UnlockAllLvl);
                    options[2] = new DialogGUIButton("Disable Costs", DisableCosts);
                    options[3] = new DialogGUIButton("Lock All Nodes", LockAll);
                    options[4] = new DialogGUIButton("Cancel", DummyVoid);
                    MultiOptionDialog diag = new MultiOptionDialog("What would you like to do?", "Tree Toppler", null, options);
                    PopupDialog.SpawnPopupDialog(diag, false, HighLogic.UISkin);
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
                    node.tech.UnlockTech(true);
            }
        }

        private void LockAll()
        {
            foreach (RDNode node in RDController.Instance.nodes)
            {
                if (node.tech.scienceCost > 0)
                {
                    ProtoTechNode protoNode = ResearchAndDevelopment.Instance.GetTechState(node.tech.techID);
                    protoNode.state = RDTech.State.Unavailable;
                    protoNode.partsPurchased.Clear();
                    ResearchAndDevelopment.Instance.SetTechState(node.tech.techID, protoNode);
                }
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
                ev.host.UnlockTech(true);
            }
            else if (OverrideCost && ev.target == RDTech.OperationResult.Successful && ev.host.techID != lastNode)
            {
                //refund any spent science
                lastNode = ev.host.techID;
                ResearchAndDevelopment.Instance.AddScience(ev.host.scienceCost, TransactionReasons.RnDTechResearch);
            }
        }
    }
}
/*
Copyright (C) 2015  Michael Marvin

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
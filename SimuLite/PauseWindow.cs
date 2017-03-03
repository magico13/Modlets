using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    public class PauseWindow : WindowBase
    {
        public PauseWindow() : base(8235, "Paused", false, true) { }

        public override void Draw(int windowID)
        {
            base.Draw(windowID);
            //show button to:
            //revert to editor
            //restart
            //cancel

            GUILayout.BeginVertical();
            if (GUILayout.Button("Revert To Editor"))
            {
                base.Close();
                SimuLite.Instance.DeactivateSimulation(true);
            }
            //if (GUILayout.Button("Restart Simulation")) //This is a harder thing to do, but we have to do it eventually
            //{

            //}
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.EndVertical();
        }


        public override void Show()
        {
            base.Show();
            Pause();
        }

        public override void Close()
        {
            base.Close();
            UnPause();
        }

        public void UnPause()
        {
            FlightDriver.SetPause(false);
        }

        public void Pause()
        {
            FlightDriver.SetPause(true);
        }
    }
}

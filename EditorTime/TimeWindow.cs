using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EditorTime  
{
    public class TimeWindow
    {
        public bool visible = false;

        private bool mouseOver = false;

        private Settings settings;
        private TimeKeeper timeKeeper;

        public TimeWindow(Settings settings, TimeKeeper timeKeeper)
        {
            this.settings = settings;
            this.timeKeeper = timeKeeper;
        }

        public void Draw()
        {
            settings.timeWindow = GUILayout.Window(1936342, settings.timeWindow, Render, "Current Time", HighLogic.Skin.window);
        }

        private void Render(int windowID)
        {
            //All this defines the window itself
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); //This setup will center the text in the window

            TimeSpan? timer = timeKeeper.OutsourceTimer();
            if (timer.HasValue)
            {
                GUILayout.Label(timer.Value.Minutes + "m " + timer.Value.Seconds + "s ", HighLogic.Skin.label);
            }
            else if (mouseOver)
            {
                if (GUILayout.Button("Outsource!", HighLogic.Skin.button))
                    timeKeeper.Outsource();
            }
            else
            {
                GUILayout.Label(KSPUtil.PrintDateCompact((int)HighLogic.CurrentGame.flightState.universalTime, true, true), HighLogic.Skin.label);
            }
            
            if (Event.current.type == EventType.Repaint)
                mouseOver = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //Allow the window to be dragged around
            GUI.DragWindow();
        }
    }
}

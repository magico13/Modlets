using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace EditorTime
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class EditorTime : MonoBehaviour
    {
        string configFilePath = KSPUtil.ApplicationRootPath + "/GameData/EditorTime/PluginData/config.txt";
        float timeRatio = 1.0F;
        DateTime lastUpdate = DateTime.MaxValue;
        bool drawing = false;
        Rect timeWindow = new Rect((Screen.width / 2) + 300, -30, 125, 1);


        public void Start()
        {
            ConfigNode config = ConfigNode.Load(configFilePath);
            float x = (Screen.width / 2) + 300, y = -30;
            if (config != null)
            {
                //Get the timeRatio from the config file
                float.TryParse(config.GetValue("timeRatio"), out timeRatio);
                if (!float.TryParse(config.GetValue("WindowX"), out x))
                    x = (Screen.width / 2) + 300;
                if (!float.TryParse(config.GetValue("WindowY"), out y))
                    y = -30;
            }

            lastUpdate = DateTime.Now;
            timeWindow.x = x;
            timeWindow.y = y;

            Debug.Log("[EditorTime] timeRatio is " + timeRatio);
            if (!drawing)
            {
                //Draw the current time window
                //RenderingManager.AddToPostDrawQueue(0, DrawTimeWindow);
                drawing = true;
            }
        }

        public void FixedUpdate()
        {
            if (lastUpdate != DateTime.MaxValue)
            {
                //Get and save the current time 
                //(we don't call this repeatedly, as it might return slightly different values at various points in the execution and we don't want to lose any time)
                DateTime now = DateTime.Now;
                //Get the amount of time that has passed since the last update
                double timeDelta = (now - lastUpdate).TotalMilliseconds / 1000.0;
                //Multiply the time passed (in seconds) by the timeRatio
                timeDelta *= timeRatio;

                //Update the in-game time
                HighLogic.CurrentGame.flightState.universalTime += timeDelta;

                //Make sure we update the lastUpdate to now
                lastUpdate = now;
            }
        }

        public void OnDestroy()
        {
            lastUpdate = DateTime.MaxValue;
            //RenderingManager.RemoveFromPostDrawQueue(0, DrawTimeWindow);
            drawing = false;

            //Save the settings
            ConfigNode config = new ConfigNode();
            config.AddValue("timeRatio", timeRatio);
            config.AddValue("WindowX", timeWindow.x);
            config.AddValue("WindowY", timeWindow.y);
            config.Save(configFilePath);
        }

        public void OnGUI()
        {
            DrawTimeWindow();
        }

        public void DrawTimeWindow()
        {
            //Actually draw the window
            timeWindow = GUILayout.Window(1936342, timeWindow, TimeWindow, "Current Time", HighLogic.Skin.window);
        }

        public void TimeWindow(int windowID)
        {
            //All this defines the window itself
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); //This setup will center the text in the window
            GUILayout.Label(KSPUtil.PrintDateCompact((int)HighLogic.CurrentGame.flightState.universalTime, true, true), HighLogic.Skin.label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //Allow the window to be dragged around
            GUI.DragWindow();
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
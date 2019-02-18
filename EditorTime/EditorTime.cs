using System;
using UnityEngine;

namespace EditorTime
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)] 
    public class EditorTime : MonoBehaviour
    {
        public Settings settings;
        public TimeKeeper timeKeeper;
        public TimeWindow timeWindow;

        public void Awake()
        {
            settings = new Settings();
            timeKeeper = new TimeKeeper(settings);
            timeWindow = new TimeWindow(settings, timeKeeper);
        }

        public void Start()
        {
            settings.Initialize();

            timeKeeper.Start();

            timeWindow.visible = true;
        }

        public void FixedUpdate()
        {
            timeKeeper.Update();
        }

        public void OnDestroy()
        {
            timeKeeper.Stop();

            timeWindow.visible = false;

            settings.Save();
        }

        public void OnGUI()
        {
            timeWindow.Draw();
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
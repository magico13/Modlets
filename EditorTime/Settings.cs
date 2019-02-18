using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EditorTime 
{
    public class Settings
    {
        public float outsourceCost = 1000;
        public int outsourceTime = 10;
        public float timeRatio = 1;

        public Rect timeWindow = new Rect((Screen.width / 2) + 300, -30, 125, 1);

        //solution to possible future problems:
        //public bool isInitialized = false;

        string configFilePath = string.Empty;

        public Settings()
        {
            configFilePath = KSPUtil.ApplicationRootPath + "/GameData/EditorTime/PluginData/config.txt";
        }

        public void Initialize()
        {
            ConfigNode config = null;
            if (System.IO.File.Exists(configFilePath))
            {
                config = ConfigNode.Load(configFilePath);
            }
            float x = (Screen.width / 2) + 300, y = -30;
            if (config != null)
            {
                //no fallback is needed as all values have already been initialized elsewhere.
                float.TryParse(config.GetValue(nameof(outsourceCost)), out outsourceCost);
                int.TryParse(config.GetValue(nameof(outsourceTime)), out outsourceTime);
                float.TryParse(config.GetValue(nameof(timeRatio)), out timeRatio);
                float.TryParse(config.GetValue("WindowX"), out x);
                float.TryParse(config.GetValue("WindowY"), out y);
            }
            timeWindow.x = x;
            timeWindow.y = y;
        }

        public void Save()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configFilePath));

            //Save the settings
            ConfigNode config = new ConfigNode();
            config.AddValue(nameof(outsourceCost), outsourceCost);
            config.AddValue(nameof(outsourceTime), outsourceTime);
            config.AddValue(nameof(timeRatio), timeRatio);
            config.AddValue("WindowX", timeWindow.x);
            config.AddValue("WindowY", timeWindow.y);
            config.Save(configFilePath);
        }
    }
}

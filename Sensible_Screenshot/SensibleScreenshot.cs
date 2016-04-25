using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using MagiCore;

namespace SensibleScreenshot
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class SensibleScreenshot : MonoBehaviour
    {
        private List<string> ScreenShotsFolder = new List<string>();
        string ssfolder = KSPUtil.ApplicationRootPath + "Screenshots/";
        private bool DoCheck = false;
        private int timeout = 30, timer = 0;
        private Configuration config = new Configuration();

        public void UpdateFolderKnowledge()
        {
            ScreenShotsFolder.Clear();
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(ssfolder);
            System.IO.FileInfo[] info = dir.GetFiles();
            foreach (System.IO.FileInfo file in info)
            {
                ScreenShotsFolder.Add(file.Name);
            }
        }

        public string GetFileName()
        {
            string name = config.fileTemplate;
            //name = AddInfo(name);
            name = MagiCore.StringTranslation.AddFormatInfo(name, "SensibleScreenshot", config.dateFormat);
            if (config.fillSpaces)
                name = name.Replace(" ", config.spaceFiller);
            return name;
        }

        public System.IO.FileInfo CheckForNewFile()
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(ssfolder);
            System.IO.FileInfo[] info = dir.GetFiles();
            foreach (System.IO.FileInfo file in info)
            {
                if (!ScreenShotsFolder.Contains(file.Name))
                    return file;
            }
            return null;
        }

        public void DoWork()
        {
            DoCheck = false;
            timer = 0;
            System.IO.FileInfo file;
            string fileExt = config.convertToJPG ? ".jpg" : ".png";
            while ((file = CheckForNewFile()) != null)
            {
                //file = CheckForNewFile();
                //if (file != null)
                {
                    string newName = GetFileName();
                    string finalName = newName+fileExt;
                    bool taken = ScreenShotsFolder.Contains(finalName);
                    int i = 1;
                    while (taken)
                    {
                        finalName = newName + "-" + i.ToString() + fileExt;
                        taken = ScreenShotsFolder.Contains(finalName);
                        i++;
                    }
                    if (config.convertToJPG)
                    {
                        Debug.Log("Converting screenshot to JPG. New name: " + finalName);
                        ConvertToJPG(ssfolder + file.Name, ssfolder + finalName, config.JPGQuality);
                        if (!config.keepOrginalPNG)
                            file.Delete();
                        else
                        {
                            string pngName = finalName.Replace(".jpg", ".png");
                            file.MoveTo(ssfolder + pngName);
                            ScreenShotsFolder.Add(pngName);
                        }
                    }
                    else
                    {
                        Debug.Log("Renaming screenshot. New name: " + finalName);
                        file.MoveTo(ssfolder + finalName);
                    }
                    ScreenShotsFolder.Add(finalName);
                }
            }
        }

        public void ConvertToJPG(string originalFile, string newFile, int quality=75)
        {
            Texture2D png = new Texture2D(1, 1);
            byte[] pngData = System.IO.File.ReadAllBytes(originalFile);
            png.LoadImage(pngData);
            byte[] jpgData = png.EncodeToJPG(quality);
            var file = System.IO.File.Open(newFile, System.IO.FileMode.Create);
            var binary = new System.IO.BinaryWriter(file);
            binary.Write(jpgData);
            file.Close();
            Destroy(png);
            //Resources.UnloadAsset(png);
        }

        public void Update()
        {
            if (DoCheck && timer >= timeout)
                DoWork();
            else if (DoCheck)
                timer++;

            if (GameSettings.TAKE_SCREENSHOT.GetKey())
            {
                DoCheck = true;
            }
        }

        public void Start()
        {
            UpdateFolderKnowledge();
            config.Load();
            config.Save();
            timer = 0;
        }
    }

    public class Configuration
    {
        public string dateFormat = "yyyy-MM-dd--HH-mm-ss";
        public string fileTemplate = "screenshot_[date]";
        public bool convertToJPG = false;
        public int JPGQuality = 75;
        public bool fillSpaces = false;
        public string spaceFiller = "_";
        public bool keepOrginalPNG = false;

        private string filename = KSPUtil.ApplicationRootPath + "/GameData/SensibleScreenshot/settings.cfg";
        public void Save()
        {
            ConfigNode cfg = new ConfigNode();
            cfg.AddValue("DateString", dateFormat);
            cfg.AddValue("FileNameTemplate", fileTemplate);
            cfg.AddValue("ConvertToJPG", convertToJPG);
            cfg.AddValue("JPGQuality", JPGQuality);
            cfg.AddValue("KeepOrigPNG", keepOrginalPNG);
            cfg.AddValue("FillSpaces", fillSpaces);
            cfg.AddValue("ReplaceChar", spaceFiller);

            cfg.Save(filename);
        }

        public void Load()
        {
            if (System.IO.File.Exists(filename))
            {
                ConfigNode cfg = ConfigNode.Load(filename);
                dateFormat = cfg.GetValue("DateString");
                fileTemplate = cfg.GetValue("FileNameTemplate");
                bool.TryParse(cfg.GetValue("ConvertToJPG"), out convertToJPG);
                int.TryParse(cfg.GetValue("JPGQuality"), out JPGQuality);
                bool.TryParse(cfg.GetValue("KeepOrigPNG"), out keepOrginalPNG);
                bool.TryParse(cfg.GetValue("FillSpaces"), out fillSpaces);
                spaceFiller = cfg.GetValue("ReplaceChar");
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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DatedQuickSaves
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DatedQuickSaves : MonoBehaviour
    {
        private static bool DoCheck = false;
        private static int timer = 0, timeout = 60;
        private DateTime lastASTime = DateTime.Now;

        private List<string> SavedQSFiles = new List<string>();
        private List<string> SavedASFiles = new List<string>();

        private Configuration config = new Configuration();
        void Start()
        {
            config.Load();
            config.Save();

            GetKnownFiles();
            lastASTime = DateTime.Now;
        }

        void Update()
        {
            if (DoCheck && timer >= timeout)
                DoWork();
            else if (DoCheck)
                timer++;

            if (GameSettings.QUICKSAVE.GetKey() && !GameSettings.MODIFIER_KEY.GetKey()) //F5 but not Alt-F5
            {
                DoCheck = true;
            }

            if ((DateTime.Now - lastASTime).TotalMinutes >= config.autoSaveFreq)
            {
                DoAutoSave();
            }
        }

        void DoWork()
        {
            //Copy the quicksave file and append the in-game UT
            // (of the file, or the current one?)
            //let's use current UT for now

            DoCheck = false;
            timer = 0;

            string saveFolder = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder,
                quicksave = saveFolder+"/quicksave.sfs";

            if (!System.IO.File.Exists(quicksave))
                return;

           // string time = FormattedTime(Planetarium.GetUniversalTime());

           // string newName = "/quicksave_"+time+".sfs";
            string newName = MagiCore.StringTranslation.AddFormatInfo(config.fileTemplate, "DatedQuickSaves", config.dateFormat);

            System.IO.File.Copy(quicksave, saveFolder+"/"+newName+".sfs");
            Debug.Log("Copied quicksave to " + newName);

            SavedQSFiles.Add(newName);
            PurgeExtraneousFiles();
            SaveKnownFiles();
        }

        void DoAutoSave()
        {
            lastASTime = DateTime.Now;
            string newName = MagiCore.StringTranslation.AddFormatInfo(config.autoSaveTemplate, "DatedQuickSaves", config.dateFormat);

            GamePersistence.SaveGame(newName, HighLogic.SaveFolder, SaveMode.OVERWRITE);

            SavedASFiles.Add(newName);

            Debug.Log("[DQS] AutoSaving to " + newName + ". Tracking " + SavedASFiles.Count + " autosaves.");

            PurgeExtraneousFiles();
            SaveKnownFiles();
        }

        void GetKnownFiles()
        {
            SavedQSFiles.Clear();
            SavedASFiles.Clear();
            string saveFolder = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder+"/";
            if (System.IO.File.Exists(saveFolder+"DQS_DataBase.cfg"))
            {
                ConfigNode database = ConfigNode.Load(saveFolder + "DQS_DataBase.cfg");
                ConfigNode QSDB, ASDB;
                if (database.HasNode("QuickSaves"))
                {
                    QSDB = database.GetNode("QuickSaves");
                    SavedQSFiles = QSDB.GetValues("file").ToList();
                }
                if (database.HasNode("AutoSaves"))
                {
                    ASDB = database.GetNode("AutoSaves");
                    SavedASFiles = ASDB.GetValues("file").ToList();
                }
            }

        }

        void SaveKnownFiles()
        {
            string saveFolder = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/";

            ConfigNode database = new ConfigNode();
            ConfigNode QSDB = new ConfigNode("QuickSaves"), ASDB = new ConfigNode("AutoSaves");

            foreach (string file in SavedQSFiles)
            {
                QSDB.AddValue("file", file);
            }
            foreach (string file in SavedASFiles)
            {
                ASDB.AddValue("file", file);
            }

            database.AddNode("QuickSaves", QSDB);
            database.AddNode("AutoSaves", ASDB);

            database.Save(saveFolder + "DQS_DataBase.cfg");
        }

        void PurgeExtraneousFiles()
        {
            int tgtQS = config.maxQSFiles;
            int tgtAS = config.maxASFiles;

            string saveFolder = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/";
            int purgedQS = 0, purgedAS = 0;
            if (tgtQS >= 0) //if negative, then keep all files
            {
                while (SavedQSFiles.Count > tgtQS)
                {
                    //purge oldest (top one)
                    string oldest = SavedQSFiles[0]+".sfs";
                    if (System.IO.File.Exists(saveFolder+oldest))
                    {
                        System.IO.File.Delete(saveFolder + oldest);
                    }
                    SavedQSFiles.RemoveAt(0);
                    purgedQS++;
                }
            }
            if (tgtAS >= 0) //if negative, then keep all files
            {
                while (SavedASFiles.Count > tgtAS)
                {
                    //purge oldest (top one)
                    string oldest = SavedASFiles[0] + ".sfs";
                    if (System.IO.File.Exists(saveFolder + oldest))
                    {
                        System.IO.File.Delete(saveFolder + oldest);
                    }
                    SavedASFiles.RemoveAt(0);
                    purgedAS++;
                }
            }
            if (purgedQS > 0 || purgedAS > 0)
                Debug.Log("[DQS] Purged " + purgedQS + " QuickSaves and " + purgedAS + " AutoSaves.");

        }
    }

    public class Configuration
    {
        public string dateFormat = "yyyy-MM-dd--HH-mm-ss";
        public string fileTemplate = "quicksave_Y[year]D[day]H[hour]M[min]S[sec]";
        public string autoSaveTemplate = "autosave_Y[year]D[day]H[hour]M[min]S[sec]";
        
        public bool fillSpaces = false;
        public string spaceFiller = "_";

        public int maxQSFiles = 20, maxASFiles = 20;
        public int autoSaveFreq = 15;

        private string filename = KSPUtil.ApplicationRootPath + "/GameData/DatedQuickSaves/settings.cfg";
        public void Save()
        {
            ConfigNode cfg = new ConfigNode();
            cfg.AddValue("DateString", dateFormat);
            cfg.AddValue("FileNameTemplate", fileTemplate);
            cfg.AddValue("MaxQuickSaveCount", maxQSFiles);

            cfg.AddValue("AutoSaveTemplate", autoSaveTemplate);
            cfg.AddValue("AutoSaveFreq", autoSaveFreq);
            cfg.AddValue("MaxAutoSaveCount", maxASFiles);

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
                int.TryParse(cfg.GetValue("MaxQuickSaveCount"), out maxQSFiles);
                
                autoSaveTemplate = cfg.GetValue("AutoSaveTemplate");
                int.TryParse(cfg.GetValue("AutoSaveFreq"), out autoSaveFreq);
                int.TryParse(cfg.GetValue("MaxAutoSaveCount"), out maxASFiles);

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
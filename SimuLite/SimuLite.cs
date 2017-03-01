using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimuLite
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class SimuLiteLoader : MonoBehaviour
    {
        public void Awake()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            { //Don't load the backup if currently in the flight scene
                SimuLite.LoadBackupFile();
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class SimuLite : MonoBehaviour
    {
        public static SimuLite Instance { get; set; }
        public const string BACKUP_FILENAME = "SimuLite_backup";

        #region Public Properties
        public bool IsSimulating { get; set; } = false;
        public double RemainingCoreHours { get; set; } = 0;
        public double CurrentComplexity { get; set; } = 0;
        #endregion Public Properties


        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            showSimulationSetup = HighLogic.LoadedSceneIsEditor; //hacky hack for now
        }


        #region GUI Code
        public bool showSimulationSetup { get; set; }
        private Rect setupWindow = new Rect((Screen.width - 300) / 2, (Screen.height / 4), 300, 1);
        private SimulationConfigWindow simConfigWindow = new SimulationConfigWindow();

        private void OnGUI()
        {
            if (showSimulationSetup)
            {
                setupWindow = GUILayout.Window(8234, setupWindow, simConfigWindow.Draw, "Simulation Setup");
            }
        }
        #endregion GUI Code

        #region Public Methods
        public void SetSimulationProperties(double complexity)
        {
            IsSimulating = true;
            CurrentComplexity = complexity;
        }
        #endregion Public Methods

        #region Static Methods
        public static bool LoadBackupFile()
        {
            string finalPath = Path.Combine(HighLogic.SaveFolder, BACKUP_FILENAME);
            if (File.Exists(finalPath))
            { //Load the backup file if it exists
                ConfigNode lastShip = ShipConstruction.ShipConfig;
                EditorFacility lastEditor = HighLogic.CurrentGame.editorFacility;

                Game newGame = GamePersistence.LoadGame(BACKUP_FILENAME, HighLogic.SaveFolder, true, false);
                GamePersistence.SaveGame(newGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                GameScenes targetScene = HighLogic.LoadedScene;
                newGame.startScene = targetScene;

                // This has to be before... newGame.Start()
                if (targetScene == GameScenes.EDITOR)
                {
                    newGame.editorFacility = lastEditor;
                }


                newGame.Start();

                // ... And this has to be after. <3 KSP
                if (targetScene == GameScenes.EDITOR)
                {
                    EditorDriver.StartupBehaviour = EditorDriver.StartupBehaviours.LOAD_FROM_CACHE;
                    ShipConstruction.ShipConfig = lastShip;
                }

                File.Delete(finalPath);

                return true;
            }
            return false;
        }

        public static void MakeBackupFile()
        {
            GamePersistence.SaveGame(BACKUP_FILENAME, HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }
        #endregion Static Methods
    }
}

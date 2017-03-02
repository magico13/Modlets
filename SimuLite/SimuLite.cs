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
        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            { //Don't load the backup if currently in the flight scene
                SimuLite.LoadBackupFile(HighLogic.LoadedScene);
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class SimuLite : MonoBehaviour
    {
        public static SimuLite Instance { get; private set; }
        public const string BACKUP_FILENAME = "SimuLite_backup";

        #region Fields
        private double lastUT = -1;
        #endregion Fields


        #region Public Properties
        
        #endregion Public Properties


        public void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            else
            {
                Instance = this;
            }
            if (HighLogic.LoadedSceneIsEditor) //hacky hack for now
            {
                simConfigWindow.Show();
            }

        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || !StaticInformation.IsSimulating)
            {
                return;
            }
            //remove some corehours based on how much time has passed since the last frame
            double UT = Planetarium.GetUniversalTime();
            StaticInformation.RemainingCoreHours -= (UT - lastUT) * StaticInformation.CurrentComplexity;
            lastUT = UT;

            if (StaticInformation.RemainingCoreHours <= 0)
            {
                //pause. Popup message saying out of time, purchase more or revert
            }


            if (PauseMenu.isOpen) //close the regular pause menu
            {
                PauseMenu.Close();
            }
            if (GameSettings.PAUSE.GetKey()) //if paused, show our window
            {   
                pauseWindow.Show(); //show ours
            }

        }

        #region GUI Code
        private SimulationConfigWindow simConfigWindow = new SimulationConfigWindow();
        private PauseWindow pauseWindow = new PauseWindow();

        private void OnGUI()
        {
            simConfigWindow.OnGUIHandler();
            pauseWindow.OnGUIHandler();
        }
        #endregion GUI Code

        #region Public Methods
        public void ActivateSimulation(double complexity)
        {
            MakeBackupFile();
            StaticInformation.IsSimulating = true;
            StaticInformation.CurrentComplexity = complexity;
            activateSimulationLocks();
            lastUT = Planetarium.GetUniversalTime();
        }

        public void DeactivateSimulation(bool returnToEditor)
        {
            if (!StaticInformation.IsSimulating)
            {
                return;
            }

            StaticInformation.IsSimulating = false;

            deactivateSimulationLocks();

            GameScenes targetScene = HighLogic.LoadedScene;
            if (returnToEditor) //if we should return to the editor, then do that rather than the current scene
            {
                targetScene = GameScenes.EDITOR;
            }

            LoadBackupFile(targetScene);
        }

        public static bool LoadBackupFile(GameScenes targetScene)
        {
            string finalPath = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/" + BACKUP_FILENAME + ".sfs";
            if (File.Exists(finalPath))
            { //Load the backup file if it exists
                ConfigNode lastShip = ShipConstruction.ShipConfig;
                EditorFacility lastEditor = HighLogic.CurrentGame.editorFacility;

                Game newGame = GamePersistence.LoadGame(BACKUP_FILENAME, HighLogic.SaveFolder, true, false);
                GamePersistence.SaveGame(newGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                //GameScenes targetScene = HighLogic.LoadedScene;
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

        public void MakeBackupFile()
        {
            GamePersistence.SaveGame(BACKUP_FILENAME, HighLogic.SaveFolder, SaveMode.OVERWRITE);
        }

        #endregion Public Methods

        #region Private Methods
        private void activateSimulationLocks()
        {
            string pre = "SIMULITE_";
            InputLockManager.SetControlLock(ControlTypes.QUICKLOAD, pre + "QUICKLOAD");
            InputLockManager.SetControlLock(ControlTypes.QUICKSAVE, pre + "QUICKSAVE");
        }

        private void deactivateSimulationLocks()
        {
            string pre = "SIMULITE_";
            InputLockManager.RemoveControlLock(pre + "QUICKLOAD");
            InputLockManager.RemoveControlLock(pre + "QUICKSAVE");
        }

        #endregion Private Methods
    }
}

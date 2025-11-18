using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoSaveDelete
{
    [BepInPlugin("com.vaniiaaa.nosavedelete", "NoSaveDelete", "1.0.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;
            var harmony = new Harmony("com.vaniiaaa.nosavedelete");
            harmony.PatchAll();
            logger.LogInfo("NoSaveDelete is loaded");
        }


        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class DataDirectorSaveDeleteCheckPatch
        {
            static bool Prefix(bool _leaveGame)
            {
                logger.LogInfo("SaveDeleteCheck blocked");
                return false;
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
        public class StatsManagerSaveFileDeletePatch
        {
            static bool Prefix(string saveFileName)
            {
                logger.LogInfo("SaveFileDelete blocked");
                return false;
            }
        }

        [HarmonyPatch(typeof(RunManager), "ResetProgress")]
        public class ResetProgressPatch
        {
            static bool Prefix()
            {
                logger.LogInfo("ResetProgress blocked - progress preserved");
                return false;
            }
        }

        [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
        public class ChangeLevelPatch
        {
            static void Prefix(ref bool completedLevel, ref bool levelFailed)
            {
                
                if (levelFailed && RunManager.instance.levelCurrent == RunManager.instance.levelArena)
                {
                    completedLevel = false;
                    levelFailed = false; 
                    logger.LogInfo("ChangeLevel after Arena - levelFailed flag reset to prevent progress loss");
                }
            }
        }
    }
}

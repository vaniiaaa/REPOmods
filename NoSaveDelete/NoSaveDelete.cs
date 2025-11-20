using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoSaveDelete
{
    [BepInPlugin("com.vaniiaaa.nosavedelete", "NoSaveDelete", "2.6.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static bool blockSaves = false;

        private void Awake()
        {
            logger = Logger;
            var harmony = new Harmony("com.vaniiaaa.nosavedelete");
            harmony.PatchAll();
            logger.LogInfo("NoSaveDelete v2.6.0 loaded");
        }

        [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
        public class RunManagerChangeLevelPatch
        {
            static void Prefix(bool _completedLevel, bool _levelFailed, RunManager __instance)
            {
                if (_levelFailed && __instance.levelCurrent != __instance.levelArena)
                {
                    blockSaves = true;
                    logger.LogInfo("Level failed - saves blocked");
                }
                else if (_completedLevel && !_levelFailed)
                {
                    blockSaves = false;
                    logger.LogInfo("Level completed - saves unblocked");
                }
            }
        }

        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class DataDirectorSaveDeleteCheckPatch
        {
            static bool Prefix()
            {
                if (SemiFunc.RunIsArena() || blockSaves)
                {
                    logger.LogInfo("SaveDeleteCheck blocked");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
        public class StatsManagerSaveFileDeletePatch
        {
            static bool Prefix(string saveFileName)
            {
                if (SemiFunc.RunIsArena() || blockSaves)
                {
                    logger.LogInfo($"SaveFileDelete blocked (file: {saveFileName})");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RunManager), "ResetProgress")]
        public class ResetProgressPatch
        {
            static bool Prefix()
            {
                if (SemiFunc.RunIsArena())
                {
                    logger.LogInfo("ResetProgress blocked - Arena active");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveGame")]
        public class StatsManagerSaveGamePatch
        {
            static bool Prefix(string fileName)
            {
                if (SemiFunc.RunIsArena() || blockSaves)
                {
                    logger.LogInfo($"SaveGame blocked (file: {fileName})");
                    return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(StatsManager), "SaveFileSave")]
        public class StatsManagerSaveFileSavePatch
        {
            static bool Prefix()
            {
                if (SemiFunc.RunIsArena() || blockSaves)
                {
                    logger.LogInfo("SaveFileSave blocked");
                    return false;
                }
                return true;
            }
        }
    }
}

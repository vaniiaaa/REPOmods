using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoSaveDelete
{
    [BepInPlugin("com.vaniiaaa.nosavedelete", "NoSaveDelete", "2.1.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;
            var harmony = new Harmony("com.vaniiaaa.nosavedelete");
            harmony.PatchAll();
            logger.LogInfo("NoSaveDelete v2.1.0 loaded");
        }

        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class DataDirectorSaveDeleteCheckPatch
        {
            static bool Prefix(bool _leaveGame)
            {
                if (SemiFunc.RunIsArena())
                {
                    logger.LogInfo("SaveDeleteCheck blocked - Arena active");
                    return false;
                }
                
                logger.LogInfo("SaveDeleteCheck allowed - normal cleanup");
                return true;
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
        public class StatsManagerSaveFileDeletePatch
        {
            static bool Prefix(string saveFileName)
            {
                if (SemiFunc.RunIsArena())
                {
                    logger.LogInfo($"SaveFileDelete blocked - Arena (file: {saveFileName})");
                    return false;
                }
                
                logger.LogInfo($"SaveFileDelete allowed (file: {saveFileName})");
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
                if (SemiFunc.RunIsArena())
                {
                    logger.LogInfo($"SaveGame blocked - Arena active (file: {fileName})");
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
                if (SemiFunc.RunIsArena())
                {
                    logger.LogInfo("SaveFileSave blocked - Arena");
                    return false;
                }
                
                return true;
            }
        }
    }
}

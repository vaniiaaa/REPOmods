using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace NoSaveDelete
{
    [BepInPlugin("com.vaniiaaa.nosavedelete", "NoSaveDelete", "2.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static bool blockSaves = false;
        private static bool needsReload = false;
        private static float reloadTimer = 0f;

        private void Awake()
        {
            logger = Logger;
            var harmony = new Harmony("com.vaniiaaa.nosavedelete");
            harmony.PatchAll();
            logger.LogInfo("NoSaveDelete loaded");
        }

        private void Update()
        {
            if (needsReload)
            {
                bool isLevelGenerated = false;
                var levelGenType = AccessTools.TypeByName("LevelGenerator");
                
                if (levelGenType != null)
                {
                    var instanceProp = AccessTools.Property(levelGenType, "Instance");
                    var instance = instanceProp?.GetValue(null);
                    
                    if (instance != null)
                    {
                        var generatedField = AccessTools.Field(levelGenType, "Generated");
                        if (generatedField != null)
                        {
                            isLevelGenerated = (bool)generatedField.GetValue(instance);
                        }
                    }
                }

                if (isLevelGenerated)
                {
                    reloadTimer += Time.deltaTime;
                    
                
                    if (reloadTimer > 0.1f)
                    {
                        blockSaves = false;
                        
                        if (StatsManager.instance != null)
                        {
                            string currentSave = Traverse.Create(StatsManager.instance).Field("saveFileCurrent").GetValue<string>();
                            
                            if (!string.IsNullOrEmpty(currentSave))
                            {
                                logger.LogInfo($"Restoring save file: {currentSave}");
                                StatsManager.instance.LoadGame(currentSave, null);
                                
                                needsReload = false;
                                reloadTimer = 0f;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
        public class RunManagerChangeLevelPatch
        {
            static void Prefix(bool _completedLevel, bool _levelFailed, RunManager __instance)
            {

                if (__instance.levelCurrent == __instance.levelLobbyMenu) return;


                if (_levelFailed && __instance.levelCurrent != __instance.levelArena)
                {
                    blockSaves = true;
                    needsReload = true;
                    reloadTimer = 0f;
                    logger.LogInfo("Level failed. Saves blocked. Reload scheduled.");
                }
            }
        }

        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class DataDirectorSaveDeleteCheckPatch
        {
            static bool Prefix()
            {
                return !(SemiFunc.RunIsArena() || blockSaves);
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
        public class StatsManagerSaveFileDeletePatch
        {
            static bool Prefix(string saveFileName)
            {
                return !(SemiFunc.RunIsArena() || blockSaves);
            }
        }

        [HarmonyPatch(typeof(RunManager), "ResetProgress")]
        public class ResetProgressPatch
        {
            static bool Prefix()
            {
                return !SemiFunc.RunIsArena();
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveGame")]
        public class StatsManagerSaveGamePatch
        {
            static bool Prefix(string fileName)
            {
                return !(SemiFunc.RunIsArena() || blockSaves);
            }
        }
        
        [HarmonyPatch(typeof(StatsManager), "SaveFileSave")]
        public class StatsManagerSaveFileSavePatch
        {
            static bool Prefix()
            {
                return !(SemiFunc.RunIsArena() || blockSaves);
            }
        }
    }
}

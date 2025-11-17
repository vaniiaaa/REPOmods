using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace NoSaveDelete
{
    [BepInPlugin("com.Vaniiaaa.nosavedelete", "NoSaveDelete", "1.0.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            
            var harmony = new Harmony("com.Vaniiaaa.nosavedelete");
            
            // Патчим оба метода для надежности
            try
            {
                harmony.PatchAll();
                Logger.LogInfo("=== NoSaveDelete загружен! ===");
                Logger.LogInfo("Патчи применены к DataDirector.SaveDeleteCheck и StatsManager.SaveFileDelete");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Ошибка при применении патчей: {e.Message}");
            }
        }

        // Патч 1: Блокируем удаление в DataDirector
        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class DataDirectorSaveDeleteCheckPatch
        {
            static bool Prefix(bool _leaveGame)
            {
                Log.LogWarning("=== DataDirector.SaveDeleteCheck ЗАБЛОКИРОВАН ===");
                Log.LogInfo($"Параметр _leaveGame = {_leaveGame}");
                
                return false; // Блокируем удаление
            }
        }

        // Патч 2: Блокируем удаление в StatsManager (на всякий случай)
        [HarmonyPatch(typeof(StatsManager), "SaveFileDelete")]
        public class StatsManagerSaveFileDeletePatch
        {
            static bool Prefix(string saveFileName)
            {
                Log.LogWarning("=== StatsManager.SaveFileDelete ЗАБЛОКИРОВАН ===");
                Log.LogInfo($"Попытка удалить: {saveFileName}");
                
                return false; // Блокируем удаление
            }
        }
    }
}

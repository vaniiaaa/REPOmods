using BepInEx;
using HarmonyLib;

namespace NoSaveDelete
{
    [BepInPlugin("com.Vaniiaaa.nosavedelete", "NoSaveDelete", "1.0.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {

        [HarmonyPatch(typeof(StatsManager), "SaveDeleteCheck")]
        public class SaveDeleteCheckPatch
        {
            static bool Prefix()
            {
                return false; // Всегда блокировать удаление
            }
        }
    }
}

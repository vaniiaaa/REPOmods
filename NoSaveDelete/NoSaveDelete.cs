using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NoSaveDelete
{
    [BepInPlugin("com.Vaniiaaa.nosavedelete", "NoSaveDelete", "1.0.0")]
    public class NoSaveDeletePlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private static ConfigEntry<bool> enableBackups;
        private static string lastBackupPath = null;
        private static bool shouldRestore = false;
        private static bool isRestoring = false; // Флаг для предотвращения рекурсии

        private static FieldInfo allPlayersDeadField;
        private static FieldInfo saveFileCurrentField;

        private void Awake()
        {
            Log = Logger;
            
            enableBackups = Config.Bind("Settings",
                "EnableBackups",
                true,
                "Включить автоматическое создание бэкапов при загрузке уровня");

            allPlayersDeadField = typeof(RunManager).GetField("allPlayersDead", 
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
            saveFileCurrentField = typeof(StatsManager).GetField("saveFileCurrent", 
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (allPlayersDeadField == null)
                Logger.LogError("Не удалось найти поле allPlayersDead!");
            
            if (saveFileCurrentField == null)
                Logger.LogError("Не удалось найти поле saveFileCurrent!");

            var harmony = new Harmony("com.Vaniiaaa.nosavedelete");
            harmony.PatchAll();
            
            Logger.LogInfo("NoSaveDelete загружен!");
        }

        [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
        public class ChangeLevelPatch
        {
            static void Prefix(bool _completedLevel, bool _levelFailed)
            {
                if (!enableBackups.Value || !SemiFunc.IsMasterClientOrSingleplayer())
                    return;

                var runManager = RunManager.instance;
                if (runManager == null) return;

                // Не создаем бэкап если это арена или переход в меню
                if (runManager.levelCurrent == runManager.levelArena ||
                    runManager.levelCurrent == runManager.levelLobbyMenu ||
                    runManager.levelCurrent == runManager.levelMainMenu ||
                    runManager.levelCurrent == runManager.levelLobby)
                {
                    return;
                }

                bool allPlayersDead = (bool)allPlayersDeadField.GetValue(runManager);

                if (_levelFailed && allPlayersDead)
                {
                    shouldRestore = true;
                    Log.LogInfo("Смерть обнаружена! Будет восстановлен бэкап.");
                }
                else if (!_levelFailed)
                {
                    CreateBackup();
                    shouldRestore = false;
                }
            }
        }

        [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
        public class SaveDeleteCheckPatch
        {
            static bool Prefix(bool _leaveGame)
            {
                Log.LogInfo($"SaveDeleteCheck перехвачен! _leaveGame = {_leaveGame}");
                Log.LogInfo("Удаление сохранения заблокировано.");
                
                return false;
            }
        }

        [HarmonyPatch(typeof(StatsManager), "LoadGame")]
        public class LoadGamePatch
        {
            static void Postfix(string fileName)
            {
                // КРИТИЧНО: Пропускаем если уже в процессе восстановления
                if (isRestoring)
                {
                    Log.LogInfo("LoadGame вызван во время восстановления, пропускаем.");
                    return;
                }

                if (!enableBackups.Value || !shouldRestore)
                    return;

                if (lastBackupPath != null && File.Exists(lastBackupPath))
                {
                    RestoreBackup(fileName);
                }
            }
        }

        [HarmonyPatch(typeof(StatsManager), "SaveGame")]
        public class SaveGamePatch
        {
            static bool Prefix(string fileName)
            {
                if (!enableBackups.Value)
                    return true;

                if (shouldRestore)
                {
                    Log.LogInfo("Сохранение заблокировано - ожидается восстановление бэкапа.");
                    return false;
                }

                return true;
            }
        }

        private static void CreateBackup()
        {
            try
            {
                var statsManager = StatsManager.instance;
                if (statsManager == null)
                {
                    Log.LogWarning("StatsManager.instance == null");
                    return;
                }

                string saveFileCurrent = (string)saveFileCurrentField.GetValue(statsManager);
                
                if (string.IsNullOrEmpty(saveFileCurrent))
                {
                    Log.LogWarning("Не удалось создать бэкап: нет текущего файла сохранения.");
                    return;
                }

                string savePath = Path.Combine(Application.persistentDataPath, "saves", saveFileCurrent, saveFileCurrent + ".es3");

                if (!File.Exists(savePath))
                {
                    Log.LogWarning($"Файл сохранения не найден: {savePath}");
                    return;
                }

                string backupDir = Path.Combine(Application.persistentDataPath, "saves", saveFileCurrent);
                lastBackupPath = Path.Combine(backupDir, saveFileCurrent + "_MODBACKUP.es3");

                File.Copy(savePath, lastBackupPath, true);
                
                Log.LogInfo($"Бэкап создан: {lastBackupPath}");
            }
            catch (System.Exception e)
            {
                Log.LogError($"Ошибка при создании бэкапа: {e.Message}");
            }
        }

        private static void RestoreBackup(string fileName)
        {
            // Предотвращаем повторный вход
            if (isRestoring)
            {
                Log.LogWarning("RestoreBackup уже выполняется, пропускаем повторный вызов.");
                return;
            }

            isRestoring = true;

            try
            {
                if (string.IsNullOrEmpty(lastBackupPath) || !File.Exists(lastBackupPath))
                {
                    Log.LogWarning("Бэкап для восстановления не найден.");
                    return;
                }

                string savePath = Path.Combine(Application.persistentDataPath, "saves", fileName, fileName + ".es3");

                // Восстанавливаем бэкап
                File.Copy(lastBackupPath, savePath, true);
                
                Log.LogInfo($"Бэкап восстановлен из {lastBackupPath} в {savePath}");
                
                // Сбрасываем флаг ПЕРЕД вызовом LoadGame
                shouldRestore = false;
                
                // Перезагружаем игру из восстановленного файла
                StatsManager.instance.LoadGame(fileName, null);
            }
            catch (System.Exception e)
            {
                Log.LogError($"Ошибка при восстановлении бэкапа: {e.Message}");
            }
            finally
            {
                // Всегда сбрасываем флаги в конце
                isRestoring = false;
            }
        }
    }
}

#pragma warning disable CS0618
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MusMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        internal static Plugin Instance;
        private static AudioClip cachedClip;
        private static string customMusicPath;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} загружен!");

            customMusicPath = Path.Combine(Paths.PluginPath, "MusMod", "menu_music.ogg");
            Logger.LogInfo($"Путь к музыке: {customMusicPath}");
            
            // Загружаем звук сразу при старте плагина
            StartCoroutine(LoadCustomMusic());
            
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        IEnumerator LoadCustomMusic()
        {
            Logger.LogInfo($"Начинаем загрузку: {customMusicPath}");

            if (!File.Exists(customMusicPath))
            {
                Logger.LogError($"Файл не найден: {customMusicPath}");
                yield break;
            }

            // Важно: заменяем обратные слеши на прямые
            string fileUrl = "file:///" + customMusicPath.Replace("\\", "/");
            Logger.LogInfo($"URL файла: {fileUrl}");

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.OGGVORBIS))
            {
                // Важно: отключаем стриминг для полной загрузки
                DownloadHandlerAudioClip handler = (DownloadHandlerAudioClip)request.downloadHandler;
                handler.streamAudio = false;
                
                Logger.LogInfo("Отправляем запрос...");
                yield return request.SendWebRequest();

                Logger.LogInfo($"Запрос завершён. Результат: {request.result}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    cachedClip = DownloadHandlerAudioClip.GetContent(request);
                    
                    if (cachedClip != null)
                    {
                        cachedClip.name = "custom main menu";
                        Logger.LogInfo($"✓ Кастомная музыка загружена! Длина: {cachedClip.length:F2} сек");
                    }
                    else
                    {
                        Logger.LogError("GetContent вернул null!");
                    }
                }
                else
                {
                    Logger.LogError($"Ошибка загрузки: {request.error}");
                    Logger.LogError($"Response code: {request.responseCode}");
                }
            }
        }

        // Патч: заменяем clip при назначении
        [HarmonyPatch(typeof(AudioSource), nameof(AudioSource.clip), MethodType.Setter)]
        [HarmonyPostfix]
        static void ReplaceAudioClip(AudioSource __instance)
        {
            // Проверяем, что это музыка главного меню
            if (__instance.clip != null && __instance.clip.name == "msc main menu")
            {
                Logger.LogInfo("Обнаружена музыка главного меню!");
                
                if (cachedClip != null)
                {
                    Logger.LogInfo("Заменяем на кастомную музыку!");
                    __instance.clip = cachedClip;
                    
                    // Если AudioSource уже играет, перезапускаем
                    if (__instance.isPlaying)
                    {
                        __instance.Stop();
                        __instance.Play();
                    }
                }
                else
                {
                    Logger.LogWarning("Кастомный клип ещё не загружен, ждём...");
                    
                    // Запускаем корутину ожидания через Instance
                    if (Instance != null)
                    {
                        Instance.StartCoroutine(WaitAndReplace(__instance));
                    }
                }
            }
        }

        // Ждём загрузки и заменяем клип
        static IEnumerator WaitAndReplace(AudioSource source)
        {
            Logger.LogInfo("Ожидаем загрузку кастомной музыки...");
            
            int attempts = 0;
            while (cachedClip == null && attempts < 100) // 10 секунд максимум
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (cachedClip != null && source != null)
            {
                Logger.LogInfo("✓ Загрузка завершена, применяем музыку!");
                source.Stop();
                source.clip = cachedClip;
                source.Play();
            }
            else
            {
                Logger.LogError($"✗ Не удалось загрузить музыку за {attempts * 0.1f:F1} секунд");
            }
        }
    }
}

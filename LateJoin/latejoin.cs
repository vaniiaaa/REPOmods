using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace LateJoinMod
{
    [BepInPlugin("com.vaniiaaa.latejoin", "LateJoinMod", "1.0.0")]
    public class LateJoinPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("com.vaniiaaa.latejoin");
            harmony.PatchAll();
            Logger.LogInfo("Late Join Mod loaded and patches applied");
        }
    }

    [HarmonyPatch(typeof(SteamManager))]
    public static class SteamManagerPatches
    {
        [HarmonyPatch("LockLobby")]
        [HarmonyPrefix]
        public static bool LockLobby_Prefix(SteamManager __instance)
        {
            Debug.Log("[LateJoin] Попытка игры закрыть лобби предотвращена");
            if (__instance.currentLobby.Id.IsValid)
            {
                __instance.currentLobby.SetJoinable(true);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(NetworkManager))]
    public static class NetworkManagerPatches
    {
        [HarmonyPatch("OnPlayerEnteredRoom")]
        [HarmonyPostfix]
        public static void OnPlayerEnteredRoom_Postfix(NetworkManager __instance, Player newPlayer)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (RunManager.instance.levelCurrent != RunManager.instance.levelLobby && 
                RunManager.instance.levelCurrent != RunManager.instance.levelLobbyMenu)
            {
                Debug.Log($"[LateJoin] Игрок {newPlayer.NickName} подключился во время игры. Начинаем синхронизацию...");
                
                __instance.StartCoroutine(SyncLateJoiner(__instance, newPlayer));
            }
        }

        private static IEnumerator SyncLateJoiner(NetworkManager netManager, Player newPlayer)
        {
            yield return new WaitForSeconds(0.5f);
            string currentLevelName = RunManager.instance.levelCurrent.name;
            int levelsCompleted = RunManager.instance.levelsCompleted;
            bool gameOver = RunManager.instance.gameOver;
            RunManager.instance.runManagerPUN.photonView.RPC("UpdateLevelRPC", newPlayer, 
                currentLevelName, 
                levelsCompleted, 
                gameOver
            );
            Debug.Log($"[LateJoin] Отправлен уровень: {currentLevelName}");

            yield return new WaitForSeconds(2.0f);

            PunManager.instance.SyncAllDictionaries();
            
            if (RoundDirector.instance)
                PunManager.instance.photonView.RPC("SyncHaul", newPlayer, RoundDirector.instance.totalHaul);
            
            if (ShopManager.instance)
                PunManager.instance.photonView.RPC("UpdateShoppingCostRPC", newPlayer, ShopManager.instance.totalCost);

            Debug.Log("[LateJoin] Данные синхронизированы.");

            netManager.photonView.RPC("AllPlayerSpawnedRPC", newPlayer);
            Debug.Log("[LateJoin] Отправлен спавн-сигнал.");
        }
    }

    [HarmonyPatch(typeof(NetworkManager), "Update")]
    public static class NetworkManagerUpdatePatch
    {
        private static float _timer;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;

            _timer += Time.deltaTime;
            if (_timer >= 2.0f)
            {
                _timer = 0f;
                
                if (!PhotonNetwork.CurrentRoom.IsOpen)
                {
                    PhotonNetwork.CurrentRoom.IsOpen = true;
                    PhotonNetwork.CurrentRoom.IsVisible = true;
                }
            }
        }
    }
}

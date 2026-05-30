using RPG.Core;
using RPG.Network;
using RPG.Network.Dto;
using System;
using System.Threading.Tasks;
using UnityEngine;
namespace RPG.Services
{
    public class PlayerService : MonoBehaviour
    {
        public static PlayerService Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Charger le profil depuis l'API
        // Appelé après chaque login réussi.
        // Remplit GameManager.Instance.Player avec les vraies données de la BDD.
        public async Task<bool> LoadProfileAsync()
        {
            try
            {
                var profile = await ApiClient.Instance.GetAsync<PlayerProfileResponse>(
                "/api/playerprofile/me"
                );
                // On transfère les données dans le GameManager (notre mémoire locale)
                GameManager.Instance.SetPlayer(profile);
                Debug.Log($"[Player] Profil chargé : {profile.characterName}, Level {profile.level}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Player] Échec du chargement : {e.Message}");
                return false;
            }
        }
        // Créer le profil (une seule fois, juste après le register)
        // Après cet appel, le personnage existe en BDD et peut être chargé.
        public async Task<bool> CreateProfileAsync(int userId, string characterName)
        {
            try
            {
                await ApiClient.Instance.PostAsync<PlayerProfileResponse>(
                    "/api/playerprofile",
                    new CreatePlayerProfileRequest
                    {
                        userId = userId,
                        characterName = characterName
                    }
                );
                Debug.Log($"[Player] Profil créé : {characterName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Player] Échec de la création : {e.Message}");
                return false;
            }
        }
        // Synchroniser l'état vers le serveur
        // Appelé à chaque checkpoint, fin de combat, changement de zone.
        // Envoie l'état COMPLET du joueur — le serveur écrase tout ce qui est en BDD.
        public async Task<bool> SyncAsync()
        {
            try
            {
                // BuildSyncPayload lit l'état courant du GameManager
                var payload = GameManager.Instance.BuildSyncPayload();
                var response = await ApiClient.Instance.PostAsync<SyncResponse>(
                "/api/playerprofile/sync",
                payload
                );
                if (response.success)
                    Debug.Log("[Player] Synchronisation réussie.");
                else
                    Debug.LogWarning($"[Player] Sync refusée : {response.message}");
                return response.success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Player] Exception lors du sync : {e.Message} | {e.InnerException?.Message}");
                return false;
            }
        }
        public async Task<GameSaveResponse> LoadLastSaveAsync()
        {
            try
            {
                var wrapper = await ApiClient.Instance.GetAsync<GameSaveArrayWrapper>("/api/gamesaves/me");
                if (wrapper.items == null || wrapper.items.Length == 0) return null;

                // Prendre la save la plus récente
                GameSaveResponse latest = wrapper.items[0];
                foreach (var save in wrapper.items)
                    if (string.Compare(save.savedAt, latest.savedAt) > 0)
                        latest = save;

                return latest;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Player] LoadLastSave : {e.Message}");
                return null;
            }
        }
    }
}
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class FirestoreService : MonoBehaviour
{
    public static FirestoreService Instance { get; private set; }

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    public bool IsInitialized { get; private set; } = false;

    private const string PETS_COLLECTION = "pets";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeFirestore()
    {
        auth = FirebaseAuth.DefaultInstance;

        try
        {
            db = FirebaseFirestore.DefaultInstance;

            if (db == null)
            {
                Debug.LogError("[FirestoreService] ❌ FirebaseFirestore.DefaultInstance devolvió NULL. Verifica google-services.json");
                IsInitialized = false;
                return;
            }

            IsInitialized = true;
            Debug.Log("[FirestoreService] Firestore Inicializado.");
        }
        catch (Exception e)
        {
            IsInitialized = false;
            Debug.LogError("[FirestoreService] ❌ Error inicializando Firestore: " + e.Message);
        }
    }


    public async Task<(bool success, string errorMessage)> SavePetAsync(PetModel pet)
    {
        if (!IsInitialized) return (false, "Firestore no está inicializado.");
        if (auth.CurrentUser == null) return (false, "Usuario no autenticado.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            string json = JsonConvert.SerializeObject(pet);
            var map = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            await db.Collection(PETS_COLLECTION).Document(userId).SetAsync(map);

            return (true, null);
        }
        catch (Exception e)
        {
            return (false, "Error guardando mascota: " + e.Message);
        }
    }

    public async Task<(PetModel pet, bool success, string errorMessage)> LoadPetAsync()
    {
        if (auth?.CurrentUser == null)
            return (null, false, "Usuario no autenticado.");

        if (db == null)
            return (null, false, "Firestore no inicializado.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            DocumentReference docRef = db.Collection("pets").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                return (null, false, "Documento no existe.");
            }

            // --- Protección CRÍTICA: snapshot.ToDictionary() puede retornar null ---
            var petMap = snapshot.ToDictionary();

            if (petMap == null || petMap.Count == 0)
            {
                Debug.LogWarning("[Firestore] Documento existe pero esta vacio.");
                return (null, false, "Datos de mascota vacios.");
            }

            // Convertimos dict → JSON → PetModel de forma segura
            string json = JsonConvert.SerializeObject(petMap);
            var pet = JsonConvert.DeserializeObject<PetModel>(json);

            if (pet == null)
            {
                Debug.LogError("[Firestore] Fallo al deserializar PetModel.");
                return (null, false, "Error al leer datos.");
            }

            return (pet, true, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] Error al cargar mascota: {e.Message}");
            return (null, false, "Excepcion: " + e.Message);
        }
    }


    public async Task<(bool success, string errorMessage)> UpdatePetStatsAsync(PetStatsModel stats)
    {
        if (!IsInitialized) return (false, "Firestore no está inicializado.");
        if (auth.CurrentUser == null) return (false, "Usuario no autenticado.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            string json = JsonConvert.SerializeObject(stats);
            var map = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            await db.Collection(PETS_COLLECTION).Document(userId).UpdateAsync(new Dictionary<string, object>
            {
                { "stats", map }
            });

            return (true, null);
        }
        catch (Exception e)
        {
            return (false, "Error actualizando estadísticas: " + e.Message);
        }
    }
}

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

    // 🔥 Cache del ID de usuario (evita errores en Android)
    private string cachedUserId = null;


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


    // ============================================================
    // 🔥 NUEVO: SetUser() para sincronizar AuthService → Firestore
    // ============================================================
    public void SetUser(FirebaseUser user)
    {
        if (user == null)
        {
            cachedUserId = null;
            return;
        }

        cachedUserId = user.UserId;
        Debug.Log("[Firestore] Usuario sincronizado: " + cachedUserId);
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


    // ============================================================
    // 🔥 Helper: Obtener UID seguro (cacheado)
    // ============================================================
    private string GetUserIdSafe()
    {
        // 1) Preferir UID cacheado por AuthService
        if (!string.IsNullOrEmpty(cachedUserId))
            return cachedUserId;

        // 2) Intentar leer desde FirebaseAuth
        if (auth != null && auth.CurrentUser != null)
        {
            cachedUserId = auth.CurrentUser.UserId;
            return cachedUserId;
        }

        return null;
    }


    // ============================================================
    // SAVE PET
    // ============================================================

    public async Task<(bool success, string errorMessage)> SavePetAsync(PetModel pet)
    {
        if (!IsInitialized) return (false, "Firestore no está inicializado.");

        string userId = GetUserIdSafe();
        if (userId == null) return (false, "Usuario no autenticado.");

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


    // ============================================================
    // LOAD PET
    // ============================================================

    public async Task<(PetModel pet, bool success, string errorMessage)> LoadPetAsync()
    {
        string userId = GetUserIdSafe();
        if (userId == null)
            return (null, false, "Usuario no autenticado.");

        if (db == null)
            return (null, false, "Firestore no inicializado.");

        try
        {
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                return (null, true, "Documento no existe.");
            }

            var petMap = snapshot.ToDictionary();
            if (petMap == null || petMap.Count == 0)
            {
                return (null, true, "Datos vacíos.");
            }

            string json = JsonConvert.SerializeObject(petMap);
            var pet = JsonConvert.DeserializeObject<PetModel>(json);

            return (pet, true, null);
        }
        catch (Exception e)
        {
            return (null, false, "Excepción: " + e.Message);
        }
    }


    // ============================================================
    // UPDATE PET STATS
    // ============================================================

    public async Task<(bool success, string errorMessage)> UpdatePetStatsAsync(PetStatsModel stats)
    {
        if (!IsInitialized) return (false, "Firestore no está inicializado.");

        string userId = GetUserIdSafe();
        if (userId == null) return (false, "Usuario no autenticado.");

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

using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using Newtonsoft.Json; 
using System;

// Clase Singleton: Centraliza todas las interacciones con Firestore (Base de datos NoSQL).
public class FirestoreService : MonoBehaviour
{
    // Singleton pattern
    public static FirestoreService Instance { get; private set; }
    
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    
    // CRÍTICO: Nueva propiedad pública para verificar si Firestore está inicializado.
    public bool IsInitialized { get; private set; } = false;

    // Constante para el nombre de la colección principal
    private const string PETS_COLLECTION = "pets";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // CRÍTICO: Asegura que el servicio persista
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Se llama una vez que AuthService ha confirmado que Firebase está inicializado.
    /// Esto asegura que podemos obtener la instancia de FirebaseAuth.
    /// </summary>
    public void InitializeFirestore()
    {
        auth = FirebaseAuth.DefaultInstance;
        try
        {
            // Obtener la instancia de Firestore.
            db = FirebaseFirestore.DefaultInstance;
            IsInitialized = true; // Establecer a true después de inicializar
            Debug.Log("[FirestoreService] ✅ Firestore Inicializado y listo para usar.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló la inicialización de Firestore: {e.Message}");
            IsInitialized = false;
        }
    }

    /// <summary>
    /// Guarda el modelo de mascota en Firestore bajo la colección 'pets' y usando el UserID como ID del documento.
    /// </summary>
    /// <param name="pet">El objeto PetModel a guardar.</param>
    /// <returns>Una tupla (bool success, string errorMessage)</returns>
    public async Task<(bool success, string errorMessage)> SavePetAsync(PetModel pet)
    {
        if (!IsInitialized) 
            return (false, "Firestore no está inicializado.");
        
        if (auth?.CurrentUser == null) 
            return (false, "Usuario no autenticado. No se puede guardar la mascota.");
        
        // El ID del documento es el ID de usuario
        string userId = auth.CurrentUser.UserId;
        
        try
        {
            // 1. Serialización: Convertir PetModel a un Dictionary<string, object>
            // Usamos Newtonsoft.Json para manejar correctamente los atributos [JsonProperty].
            string json = JsonConvert.SerializeObject(pet);
            Dictionary<string, object> petMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            
            // 2. Referencia al documento: db/pets/{userId}
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);

            // 3. Guardar (SetAsync sobrescribe si existe, crea si no existe)
            await docRef.SetAsync(petMap);
            
            Debug.Log($"[FirestoreService] ✅ Mascota guardada para el usuario: {userId}");
            return (true, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló al guardar la mascota: {e.Message}");
            return (false, $"Error al guardar en Firestore: {e.Message}");
        }
    }
    
    /// <summary>
    /// Carga el modelo de mascota del usuario actualmente autenticado.
    /// </summary>
    /// <returns>Una tupla (PetModel pet, bool success, string errorMessage)</returns>
    public async Task<(PetModel pet, bool success, string errorMessage)> LoadPetDataAsync()
    {
        if (auth?.CurrentUser == null) 
            return (null, false, "Usuario no autenticado.");
        
        if (db == null) 
            return (null, false, "Firestore no está inicializado.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            // 2. Obtener el snapshot del documento
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                // 3. Deserialización: Convertir de Firestore Map a PetModel
                Dictionary<string, object> petMap = snapshot.ToDictionary();
                
                // Usamos el serializador JSON para convertir el Dictionary a nuestro objeto PetModel
                string json = JsonConvert.SerializeObject(petMap);
                PetModel pet = JsonConvert.DeserializeObject<PetModel>(json);

                Debug.Log($"[FirestoreService] ✅ Mascota cargada: {pet.name}");
                return (pet, true, null);
            }
            else
            {
                return (null, false, "No se encontraron datos de mascota para este usuario.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló la carga de la mascota: {e.Message}");
            return (null, false, $"Error al cargar de Firestore: {e.Message}");
        }
    }
}

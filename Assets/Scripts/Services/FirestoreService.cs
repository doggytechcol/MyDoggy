using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using System.Collections.Generic;
using Newtonsoft.Json; 
using System;

/// <summary>
/// Clase Singleton: Centraliza todas las interacciones con Firestore (Base de datos NoSQL).
/// Maneja la serialización y deserialización de modelos complejos como PetModel.
/// </summary>
public class FirestoreService : MonoBehaviour
{
    // Singleton pattern
    public static FirestoreService Instance { get; private set; }
    
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    
    // CRÍTICO: Nueva propiedad pública para verificar si Firestore está inicializado.
    public bool IsInitialized { get; private set; } = false;

    // Constante para el nombre de la colección principal
    private const string PETS_COLLECTION = "pets"; // Colección donde se guarda el perfil de la mascota

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
            IsInitialized = true;
            Debug.Log("[FirestoreService] ✅ Firestore Inicializado y listo para usar.");
        }
        catch (Exception e)
        {
            IsInitialized = false;
            Debug.LogError($"[FirestoreService] ❌ Falló la inicialización de Firestore: {e.Message}");
        }
    }
    
    // --------------------------------------------------------------------------------
    // --- PET PROFILE OPERATIONS (CRUD) ---
    // --------------------------------------------------------------------------------

    /// <summary>
    /// Guarda el perfil inicial de la mascota en Firestore.
    /// Utiliza el ID de usuario como ID del documento.
    /// </summary>
    /// <param name="pet">El objeto PetModel a guardar.</param>
    /// <returns>Una tupla con éxito (bool) y mensaje de error (string).</returns>
    public async Task<(bool success, string errorMessage)> SavePetAsync(PetModel pet)
    {
        if (auth?.CurrentUser == null) 
            return (false, "Usuario no autenticado para guardar.");
        
        if (db == null) 
            return (false, "Base de datos no iniciada, no es posible guardar.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            // 1. Serialización: Convertir PetModel a un formato que Firestore pueda entender (Dictionary).
            // Convertimos a JSON y luego a Dictionary<string, object> para manejar las propiedades anidadas.
            string json = JsonConvert.SerializeObject(pet);
            Dictionary<string, object> petMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // 2. Guardar en Firestore con el ID de usuario como ID de documento.
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);
            // Usamos SetAsync para crear o sobrescribir el documento completo.
            await docRef.SetAsync(petMap); 

            Debug.Log($"[FirestoreService] ✅ Mascota guardada para UserId: {userId}");
            return (true, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló el guardado de la mascota: {e.Message}");
            return (false, $"Error al guardar en Firestore: {e.Message}");
        }
    }

    /// <summary>
    /// Carga el perfil de la mascota del usuario actual desde Firestore.
    /// </summary>
    /// <returns>
    /// CRÍTICO: Tupla con el PetModel cargado (puede ser null), éxito (bool) y un mensaje de error (string).
    /// </returns>
    // CORRECCIÓN PARA EL ERROR 1: Aseguramos la definición explícita de los nombres de los elementos de la tupla.
    public async Task<(PetModel pet, bool success, string errorMessage)> LoadPetAsync()
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
                // Usamos los nombres explícitos de la tupla para garantizar la coincidencia
                return (pet: pet, success: true, errorMessage: null);
            }
            else
            {
                // Usamos los nombres explícitos de la tupla para garantizar la coincidencia
                return (pet: null, success: false, errorMessage: "No se encontraron datos de mascota para este usuario.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló la carga de la mascota: {e.Message}");
            // Usamos los nombres explícitos de la tupla para garantizar la coincidencia
            return (pet: null, success: false, errorMessage: $"Error al cargar de Firestore: {e.Message}");
        }
    }

    /// <summary>
    /// Actualiza solo las estadísticas anidadas de la mascota.
    /// </summary>
    /// <param name="stats">El objeto PetStatsModel con los nuevos valores.</param>
    /// <returns>Una tupla con éxito (bool) y mensaje de error (string).</returns>
    public async Task<(bool success, string errorMessage)> UpdatePetStatsAsync(PetStatsModel stats)
    {
        if (auth?.CurrentUser == null) 
            return (false, "Usuario no autenticado para actualizar estadísticas.");
        
        if (db == null) 
            return (false, "Firestore no está inicializado para actualizar estadísticas.");

        string userId = auth.CurrentUser.UserId;

        try
        {
            // 1. Serialización del objeto anidado (PetStatsModel)
            string statsJson = JsonConvert.SerializeObject(stats);
            // Deserialización a un diccionario que Firestore pueda usar como mapa anidado
            Dictionary<string, object> statsMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(statsJson);
            
            // 2. Crear un mapa de actualización que solo apunte al campo anidado 'stats'
            // El formato es: { "stats": { "hunger": 50, "fun": 75, ... } }
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                // Este enfoque requiere que el objeto statsMap sea aceptado directamente por Firestore como el valor
                // de un campo. Lo correcto en Unity con el SDK de Firebase/Google es usar dot notation.
                // Sin embargo, para la compatibilidad con el JSON deserializado, es mejor
                // sobrescribir el campo anidado 'stats' con el nuevo mapa.
                {"stats", statsMap} 
            };
            
            // 3. Escribir en Firestore usando UpdateAsync para no sobrescribir todo el documento
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);
            await docRef.UpdateAsync(updates); 

            Debug.Log($"[FirestoreService] ✅ Estadísticas de mascota actualizadas para UserId: {userId}");
            return (true, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló la actualización de estadísticas: {e.Message}");
            return (false, $"Error al actualizar estadísticas en Firestore: {e.Message}");
        }
    }

}
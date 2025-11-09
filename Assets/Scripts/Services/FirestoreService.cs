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

    // --- GUARDAR DATOS (CREATE / UPDATE) ---

    /// <summary>
    /// Guarda el objeto PetModel en Firestore, usando el ID del usuario como ID del documento.
    /// </summary>
    /// <param name="pet">El objeto PetModel a guardar.</param>
    /// <returns>Una tupla con éxito y mensaje de error.</returns>
    public async Task<(bool success, string errorMessage)> SavePetAsync(PetModel pet)
    {
        // 1. Validaciones
        if (!IsInitialized || auth?.CurrentUser == null) 
            return (false, "Servicio no inicializado o usuario no autenticado.");
        
        string userId = auth.CurrentUser.UserId;

        try
        {
            // 2. Serialización: Convertir PetModel (con PetStatsModel anidado) a un Dictionary
            // CRÍTICO: Newtonsoft.Json maneja la estructura anidada automáticamente.
            string json = JsonConvert.SerializeObject(pet);
            // Convertir el JSON de vuelta a un Dictionary para que Firestore lo acepte
            Dictionary<string, object> petMap = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // 3. Escribir en Firestore
            DocumentReference docRef = db.Collection(PETS_COLLECTION).Document(userId);
            // .SetAsync() crea el documento si no existe, o lo sobrescribe si ya existe.
            await docRef.SetAsync(petMap); 

            Debug.Log($"[FirestoreService] ✅ PetModel guardado/actualizado para UserId: {userId}");
            return (true, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirestoreService] ❌ Falló el guardado de la mascota: {e.Message}");
            return (false, $"Error al guardar en Firestore: {e.Message}");
        }
    }

    // --- CARGAR DATOS (READ) ---

    /// <summary>
    /// Carga el objeto PetModel de Firestore para el usuario actual.
    /// </summary>
    /// <returns>Una tupla con el PetModel, éxito y mensaje de error.</returns>
    public async Task<(PetModel pet, bool success, string errorMessage)> LoadPetAsync()
    {
        // 1. Validaciones
        if (!IsInitialized || auth?.CurrentUser == null) 
            return (null, false, "Usuario no autenticado.");
        
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
                
                // Usamos el serializador JSON para convertir el Dictionary (incluyendo el submap 'stats') 
                // a nuestro objeto PetModel, lo que maneja PetStatsModel automáticamente.
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
    
    // --- MÉTODOS DE ACTUALIZACIÓN ESPECÍFICA (Para uso futuro) ---
    
    /// <summary>
    /// Actualiza solo las estadísticas de la mascota sin tocar otros campos.
    /// Esto es más eficiente que guardar todo el PetModel.
    /// </summary>
    /// <param name="stats">El objeto PetStatsModel con los nuevos valores.</param>
    /// <returns>Una tupla con éxito y mensaje de error.</returns>
    public async Task<(bool success, string errorMessage)> UpdatePetStatsAsync(PetStatsModel stats)
    {
        if (!IsInitialized || auth?.CurrentUser == null) 
            return (false, "Servicio no inicializado o usuario no autenticado.");
        
        string userId = auth.CurrentUser.UserId;

        try
        {
            // 1. Serializar PetStatsModel a un Map
            string statsJson = JsonConvert.SerializeObject(stats);
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

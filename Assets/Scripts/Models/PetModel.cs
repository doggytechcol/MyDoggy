using System;
using Newtonsoft.Json;

// Clase POCO (Plain Old C# Object) que representa la estructura de datos
// de la mascota tal como se guardará en Firestore.
[System.Serializable]
public class PetModel
{
    // Datos de registro inicial
    [JsonProperty("name")]
    public string name;
    
    [JsonProperty("breed")]
    public string breed;
    
    [JsonProperty("birthYear")]
    public int birthYear; // Usamos el año para calcular la edad

    // Metadatos y estado
    [JsonProperty("creationTimestamp")]
    public long creationTimestamp; // Unix timestamp de cuándo se creó el perfil

    // --- Futuros campos de Gamificación ---
    [JsonProperty("level")]
    public int level;

    [JsonProperty("xp")]
    public int xp;

    // Constructor para crear nuevas instancias del modelo
    public PetModel(string name, string breed, int birthYear)
    {
        this.name = name;
        this.breed = breed;
        this.birthYear = birthYear;
        this.creationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Inicialización de valores base
        this.level = 1;
        this.xp = 0;
    }
    
    // Constructor vacío requerido por algunos serializadores (como Newtonsoft.Json)
    public PetModel() { }
}
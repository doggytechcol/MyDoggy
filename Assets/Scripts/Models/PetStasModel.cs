using Newtonsoft.Json;

/// <summary>
/// Modelo de datos para las estadísticas de la mascota (POCO).
/// Se centra en el bienestar y las necesidades de cuidado.
/// </summary>
[System.Serializable]
public class PetStatsModel
{
    // --- Atributos de Estado Vital ---
    // (Valores de 0 a 100)
    
    [JsonProperty("health")]
    public int Health;       // La salud general de la mascota.
    
    [JsonProperty("energy")]
    public int Energy;       // Nivel de energía para jugar y realizar acciones.

    [JsonProperty("happiness")]
    public int Happiness;    // Nivel de felicidad.

    // --- Atributos de Necesidades Fisiológicas ---
    
    [JsonProperty("hunger")]
    public float Hunger;     // De 0.0f (Satisfecho) a 1.0f (Hambriento).
    
    [JsonProperty("peeNeed")]
    public float PeeNeed;    // Necesidad de orinar: de 0.0f (Nula) a 1.0f (Urgente).

    [JsonProperty("poopNeed")]
    public float PoopNeed;   // Necesidad de defecar: de 0.0f (Nula) a 1.0f (Urgente).

    /// <summary>
    /// Constructor para inicializar las estadísticas base de la mascota.
    /// </summary>
    public PetStatsModel()
    {
        // Valores de inicio:
        Health = 100;    // Salud completa
        Energy = 100;    // Energía completa
        Happiness = 75;  // Alto (recién adoptado/creado)
        
        // Necesidades iniciales a un nivel bajo o medio
        Hunger = 0.2f;   // Ligeramente con hambre (para empezar a interactuar)
        PeeNeed = 0.1f;  // Baja
        PoopNeed = 0.05f; // Muy baja
    }
}
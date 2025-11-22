using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clase ScriptableObject que define las características y necesidades 
/// de una raza de perro específica. Esto permite crear y gestionar diferentes 
/// perfiles de raza como assets dentro del editor de Unity.
/// </summary>
[CreateAssetMenu(fileName = "BreedData", menuName = "PetApp/Breed Data", order = 1)]
public class BreedData : ScriptableObject
{
    [Header("Identificación")]
    public string breedName = "Labrador Retriever";
    
    [Tooltip("Descripción corta de la personalidad y tamaño.")]
    [TextArea(3, 5)]
    public string description = "Perro de tamaño mediano a grande, muy activo, sociable e inteligente. Requiere ejercicio diario intenso.";

    [Header("Características Físicas y Salud")]
    [Tooltip("Peso promedio en kilogramos (rango recomendado).")]
    public Vector2 weightRangeKg = new Vector2(25f, 36f);
    public int lifeExpectancyYears = 12;

    [Header("Necesidades Diarias (1-5, siendo 5 la más alta)")]
    [Tooltip("Nivel de ejercicio diario necesario (1=Bajo, 5=Extremo).")]
    [Range(1, 5)]
    public int exerciseNeed = 5; 
    
    [Tooltip("Nivel de atención y compañía necesario (1=Independiente, 5=Pegajoso).")]
    [Range(1, 5)]
    public int companionshipNeed = 4;

    [Tooltip("Nivel de entrenamiento que requiere para un buen comportamiento (1=Fácil, 5=Desafiante).")]
    [Range(1, 5)]
    public int trainingDifficulty = 2; // Labradores son fáciles de entrenar

    [Header("Nutrición")]
    [Tooltip("Tipo de dieta recomendada (e.g., Alto en Proteína, Bajo en Grasa, Sensible, etc.).")]
    public string recommendedDietType = "Dieta balanceada para razas grandes y activas.";

    [Tooltip("Cantidad calórica diaria promedio (en kcal) para un adulto promedio de la raza.")]
    public int averageDailyCalories = 1800;
    
    // --- Métodos de Utilidad ---

    /// <summary>
    /// Retorna un mensaje basado en la necesidad de ejercicio.
    /// Esto es un ejemplo de cómo podemos añadir lógica de datos de raza.
    /// </summary>
    public string GetExerciseAdvice()
    {
        if (exerciseNeed >= 5)
        {
            return "¡Esta raza es un torbellino de energía! Necesita al menos 60-90 minutos de ejercicio vigoroso diario.";
        }
        else if (exerciseNeed >= 3)
        {
            return "Necesita una cantidad moderada de ejercicio. Unas 2 caminatas largas y tiempo de juego son ideales.";
        }
        else
        {
            return "Una raza tranquila. Le bastarán con paseos cortos y tranquilos.";
        }
    }
}
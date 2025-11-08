using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Este script maneja la secuencia de introducción y luego inicia el chequeo de servicios.
public class BootSceneManager : MonoBehaviour
{
    // --- Referencias de UI (Vincular en el Inspector) ---
    [Header("UI Introduction")]
    [Tooltip("Etiqueta donde aparecerá el mensaje de introducción.")]
    public TMP_Text introductionText;

    [Tooltip("Panel principal que contiene la animación del perrito (para activarlo/desactivarlo).")]
    public GameObject introPanel; 
    
    [Tooltip("Duración total de la animación de introducción en segundos.")]
    public float introDuration = 8.0f; // Ajusta este valor para que coincida con tu animación

    // --- Referencias de Servicios (Vincular Prefabs) ---
    [Header("Service Prefabs")]
    [Tooltip("Prefab del AuthService (con el FirestoreService vinculado)")]
    public GameObject authServicePrefab; 
    [Tooltip("Prefab del FirestoreService (aunque se instancia a través de AuthService, lo mantenemos por seguridad)")]
    public GameObject firestoreServicePrefab; 

    // --- Mensaje de Bienvenida ---
    private const string WELCOME_MESSAGE = "... Este videojuego fue creado por profesionales en bienestar animal, su objetivo es brindarte un reflejo real de las necesidades de tu mascota y orientarte en su cuidado. ¡Disfrútalo!";

    // Bandera para asegurar que la inicialización solo se haga una vez.
    private bool servicesInitialized = false;

    void Start()
    {
        // 1. Configuración Inicial de UI
        if (introductionText != null)
        {
            // Puedes configurar aquí la fuente si ya la importaste como asset de TMP_Font
            // introductionText.font = Resources.Load<TMP_FontAsset>("PixeloidSans_TMP"); 
            introductionText.text = ""; // Empieza vacío
        }

        // Aseguramos que el panel de introducción esté activo.
        if (introPanel != null) introPanel.SetActive(true); 

        // 2. Iniciar la secuencia de introducción.
        StartCoroutine(IntroductionSequence());
    }
    
    // Corrutina para manejar la animación del texto y la transición.
    private IEnumerator IntroductionSequence()
    {
        // --- FASE 1: Animación de Tipografía ---
        
        // Esperar un momento antes de empezar a escribir.
        yield return new WaitForSeconds(1.0f); 

        // Simula el efecto de "typing" (escritura letra por letra)
        for (int i = 0; i < WELCOME_MESSAGE.Length; i++)
        {
            introductionText.text = WELCOME_MESSAGE.Substring(0, i + 1);
            // Puedes variar el delay para un efecto más natural
            yield return new WaitForSeconds(0.05f); 
        }

        // --- FASE 2: Esperar la Animación del Perrito ---

        // Esperar el tiempo restante de la animación (ejemplo: 2 segundos después del texto)
        // CRÍTICO: Asegúrate de que este tiempo se coordine con tu animación visual.
        float remainingTime = introDuration - (Time.time - 1.0f); // Restamos el tiempo que tardó el typing
        if (remainingTime > 0)
        {
             yield return new WaitForSeconds(remainingTime);
        }

        // --- FASE 3: Inicialización de Servicios ---
        
        // El perrito "sale a correr dando paso a la escena Login" - Desactivamos el panel de intro.
        if (introPanel != null) introPanel.SetActive(false);
        
        // Inicializar los servicios (ahora que el usuario ha visto la introducción)
        InitializeServices();
    }

    /// <summary>
    /// Instancia los Prefabs de servicios que deben persistir (DontDestroyOnLoad).
    /// </summary>
    private void InitializeServices()
    {
        if (servicesInitialized) return;
        servicesInitialized = true;

        // 1. Instanciar AuthService y FirestoreService
        if (authServicePrefab != null && AuthService.Instance == null)
        {
            Instantiate(authServicePrefab);
            Debug.Log("[Boot] Services instantiated.");

            // AuthService.Start() se encargará de:
            // a) Inicializar Firebase.
            // b) Poner el listener OnAuthStateChanged.
            // c) Redirigir a la escena correcta (Login, CreatePet o PetProfile).
        }
        else if (AuthService.Instance != null)
        {
            Debug.LogWarning("[Boot] AuthService ya existía. Saltando inicialización.");
            // Si ya existe, podemos forzar una redirección para asegurar el flujo.
            // AuthService.Instance.ForceRedirectionCheck(); // (Función hipotética, pero la lógica ya está en AuthService.cs)
        }
        else
        {
            Debug.LogError("[Boot] CRÍTICO: Falta el prefab de AuthService en el Inspector.");
            // Si falla, al menos que cargue la escena de login para que el usuario pueda empezar.
            SceneManager.LoadScene(Constants.SCENE_LOGIN); 
        }
    }
}
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;             // Contiene AuthError y métodos de extensión
using Firebase;                  // Contiene FirebaseException
using System;

// Clase Singleton: Maneja la inicialización de Firebase y la lógica de autenticación.
// Se encarga de la persistencia de la sesión y la redirección de escenas post-login.
public class AuthService : MonoBehaviour
{
    // Singleton pattern
    public static AuthService Instance { get; private set; }
    
    private FirebaseAuth auth;
    public bool IsInitialized { get; private set; } = false;
    
    // Propiedad para acceder al ID del usuario actualmente autenticado.
    public string CurrentUserId => auth?.CurrentUser?.UserId; 

    // CRÍTICO: Referencia al servicio de Firestore (se asigna en el Inspector).
    [SerializeField] 
    private FirestoreService firestoreService; 
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // CRÍTICO: Asegura que el servicio persista entre escenas (desde Boot hasta el juego)
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async void Start()
    {
        // 1. Inicialización de Firebase
        Debug.Log("[AuthService] Chequeando dependencias de Firebase...");
        
        // Verifica si Firebase está configurado correctamente en el entorno de Unity
        Firebase.DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            IsInitialized = true;
            Debug.Log("[AuthService] ✅ Firebase Auth y Dependencias Listas.");
            
            // 2. Adjuntar el listener de estado de autenticación
            auth.StateChanged += OnAuthStateChanged;
            
            // CRÍTICO: Disparar el chequeo inicial con el usuario actual.
            HandleAuthState(auth.CurrentUser);
        }
        else
        {
            Debug.LogError($"[AuthService] ❌ Dependencias de Firebase No Disponibles: {dependencyStatus}");
        }
    }
    
    /// <summary>
    /// Listener CRÍTICO que se dispara automáticamente cada vez que el estado de autenticación cambia.
    /// </summary>
    private void OnAuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        // El evento StateChanged nos notifica. Llamamos a HandleAuthState con el usuario actual.
        HandleAuthState(auth.CurrentUser);
    }
    
    /// <summary>
    /// Lógica central de redirección y manejo de sesión. Se llama en el inicio y en cada cambio de sesión.
    /// </summary>
    private async void HandleAuthState(FirebaseUser user)
    {
        if (user != null)
        {
            // Usuario Autenticado
            Debug.Log($"[AuthService] Estado de Auth: Usuario {user.UserId} Loggeado.");

            // Inicializa Firestore solo si NO está inicializado.
            if (firestoreService != null && !firestoreService.IsInitialized)
            {
                firestoreService.InitializeFirestore();
            }

            // CRÍTICO: Decide a dónde ir (PetProfile o CreatePet)
            if (firestoreService != null) 
            {
                // Verifica si el usuario ya tiene una mascota
                (PetModel pet, bool success, string errorMessage) = await firestoreService.LoadPetDataAsync();

                if (success && pet != null)
                {
                    // Tiene mascota -> Ir al Perfil principal
                    Debug.Log("[AuthService] Redirigiendo a PetProfile (Mascota Encontrada).");
                    SceneManager.LoadScene(Constants.SCENE_PET_PROFILE);
                }
                else
                {
                    // No tiene mascota -> Ir a Crear Mascota
                    Debug.Log("[AuthService] Redirigiendo a CreatePet (Mascota NO Encontrada).");
                    SceneManager.LoadScene(Constants.SCENE_CREATE_PET); 
                }
            }
        }
        else
        {
            // Usuario Desloggeado o Sesión Expirada
            Debug.Log("[AuthService] Estado de Auth: Usuario Desloggeado.");

            // Solo redirigir si NO estamos ya en la escena de Login
            if (SceneManager.GetActiveScene().name != Constants.SCENE_LOGIN) 
            {
                SceneManager.LoadScene(Constants.SCENE_LOGIN);
            }
        }
    }

    // --- Métodos de Autenticación Básica (Email/Password) ---

    public async Task<(bool success, string errorMessage)> RegisterAsync(string email, string password)
    {
        if (!IsInitialized) return (false, "El servicio de autenticación no está listo.");

        try
        {
            await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            Debug.Log("[AuthService] ✅ Registro exitoso");
            return (true, null);
        }
        // CRÍTICO: Usamos la clase base FirebaseException, que siempre está disponible.
        catch (FirebaseException e)
        {
            // Luego obtenemos el error específico usando el método de extensión
            AuthError errorCode = (AuthError)e.ErrorCode;
            string cleanMessage = GetFirebaseErrorMessage(errorCode);
            Debug.LogWarning($"[AuthService] ❌ Falló el registro: {cleanMessage}");
            return (false, cleanMessage);
        }
        catch (Exception e)
        {
            return (false, $"Error desconocido: {e.Message}");
        }
    }

    public async Task<(bool success, string errorMessage)> LoginAsync(string email, string password)
    {
        if (!IsInitialized) return (false, "El servicio de autenticación no está listo.");

        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            Debug.Log("[AuthService] ✅ Inicio de sesión exitoso");
            return (true, null);
        }
        // CRÍTICO: Usamos la clase base FirebaseException, que siempre está disponible.
        catch (FirebaseException e)
        {
            // Luego obtenemos el error específico usando el método de extensión
            AuthError errorCode = (AuthError)e.ErrorCode;
            string cleanMessage = GetFirebaseErrorMessage(errorCode);
            Debug.LogWarning($"[AuthService] ❌ Falló el inicio de sesión: {cleanMessage}");
            return (false, cleanMessage);
        }
        catch (Exception e)
        {
            return (false, $"Error desconocido: {e.Message}");
        }
    }
    
    // --- Métodos de Autenticación Social (Implementación pendiente de SDKs) ---
    public Task<(bool success, string errorMessage)> LoginWithGoogleAsync()
    {
        return Task.FromResult((false, "Login con Google no implementado."));
    }

    public Task<(bool success, string errorMessage)> LoginWithFacebookAsync()
    {
        return Task.FromResult((false, "Login con Facebook no implementado."));
    }

    // --- Logout ---
    
    public void Logout()
    {
        if (auth != null)
        {
            auth.SignOut();
            // El listener OnAuthStateChanged se encargará de la redirección a "Login"
            Debug.Log("[AuthService] Sesión cerrada.");
        }
    }

    // --- Manejo de Errores de Firebase ---
    
    private string GetFirebaseErrorMessage(AuthError errorCode)
    {
        switch (errorCode)
        {
            case AuthError.InvalidEmail:
                return "El formato del correo electrónico no es válido.";
            case AuthError.UserNotFound:
                return "Usuario no encontrado.";
            case AuthError.WrongPassword:
                return "Contraseña incorrecta.";
            case AuthError.EmailAlreadyInUse:
                return "Este correo electrónico ya está registrado.";
            case AuthError.WeakPassword:
                // Ahora usa la constante definida en Constants.cs
                return $"La contraseña es débil. Debe tener {Constants.MIN_PASSWORD_LENGTH} caracteres, 1 mayúscula, 1 minúscula y 1 número.";
            case AuthError.RequiresRecentLogin:
                return "Esta acción requiere que inicies sesión recientemente. Por favor, vuelve a iniciar sesión.";
            default:
                return "Error de autenticación desconocido. Código: " + errorCode;
        }
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }
}
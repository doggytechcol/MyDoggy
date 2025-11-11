using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase; 
using System;
using System.Collections; // Necesario para la corrutina de redirección

public class AuthService : MonoBehaviour
{
    public static AuthService Instance { get; private set; }
    
    private FirebaseAuth auth;
    public bool IsInitialized { get; private set; } = false;
    private bool isRedirecting = false; 

    public string CurrentUserId => auth?.CurrentUser?.UserId; 

    [SerializeField] 
    private FirestoreService firestoreService; 
    
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
            return;
        }
    }

    private async void Start()
    {
        Debug.Log("[AuthService] Chequeando dependencias de Firebase...");

        // --- CORRECCIÓN: usar await en lugar de ContinueWith + task.Result (evita problemas de hilo)
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            InitializeFirebase();
        }
        else
        {
            Debug.LogError($"[AuthService] ❌ No se pudieron resolver las dependencias de Firebase: {dependencyStatus}");
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;
        IsInitialized = true;
        Debug.Log("[AuthService] ✅ Firebase y AuthService Inicializado. Chequeando sesión...");
        OnAuthStateChanged(this, EventArgs.Empty);
    }
    
    private async void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (!IsInitialized || isRedirecting) return;

        FirebaseUser user = auth.CurrentUser;
        
        if (user != null)
        {
            Debug.Log($"[AuthService] Usuario autenticado: {user.UserId}. Redirigiendo...");

            if (firestoreService != null)
            {
                firestoreService.InitializeFirestore();
            } 
            else 
            {
                Debug.LogError("[AuthService] ❌ ERROR CRÍTICO: FirestoreService no está vinculado.");
                return;
            }
            
            // --- CORRECCIÓN: llamar al método que tu FirestoreService sí implementa (LoadPetAsync)
            var petResult = await firestoreService.LoadPetAsync();

            // petResult tiene (PetModel pet, bool success, string errorMessage)
            if (petResult.success && petResult.pet != null)
            {
                Debug.Log("[AuthService] Mascota encontrada. Redirigiendo a PetProfile.");
                StartCoroutine(RedirectToScene(Constants.SCENE_MAP));
            }
            else
            {
                Debug.Log("[AuthService] Mascota NO encontrada. Redirigiendo a CreatePet.");
                StartCoroutine(RedirectToScene(Constants.SCENE_CREATE_PET));
            }
        }
        else
        {
            if (SceneManager.GetActiveScene().name != Constants.SCENE_LOGIN)
            {
                Debug.Log("[AuthService] Usuario desautenticado. Redirigiendo a Login.");
                StartCoroutine(RedirectToScene(Constants.SCENE_LOGIN));
            }
        }
    }
    
    private IEnumerator RedirectToScene(string sceneName)
    {
        isRedirecting = true;
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(sceneName);
        isRedirecting = false; 
    }

    public async Task<(bool success, string errorMessage)> RegisterAsync(string email, string password)
    {
        try
        {
            await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return (true, null);
        }
        catch (Exception e)
        {
            if (e is FirebaseException firebaseEx)
            {
                string friendlyMessage = GetFirebaseErrorMessage((AuthError)firebaseEx.ErrorCode);
                Debug.LogError($"[AuthService] ❌ Falló el Registro ({firebaseEx.ErrorCode}): {friendlyMessage}");
                return (false, friendlyMessage);
            }
            return (false, "Error desconocido en el registro: " + e.Message);
        }
    }

    public async Task<(bool success, string errorMessage)> LoginAsync(string email, string password)
    {
        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            return (true, null);
        }
        catch (Exception e)
        {
            if (e is FirebaseException firebaseEx)
            {
                string friendlyMessage = GetFirebaseErrorMessage((AuthError)firebaseEx.ErrorCode);
                Debug.LogError($"[AuthService] ❌ Falló el Login ({firebaseEx.ErrorCode}): {friendlyMessage}");
                return (false, friendlyMessage);
            }
            return (false, "Error desconocido en el login: " + e.Message);
        }
    }

    public void Logout()
    {
        if (auth != null)
        {
            auth.SignOut();
            Debug.Log("[AuthService] Sesión cerrada.");
        }
    }

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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase;
using System;
using System.Collections;

public class AuthService : MonoBehaviour
{
    public static AuthService Instance { get; private set; }

    private FirebaseAuth auth;
    public bool IsInitialized { get; private set; } = false;

    private bool isRedirecting = false;
    private bool firebaseReady = false;

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
        }
    }

    private async void Start()
    {
        Debug.Log("[AuthService] Verificando dependencias Firebase...");

        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            InitializeFirebase();
        }
        else
        {
            Debug.LogError($"[AuthService] ❌ Firebase no disponible: {dependencyStatus}");
        }
    }

    private void InitializeFirebase()
    {
        if (firebaseReady) return;

        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;
        firebaseReady = true;
        IsInitialized = true;

        Debug.Log("[AuthService] Firebase inicializado correctamente.");
        OnAuthStateChanged(this, EventArgs.Empty);
    }

    // ===============================================================
    // 🔥 AUTH STATE CHANGED
    // ===============================================================

    private async void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (!firebaseReady) return;

#if UNITY_ANDROID
        // 🔥 Fix fundamental para que Android espere a que Firebase termine la vinculación de sesión
        await Task.Delay(400);
#endif

        if (isRedirecting) return;

        FirebaseUser user = auth.CurrentUser;

        // ------------------------------------------------------------
        // ❌ NO AUTENTICADO
        // ------------------------------------------------------------
        if (user == null)
        {
            Debug.Log("[Auth] Usuario no autenticado → Login");

            if (SceneManager.GetActiveScene().name != Constants.SCENE_LOGIN)
                StartCoroutine(RedirectToScene(Constants.SCENE_LOGIN));

            return;
        }

        // ------------------------------------------------------------
        // ✔️ AUTENTICADO
        // ------------------------------------------------------------
        Debug.Log($"[Auth] Usuario autenticado: {user.UserId}");

        if (firestoreService == null)
        {
            Debug.LogError("[Auth] ❌ FirestoreService es NULL en el Inspector.");
            return;
        }

        // Inicializar Firestore si no está listo
        if (!firestoreService.IsInitialized)
        {
            firestoreService.InitializeFirestore();
            if (!firestoreService.IsInitialized)
            {
                Debug.LogError("[Auth] ❌ Firestore no logró inicializarse.");
                return;
            }
        }

        // 🔥 Fix permisos Firestore
        firestoreService.SetUser(user);

        // ------------------------------------------------------------
        // 🔄 Intentar cargar mascota
        // ------------------------------------------------------------
        var petResult = await firestoreService.LoadPetAsync();

        if (!petResult.success)
        {
            Debug.LogWarning("[Auth] No se pudo consultar Firestore. Enviando a CreatePet.");
            StartCoroutine(RedirectToScene(Constants.SCENE_CREATE_PET));
            return;
        }

        if (petResult.pet == null)
        {
            Debug.Log("[Auth] Usuario sin mascota → CreatePet");
            StartCoroutine(RedirectToScene(Constants.SCENE_CREATE_PET));
            return;
        }

        // ------------------------------------------------------------
        // ✔️ Mascota existente → decidir escena
        // ------------------------------------------------------------
        string sceneToLoad = Constants.SCENE_MAP;

        if (!string.IsNullOrEmpty(petResult.pet.lastScene))
        {
            sceneToLoad = petResult.pet.lastScene;
            Debug.Log($"[Auth] Mascota encontrada. Última escena: {sceneToLoad}");
        }
        else
        {
            Debug.Log("[Auth] Mascota existe pero sin lastScene → Map");
        }

        StartCoroutine(RedirectToScene(sceneToLoad));
    }

    // ===============================================================
    // 🔄 REDIRECT (SIN LOADER)
    // ===============================================================

    private IEnumerator RedirectToScene(string sceneName)
    {
        isRedirecting = true;

        yield return new WaitForSeconds(0.05f);

        SceneManager.LoadScene(sceneName);

        isRedirecting = false;
    }

    // ===============================================================
    // 🔐 REGISTRO / LOGIN / PASSWORD RESET
    // ===============================================================

    public async Task<(bool success, string errorMessage)> RegisterAsync(string email, string password)
    {
        try
        {
            await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            return (true, null);
        }
        catch (Exception e)
        {
            string readable = (e is FirebaseException fex)
                ? GetFirebaseErrorMessage((AuthError)fex.ErrorCode)
                : e.Message;

            return (false, readable);
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
            string readable = (e is FirebaseException fex)
                ? GetFirebaseErrorMessage((AuthError)fex.ErrorCode)
                : e.Message;

            return (false, readable);
        }
    }

    public async Task<(bool success, string errorMessage)> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            await auth.SendPasswordResetEmailAsync(email);
            Debug.Log($"[Auth] Email de recuperación enviado: {email}");
            return (true, null);
        }
        catch (Exception e)
        {
            string readable = (e is FirebaseException fex)
                ? GetFirebaseErrorMessage((AuthError)fex.ErrorCode)
                : e.Message;

            return (false, readable);
        }
    }

    public async Task<(bool success, string errorMessage)> FacebookLoginAsync(string accessToken)
    {
        try
        {
            Credential credential = FacebookAuthProvider.GetCredential(accessToken);
            await auth.SignInWithCredentialAsync(credential);
            return (true, null);
        }
        catch (Exception e)
        {
            string readable = (e is FirebaseException fex)
                ? GetFirebaseErrorMessage((AuthError)fex.ErrorCode)
                : e.Message;

            return (false, readable);
        }
    }

    public void Logout()
    {
        auth?.SignOut();
        Debug.Log("[AuthService] Sesión cerrada.");
    }

    private string GetFirebaseErrorMessage(AuthError err)
    {
        return err switch
        {
            AuthError.InvalidEmail => "Correo inválido.",
            AuthError.UserNotFound => "Usuario no encontrado.",
            AuthError.WrongPassword => "Contraseña incorrecta.",
            AuthError.EmailAlreadyInUse => "Correo ya registrado.",
            AuthError.WeakPassword => "Contraseña demasiado débil.",
            _ => "Error desconocido: " + err,
        };
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }
}

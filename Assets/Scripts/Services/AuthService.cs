using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase;
using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;

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

        // Inicializar Facebook SDK
        if (!FB.IsInitialized)
        {
            FB.Init(() => {
                Debug.Log("[AuthService] Facebook SDK inicializado.");
                FB.ActivateApp();
            });
        }
        else
        {
            FB.ActivateApp();
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
        await Task.Delay(400);
#endif

        if (isRedirecting) return;

        FirebaseUser user = auth.CurrentUser;

        if (user == null)
        {
            Debug.Log("[Auth] Usuario no autenticado → Login");

            if (SceneManager.GetActiveScene().name != Constants.SCENE_LOGIN)
                StartCoroutine(RedirectToScene(Constants.SCENE_LOGIN));

            return;
        }

        Debug.Log($"[Auth] Usuario autenticado: {user.UserId}");

        if (firestoreService == null)
        {
            Debug.LogError("[Auth] ❌ FirestoreService es NULL en el Inspector.");
            return;
        }

        if (!firestoreService.IsInitialized)
        {
            firestoreService.InitializeFirestore();
            if (!firestoreService.IsInitialized)
            {
                Debug.LogError("[Auth] ❌ Firestore no logró inicializarse.");
                return;
            }
        }

        firestoreService.SetUser(user);

        var petResult = await firestoreService.LoadPetAsync();

        if (!petResult.success || petResult.pet == null)
        {
            Debug.Log("[Auth] Usuario sin mascota → CreatePet");
            StartCoroutine(RedirectToScene(Constants.SCENE_CREATE_PET));
            return;
        }

        string sceneToLoad = string.IsNullOrEmpty(petResult.pet.lastScene)
            ? Constants.SCENE_MAP
            : petResult.pet.lastScene;

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

    // ===============================================================
    // 🔐 FACEBOOK LOGIN
    // ===============================================================

    public void LoginWithFacebook()
    {
        FB.LogInWithReadPermissions(new List<string> { "public_profile", "email" }, async result =>
        {
            if (FB.IsLoggedIn)
            {
                string token = AccessToken.CurrentAccessToken.TokenString;
                var loginResult = await FacebookLoginAsync(token);

                if (loginResult.success)
                    Debug.Log("[AuthService] Login Facebook exitoso.");
                else
                    Debug.LogError("[AuthService] Error login Facebook: " + loginResult.errorMessage);
            }
            else
            {
                Debug.LogWarning("[AuthService] Login Facebook cancelado o fallido.");
            }
        });
    }

    public async Task<(bool success, string errorMessage)> FacebookLoginAsync(string accessToken)
    {
        try
        {
            Credential credential = FacebookAuthProvider.GetCredential(accessToken);
            await auth.SignInWithCredentialAsync(credential);
            Debug.Log("[AuthService] Usuario autenticado con Facebook correctamente.");
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


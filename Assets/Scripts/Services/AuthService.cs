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

    // 🔥 Web Client ID de Firebase (Este valor DEBE coincidir con el de tu proyecto)
    private const string FIREBASE_WEB_CLIENT_ID = "671856843241-ql6d59rkugk6uoeobah4j3hm343781pq.apps.googleusercontent.com";


    [SerializeField]
    private FirestoreService firestoreService;

    // ⭐ NUEVA REFERENCIA: Guardaremos aquí la instancia de la UI de Login
    private LoginUI loginUI;

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
            Debug.LogError($"[AuthService] ❌ Error: No se pudo resolver las dependencias de Firebase: {dependencyStatus}");
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += OnAuthStateChanged;
        OnAuthStateChanged(this, null); // Revisar el estado inicial
        IsInitialized = true;
        firebaseReady = true;
        Debug.Log("[AuthService] ✅ Firebase Auth inicializado.");
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (!firebaseReady) return;

        FirebaseUser user = auth.CurrentUser;
        firestoreService?.SetUser(user);

        if (user != null)
        {
            if (!isRedirecting)
            {
                isRedirecting = true;
                Debug.Log($"[AuthService] Usuario logueado: {user.DisplayName ?? user.Email}. Redirigiendo...");
                SceneManager.LoadScene("Pet");
            }
        }
        else
        {
            isRedirecting = false;
            if (SceneManager.GetActiveScene().name != "Login")
            {
                Debug.Log("[AuthService] Usuario deslogueado. Redirigiendo al Login...");
                SceneManager.LoadScene("Login");
            }
        }
    }

    // ===============================================================
    // ⭐ MÉTODO PARA REGISTRAR LA UI DE LOGIN (Inversión de Control)
    // ===============================================================

    /// <summary>
    /// Llamado por LoginUI.cs en su Start() para registrarse.
    /// Esto permite al servicio persistente dar feedback a la UI transitoria.
    /// </summary>
    public void RegisterLoginUI(LoginUI ui)
    {
        loginUI = ui;
        Debug.Log("[AuthService] ✅ LoginUI registrado y listo para feedback.");
    }

    // ===============================================================
    // AUTH: Email/Password
    // ===============================================================

    public async Task<(bool success, string errorMessage)> RegisterAsync(string email, string password)
    {
        if (!IsInitialized) return (false, "Firebase no está inicializado.");
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
        if (!IsInitialized) return (false, "Firebase no está inicializado.");
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
        if (!IsInitialized) return (false, "Firebase no está inicializado.");
        try
        {
            await auth.SendPasswordResetEmailAsync(email);
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
    // AUTH: Facebook
    // ===============================================================

    public void FacebookLoginUI()
    {
        if (!FB.IsInitialized)
        {
            Debug.LogError("[Facebook] SDK no inicializado.");
            return;
        }

        var permissions = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(permissions, async (result) =>
        {
            // El loadingPanel se activa en LoginUI.OnFacebookLoginPressed()

            if (result.Cancelled)
            {
                Debug.Log("[Facebook] Login cancelado por el usuario.");
                loginUI?.SetFeedback("Login de Facebook cancelado.", true);
            }
            else if (result.Error != null)
            {
                Debug.LogError($"[Facebook] Error en el Login: {result.Error}");
                loginUI?.SetFeedback($"Error de Facebook: {result.Error}", true);
            }
            else if (!FB.IsLoggedIn)
            {
                Debug.LogError("[Facebook] FB.IsLoggedIn es falso.");
                loginUI?.SetFeedback("Fallo al obtener credenciales de Facebook.", true);
            }
            else
            {
                string facebookAccessToken = result.AccessToken.TokenString;
                Debug.Log("[Facebook] Access Token obtenido. Autenticando con Firebase...");

                var authResult = await FacebookLoginAsync(facebookAccessToken);
                if (!authResult.success)
                {
                    // Si falla el paso de Firebase, mostramos el error
                    Debug.LogError("[Facebook] ❌ Error autenticando con Firebase: " + authResult.errorMessage);
                    loginUI?.SetFeedback($"Error Firebase con Facebook: {authResult.errorMessage}", true);
                }
            }

            // Asegurarse de desactivar el panel de carga después de cualquier resultado
            loginUI?.loadingPanel.SetActive(false);
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

    // ===============================================================
    // 🎯 AUTH: GOOGLE NATIVO (PUENTE JAVA)
    // ===============================================================

    /// <summary>
    /// Llamado por el código Java tras el login exitoso de Google.
    /// Recibe el ID Token para autenticarse en Firebase.
    /// </summary>
    public async void LoginNativeGoogle(string idToken)
    {
        Debug.Log("[AuthService] Token de Google nativo recibido. Autenticando con Firebase...");

        // Verifica si ya estamos en un proceso de redirección para evitar doble procesamiento
        if (isRedirecting) return;

        try
        {
            // 1. Crear Credencial usando el ID Token
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

            // 2. Iniciar sesión en Firebase
            await auth.SignInWithCredentialAsync(credential);

            Debug.Log("[AuthService] ✅ Usuario autenticado en Firebase con Google nativo.");
            // OnAuthStateChanged se encargará de la redirección y el feedback de éxito.

        }
        catch (Exception e)
        {
            string readable = (e is FirebaseException fex)
                ? GetFirebaseErrorMessage((AuthError)fex.ErrorCode)
                : e.Message;

            Debug.LogError($"[AuthService] ❌ Error Firebase Google Nativo: {readable}");

            // ⭐ USAMOS LA REFERENCIA GUARDADA PARA DAR FEEDBACK Y QUITAR EL LOADING
            loginUI?.SetFeedback($"Error Firebase con Google: {readable}", true);
            if (loginUI?.loadingPanel != null) loginUI.loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Llamado por el código Java en caso de fallo (antes de Firebase).
    /// </summary>
    public void GoogleLoginFailed(string errorMessage)
    {
        Debug.LogError($"[AuthService] ❌ Fallo del Login Nativo de Google: {errorMessage}");

        // ⭐ USAMOS LA REFERENCIA GUARDADA PARA DAR FEEDBACK Y QUITAR EL LOADING
        loginUI?.SetFeedback($"Fallo de Google: {errorMessage}", true);
        if (loginUI?.loadingPanel != null) loginUI.loadingPanel.SetActive(false);
    }


    // ===============================================================
    // LOGOUT & HELPERS
    // ===============================================================

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
            AuthError.InvalidCredential => "Credenciales de Google o Facebook no válidas.",
            _ => "Error desconocido: " + err,
        };
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }
}
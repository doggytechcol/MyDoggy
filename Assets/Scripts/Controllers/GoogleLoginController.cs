using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Clase para manejar las llamadas nativas a Google Sign-In en Android.
/// Actúa como intermediario entre el botón de UI y el código Java.
/// </summary>
public class GoogleLoginController : MonoBehaviour
{
    private const string UNITY_ACTIVITY_CLASS = "com.unity3d.player.UnityPlayer";
    private const string GOOGLE_ACTIVITY_CLASS = "com.doggytech.mydoggy.GoogleActivity";

    // Referencia al objeto Java que maneja el login
    private AndroidJavaObject googleLoginControllerJava;

    // ====================================================================
    //  INICIALIZACIÓN
    // ====================================================================

    void Awake()
    {
#if UNITY_ANDROID
        InitializeJavaBridge();
#endif
    }

    private void InitializeJavaBridge()
    {
        // Obtener la actividad de Unity
        AndroidJavaClass unityPlayer = new AndroidJavaClass(UNITY_ACTIVITY_CLASS);
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // Obtener la instancia del controlador Java de Google desde la actividad
        AndroidJavaObject googleActivity = new AndroidJavaObject(GOOGLE_ACTIVITY_CLASS);
        googleLoginControllerJava = googleActivity.Call<AndroidJavaObject>("getGoogleLoginController");

        if (googleLoginControllerJava == null)
        {
            Debug.LogError("[Native Google] ❌ Falló al obtener la instancia del controlador Java.");
        }
        else
        {
            Debug.Log("[Native Google] ✅ Puente Java inicializado.");
        }
    }

    // ====================================================================
    //  MÉTODO LLAMADO POR LA UI
    // ====================================================================

    /// <summary>
    /// Inicia el proceso de login de Google Sign-In llamando al código Java.
    /// </summary>
    public void StartGoogleSignIn()
    {
#if UNITY_ANDROID
        if (googleLoginControllerJava != null)
        {
            Debug.Log("[Native Google] Llamando a signIn() de Java.");
            googleLoginControllerJava.Call("signIn");
        }
        else
        {
            Debug.LogError("[Native Google] No se puede iniciar sesión: El puente Java no está listo.");
            // Si falla, enviamos un error a AuthService para manejar la UI
            AuthService.Instance.GoogleLoginFailed("Error interno del puente Java.");
        }
#else
        Debug.LogWarning("[Native Google] Google Sign-In Nativo solo funciona en Android.");
#endif
    }
}

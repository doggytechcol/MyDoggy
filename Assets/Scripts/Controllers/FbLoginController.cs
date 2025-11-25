using UnityEngine;
using UnityEngine.UI;
using Facebook.Unity;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro; // Necesario para TMP_Text

/// <summary>
/// Maneja la inicialización del SDK de Facebook y la lógica del botón de Login.
/// </summary>
public class SocialLoginController : MonoBehaviour
{
    [Header("UI References")]
    public Button facebookSignInButton;
    public TMP_Text statusLabel; // Para feedback de error/éxito

    private void Start()
    {
        if (facebookSignInButton != null)
        {
            facebookSignInButton.onClick.AddListener(OnFacebookSignInClicked);
        }
        SetFeedback("", false);

        // Verificación de seguridad: Si FB no está init (raro si AuthService ya cargó), lo forzamos.
        if (!FB.IsInitialized)
        {
            FB.Init(() => FB.ActivateApp());
        }
        else
        {
            FB.ActivateApp();
        }
    }

    private void OnInitComplete()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            Debug.Log("[Facebook] SDK Inicializado correctamente.");
        }
        else
        {
            Debug.LogError("[Facebook] ❌ No se pudo inicializar el SDK.");
            SetFeedback("Error al inicializar Facebook.", true);
        }
    }

    private void OnHideUnity(bool isGameShown) { }

    /// <summary>
    /// Se llama cuando se pulsa el botón de Facebook.
    /// </summary>
    public void OnFacebookSignInClicked()
    {
        if (AuthService.Instance == null || !AuthService.Instance.IsInitialized)
        {
            Debug.LogWarning("[Facebook] Firebase no está listo. Reintentando...");
            SetFeedback("Servicios de Firebase no listos.", true);
            return;
        }

        facebookSignInButton.interactable = false;
        SetFeedback("Iniciando sesión con Facebook...", false);

        // Permisos mínimos requeridos. Usar solo los necesarios.
        // Importante: solo public_profile
        var permissions = new List<string>() { "public_profile" };

        FB.LogInWithReadPermissions(permissions, HandleFacebookLoginResult);
    }

    /// <summary>
    /// Callback del SDK de Facebook después de intentar el Login.
    /// </summary>
    private async void HandleFacebookLoginResult(ILoginResult result)
    {
        facebookSignInButton.interactable = true; // Restaurar botón
        SetFeedback("", false); // Limpiar mensaje de "Iniciando..."

        if (result == null)
        {
            Debug.LogError("[Facebook] ❌ Resultado nulo.");
            SetFeedback("Error de conexión. Inténtalo de nuevo.", true);
            return;
        }

        if (result.Cancelled)
        {
            Debug.Log("[Facebook] Login cancelado por el usuario.");
            SetFeedback("Inicio de sesión cancelado.", false);
            return;
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("[Facebook] ❌ Error de login: " + result.Error);
            SetFeedback("Error de Facebook: " + result.Error, true);
            return;
        }

        if (!FB.IsLoggedIn)
        {
            Debug.LogError("[Facebook] ❌ FB.IsLoggedIn es falso.");
            SetFeedback("Error al iniciar sesión con Facebook.", true);
            return;
        }

        // Obtener el Access Token (propiedad .TokenString del AccessToken)
        string facebookAccessToken = result.AccessToken.TokenString;
        Debug.Log("[Facebook] Access Token obtenido. Autenticando con Firebase...");

        // Llamar a AuthService para autenticar con Firebase usando el token de Facebook
        SetFeedback("Autenticando con Firebase...", false);
        var authResult = await AuthService.Instance.FacebookLoginAsync(facebookAccessToken);

        if (authResult.success)
        {
            SetFeedback("¡Bienvenido! Redirigiendo...", false);
            // La redirección a la escena correcta es manejada por AuthService.OnAuthStateChanged
        }
        else
        {
            Debug.LogError("[Facebook] ❌ Error autenticando con Firebase: " + authResult.errorMessage);
            SetFeedback($"Login fallido: {authResult.errorMessage}", true);
        }
    }

    /// <summary>
    /// Muestra un mensaje al usuario en la etiqueta de estado.
    /// </summary>
    private void SetFeedback(string message, bool isError)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
        statusLabel.color = isError ? Color.red : Color.white; // Asumiendo color blanco para normal
    }
}


